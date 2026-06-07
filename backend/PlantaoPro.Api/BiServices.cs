using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

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

    public async Task<ApiResponse<BiResumoExecutivoDto>> GetResumoExecutivoAsync()
    {
        var started = DateTime.UtcNow;
        try
        {
            logger.LogInformation("BI resumo executivo iniciado");
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var row = await cn.QueryFirstAsync<BiResumoExecutivoDto>(@"
select
 (select count(1) from plantaopro.clientes c where c.reg_status='A' and c.status='ativo') as TotalClientesAtivos,
 (select coalesce(sum(a.valor_contratado),0) from plantaopro.assinaturas a where a.reg_status='A' and a.status='ativa') as ReceitaMensalEstimada,
 (select coalesce(sum(f.valor_total),0) from plantaopro.faturas_saas f where f.reg_status='A' and f.status='VENCIDA') as ReceitaVencida,
 (select count(1) from plantaopro.plantoes p where p.reg_status='A' and date_trunc('month', p.data_inicio)=date_trunc('month', now())) as PlantoesPublicadosMes,
 (select coalesce(avg(case when p.vagas > 0 then ((p.vagas - p.vagas_disponiveis)::decimal / p.vagas::decimal) * 100 else 0 end),0) from plantaopro.plantoes p where p.reg_status='A' and date_trunc('month', p.data_inicio)=date_trunc('month', now())) as PercentualCobertura,
 (select count(1) from plantaopro.escalas e where e.reg_status='A' and e.status='confirmado') as EscalasConfirmadas,
 (select count(1) from plantaopro.escalas e where e.reg_status='A' and e.status='cancelado') as EscalasCanceladas,
 (select count(1) from plantaopro.pagamentos p where p.reg_status='A' and p.status in ('pendente','atrasado')) as PagamentosPendentes,
 (select count(1) from plantaopro.pagamentos p where p.reg_status='A' and p.status='pago') as PagamentosConfirmados,
 (select coalesce(avg(extract(epoch from (e.reg_date - p.reg_date))/3600.0),0) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A') as TempoMedioPreenchimentoHoras;");
            logger.LogInformation("BI resumo executivo concluído em {Elapsed}ms", (DateTime.UtcNow - started).TotalMilliseconds);
            return ApiResponse<BiResumoExecutivoDto>.Ok(row);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no BI resumo executivo");
            return ApiResponse<BiResumoExecutivoDto>.Fail("Não foi possível carregar o resumo executivo.", 500);
        }
    }
}
