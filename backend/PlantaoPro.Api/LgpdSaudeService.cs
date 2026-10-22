using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

/// <summary>
/// R87 - Motor de LGPD + saúde: consentimento por escopo controlado (PRONTUARIO/COAM),
/// direitos do titular (LISTAR/EXPORTAR/APAGAR com trilha em lgpd_eventos_privacidade) e
/// retenção mínima legal do prontuário (Lei 13.787/2018, art. 6 — 20 anos do último
/// registro clínico). Sem IO além de Npgsql/Dapper; regras puras em LgpdSaudeRegras.
/// O "último registro" é a maior data entre consultas (finalizada_em/inicio_em/reg_date)
/// e documentos clínicos (emitido_em) do paciente.
/// </summary>
public sealed class LgpdSaudeService
{
    private readonly string _conexao;

    public LgpdSaudeService(string connectionString) => _conexao = connectionString;

    private static CommandDefinition Cmd(string sql, object? param = null, NpgsqlTransaction? tx = null, CancellationToken ct = default) =>
        new(sql, param, tx, cancellationToken: ct);

    private async Task<int> PrazoMesesAsync(CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var valor = await cn.ExecuteScalarAsync<string?>(
            Cmd("select valor from plantaopro.parametros_legais where codigo='LGPD_SA_RETENCAO_PRONTUARIO_MESES' and reg_status='A' order by ano desc limit 1", null, null, ct));
        return int.TryParse(valor, out var meses) && meses > 0 ? meses : LgpdSaudeRegras.PrazoProntuarioMeses;
    }

