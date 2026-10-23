using System.Globalization;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

/// <summary>
/// R101 - relatório com benchmark ("seus indicadores vs o segmento" por plano): compara os
/// indicadores mensais do tenant autenticado com os pares que mantêm assinatura ativa no MESMO
/// plano. "Segmento" = grupo de pares do plano (não há campo de segmento persistido por
/// tenant). O próprio tenant sai da amostra; só estatísticas agregadas (mediana, média, posição
/// e tamanho) são expostas - nenhum valor individual de vizinho aparece. Com menos pares que a
/// amostra mínima, cada indicador permanece nulo e o resultado sinaliza amostra insuficiente.
/// Critério de período: mês calendário UTC (fronteira semiaberta [1º dia 00:00, 1º dia seguinte)).
/// Escopo multi-tenant: cliente_id (mesmo escopo canônico dos relatórios operacionais).
/// </summary>
public sealed record BenchmarkDefinicao(string Codigo, string Rotulo, string Unidade, string Definicao);

public sealed record BenchmarkIndicadorDto(
    string Codigo,
    string Rotulo,
    decimal? MeuValor,
    string Unidade,
    double? MedianaPlano,
    double? MediaPlano,
    int? PercentilMeu,
    int PeersNaAmostra,
    string Definicao);

public sealed record BenchmarkResultadoDto(
    string MesReferencia,
    string PlanoCodigo,
    string PlanoNome,
    int AmostraMinimaPeers,
    bool AmostraInsuficiente,
    bool TemDadosProprios,
    IReadOnlyList<BenchmarkIndicadorDto> Indicadores);

public sealed class RelatorioBenchmarkService
{
    /// <summary>Amostra mínima de pares para publicar estatísticas (regra de produto fixa;
    /// abaixo disso os números individuais de vizinhos ficariam expostos).</summary>
    public const int AmostraMinimaPeers = 3;

    private static readonly BenchmarkDefinicao[] Definicoes =
    {
        new("BENCH_PLANTOES_POR_MEDICO", "Plantões publicados por médico", "plantões/médico", "Plantões publicados no mês (situação diferente de rascunho/cancelado) divididos pelos médicos ativos da instituição."),
        new("BENCH_COBERTURA_VAGAS_PCT", "Cobertura média de vagas", "%", "Média, plantão a plantão do mês, do percentual de vagas preenchidas (vagas menos vagas disponíveis)."),
        new("BENCH_CANCELAMENTO_ESCALAS_PCT", "Cancelamento de escalas", "%", "Escalas canceladas sobre confirmadas+canceladas ligadas aos plantões iniciados no mês."),
        new("BENCH_TEMPO_MEDIO_PREENCHIMENTO_H", "Tempo médio de preenchimento", "horas", "Média do intervalo entre a criação do plantão e o registro da escala (somente escalas registradas depois do plantão)."),
        new("BENCH_VALOR_MEDIO_PLANTAO_PAGO", "Valor médio pago por plantão", "R$", "Média dos pagamentos pagos no mês, pela data de pagamento dentro do período.")
    };

    private readonly IConfiguration configuration;
    private readonly ICurrentUserService current;
    private readonly PermissionGuardService permission;

    public RelatorioBenchmarkService(IConfiguration configuration, ICurrentUserService current, PermissionGuardService permission)
    {
        this.configuration = configuration;
        this.current = current;
        this.permission = permission;
    }

    /// <summary>Porta HTTP: valida permissão (RELATORIOS_VER pelo catálogo de papéis) e contexto
    /// organizacional antes de delegar ao núcleo determinístico.</summary>
    public async Task<ApiResponse<BenchmarkResultadoDto>> ObterAsync(string? mesTexto, CancellationToken ct)
    {
        if (!TentarParsearMes(mesTexto, out var primeiroDia))
            return ApiResponse<BenchmarkResultadoDto>.Fail("Informe o mês no formato yyyy-MM.", 422);
        var access = await permission.ValidarPermissaoAsync(PermissionConstants.RelatoriosVer);
        if (!access.Success) return ApiResponse<BenchmarkResultadoDto>.Fail(access.Message, access.StatusCode);
        if (!current.TenantId.HasValue || !current.ClienteId.HasValue)
            return ApiResponse<BenchmarkResultadoDto>.Fail("Contexto organizacional válido é obrigatório.", 403);
        return await CalcularAsync(current.ClienteId.Value, primeiroDia, ct);
    }

