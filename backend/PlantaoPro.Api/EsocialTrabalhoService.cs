using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

// ============================================================================
// R88 (trabalhista / eSocial): previa dos eventos eSocial da faixa S-1000 a
// S-1299 por colaborador (forma_envio fixa PREVIEW), analise de horas/jornada
// semanal contra a carga contratual e o teto legal de 44h (CF/88 art. 7, XIII),
// estimativa de horas extras (CLT art. 59 §1º sobre salário/220h - CLT art. 74
// §2º) e calculo do aviso previo proporcional (Lei 12.506/2011 + NT MTE 184/2012).
// Regras puras em EsocialTrabalhoRegras; valores legais vem de parametros_legais.
// ============================================================================

public sealed record EsocialEventoCatalogoDto(string Codigo, string Nome, string Categoria, string Descricao, string Vencimento, string FonteOficial);

public sealed record EsocialColaboradorDto(
    Guid Id, string Matricula, string Nome, string Cpf, string StatusColaborador,
    bool ContratoVigente, string? TipoContrato, decimal? SalarioVigente, short? CargaHorariaSemanal,
    DateOnly? AdmissaoPrimeiroContrato);

public sealed record EsocialSemanaJornadaDto(
    Guid? Id, DateOnly SemanaInicio, string PeriodoLabel, bool Declarada,
    decimal Normais, decimal Extras, decimal Total, bool ExcedeLimiteLegal, bool? ExcedeContrato,
    decimal HorasExcedentes, string Situacao);

public sealed record EsocialJornadaResumoDto(
    Guid ColaboradorId, string ColaboradorNome, string Mes,
    bool ContratoVigente, string? TipoContrato, decimal? SalarioContratado, int? CargaHorariaSemanal,
    decimal JornadaSemanalMax,
    IReadOnlyList<EsocialSemanaJornadaDto> Semanas,
    decimal TotalNormais, decimal TotalExtras, decimal TotalHoras, decimal HorasExcedentesMes,
    decimal? ValorHoraExtra, decimal? ValorTotalHorasExtras,
    IReadOnlyList<string> Fontes);

public sealed record EsocialJornadaDeclararCommand(Guid ColaboradorId, DateOnly SemanaInicio, decimal HorasNormais, decimal? HorasExtras, string? Observacao);

public sealed record EsocialAvisoPrevioCommand(DateOnly AdmissaoEm, DateOnly DemissaoEm);

public sealed record EsocialAvisoPrevioDto(
    DateOnly AdmissaoEm, DateOnly DemissaoEm, int DiasBase, int AnosCompletos, int DiasAcrecimo, int DiasTotal,
    string ReducaoJornadaAviso, IReadOnlyList<string> Fontes);

public sealed record EsocialEventoDeclararCommand(Guid? ColaboradorId, string CodigoEvento, string? PeriodoApuracao, DateOnly DataFato, string? Descricao);

public sealed record EsocialEventoDeclaradoDto(
    Guid Id, Guid? ColaboradorId, string? ColaboradorNome,
    string CodigoEvento, string NomeEvento, string Categoria,
    string? PeriodoApuracao, DateOnly DataFato, string? Descricao, string FormaEnvio, DateTime CriadoEm);

public sealed record EsocialExcluirCommand(Guid Id);

public sealed class EsocialTrabalhoService
{
    private static readonly System.Text.RegularExpressions.Regex MesRegex = new("^[0-9]{4}-(0[1-9]|1[0-2])$");

    private sealed record ColabNomeDto(string Nome);
    private sealed record ContratoVigenteDto(string? Tipo, decimal Salario, short Carga);
    private sealed record EventoCatalogoBaseDto(string Nome, string Categoria);
    private sealed record ParamLinhaDto(string Codigo, string? Valor);
    private sealed record SemanaDeclaradaDto(Guid Id, DateOnly SemanaInicio, decimal Normais, decimal Extras);

    private readonly string _conexao;

    public EsocialTrabalhoService(string connectionString) => _conexao = connectionString;

    private static CommandDefinition Cmd(string sql, object? param = null, NpgsqlTransaction? tx = null, CancellationToken ct = default) =>
        new(sql, param, tx, cancellationToken: ct);