    private static async Task<PacienteBase?> ObterPacienteAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid pacienteId, CancellationToken ct) =>
        await cn.QueryFirstOrDefaultAsync<PacienteBase>(Cmd(@"
select id Id, coalesce(nome_completo,'') Nome, cpf Cpf, email Email, telefone Telefone,
       case when data_nascimento is null then null else data_nascimento::date end DataNascimento
from plantaopro.pacientes
where id=@pacienteId and cliente_id=@tenantId and reg_status='A'",
        new { pacienteId, tenantId }, tx, ct));

    private static readonly DateOnly DataSentinela = new(1970, 1, 1); // impossível em dado clínico; evita DBNull→DateOnly no Dapper

    private static async Task<DateOnly?> UltimoRegistroAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid pacienteId, CancellationToken ct)
    {
        var cRaw = await cn.ExecuteScalarAsync<DateOnly>(Cmd(
            "select coalesce(max(coalesce(finalizada_em, inicio_em, reg_date))::date, @sentinel) from plantaopro.consultas where paciente_id=@p and cliente_id=@t and reg_status='A'",
            new { p = pacienteId, t = tenantId, sentinel = DataSentinela }, tx, ct));
        var dRaw = await cn.ExecuteScalarAsync<DateOnly>(Cmd(
            "select coalesce(max(emitido_em)::date, @sentinel) from plantaopro.documentos_clinicos where paciente_id=@p and tenant_id=@t",
            new { p = pacienteId, t = tenantId, sentinel = DataSentinela }, tx, ct));
        var c = cRaw == DataSentinela ? (DateOnly?)null : cRaw;
        var d = dRaw == DataSentinela ? (DateOnly?)null : dRaw;
        if (c.HasValue && d.HasValue) return c.Value >= d.Value ? c : d;
        return c ?? d;
    }

    private static async Task<List<LgpdConsentimentoDto>> ConsentimentosAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid pacienteId, CancellationToken ct)
    {
        var lista = new List<LgpdConsentimentoDto>(await cn.QueryAsync<LgpdConsentimentoDto>(Cmd(@"
select id Id, paciente_id PacienteId, finalidade Finalidade, concedido Concedido,
       coalesce(canal,'') Canal, versao_termo VersaoTermo, coalesce(base_legal,'') BaseLegal,
       coalesce(status,'') Status, usuario_id UsuarioId, coalesce(reg_date, now()) RegDate
from plantaopro.paciente_consentimentos
where paciente_id=@p and cliente_id=@t and reg_status='A'
order by reg_date desc, id desc",
        new { p = pacienteId, t = tenantId }, tx, ct)));
        return lista;
    }

    private static Task<int> TrilhaAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid? userId, string? ip, string acao, object detalhes, CancellationToken ct) =>
        cn.ExecuteAsync(Cmd(@"
insert into plantaopro.lgpd_eventos_privacidade (id, usuario_id, acao, detalhes, ip, cliente_id, tenant_id, reg_status, reg_date)
values (gen_random_uuid(), @u, @acao, @detalhes, @ip, @t, @t, 'A', now())",
        new { u = userId, acao, detalhes = JsonSerializer.Serialize(detalhes), ip, t = tenantId }, tx, ct));

    private static async Task<LgpdRetencaoDto> RetencaoInfoAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid pacienteId, int prazoMeses, CancellationToken ct)
    {
        var ultimo = await UltimoRegistroAsync(cn, tx, tenantId, pacienteId, ct);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var apto = ultimo.HasValue ? LgpdSaudeRegras.AptoDesdeUltimoRegistro(ultimo.Value, prazoMeses) : (DateOnly?)null;
        return new LgpdRetencaoDto
        {
            UltimoRegistro = ultimo,
            PrazoMeses = prazoMeses,
            AptoDesde = apto,
            PodeEliminarAgora = !apto.HasValue || LgpdSaudeRegras.PodeEliminarEm(hoje, apto.Value),
            Fonte = LgpdSaudeRegras.FonteRetencaoProntuario
        };
    }

    /// <summary>Consentimentos registrados do paciente por escopo (mais recente primeiro).</summary>
    public async Task<ApiResponse<IReadOnlyList<LgpdConsentimentoDto>>> ConsentimentosAsync(Guid tenantId, Guid pacienteId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<IReadOnlyList<LgpdConsentimentoDto>>.Fail("Contexto de tenant obrigatório.", 403);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        if (await ObterPacienteAsync(cn, null, tenantId, pacienteId, ct) is null)
            return ApiResponse<IReadOnlyList<LgpdConsentimentoDto>>.Fail("Paciente não encontrado.", 404);
        var lista = await ConsentimentosAsync(cn, null, tenantId, pacienteId, ct);
        return ApiResponse<IReadOnlyList<LgpdConsentimentoDto>>.Ok(lista, "Consentimentos carregados.");
    }

    /// <summary>
    /// Registra consentimento de dados de saúde por escopo: valida escopo/canal/versão do termo
    /// (dados sensíveis exigem consentimento específico e destacado — LGPD art. 11, I), substitui
    /// o registro vigente do escopo e grava trilha auditável.
    /// </summary>
    public async Task<ApiResponse<LgpdConsentimentoDto>> RegistrarConsentimentoAsync(Guid tenantId, Guid? userId, string? ip, LgpdConsentimentoCommand cmd, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<LgpdConsentimentoDto>.Fail("Contexto de tenant obrigatório.", 403);
        var erros = LgpdSaudeRegras.ValidarConsentimento(cmd.Escopo, cmd.Concedido, cmd.Canal, cmd.VersaoTermo);
        if (erros.Count > 0) return ApiResponse<LgpdConsentimentoDto>.Fail("Consentimento inválido.", 400, erros);

        var escopoNormalizado = cmd.Escopo!.Trim().ToUpperInvariant();
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        if (await ObterPacienteAsync(cn, null, tenantId, cmd.PacienteId, ct) is null)
            return ApiResponse<LgpdConsentimentoDto>.Fail("Paciente não encontrado.", 404);

        var catalogo = await cn.QueryFirstOrDefaultAsync<LgpdEscopoDto>(Cmd(
            "select codigo Codigo, nome Nome, descricao Descricao, exige_consentimento_explicito Exige, coalesce(fonte_oficial,'') Fonte from plantaopro.lgpd_escopos_consentimento where upper(codigo)=@e and ativo=true and reg_status='A'",
            new { e = escopoNormalizado }, null, ct));
        if (catalogo is null)
            return ApiResponse<LgpdConsentimentoDto>.Fail("Escopo não cadastrado no catálogo institucional.", 400, new[] { "Escopo inativo ou inexistente: " + escopoNormalizado });

        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            // O registro vigente anterior do mesmo escopo sai de cena (substituído ou revogado).
            await cn.ExecuteAsync(Cmd(@"
update plantaopro.paciente_consentimentos
set status=@novo, updated_by=@u, reg_update=now()
where paciente_id=@p and cliente_id=@t and finalidade=@e and status='VIGENTE' and reg_status='A'",
                new { novo = cmd.Concedido ? "SUBSTITUIDO" : "REVOGADO", u = userId, p = cmd.PacienteId, t = tenantId, e = escopoNormalizado }, tx, ct));

            var dto = await cn.QuerySingleAsync<LgpdConsentimentoDto>(Cmd(@"
insert into plantaopro.paciente_consentimentos
    (id, cliente_id, tenant_id, paciente_id, finalidade, concedido, canal, ip, usuario_id, status, versao_termo, base_legal, created_by, updated_by, reg_update)
values
    (gen_random_uuid(), @t, @t, @p, @e, @c, @canal, @ip, @u, 'VIGENTE', @v, @baseLegal, @u, @u, now())
returning id Id, paciente_id PacienteId, finalidade Finalidade, concedido Concedido,
          coalesce(canal,'') Canal, versao_termo VersaoTermo, coalesce(base_legal,'') BaseLegal,
          status Status, usuario_id UsuarioId, coalesce(reg_date, now()) RegDate",
                new { t = tenantId, p = cmd.PacienteId, e = escopoNormalizado, c = cmd.Concedido, canal = cmd.Canal!.Trim().ToUpperInvariant(), ip, u = userId, v = cmd.Concedido ? cmd.VersaoTermo?.Trim() : null, baseLegal = LgpdSaudeRegras.BaseSensivel }, tx, ct));

            await TrilhaAsync(cn, tx, tenantId, userId, ip, LgpdSaudeRegras.EventoTrilhaConsentimento,
                new { pacienteId = cmd.PacienteId, escopo = escopoNormalizado, concedido = cmd.Concedido, canal = dto.Canal, versaoTermo = dto.VersaoTermo, baseLegal = catalogo.Exige ? LgpdSaudeRegras.BaseSensivel : null, fonte = catalogo.Fonte }, ct);
            await tx.CommitAsync(ct);
            return ApiResponse<LgpdConsentimentoDto>.Ok(dto, "Consentimento registrado com trilha.");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Envelope de dados do paciente (leitura simples, sem trilha — trilha ocorre ao exercer o direito).</summary>
    public async Task<ApiResponse<LgpdPacienteEnvelope>> ListarDadosAsync(Guid tenantId, Guid pacienteId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<LgpdPacienteEnvelope>.Fail("Contexto de tenant obrigatório.", 403);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var p = await ObterPacienteAsync(cn, null, tenantId, pacienteId, ct);
        if (p is null) return ApiResponse<LgpdPacienteEnvelope>.Fail("Paciente não encontrado.", 404);

        var prazo = await PrazoMesesAsync(ct);
        var retencao = await RetencaoInfoAsync(cn, null, tenantId, pacienteId, prazo, ct);
        var env = new LgpdPacienteEnvelope
        {
            PacienteId = p.Id,
            Nome = p.Nome,
            Cpf = p.Cpf,
            Email = p.Email,
            Telefone = p.Telefone,
            DataNascimento = p.DataNascimento,
            UltimoRegistro = retencao.UltimoRegistro,
            Consentimentos = await ConsentimentosAsync(cn, null, tenantId, pacienteId, ct),
            Retencao = retencao
        };
        return ApiResponse<LgpdPacienteEnvelope>.Ok(env, "Dados do paciente carregados.");
    }

    /// <summary>
    /// Exerce direito do titular (LGPD art. 18) com trilha: LISTAR/EXPORTAR retornam o envelope
    /// completo; APAGAR aplica a guarda da retenção mínima legal (Lei 13.787/2018, art. 6) e,
    /// fora da janela, anonimiza identificadores preservando o histórico clínico.
    /// </summary>
    public async Task<ApiResponse<LgpdPacienteEnvelope>> ExercerDireitoAsync(Guid tenantId, Guid? userId, string? ip, ExercerDiretoCommand cmd, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<LgpdPacienteEnvelope>.Fail("Contexto de tenant obrigatório.", 403);
        if (cmd is null) return ApiResponse<LgpdPacienteEnvelope>.Fail("Comando vazio.", 400);
        var erro = LgpdSaudeRegras.ValidarDireto(cmd.Direto);
        if (erro != null) return ApiResponse<LgpdPacienteEnvelope>.Fail(erro, 400);
        var direto = cmd.Direto.Trim().ToUpperInvariant();

        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var p = await ObterPacienteAsync(cn, null, tenantId, cmd.PacienteId, ct);
        if (p is null) return ApiResponse<LgpdPacienteEnvelope>.Fail("Paciente não encontrado.", 404);

        var prazo = await PrazoMesesAsync(ct);
        if (direto == "APAGAR" && p.Nome.StartsWith("ANONIMIZADO-", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<LgpdPacienteEnvelope>.Fail("Paciente já foi anonimizado.", 409, new[] { LgpdSaudeRegras.MotivoRetencaoMinima });

        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            var retencao = await RetencaoInfoAsync(cn, tx, tenantId, cmd.PacienteId, prazo, ct);
            if (direto == "APAGAR")
            {
                if (!retencao.PodeEliminarAgora)
                {
                    await tx.RollbackAsync(ct);
                    return new ApiResponse<LgpdPacienteEnvelope>(false,
                        $"Dado em retenção mínima legal ({retencao.Fonte}): eliminação apta em {retencao.AptoDesde!.Value:yyyy-MM-dd}.",
                        new LgpdPacienteEnvelope { PacienteId = cmd.PacienteId, Nome = p.Nome, UltimoRegistro = retencao.UltimoRegistro, Retencao = retencao },
                        new[] { LgpdSaudeRegras.MotivoRetencaoMinima }, 409, DateTime.UtcNow);
                }

                var rotulo = LgpdSaudeRegras.RotuloAnonimo(p.Id.ToString("N"));
                await cn.ExecuteAsync(Cmd(@"
update plantaopro.pacientes set
    nome_completo=@rotulo, nome=@rotulo, nome_social=null, endereco=null, responsavel_nome=null,
    cpf=null, documento='', cns=null, documento_alternativo=null, email='', telefone='', data_nascimento=null,
    consentimento_lgpd=false, ver_dados_sensiveis=false, dados='{}'::jsonb,
    atualizado_em=now(), updated_by=@u, reg_update=now()
where id=@p and cliente_id=@t and reg_status='A'",
                    new { rotulo, u = userId, p = cmd.PacienteId, t = tenantId }, tx, ct));
                await TrilhaAsync(cn, tx, tenantId, userId, ip, LgpdSaudeRegras.EventoTrilha("APAGAR"),
                    new { pacienteId = cmd.PacienteId, direto, ultimoRegistro = retencao.UltimoRegistro, aptoDesde = retencao.AptoDesde, rotulo, forma = "ANONIMIZACAO+NULECAO_IDENTIFICADORES", fonte = retencao.Fonte }, ct);
                await tx.CommitAsync(ct);

                var envApos = new LgpdPacienteEnvelope
                {
                    PacienteId = cmd.PacienteId,
                    Nome = rotulo,
                    Cpf = null,
                    Email = null,
                    Telefone = null,
                    DataNascimento = null,
                    UltimoRegistro = retencao.UltimoRegistro,
                    Consentimentos = await ConsentimentosAsync(cn, null, tenantId, cmd.PacienteId, ct),
                    Retencao = retencao
                };
                return ApiResponse<LgpdPacienteEnvelope>.Ok(envApos, "Direito de eliminação exercido via anonimização (LGPD art. 18, IV), com trilha.");
            }

            await TrilhaAsync(cn, tx, tenantId, userId, ip, LgpdSaudeRegras.EventoTrilha(direto),
                new { pacienteId = cmd.PacienteId, direto, ultimoRegistro = retencao.UltimoRegistro }, ct);
            await tx.CommitAsync(ct);

            var env = new LgpdPacienteEnvelope
            {
                PacienteId = p.Id,
                Nome = p.Nome,
                Cpf = p.Cpf,
                Email = p.Email,
                Telefone = p.Telefone,
                DataNascimento = p.DataNascimento,
                UltimoRegistro = retencao.UltimoRegistro,
                Consentimentos = await ConsentimentosAsync(cn, null, tenantId, cmd.PacienteId, ct),
                Retencao = retencao
            };
            return ApiResponse<LgpdPacienteEnvelope>.Ok(env, direto == "EXPORTAR" ? "Dados exportados com trilha." : "Dados listados com trilha.");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Preview da retenção mínima legal do prontuário do paciente (último registro, data apta e fonte).</summary>
    public async Task<ApiResponse<LgpdRetencaoDto>> RetencaoAsync(Guid tenantId, Guid pacienteId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<LgpdRetencaoDto>.Fail("Contexto de tenant obrigatório.", 403);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        if (await ObterPacienteAsync(cn, null, tenantId, pacienteId, ct) is null)
            return ApiResponse<LgpdRetencaoDto>.Fail("Paciente não encontrado.", 404);
        var prazo = await PrazoMesesAsync(ct);
        var info = await RetencaoInfoAsync(cn, null, tenantId, pacienteId, prazo, ct);
        return ApiResponse<LgpdRetencaoDto>.Ok(info, "Retenção calculada a partir do último registro clínico.");
    }
}

public sealed record LgpdConsentimentoCommand(Guid PacienteId, string Escopo, bool Concedido, string Canal, string? VersaoTermo);
public sealed record ExercerDiretoCommand(Guid PacienteId, string Direto);

public sealed class LgpdEscopoDto
{
    public string Codigo { get; init; } = "";
    public string Nome { get; init; } = "";
    public string Descricao { get; init; } = "";
    public bool Exige { get; init; }
    public string Fonte { get; init; } = "";
}

public sealed class PacienteBase
{
    public Guid Id { get; init; } = Guid.Empty;
    public string Nome { get; init; } = "";
    public string? Cpf { get; init; }
    public string? Email { get; init; }
    public string? Telefone { get; init; }
    public DateOnly? DataNascimento { get; init; }
}

public sealed class LgpdConsentimentoDto
{
    public Guid Id { get; init; } = Guid.Empty;
    public Guid PacienteId { get; init; } = Guid.Empty;
    public string Finalidade { get; init; } = "";
    public bool Concedido { get; set; }
    public string Canal { get; init; } = "";
    public string? VersaoTermo { get; init; }
    public string BaseLegal { get; init; } = "";
    public string Status { get; init; } = "";
    public Guid? UsuarioId { get; init; }
    public DateTime RegDate { get; set; }
}

public sealed class LgpdRetencaoDto
{
    public DateOnly? UltimoRegistro { get; init; }
    public int PrazoMeses { get; init; }
    public DateOnly? AptoDesde { get; init; }
    public bool PodeEliminarAgora { get; init; }
    public string Fonte { get; init; } = "";
}

public sealed class LgpdPacienteEnvelope
{
    public Guid PacienteId { get; init; } = Guid.Empty;
    public string Nome { get; init; } = "";
    public string? Cpf { get; init; }
    public string? Email { get; init; }
    public string? Telefone { get; init; }
    public DateOnly? DataNascimento { get; init; }
    public DateOnly? UltimoRegistro { get; init; }
    public List<LgpdConsentimentoDto> Consentimentos { get; set; } = new();
    public LgpdRetencaoDto Retencao { get; init; } = new LgpdRetencaoDto();
}
