using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class OperacaoService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    private readonly ILogger<OperacaoService> logger;

    public OperacaoService(IConfiguration cfg, IAuditService audit, ILogger<OperacaoService> logger)
    {
        this.cfg = cfg;
        this.audit = audit;
        this.logger = logger;
    }

    public async Task<ApiResponse<OperacaoResumoDto>> GetResumoAsync(Guid userId, string? ip, string? ua)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));

            var resumo = await cn.QueryFirstAsync<OperacaoResumoDto>(@"
select
  (select count(1) from plantaopro.plantoes p where p.reg_status='A' and date_trunc('day', p.data_inicio)=date_trunc('day', now())) as TotalPlantoesHoje,
  (select count(1) from plantaopro.plantoes p where p.reg_status='A' and lower(coalesce(p.status,''))='aberto') as TotalPlantoesAbertos,
  (select count(1) from plantaopro.escalas e where e.reg_status='A' and lower(coalesce(e.status,''))='solicitado') as TotalEscalasSolicitadas,
  (select count(1) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and lower(coalesce(e.status,''))='confirmado' and date_trunc('day', p.data_inicio)=date_trunc('day', now())) as TotalEscalasConfirmadasHoje,
  (select count(1)
    from plantaopro.escalas e1
    join plantaopro.plantoes p1 on p1.id = e1.plantao_id
    join plantaopro.escalas e2 on e1.medico_id=e2.medico_id and e1.id<>e2.id and e1.reg_status='A' and e2.reg_status='A'
    join plantaopro.plantoes p2 on p2.id=e2.plantao_id
   where e1.status in ('solicitado','confirmado') and e2.status in ('solicitado','confirmado')
     and p1.data_inicio < p2.data_fim and p2.data_inicio < p1.data_fim) as TotalConflitos,
  (select count(1) from plantaopro.pagamentos pg where pg.reg_status='A' and lower(coalesce(pg.status,'')) in ('pendente','atrasado')) as TotalPagamentosPendentes,
  (select coalesce(sum(pg.valor_previsto),0) from plantaopro.pagamentos pg where pg.reg_status='A' and lower(coalesce(pg.status,'')) in ('pendente','atrasado')) as ValorPendente,
  (select count(1) from plantaopro.notificacoes n where n.reg_status='A' and coalesce(n.lida,false)=false and (n.usuario_id=@userId or n.usuario_id is null)) as NotificacoesNaoLidas", new { userId });

            var plantoesCriticos = await cn.QueryAsync<OperacaoPlantaoCriticoDto>(@"
select p.id, h.nome_fantasia as HospitalNome, e.nome as EspecialidadeNome, p.data_inicio as DataInicio, p.data_fim as DataFim, p.vagas_disponiveis as VagasDisponiveis, p.status
from plantaopro.plantoes p
join plantaopro.hospitais h on h.id = p.hospital_id
join plantaopro.especialidades e on e.id = p.especialidade_id
where p.reg_status='A' and lower(coalesce(p.status,'')) in ('aberto','rascunho') and p.vagas_disponiveis > 0
order by p.data_inicio asc
limit 8");

            var escalasPendentes = await cn.QueryAsync<OperacaoEscalaPendenteDto>(@"
select es.id, m.nome as MedicoNome, h.nome_fantasia as HospitalNome, p.data_inicio as DataInicio, p.data_fim as DataFim, es.status
from plantaopro.escalas es
join plantaopro.medicos m on m.id=es.medico_id
join plantaopro.plantoes p on p.id=es.plantao_id
join plantaopro.hospitais h on h.id=p.hospital_id
where es.reg_status='A' and lower(coalesce(es.status,''))='solicitado'
order by es.reg_date asc
limit 8");

            var pagamentosPendentes = await cn.QueryAsync<OperacaoPagamentoPendenteDto>(@"
select pg.id, m.nome as MedicoNome, h.nome_fantasia as HospitalNome, pg.valor_previsto as ValorPrevisto, pg.status, pg.data_prevista as DataPrevista
from plantaopro.pagamentos pg
join plantaopro.escalas es on es.id=pg.escala_id
join plantaopro.medicos m on m.id=es.medico_id
join plantaopro.plantoes p on p.id=es.plantao_id
join plantaopro.hospitais h on h.id=p.hospital_id
where pg.reg_status='A' and lower(coalesce(pg.status,'')) in ('pendente','atrasado')
order by pg.data_prevista asc nulls last
limit 8");

            var payload = resumo with
            {
                PlantoesCriticos = plantoesCriticos,
                EscalasPendentes = escalasPendentes,
                PagamentosPendentes = pagamentosPendentes
            };

            await audit.LogAsync(userId, "OPERACAO_RESUMO", "operacao", null, "Consulta da central operacional", ip: ip, userAgent: ua);
            return ApiResponse<OperacaoResumoDto>.Ok(payload);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao consultar resumo operacional para usuário {UserId}", userId);
            return ApiResponse<OperacaoResumoDto>.Fail("Não foi possível carregar a central operacional no momento.", 500);
        }
    }
}
