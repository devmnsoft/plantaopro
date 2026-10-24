using System.Security.Claims;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

// R103 (integracoes - WhatsApp opt-in + e-mail transacional): caixa de saida unica dos dois
// canais de mensagem. O opt-in segue o consentimento LGPD do paciente (consentimento_lgpd +
// canal em lista separada por virgula); o provedor padrao e LOCAL (registro deterministico,
// sem segredos em config) e outros provedores entram por Integracoes:MensageriaProvider sem DDL.

public sealed record MensagemResultadoDto(Guid Id, string Canal, string Status, string Provider, string Destino, string TipoEvento, string Detalhe, DateTime CriadoEm);
public sealed record MensagemOutboxItemDto(Guid Id, string Canal, string Destino, string TipoEvento, string Status, string Provider, DateTime CriadoEm);
public sealed record MensageriaResumoDto(int Total, int Whatsapp, int Email, int Enviadas, int BloqueadasSemConsentimento, int SemDestino);
public sealed record OptInPacienteDto(Guid PacienteId, string Canal, bool Consentido, List<string> Canais);
public sealed class OptInPacienteRequest
{
    public Guid PacienteId { get; set; }
    public string Canal { get; set; } = string.Empty;
    public bool Conceder { get; set; }
}
public sealed class MensagemEnviarRequest
{
    public Guid PacienteId { get; set; }
    public string Canal { get; set; } = string.Empty;
    public string TipoEvento { get; set; } = string.Empty;
    public string? Assunto { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public sealed class MensageriaService
{
    private static readonly string[] CanaisPermitidos = { "WHATSAPP", "EMAIL" };
    private const string StatusEnviada = "ENVIADA";
    private const string StatusBloqueada = "BLOQUEADA_SEM_CONSENTIMENTO";
    private const string StatusSemDestino = "SEM_DESTINO";

    private const string PacienteSelect = @"select p.id, p.cliente_id as ClienteId, coalesce(p.nome_completo, p.nome, '') as Nome,
coalesce(p.telefone,'') as Telefone, coalesce(p.email,'') as Email, coalesce(p.consentimento_lgpd,false) as Consentimento,
coalesce(p.consentimento_lgpd_canal,'') as Canais
from plantaopro.pacientes p
where p.reg_status='A' and p.id=@id and (@isGlobal or (@tenantId is not null and p.cliente_id=@tenantId))";

    private readonly IConfiguration _cfg;
    private readonly TenantContextService _tenantContext;
    private readonly IAuditService _audit;
    private readonly ILogger<MensageriaService> _logger;

    public MensageriaService(IConfiguration cfg, TenantContextService tenantContext, IAuditService audit, ILogger<MensageriaService> logger)
    {
        _cfg = cfg;
        _tenantContext = tenantContext;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>Provedor efetivo: LOCAL por padrao (registro deterministico, sem segredo em config).</summary>
    public string ProviderEfetivo()
    {
        var valor = _cfg["Integracoes:MensageriaProvider"];
        return string.IsNullOrWhiteSpace(valor) ? "LOCAL" : valor.Trim().ToUpperInvariant();
    }

    public async Task<ApiResponse<OptInPacienteDto>> OptInAsync(OptInPacienteRequest request, string? ip)
    {
        try
        {
            var canal = (request.Canal ?? string.Empty).Trim().ToUpperInvariant();
            if (!CanaisPermitidos.Contains(canal)) return ApiResponse<OptInPacienteDto>.Fail("Canal inválido. Use WHATSAPP ou EMAIL.", 400);
            var ctx = await ContextoAsync();
            if (!ctx.Ok) return ApiResponse<OptInPacienteDto>.Fail(ctx.Message!, ctx.Code);

            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            var paciente = await cn.QueryFirstOrDefaultAsync<PacienteRow>(new CommandDefinition(PacienteSelect, new { id = request.PacienteId, tenantId = (object?)ctx.Escopo, isGlobal = ctx.IsGlobal }));
            if (paciente is null) return ApiResponse<OptInPacienteDto>.Fail("Paciente não encontrado para mensageria.", 404);

            var atuais = paciente.Canais.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length > 0)
                .Select(x => x.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (request.Conceder) atuais.Add(canal); else atuais.Remove(canal);
            var novos = atuais.OrderBy(x => x, StringComparer.Ordinal).ToList();
            var consentido = request.Conceder || novos.Count > 0;

            await cn.ExecuteAsync(@"update plantaopro.pacientes
set consentimento_lgpd=@consentido,
    consentimento_lgpd_em=case when @consentido then coalesce(consentimento_lgpd_em, now()) else null end,
    consentimento_lgpd_canal=@canais, atualizado_em=now()
where id=@id", new { consentido, canais = string.Join(",", novos), id = request.PacienteId });

            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.ClienteId, "MENSAGERIA_OPTIN", request.PacienteId,
                request.Conceder ? "OPT_IN" : "OPT_OUT", new { canal, canais = novos }, true, ip, "MENSAGERIA");
            return ApiResponse<OptInPacienteDto>.Ok(new OptInPacienteDto(request.PacienteId, canal, consentido, novos),
                request.Conceder ? $"Opt-in {canal} registrado." : $"Opt-out {canal} registrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no opt-in de mensageria do paciente {PacienteId}", request.PacienteId);
            return ApiResponse<OptInPacienteDto>.Fail("Não foi possível registrar o opt-in.", 500);
        }
    }

    public async Task<ApiResponse<MensagemResultadoDto>> EnviarAsync(MensagemEnviarRequest request, string? ip)
    {
        try
        {
            var canal = (request.Canal ?? string.Empty).Trim().ToUpperInvariant();
            if (!CanaisPermitidos.Contains(canal)) return ApiResponse<MensagemResultadoDto>.Fail("Canal inválido. Use WHATSAPP ou EMAIL.", 400);
            if (string.IsNullOrWhiteSpace(request.Mensagem)) return ApiResponse<MensagemResultadoDto>.Fail("Mensagem é obrigatória.", 400);
            var tipoEvento = string.IsNullOrWhiteSpace(request.TipoEvento) ? "MANUAL" : request.TipoEvento.Trim();
            var ctx = await ContextoAsync();
            if (!ctx.Ok) return ApiResponse<MensagemResultadoDto>.Fail(ctx.Message!, ctx.Code);

            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            var paciente = await cn.QueryFirstOrDefaultAsync<PacienteRow>(new CommandDefinition(PacienteSelect, new { id = request.PacienteId, tenantId = (object?)ctx.Escopo, isGlobal = ctx.IsGlobal }));
            if (paciente is null) return ApiResponse<MensagemResultadoDto>.Fail("Paciente não encontrado para mensageria.", 404);

            var destino = canal == "WHATSAPP" ? paciente.Telefone : paciente.Email;
            var temOptIn = paciente.Consentimento && paciente.Canais.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(x => string.Equals(x.Trim().ToUpperInvariant(), canal, StringComparison.OrdinalIgnoreCase));

            string status, detalhe;
            if (string.IsNullOrWhiteSpace(destino))
            {
                status = StatusSemDestino;
                detalhe = canal == "WHATSAPP" ? "Paciente sem telefone cadastrado." : "Paciente sem e-mail cadastrado.";
            }
            else if (!temOptIn)
            {
                status = StatusBloqueada;
                detalhe = "Paciente sem opt-in para este canal.";
            }
            else
            {
                status = StatusEnviada;
                var provider = ProviderEfetivo();
                detalhe = provider == "LOCAL" ? "Registro local (provedor LOCAL, sem integração externa configurada)." : string.Empty;
            }

            var assunto = canal == "EMAIL" ? (string.IsNullOrWhiteSpace(request.Assunto) ? "PlantãoPro" : request.Assunto.Trim()) : null;
            var enviado = await cn.QueryAsync<Guid>(new CommandDefinition($@"insert into plantaopro.mensagens_saida
(id, tenant_id, cliente_id, paciente_id, canal, destino, tipo_evento, assunto, mensagem, status, provider, detalhe, criado_por, criado_em, processado_em, reg_date, reg_status)
values(gen_random_uuid(), @tenantId, @clienteId, @pacienteId, @canal, @destino, @tipoEvento, @assunto, @mensagem, @status, @provider, @detalhe, @criadoPor, now(), now(), now(), 'A')
returning id", new
            {
                tenantId = (object?)ctx.TenantId,
                clienteId = (object?)ctx.ClienteId,
                pacienteId = request.PacienteId,
                canal,
                destino = destino ?? string.Empty,
                tipoEvento,
                assunto,
                mensagem = request.Mensagem,
                status,
                provider = ProviderEfetivo(),
                detalhe,
                criadoPor = (object?)_tenantContext.ObterUsuarioId()
            }, cancellationToken: System.Threading.CancellationToken.None));
            var idMensagem = enviado.First();

            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.ClienteId, "MENSAGENS_SAIDA", idMensagem,
                "ENVIAR", new { canal, tipoEvento, status }, true, ip, "MENSAGERIA");
            return ApiResponse<MensagemResultadoDto>.Ok(new MensagemResultadoDto(idMensagem, canal, status, ProviderEfetivo(), destino ?? string.Empty, tipoEvento, detalhe, DateTime.UtcNow),
                status == StatusEnviada ? "Mensagem enviada." : $"Mensagem registrada como {status}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar mensagem do paciente {PacienteId}", request.PacienteId);
            return ApiResponse<MensagemResultadoDto>.Fail("Não foi possível enviar a mensagem.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<MensagemOutboxItemDto>>> ListarOutboxAsync(int limite)
    {
        var ctx = await ContextoAsync();
        if (!ctx.Ok) return ApiResponse<IEnumerable<MensagemOutboxItemDto>>.Fail(ctx.Message!, ctx.Code);
        var q = Math.Clamp(limite <= 0 ? 50 : limite, 1, 200);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        var rows = await cn.QueryAsync<MensagemOutboxItemDto>(new CommandDefinition(@"select id as Id, canal as Canal, coalesce(destino,'') as Destino, coalesce(tipo_evento,'') as TipoEvento,
status as Status, provider as Provider, criado_em as CriadoEm
from plantaopro.mensagens_saida
where reg_status='A' and (@isGlobal or (@tenantId is not null and cliente_id=@tenantId))
order by criado_em desc limit @limite", new { tenantId = (object?)ctx.Escopo, isGlobal = ctx.IsGlobal, limite = q }));
        return ApiResponse<IEnumerable<MensagemOutboxItemDto>>.Ok(rows.AsEnumerable());
    }

    public async Task<ApiResponse<MensageriaResumoDto>> ResumoAsync()
    {
        var ctx = await ContextoAsync();
        if (!ctx.Ok) return ApiResponse<MensageriaResumoDto>.Fail(ctx.Message!, ctx.Code);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        var linha = await cn.QueryFirstOrDefaultAsync<MensageriaResumoDto>(new CommandDefinition(@"select count(*)::int as Total,
count(*) filter (where canal='WHATSAPP')::int as Whatsapp,
count(*) filter (where canal='EMAIL')::int as Email,
count(*) filter (where status='ENVIADA')::int as Enviadas,
count(*) filter (where status='BLOQUEADA_SEM_CONSENTIMENTO')::int as BloqueadasSemConsentimento,
count(*) filter (where status='SEM_DESTINO')::int as SemDestino
from plantaopro.mensagens_saida
where reg_status='A' and (@isGlobal or (@tenantId is not null and cliente_id=@tenantId))", new { tenantId = (object?)ctx.Escopo, isGlobal = ctx.IsGlobal }));
        // count(*) sempre devolve uma linha; o fallback cobre o caminho inalcançável sem virar nulo.
        return ApiResponse<MensageriaResumoDto>.Ok(linha ?? new MensageriaResumoDto(0, 0, 0, 0, 0, 0));
    }

    private async Task<(bool Ok, Guid? Escopo, Guid? TenantId, Guid? ClienteId, bool IsGlobal, string? Message, int Code)> ContextoAsync()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data is null)
            return (false, null, null, null, false, ctx.Message, ctx.StatusCode == 0 ? 409 : ctx.StatusCode);
        var escopo = ctx.Data.ClienteId ?? ctx.Data.TenantId;
        if (escopo is null)
            return (false, null, ctx.Data.TenantId, ctx.Data.ClienteId, false, "Contexto de tenant obrigatório.", 409);
        return (true, escopo, ctx.Data.TenantId, ctx.Data.ClienteId, _tenantContext.UsuarioEhAdminGlobal(), null, 200);
    }

    private sealed record PacienteRow(Guid Id, Guid? ClienteId, string Nome, string Telefone, string Email, bool Consentimento, string Canais);
}

// R103 (integracoes - API publica com chaves): leitura somente, sempre no escopo da chave
// (instituição vinculada no momento da criação — mesmo valor de escopo clínico).
public sealed record PublicoIdentidadeDto(string ChaveId, string Nome, List<string> Escopos, string? TenantId, string? ClienteId);
public sealed record PublicoSituacaoAgendamentoDto(string AgendamentoId, string Status, string DataInicio, string PacienteNome);
public sealed record PublicoPacienteDto(string Nome, string Status, string DataNascimento);

public sealed class IntegracoesPublicaService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<IntegracoesPublicaService> _logger;

    public IntegracoesPublicaService(IConfiguration cfg, ILogger<IntegracoesPublicaService> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    public ApiResponse<PublicoIdentidadeDto> IdentidadeAsync(ClaimsPrincipal user)
    {
        var chaveId = user.FindFirstValue("chave_id") ?? string.Empty;
        var nome = user.FindFirstValue("chave_nome") ?? string.Empty;
        var escopos = user.Claims.Where(c => c.Type == "escopo").Select(c => c.Value).OrderBy(x => x, StringComparer.Ordinal).ToList();
        return ApiResponse<PublicoIdentidadeDto>.Ok(new PublicoIdentidadeDto(chaveId, nome, escopos, user.FindFirstValue("tenant_id"), user.FindFirstValue("cliente_id")));
    }

    public async Task<ApiResponse<PublicoSituacaoAgendamentoDto>> SituacaoAgendamentoAsync(ClaimsPrincipal user, Guid id)
    {
        var falha = EscopoDe(user);
        if (falha.Erro is not null) return ApiResponse<PublicoSituacaoAgendamentoDto>.Fail(falha.Erro.Message!, falha.Erro.StatusCode);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        var linha = await cn.QueryFirstOrDefaultAsync<PublicoSituacaoAgendamentoDto>(new CommandDefinition(@"select a.id::text as AgendamentoId, coalesce(a.status,'') as Status,
a.data_inicio::text as DataInicio, coalesce(nullif(p.nome_completo,''), p.nome, '') as PacienteNome
from plantaopro.agendamentos a
left join plantaopro.pacientes p on p.id=a.paciente_id and p.reg_status='A'
where a.reg_status='A' and a.id=@id and a.cliente_id=@tenantId", new { id, tenantId = falha.TenantId }));
        if (linha is null) return ApiResponse<PublicoSituacaoAgendamentoDto>.Fail("Agendamento não encontrado para esta chave.", 404);
        return ApiResponse<PublicoSituacaoAgendamentoDto>.Ok(linha);
    }

    public async Task<ApiResponse<PublicoPacienteDto>> PacienteAsync(ClaimsPrincipal user, Guid id)
    {
        var falha = EscopoDe(user);
        if (falha.Erro is not null) return ApiResponse<PublicoPacienteDto>.Fail(falha.Erro.Message!, falha.Erro.StatusCode);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        var linha = await cn.QueryFirstOrDefaultAsync<PublicoPacienteDto>(new CommandDefinition(@"select coalesce(nullif(p.nome_completo,''), p.nome, '') as Nome, coalesce(p.status,'') as Status,
coalesce(to_char(p.data_nascimento,'YYYY-MM-DD'),'') as DataNascimento
from plantaopro.pacientes p
where p.reg_status='A' and p.id=@id and p.cliente_id=@tenantId", new { id, tenantId = falha.TenantId }));
        if (linha is null) return ApiResponse<PublicoPacienteDto>.Fail("Paciente não encontrado para esta chave.", 404);
        return ApiResponse<PublicoPacienteDto>.Ok(linha);
    }

    private (ApiResponse<string>? Erro, Guid TenantId) EscopoDe(ClaimsPrincipal user)
    {
        var valor = user.FindFirstValue("cliente_id");
        if (!Guid.TryParse(valor, out var tenantId))
            return (ApiResponse<string>.Fail("Chave sem vínculo de instituição.", 403), Guid.Empty);
        return (null, tenantId);
    }
}