    private sealed record Parametros(
        IReadOnlyList<string> CatalogoEventos, int VencimentoPeriodicoDia,
        int AvisoBaseDias, int AvisoAcrescimoPorAno, int AvisoTetoTotal,
        decimal JornadaMaxSemanal, decimal HoraExtraPct, decimal DivisorMensal)
    {
        public static Parametros Padrao() => new(
            new[] { "S-1000", "S-1005", "S-1010", "S-1020", "S-1070", "S-1200", "S-1202", "S-1207", "S-1210", "S-1260", "S-1270", "S-1280", "S-1298", "S-1299" },
            15, EsocialTrabalhoRegras.AvisoPrevioBaseDias, EsocialTrabalhoRegras.AvisoPrevioAcrescimoPorAnoDias, EsocialTrabalhoRegras.AvisoPrevioTetoTotalDias,
            EsocialTrabalhoRegras.JornadaSemanalMaxHoras, EsocialTrabalhoRegras.HoraExtraAcrescimoMinPct, EsocialTrabalhoRegras.SalarioDivisorMensalHoras);
    }

    private static async Task<Parametros> LerParametrosAsync(NpgsqlConnection cn, CancellationToken ct)
    {
        var linhas = await cn.QueryAsync<ParamLinhaDto>(Cmd(
            @"select codigo Codigo, valor Valor from plantaopro.parametros_legais
where codigo in ('ESOCIAL_EVENTOS_CATALOGO','ESOCIAL_VENCIMENTO_PERIODICO_DIA','AVISO_PREVIO_BASE_DIAS',
 'AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS','AVISO_PREVIO_TETO_TOTAL_DIAS','JORNADA_SEMANAL_MAX_HORAS',
 'HORA_EXTRA_ACRESIMO_MIN_PCT','SALARIO_DIVISOR_MENSAL_HORAS') and reg_status='A'
order by ano desc", null, null, ct));
        var porCodigo = linhas.GroupBy(l => l.Codigo).ToDictionary(g => g.Key, g => g.First().Valor, StringComparer.OrdinalIgnoreCase);
        var p = Parametros.Padrao();
        if (porCodigo.TryGetValue("ESOCIAL_EVENTOS_CATALOGO", out var cat) && !string.IsNullOrWhiteSpace(cat))
            p = p with { CatalogoEventos = cat.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) };
        if (int.TryParse(porCodigo.GetValueOrDefault("ESOCIAL_VENCIMENTO_PERIODICO_DIA"), out var dia) && dia >= 1 && dia <= 31) p = p with { VencimentoPeriodicoDia = dia };
        if (int.TryParse(porCodigo.GetValueOrDefault("AVISO_PREVIO_BASE_DIAS"), out var baseDias) && baseDias > 0) p = p with { AvisoBaseDias = baseDias };
        if (int.TryParse(porCodigo.GetValueOrDefault("AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS"), out var porAno) && porAno > 0) p = p with { AvisoAcrescimoPorAno = porAno };
        if (int.TryParse(porCodigo.GetValueOrDefault("AVISO_PREVIO_TETO_TOTAL_DIAS"), out var teto) && teto > 0) p = p with { AvisoTetoTotal = teto };
        if (decimal.TryParse(porCodigo.GetValueOrDefault("JORNADA_SEMANAL_MAX_HORAS"), NumberStyles.None, CultureInfo.InvariantCulture, out var maxH) && maxH > 0) p = p with { JornadaMaxSemanal = maxH };
        if (decimal.TryParse(porCodigo.GetValueOrDefault("HORA_EXTRA_ACRESIMO_MIN_PCT"), NumberStyles.None, CultureInfo.InvariantCulture, out var pct) && pct >= 0) p = p with { HoraExtraPct = pct };
        if (decimal.TryParse(porCodigo.GetValueOrDefault("SALARIO_DIVISOR_MENSAL_HORAS"), NumberStyles.None, CultureInfo.InvariantCulture, out var divisor) && divisor > 0) p = p with { DivisorMensal = divisor };
        return p;
    }

    // ---------------------------------------------------------------------
    // Catalogo e colaboradores
    // ---------------------------------------------------------------------

    /// <summary>Catálogo oficial dos eventos eSocial da faixa S-1000 a S-1299 (leiaute S-1.3).</summary>
    public async Task<ApiResponse<IReadOnlyList<EsocialEventoCatalogoDto>>> CatalogoAsync(Guid tenantId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<IReadOnlyList<EsocialEventoCatalogoDto>>.Fail("Contexto de tenant obrigatório.", 403);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var lista = (await cn.QueryAsync<EsocialEventoCatalogoDto>(Cmd(@"
select codigo Codigo, nome Nome, categoria Categoria, descricao Descricao, vencimento Vencimento, fonte_oficial FonteOficial
from plantaopro.esocial_eventos_catalogo
where ativo and reg_status='A'
order by codigo", null, null, ct))).ToList();
        return ApiResponse<IReadOnlyList<EsocialEventoCatalogoDto>>.Ok(lista);
    }

    /// <summary>Colaboradores com o contexto trabalhista: contrato vigente (tipo/salário/carga), status e primeira admissão registrada.</summary>
    public async Task<ApiResponse<IReadOnlyList<EsocialColaboradorDto>>> ColaboradoresAsync(Guid tenantId, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<IReadOnlyList<EsocialColaboradorDto>>.Fail("Contexto de tenant obrigatório.", 403);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var lista = (await cn.QueryAsync<EsocialColaboradorDto>(Cmd(@"
select p.id Id, p.matricula Matricula, p.nome Nome, p.cpf Cpf, p.status StatusColaborador,
       coalesce((ctv.id is not null), false) ContratoVigente,
       ctv.tipo TipoContrato, ctv.salario SalarioVigente, ctv.carga_horaria_semanal CargaHorariaSemanal,
       adm.primeira_admissao AdmissaoPrimeiroContrato
from plantaopro.adm_colaboradores p
left join (
    select x.* from (
        select id, colaborador_id, tipo, salario, carga_horaria_semanal,
               row_number() over (partition by colaborador_id order by inicio desc, reg_date desc) rn
        from plantaopro.adm_contratos_trabalho
        where tenant_id=@t and status='VIGENTE' and reg_status='A'
    ) x where x.rn=1
) ctv on ctv.colaborador_id=p.id
left join (
    select colaborador_id, min(inicio)::date primeira_admissao
    from plantaopro.adm_contratos_trabalho
    where tenant_id=@t and reg_status='A'
    group by colaborador_id
) adm on adm.colaborador_id=p.id
where p.tenant_id=@t and p.reg_status='A'
order by p.nome", new { t = tenantId }, null, ct))).ToList();
        return ApiResponse<IReadOnlyList<EsocialColaboradorDto>>.Ok(lista);
    }

    // ---------------------------------------------------------------------
    // Jornada semanal por colaborador (horas × contratual × teto legal)
    // ---------------------------------------------------------------------

    public async Task<ApiResponse<EsocialJornadaResumoDto>> JornadaAsync(Guid tenantId, Guid colaboradorId, string mes, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<EsocialJornadaResumoDto>.Fail("Contexto de tenant obrigatório.", 403);
        if (!MesRegex.IsMatch(mes ?? string.Empty)) return ApiResponse<EsocialJornadaResumoDto>.Fail("Informe o mês no formato AAAA-MM.", 400);
        var partes = (mes ?? string.Empty).Split('-');
        var primeiroDia = new DateOnly(int.Parse(partes[0], CultureInfo.InvariantCulture), int.Parse(partes[1], CultureInfo.InvariantCulture), 1);

        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var resumo = await ResumirJornadaAsync(cn, null, tenantId, colaboradorId, primeiroDia, await LerParametrosAsync(cn, ct), ct);
        return resumo is null ? ApiResponse<EsocialJornadaResumoDto>.Fail("Colaborador não encontrado nesta organização.", 404) : ApiResponse<EsocialJornadaResumoDto>.Ok(resumo);
    }

    public async Task<ApiResponse<EsocialJornadaResumoDto>> DeclararJornadaAsync(Guid tenantId, Guid? userId, EsocialJornadaDeclararCommand cmd, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<EsocialJornadaResumoDto>.Fail("Contexto de tenant obrigatório.", 403);
        if (cmd.ColaboradorId == Guid.Empty) return ApiResponse<EsocialJornadaResumoDto>.Fail("Selecione o colaborador.", 400);
        if (cmd.SemanaInicio < new DateOnly(2020, 1, 1)) return ApiResponse<EsocialJornadaResumoDto>.Fail("Informe a data da semana (a partir de 2020-01-01).", 400);
        var horasExtras = cmd.HorasExtras ?? 0m;
        if (cmd.HorasNormais < 0m || horasExtras < 0m) return ApiResponse<EsocialJornadaResumoDto>.Fail("As horas não podem ser negativas.", 400);
        if (cmd.HorasNormais + horasExtras > EsocialTrabalhoRegras.HorasSemanaisMaxSanidade)
            return ApiResponse<EsocialJornadaResumoDto>.Fail("A soma das horas normais e extras não pode ultrapassar 120 horas na semana.", 400);
        var observacao = string.IsNullOrWhiteSpace(cmd.Observacao) ? null : cmd.Observacao.Trim();
        if (observacao is not null && observacao.Length > 400) return ApiResponse<EsocialJornadaResumoDto>.Fail("A observação pode ter no máximo 400 caracteres.", 400);
        var semana = EsocialTrabalhoRegras.SemanaInicio(cmd.SemanaInicio);

        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var existe = await cn.ExecuteScalarAsync<bool>(Cmd(
            "select exists(select 1 from plantaopro.adm_colaboradores where id=@c and tenant_id=@t and reg_status='A')",
            new { c = cmd.ColaboradorId, t = tenantId }, null, ct));
        if (!existe) return ApiResponse<EsocialJornadaResumoDto>.Fail("Colaborador não encontrado nesta organização.", 404);

        await using var tx = (await cn.BeginTransactionAsync(ct))!;
        try
        {
            await cn.ExecuteAsync(Cmd(@"
insert into plantaopro.colaborador_jornadas_semanais (tenant_id, colaborador_id, semana_inicio, horas_normais, horas_extras, observacao, criado_por)
values (@t, @c, @s, @n, @e, @o, @u)
on conflict (tenant_id, colaborador_id, semana_inicio) do update
set horas_normais=excluded.horas_normais,
    horas_extras=excluded.horas_extras,
    observacao=excluded.observacao,
    criado_por=coalesce(excluded.criado_por, colaborador_jornadas_semanais.criado_por),
    reg_update=now()",
            new { t = tenantId, c = cmd.ColaboradorId, s = semana, n = cmd.HorasNormais, e = horasExtras, o = observacao, u = userId }, tx, ct));
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        var parametros = await LerParametrosAsync(cn, ct);
        var resumo = await ResumirJornadaAsync(cn, null, tenantId, cmd.ColaboradorId, new DateOnly(semana.Year, semana.Month, 1), parametros, ct);
        return resumo is null ? ApiResponse<EsocialJornadaResumoDto>.Fail("Colaborador não encontrado nesta organização.", 404) : ApiResponse<EsocialJornadaResumoDto>.Ok(resumo, "Semana declarada.");
    }

    public async Task<ApiResponse<object>> ExcluirJornadaAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<object>.Fail("Contexto de tenant obrigatório.", 403);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var afetos = await cn.ExecuteAsync(Cmd(
            "delete from plantaopro.colaborador_jornadas_semanais where id=@id and tenant_id=@t",
            new { id, t = tenantId }, null, ct));
        if (afetos == 0) return ApiResponse<object>.Fail("Declaração de jornada não encontrada nesta organização.", 404);
        return ApiResponse<object>.Ok(new object(), "Declaração removida.");
    }

    private static async Task<EsocialJornadaResumoDto?> ResumirJornadaAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid colaboradorId, DateOnly primeiroDiaMes, Parametros parametros, CancellationToken ct)
    {
        var colab = await cn.QueryFirstOrDefaultAsync<ColabNomeDto>(Cmd(
            "select nome Nome from plantaopro.adm_colaboradores where id=@c and tenant_id=@t and reg_status='A'",
            new { c = colaboradorId, t = tenantId }, tx, ct));
        if (colab is null) return null;

        var contrato = await cn.QueryFirstOrDefaultAsync<ContratoVigenteDto>(Cmd(@"
select x.tipo Tipo, x.salario Salario, x.carga_horaria_semanal Carga
from (
    select tipo, salario, carga_horaria_semanal,
           row_number() over (partition by colaborador_id order by inicio desc, reg_date desc) rn
    from plantaopro.adm_contratos_trabalho
    where tenant_id=@t and colaborador_id=@c and status='VIGENTE' and reg_status='A'
) x where x.rn=1", new { t = tenantId, c = colaboradorId }, tx, ct));

        var ultimoDia = new DateOnly(primeiroDiaMes.Year, primeiroDiaMes.Month, DateTime.DaysInMonth(primeiroDiaMes.Year, primeiroDiaMes.Month));
        var declaradas = (await cn.QueryAsync<SemanaDeclaradaDto>(Cmd(@"
select id Id, semana_inicio::date SemanaInicio, horas_normais Normais, horas_extras Extras
from plantaopro.colaborador_jornadas_semanais
where tenant_id=@t and colaborador_id=@c and semana_inicio between @de and @ate and reg_status='A'
order by semana_inicio", new { t = tenantId, c = colaboradorId, de = primeiroDiaMes, ate = ultimoDia }, tx, ct)))
            .ToDictionary(d => d.SemanaInicio);

        decimal? carga = contrato is null ? null : (decimal)contrato.Carga;
        decimal? salario = contrato is null ? null : contrato.Salario;
        var maxSemanal = parametros.JornadaMaxSemanal;

        var semanas = new List<EsocialSemanaJornadaDto>();
        for (var inicio = EsocialTrabalhoRegras.SemanaInicio(primeiroDiaMes); inicio <= ultimoDia; inicio = inicio.AddDays(7))
        {
            if (declaradas.TryGetValue(inicio, out var d))
            {
                var r = EsocialTrabalhoRegras.ResumoSemanaJornada.Calcular(d.Normais, d.Extras, carga, maxSemanal);
                semanas.Add(new EsocialSemanaJornadaDto(d.Id, inicio, inicio.ToString("yyyy-'W'ww"), true, r.Normais, r.Extras, r.Total, r.ExcedeLimiteLegal, r.ExcedeContrato, r.HorasExcedentes, r.Situacao));
            }
            else
            {
                semanas.Add(new EsocialSemanaJornadaDto(null, inicio, inicio.ToString("yyyy-'W'ww"), false, 0m, 0m, 0m, false, carga.HasValue ? false : null, 0m, EsocialTrabalhoRegras.SituacoesJornada.NaoDeclarada));
            }
        }

        var totalNormais = semanas.Where(s => s.Declarada).Sum(s => s.Normais);
        var totalExtras = semanas.Where(s => s.Declarada).Sum(s => s.Extras);
        var totalHoras = totalNormais + totalExtras;
        var excedentes = semanas.Sum(s => s.HorasExcedentes);
        var valorHora = EsocialTrabalhoRegras.ValorHoraExtra(salario, parametros.DivisorMensal, parametros.HoraExtraPct);
        var valorExcedentes = EsocialTrabalhoRegras.ValorHorasExtras(valorHora, excedentes);

        return new EsocialJornadaResumoDto(
            colaboradorId, colab.Nome, primeiroDiaMes.ToString("yyyy-MM"),
            contrato is not null, contrato?.Tipo, salario, contrato is null ? null : contrato.Carga,
            maxSemanal,
            semanas,
            totalNormais, totalExtras, totalHoras, excedentes,
            valorHora, valorExcedentes,
            new[]
            {
                "CF/88, art. 7, XIII - duração da jornada de trabalho de, no máximo, 44 horas semanais",
                "CLT (Decreto-Lei 5.452/1943), art. 59, parágrafo único - hora extra remunerada com acréscimo de, no mínimo, 50%",
                "CLT (Decreto-Lei 5.452/1943), art. 74, parágrafo único - salário mensal equivale a, no máximo, 220 horas de trabalho mensalmente"
            });
    }

    // ---------------------------------------------------------------------
    // Aviso prévio proporcional (Lei 12.506/2011)
    // ---------------------------------------------------------------------

    public async Task<ApiResponse<EsocialAvisoPrevioDto>> AvisoPrevioAsync(Guid tenantId, EsocialAvisoPrevioCommand cmd, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<EsocialAvisoPrevioDto>.Fail("Contexto de tenant obrigatório.", 403);
        var erros = EsocialTrabalhoRegras.ValidarAvisoPrevio(cmd.AdmissaoEm, cmd.DemissaoEm);
        if (erros.Count > 0) return ApiResponse<EsocialAvisoPrevioDto>.Fail(string.Join(" ", erros), 400, erros);

        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var parametros = await LerParametrosAsync(cn, ct);

        var anos = EsocialTrabalhoRegras.AnosCompletos(cmd.AdmissaoEm, cmd.DemissaoEm);
        var diasTotal = EsocialTrabalhoRegras.DiasAvisoPrevio(cmd.AdmissaoEm, cmd.DemissaoEm, parametros.AvisoBaseDias, parametros.AvisoAcrescimoPorAno, parametros.AvisoTetoTotal);
        return ApiResponse<EsocialAvisoPrevioDto>.Ok(new EsocialAvisoPrevioDto(
            cmd.AdmissaoEm, cmd.DemissaoEm,
            parametros.AvisoBaseDias, anos, Math.Max(0, diasTotal - parametros.AvisoBaseDias), diasTotal,
            "Durante o aviso prévio dado pelo empregador, o horário é reduzido em 2 horas diárias (ou o empregado pode faltar 7 dias corridos), sem prejuízo do salário integral (CLT art. 488).",
            new[]
            {
                "CLT (Decreto-Lei 5.452/1943), art. 487, II - aviso prévio mínimo de 30 dias",
                "Lei 12.506/2011, art. 1 - base de 30 dias para até 1 ano de serviço na mesma empresa",
                "Lei 12.506/2011, parágrafo único - acrescido de 3 dias por ano de serviço prestado, limitado o total a 90 dias",
                "Nota Técnica MTE nº 184/2012 - o acréscimo incide a partir do tempo superior a 1 ano na mesma empresa; até 1 ano completo permanece a base de 30 dias",
                "CLT (Decreto-Lei 5.452/1943), art. 488 - redução de 2 horas diárias durante o aviso, sem prejuízo do salário integral"
            }));
    }

    // ---------------------------------------------------------------------
    // Eventos eSocial em prévia (por colaborador ou institucionais)
    // ---------------------------------------------------------------------

    public async Task<ApiResponse<IReadOnlyList<EsocialEventoDeclaradoDto>>> EventosAsync(Guid tenantId, Guid? colaboradorId, string? periodoApuracao, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<IReadOnlyList<EsocialEventoDeclaradoDto>>.Fail("Contexto de tenant obrigatório.", 403);
        if (!string.IsNullOrEmpty(periodoApuracao) && !MesRegex.IsMatch(periodoApuracao!))
            return ApiResponse<IReadOnlyList<EsocialEventoDeclaradoDto>>.Fail("Informe o período no formato AAAA-MM.", 400);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var lista = (await cn.QueryAsync<EsocialEventoDeclaradoDto>(Cmd(@"
select e.id Id, e.colaborador_id ColaboradorId, p.nome ColaboradorNome,
       e.codigo_evento CodigoEvento, c.nome NomeEvento, c.categoria Categoria,
       e.periodo_apuracao PeriodoApuracao, e.data_fato DataFato, e.descricao Descricao,
       e.forma_envio FormaEnvio, coalesce(e.reg_date, now()) CriadoEm
from plantaopro.colaborador_eventos_esocial e
join plantaopro.esocial_eventos_catalogo c on c.codigo=e.codigo_evento
left join plantaopro.adm_colaboradores p on p.id=e.colaborador_id
where e.tenant_id=@t and e.reg_status='A'
  and (@c is null or e.colaborador_id=@c)
  and (@per is null or e.periodo_apuracao=@per)
order by e.data_fato desc, e.reg_date desc
limit 300", new { t = tenantId, c = colaboradorId, per = periodoApuracao }, null, ct))).ToList();
        return ApiResponse<IReadOnlyList<EsocialEventoDeclaradoDto>>.Ok(lista);
    }

    public async Task<ApiResponse<EsocialEventoDeclaradoDto>> DeclararEventoAsync(Guid tenantId, Guid? userId, EsocialEventoDeclararCommand cmd, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<EsocialEventoDeclaradoDto>.Fail("Contexto de tenant obrigatório.", 403);
        var erros = new List<string>();
        var codigo = string.IsNullOrWhiteSpace(cmd.CodigoEvento) ? null : cmd.CodigoEvento.Trim();
        if (codigo is null) erros.Add("Selecione o evento eSocial.");
        if (cmd.DataFato < new DateOnly(2020, 1, 1)) erros.Add("Informe a data do fato (a partir de 2020-01-01).");
        var periodo = string.IsNullOrWhiteSpace(cmd.PeriodoApuracao) ? null : cmd.PeriodoApuracao.Trim();
        if (periodo is not null && !MesRegex.IsMatch(periodo!)) erros.Add("O período de apuração deve estar no formato AAAA-MM.");
        var descricao = string.IsNullOrWhiteSpace(cmd.Descricao) ? null : cmd.Descricao.Trim();
        if (descricao is not null && descricao.Length > 400) erros.Add("A descrição pode ter no máximo 400 caracteres.");
        if (erros.Count > 0) return ApiResponse<EsocialEventoDeclaradoDto>.Fail(string.Join(" ", erros), 400, erros);

        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var evento = await cn.QueryFirstOrDefaultAsync<EventoCatalogoBaseDto>(Cmd(
            "select nome Nome, categoria Categoria from plantaopro.esocial_eventos_catalogo where codigo=@c and ativo and reg_status='A'",
            new { c = codigo }, null, ct));
        if (evento is null) return ApiResponse<EsocialEventoDeclaradoDto>.Fail("Evento eSocial não presente no catálogo oficial gerenciado pelo produto.", 400);
        if (evento.Categoria == "PERIODICO" && periodo is null)
            return ApiResponse<EsocialEventoDeclaradoDto>.Fail("Eventos periódicos exigem o período de apuração (AAAA-MM).", 400);
        if (cmd.ColaboradorId is not null && cmd.ColaboradorId != Guid.Empty)
        {
            var existe = await cn.ExecuteScalarAsync<bool>(Cmd(
                "select exists(select 1 from plantaopro.adm_colaboradores where id=@c and tenant_id=@t and reg_status='A')",
                new { c = cmd.ColaboradorId.Value, t = tenantId }, null, ct));
            if (!existe) return ApiResponse<EsocialEventoDeclaradoDto>.Fail("Colaborador não encontrado nesta organização.", 404);
        }

        var linha = await cn.QuerySingleAsync<EsocialEventoDeclaradoDto>(Cmd(@"
with novo as (
    insert into plantaopro.colaborador_eventos_esocial (tenant_id, colaborador_id, codigo_evento, periodo_apuracao, data_fato, descricao, forma_envio, criado_por)
    values (@t, @c, @codigo, @per, @data, @desc, 'PREVIEW', @u)
    returning *)
select n.id Id, n.colaborador_id ColaboradorId, p.nome ColaboradorNome,
       n.codigo_evento CodigoEvento, c.nome NomeEvento, c.categoria Categoria,
       n.periodo_apuracao PeriodoApuracao, n.data_fato DataFato, n.descricao Descricao,
       n.forma_envio FormaEnvio, coalesce(n.reg_date, now()) CriadoEm
from novo n
join plantaopro.esocial_eventos_catalogo c on c.codigo=n.codigo_evento
left join plantaopro.adm_colaboradores p on p.id=n.colaborador_id",
        new { t = tenantId, c = cmd.ColaboradorId, codigo, per = periodo, data = cmd.DataFato, desc = descricao, u = userId }, null, ct));
        return ApiResponse<EsocialEventoDeclaradoDto>.Ok(linha, "Evento registrado em prévia.");
    }

    public async Task<ApiResponse<object>> ExcluirEventoAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        if (tenantId == Guid.Empty) return ApiResponse<object>.Fail("Contexto de tenant obrigatório.", 403);
        await using var cn = new NpgsqlConnection(_conexao);
        await cn.OpenAsync(ct);
        var afetos = await cn.ExecuteAsync(Cmd(
            "delete from plantaopro.colaborador_eventos_esocial where id=@id and tenant_id=@t",
            new { id, t = tenantId }, null, ct));
        if (afetos == 0) return ApiResponse<object>.Fail("Evento não encontrado nesta organização.", 404);
        return ApiResponse<object>.Ok(new object(), "Evento removido.");
    }
}