    /// <summary>Núcleo determinístico (nível de serviço/banco): sem contexto HTTP, chamado pela
    /// porta HTTP e pelos testes de contrato com ids explícitos.</summary>
    internal async Task<ApiResponse<BenchmarkResultadoDto>> CalcularAsync(Guid clienteId, DateOnly primeiroDiaMes, CancellationToken ct)
    {
        var ini = primeiroDiaMes.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = primeiroDiaMes.AddMonths(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        await using var cn = new NpgsqlConnection(configuration.GetConnectionString("Default"));

        var plano = await cn.QueryFirstOrDefaultAsync<(Guid PlanoId, string Codigo, string Nome)>(new CommandDefinition($@"
select a.plano_id, p.codigo, p.nome
from plantaopro.assinaturas a join plantaopro.planos p on p.id=a.plano_id and p.reg_status='A'
where a.cliente_id=@cid and a.reg_status='A' and a.status in ('ATIVA','TRIAL')
order by (a.status<>'ATIVA') asc, a.data_inicio desc nulls last
limit 1", new { cid = clienteId }, cancellationToken: ct));
        if (plano.PlanoId == Guid.Empty)
            return ApiResponse<BenchmarkResultadoDto>.Fail("Assinatura ativa não encontrada para calcular o benchmark.", 404);

        var rows = (await cn.QueryAsync<RowBenchmark>(new CommandDefinition(Sql(), new { cid = clienteId, plano = plano.PlanoId, ini, fim }, cancellationToken: ct))).ToArray();
        var maxPeers = rows.Length == 0 ? 0 : rows.Max(r => (long)r.NPeers);
        var resultado = Definicoes.Select(d =>
        {
            var row = rows.FirstOrDefault(r => r.Kpi == d.Codigo);
            return new BenchmarkIndicadorDto(
                d.Codigo, d.Rotulo,
                row?.MeuValor.HasValue == true ? (decimal?)Math.Round(row.MeuValor.Value, 4, MidpointRounding.AwayFromZero) : null,
                d.Unidade,
                row?.Mediana,
                row?.Media,
                row?.Percentil.HasValue == true ? (int?)Math.Round(row.Percentil.Value, MidpointRounding.AwayFromZero) : null,
                row is null ? 0 : (int)row.NPeers,
                d.Definicao);
        }).ToList();

        return ApiResponse<BenchmarkResultadoDto>.Ok(new BenchmarkResultadoDto(
            primeiroDiaMes.ToString("yyyy-MM"),
            plano.Codigo, plano.Nome,
            AmostraMinimaPeers,
            maxPeers < AmostraMinimaPeers,
            resultado.Any(i => i.MeuValor.HasValue),
            resultado), "Benchmark comparado com os pares do mesmo plano.");
    }

    /// <summary>Função pura de entrada: null/vazio = mês atual UTC; "yyyy-MM" exato = primeiro dia
    /// do mês informado; qualquer outra forma = inválida.</summary>
    public static bool TentarParsearMes(string? texto, out DateOnly primeiroDia)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            primeiroDia = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            return true;
        }
        // Validacao manual e deterministica (estilo "yyyy-MM" exato): evita peculiaridades de
        // DateTimeStyles em formatos somente-data (ex.: AssumeUniversal falha no .NET atual).
        var t = texto.Trim();
        if (t.Length != 7 || t[4] != '-' || !ApenasDigitos(t, 0, 4) || !ApenasDigitos(t, 5, 2))
        {
            primeiroDia = default;
            return false;
        }
        int ano = int.Parse(t.AsSpan(0, 4), CultureInfo.InvariantCulture);
        int mes = int.Parse(t.AsSpan(5, 2), CultureInfo.InvariantCulture);
        if (mes < 1 || mes > 12)
        {
            primeiroDia = default;
            return false;
        }
        try
        {
            primeiroDia = new DateOnly(ano, mes, 1);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            primeiroDia = default;
            return false;
        }
    }

    private static bool ApenasDigitos(string s, int inicio, int comprimento)
    {
        for (int i = inicio; i < inicio + comprimento; i++)
        {
            if (s[i] < '0' || s[i] > '9')
            {
                return false;
            }
        }
        return true;
    }

    private static string Sql() => @"
with pares as (
  select distinct pa.cliente_id
  from plantaopro.assinaturas pa
  where pa.plano_id=@plano and pa.reg_status='A' and pa.status in ('ATIVA','TRIAL') and pa.cliente_id<>@cid
),
donos as (select @cid::uuid as id union select cliente_id from pares),
medicos_ativos as (
  select m.cliente_id dono, count(*)::int n
  from plantaopro.medicos m join donos d on d.id=m.cliente_id
  where m.reg_status='A' and upper(coalesce(m.status,'ATIVO'))='ATIVO'
  group by m.cliente_id
),
base_plantoes as (
  select p.cliente_id dono, p.id
  from plantaopro.plantoes p join donos d on d.id=p.cliente_id
  where p.reg_status='A' and upper(coalesce(p.status,'')) not in ('RASCUNHO','CANCELADO')
    and p.data_inicio>=@ini::timestamptz and p.data_inicio<@fim::timestamptz
),
plantoes_mes as (
  select bp.dono dono, count(*)::int publicados,
         avg(case when p.vagas>0 then (p.vagas-coalesce(p.vagas_disponiveis,0))*100.0/p.vagas end) cobertura_pct
  from base_plantoes bp join plantaopro.plantoes p on p.id=bp.id
  group by bp.dono
),
escalas_mes as (
  select bp.dono dono,
         count(*) filter (where lower(e.status) in ('confirmado','confirmada','realizado','realizada')) confirmadas,
         count(*) filter (where lower(e.status)='cancelado') canceladas
  from plantaopro.escalas e join base_plantoes bp on bp.id=e.plantao_id
  where e.reg_status='A'
  group by bp.dono
),
tempo_mes as (
  select bp.dono dono, avg(extract(epoch from (e.reg_date-p.reg_date))/3600.0) horas
  from plantaopro.escalas e
  join base_plantoes bp on bp.id=e.plantao_id
  join plantaopro.plantoes p on p.id=e.plantao_id
  where e.reg_status='A' and e.reg_date is not null and p.reg_date is not null and e.reg_date>=p.reg_date
  group by bp.dono
),
pagamentos_mes as (
  select g.cliente_id dono, avg(g.valor_pago) valor_medio
  from plantaopro.pagamentos g join donos d on d.id=g.cliente_id
  where g.reg_status='A' and lower(g.status)='pago'
    and g.data_pagamento>=@ini::date and g.data_pagamento<@fim::date
  group by g.cliente_id
),
valores as (
  select d.id dono,
         case when pm.publicados>0 and ma.n>0 then pm.publicados*1.0/ma.n end plantoes_por_medico,
         pm.cobertura_pct,
         case when es.confirmadas+es.canceladas>0 then es.canceladas*100.0/(es.confirmadas+es.canceladas) end cancelamento_pct,
         tm.horas,
         gm.valor_medio
  from donos d
  left join plantoes_mes pm on pm.dono=d.id
  left join medicos_ativos ma on ma.dono=d.id
  left join escalas_mes es on es.dono=d.id
  left join tempo_mes tm on tm.dono=d.id
  left join pagamentos_mes gm on gm.dono=d.id
),
long_form as (
  select 'BENCH_PLANTOES_POR_MEDICO' kpi, dono, plantoes_por_medico v from valores where plantoes_por_medico is not null
  union all select 'BENCH_COBERTURA_VAGAS_PCT', dono, cobertura_pct from valores where cobertura_pct is not null
  union all select 'BENCH_CANCELAMENTO_ESCALAS_PCT', dono, cancelamento_pct from valores where cancelamento_pct is not null
  union all select 'BENCH_TEMPO_MEDIO_PREENCHIMENTO_H', dono, horas from valores where horas is not null
  union all select 'BENCH_VALOR_MEDIO_PLANTAO_PAGO', dono, valor_medio from valores where valor_medio is not null
),
agregados as (
  select kpi,
         percentile_cont(0.5) within group (order by v) mediana,
         avg(v) media,
         count(*) filter (where dono<>@cid) n_peers
  from long_form group by kpi
),
percentil_meu as (
  select l.kpi, count(l2.v) filter (where l2.v<l.v)::double precision / nullif(count(l2.v)-1,0)*100.0 percentil
  from long_form l join long_form l2 on l2.kpi=l.kpi
  where l.dono=@cid
  group by l.kpi, l.v
)
select a.kpi Kpi, pf.v::float8 MeuValor, a.mediana::float8 Mediana, a.media::float8 Media, a.n_peers NPeers, pm.percentil::float8 Percentil
from agregados a
left join long_form pf on pf.kpi=a.kpi and pf.dono=@cid
left join percentil_meu pm on pm.kpi=a.kpi";

    private sealed record RowBenchmark(string Kpi, double? MeuValor, double? Mediana, double? Media, long NPeers, double? Percentil);
}
