using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using System.Text.RegularExpressions;

namespace PlantaoPro.Api;

public sealed class BiService
{
    private readonly IConfiguration cfg;
    private readonly ILogger<BiService> logger;

    public BiService(IConfiguration cfg, ILogger<BiService> logger)
    {
        this.cfg = cfg;
        this.logger = logger;
    }

    public async Task<ApiResponse<BiResumoExecutivoDto>> GetResumoExecutivoAsync(Guid? clienteId, string? mes = null, string? fuso = null)
    {
        var started = DateTime.UtcNow;
        try
        {
            var mesNorm=(mes??string.Empty).Trim();
            if(mesNorm.Length>0)
            {
                if(!Regex.IsMatch(mesNorm,@"^\d{4}-\d{2}$")||!DateOnly.TryParse($"{mesNorm}-01",out _))
                    return ApiResponse<BiResumoExecutivoDto>.Fail("Período deve estar no formato yyyy-MM.",400);
            }
            DateOnly? mesRef=mesNorm.Length>0?DateOnly.Parse($"{mesNorm}-01"):null;
            logger.LogInformation("BI resumo executivo iniciado (escopo={Escopo}, periodo={Periodo}, fuso={Fuso})", clienteId?.ToString() ?? "global", mesNorm.Length>0?mesNorm:"atual", fuso ?? "servidor");
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            var fusoNorm=(fuso??string.Empty).Trim();
            string fusoEfetivo;
            if(fusoNorm.Length>0)
            {
                var valido=await cn.ExecuteScalarAsync<bool>("select exists(select 1 from pg_timezone_names where name=@fuso)",new{fuso=fusoNorm});
                if(!valido)return ApiResponse<BiResumoExecutivoDto>.Fail("Fuso horário inválido.",400);
                fusoEfetivo=fusoNorm;
            }
            else fusoEfetivo=await cn.ExecuteScalarAsync<string>("select current_setting('TimeZone')")??UTC_TZ;
            // B9: KPIs de periodo usam a janela mes(fuso); demais KPIs sao estado atual/acumulado (documentado no contrato de homologacao).
            var row = await cn.QueryFirstAsync<BiResumoExecutivoDto>(@"
with periodo as (
  select
    date_trunc('month', coalesce(@mesRef::date, (now() at time zone @fuso)::date))::timestamp at time zone @fuso as inicio,
    date_trunc('month', coalesce(@mesRef::date, (now() at time zone @fuso)::date))::timestamp at time zone @fuso + interval '1 month' as fim
)
select
 (select count(1) from plantaopro.clientes c where c.reg_status='A' and upper(c.status)='ATIVO' and (@cid is null or c.id=@cid)) as TotalClientesAtivos,
 (select coalesce(sum(a.valor_contratado),0) from plantaopro.assinaturas a where a.reg_status='A' and lower(a.status)='ativa' and (@cid is null or a.cliente_id=@cid or a.tenant_id=@cid)) as ReceitaMensalEstimada,
 (select coalesce(sum(f.valor_total),0) from plantaopro.faturas_saas f where f.reg_status='A' and upper(f.status)='VENCIDA' and (@cid is null or f.cliente_id=@cid or f.tenant_id=@cid)) as ReceitaVencida,
 (select count(1) from plantaopro.plantoes p where p.reg_status='A' and lower(p.status) not in ('rascunho','cancelado') and (@cid is null or p.cliente_id=@cid or p.tenant_id=@cid) and p.data_inicio>=(select inicio from periodo) and p.data_inicio<(select fim from periodo)) as PlantoesPublicadosMes,
 (select coalesce(avg(case when p.vagas > 0 then ((p.vagas - p.vagas_disponiveis)::decimal / p.vagas::decimal) * 100 else 0 end),0) from plantaopro.plantoes p where p.reg_status='A' and lower(p.status) not in ('rascunho','cancelado') and (@cid is null or p.cliente_id=@cid or p.tenant_id=@cid) and p.data_inicio>=(select inicio from periodo) and p.data_inicio<(select fim from periodo)) as PercentualCobertura,
 (select count(1) from plantaopro.escalas e where e.reg_status='A' and lower(e.status) in ('confirmado','confirmada') and (@cid is null or e.cliente_id=@cid or e.tenant_id=@cid)) as EscalasConfirmadas,
 (select count(1) from plantaopro.escalas e where e.reg_status='A' and lower(e.status)='cancelado' and (@cid is null or e.cliente_id=@cid or e.tenant_id=@cid)) as EscalasCanceladas,
 (select count(1) from plantaopro.pagamentos p where p.reg_status='A' and lower(p.status) in ('pendente','atrasado') and (@cid is null or p.cliente_id=@cid or p.tenant_id=@cid)) as PagamentosPendentes,
 (select count(1) from plantaopro.pagamentos p where p.reg_status='A' and lower(p.status)='pago' and (@cid is null or p.cliente_id=@cid or p.tenant_id=@cid)) as PagamentosConfirmados,
 (select coalesce(avg(extract(epoch from (e.reg_date - p.reg_date))/3600.0),0) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and p.reg_status='A' and (@cid is null or p.cliente_id=@cid or p.tenant_id=@cid)) as TempoMedioPreenchimentoHoras,
 (select count(1) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and p.reg_status='A' and lower(e.status) in ('confirmado','confirmada') and (@cid is null or p.cliente_id=@cid or p.tenant_id=@cid) and p.data_inicio>=(select inicio from periodo) and p.data_inicio<(select fim from periodo)) as EscalasConfirmadasPeriodo,
 (select coalesce(sum(p.valor_pago),0) from plantaopro.pagamentos p where p.reg_status='A' and lower(p.status)='pago' and p.data_pagamento is not null and (p.data_pagamento::timestamp at time zone @fuso)>=(select inicio from periodo) and (p.data_pagamento::timestamp at time zone @fuso)<(select fim from periodo) and (@cid is null or p.cliente_id=@cid or p.tenant_id=@cid)) as ValorPagoPeriodo,
 to_char(coalesce(@mesRef::date,(now() at time zone @fuso)::date),'YYYY-MM') as Periodo,
 @fuso::text as Fuso", new { cid=clienteId, mesRef=mesRef?.ToString("yyyy-MM-dd"), fuso=fusoEfetivo });
            row.Definicoes = Definicoes();
            row.TimeCriterion = $"Janela do mês {row.Periodo} no fuso {row.Fuso}; KPIs de período usam essa janela, demais em estado atual (B9).";
            logger.LogInformation("BI resumo executivo concluído em {Elapsed}ms", (DateTime.UtcNow - started).TotalMilliseconds);
            return ApiResponse<BiResumoExecutivoDto>.Ok(row);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no BI resumo executivo");
            return ApiResponse<BiResumoExecutivoDto>.Fail("Não foi possível carregar o resumo executivo.", 500);
        }
    }

    private static IReadOnlyList<KpiDefinicao> Definicoes() => new[]
    {
        new KpiDefinicao("TotalClientesAtivos", "Clientes ativos", "Contagem de clientes com reg_status='A' e status ATIVO no escopo (estado atual)."),
        new KpiDefinicao("ReceitaMensalEstimada", "Receita mensal estimada", "Soma de valor_contratado de assinaturas ativas (status 'ativa') no escopo (estado atual)."),
        new KpiDefinicao("ReceitaVencida", "Receita vencida", "Soma de valor_total de faturas SaaS vencidas (status 'VENCIDA') no escopo (estado atual)."),
        new KpiDefinicao("PlantoesPublicadosMes", "Plantões publicados no mês", "Plantões ativos não rascunho/cancelado cujo data_inicio cai na janela do mês consultado no fuso informado (período)."),
        new KpiDefinicao("PercentualCobertura", "% de cobertura", "Média percentual de vagas ocupadas ((vagas - vagas_disponiveis)/vagas*100; 0 sem vagas) dos plantões ativos na janela do mês (período)."),
        new KpiDefinicao("EscalasConfirmadas", "Escalas confirmadas", "Contagem de escalas com status confirmado/confirmada no escopo (estado atual)."),
        new KpiDefinicao("EscalasCanceladas", "Escalas canceladas", "Contagem de escalas canceladas no escopo (estado atual)."),
        new KpiDefinicao("PagamentosPendentes", "Pagamentos pendentes", "Contagem de pagamentos pendente/atrasado no escopo (estado atual)."),
        new KpiDefinicao("PagamentosConfirmados", "Pagamentos confirmados", "Contagem de pagamentos pagos no escopo (estado atual)."),
        new KpiDefinicao("TempoMedioPreenchimentoHoras", "Tempo médio de preenchimento (h)", "Média em horas entre a criação do plantão e da escala ligada (reg_date da escala - reg_date do plantao), escalas ativas no escopo (estado atual)."),
        new KpiDefinicao("EscalasConfirmadasPeriodo", "Escalas confirmadas no período", "Escalas confirmadas cujo plantão inicia dentro da janela do mês consultado no fuso (período)."),
        new KpiDefinicao("ValorPagoPeriodo", "Valor pago no período", "Soma de valor_pago de pagamentos pagos com data_pagamento dentro da janela do mês no fuso consultado (período).")
    };

    private const string UTC_TZ = "UTC";
}
