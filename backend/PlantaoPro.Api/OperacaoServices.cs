using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class OperacaoService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    private readonly UsuarioContextService usuarioContext;
    private readonly ILogger<OperacaoService> logger;

    public OperacaoService(IConfiguration cfg, IAuditService audit, UsuarioContextService usuarioContext, ILogger<OperacaoService> logger)
    {
        this.cfg = cfg;
        this.audit = audit;
        this.usuarioContext = usuarioContext;
        this.logger = logger;
    }

    public async Task<ApiResponse<OperacaoResumoDto>> GetResumoAsync(Guid userId, string? ip, string? ua)
    {
        try
        {
            var clienteId = usuarioContext.GetClienteId();
            var isAdminGlobal = usuarioContext.IsAdminGlobal();
            if (!isAdminGlobal && !clienteId.HasValue)
            {
                await audit.RegistrarAsync(userId, null, AuditoriaConstants.Entidades.Operacao, null, AuditoriaConstants.Acoes.AcessoNegado, new { motivo = "cliente_nao_identificado" }, false, ip, "sem-cliente");
                return ApiResponse<OperacaoResumoDto>.Fail("Cliente do usuário não identificado para consultar a central operacional.", 403);
            }

            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));

            var tenantParams = new { userId, clienteId, isAdminGlobal };
            var resumo = await cn.QueryFirstOrDefaultAsync<OperacaoResumoContadoresRow>(@"
select
  (select count(1) from plantaopro.plantoes p where p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and date_trunc('day', p.data_inicio)=date_trunc('day', now())) as TotalPlantoesHoje,
  (select count(1) from plantaopro.plantoes p where p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and lower(coalesce(p.status,''))='aberto') as TotalPlantoesAbertos,
  (select count(1) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and lower(coalesce(e.status,'')) in ('solicitada','solicitado')) as TotalEscalasSolicitadas,
  (select count(1) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.reg_status='A' and p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and lower(coalesce(e.status,'')) in ('confirmada','confirmado') and date_trunc('day', p.data_inicio)=date_trunc('day', now())) as TotalEscalasConfirmadasHoje,
  (select count(1)
    from plantaopro.escalas e1
    join plantaopro.plantoes p1 on p1.id = e1.plantao_id
    join plantaopro.escalas e2 on e1.medico_id=e2.medico_id and e1.id<>e2.id and e1.reg_status='A' and e2.reg_status='A'
    join plantaopro.plantoes p2 on p2.id=e2.plantao_id
   where lower(coalesce(e1.status,'')) in ('solicitada','solicitado','confirmada','confirmado') and lower(coalesce(e2.status,'')) in ('solicitada','solicitado','confirmada','confirmado')
     and (@isAdminGlobal or p1.cliente_id=@clienteId)
     and (@isAdminGlobal or p2.cliente_id=@clienteId)
     and p1.data_inicio < p2.data_fim and p2.data_inicio < p1.data_fim) as TotalConflitos,
  (select count(1) from plantaopro.pagamentos pg join plantaopro.plantoes p on p.id=pg.plantao_id where pg.reg_status='A' and p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and lower(coalesce(pg.status,'')) in ('pendente','atrasado')) as TotalPagamentosPendentes,
  (select coalesce(sum(pg.valor_previsto),0) from plantaopro.pagamentos pg join plantaopro.plantoes p on p.id=pg.plantao_id where pg.reg_status='A' and p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and lower(coalesce(pg.status,'')) in ('pendente','atrasado')) as ValorPendente,
  (select count(1) from plantaopro.notificacoes n where n.reg_status='A' and (@isAdminGlobal or n.cliente_id=@clienteId or n.cliente_id is null) and coalesce(n.lida,false)=false and (n.usuario_id=@userId or n.usuario_id is null)) as NotificacoesNaoLidas", tenantParams) ?? new OperacaoResumoContadoresRow();

            var plantoesCriticos = await cn.QueryAsync<OperacaoPlantaoCriticoDto>(@"
select p.id, h.nome_fantasia as HospitalNome, e.nome as EspecialidadeNome, p.data_inicio as DataInicio, p.data_fim as DataFim, p.vagas_disponiveis as VagasDisponiveis, p.status
from plantaopro.plantoes p
join plantaopro.hospitais h on h.id = p.hospital_id
join plantaopro.especialidades e on e.id = p.especialidade_id
where p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and lower(coalesce(p.status,'')) in ('aberto','rascunho') and p.vagas_disponiveis > 0
order by p.data_inicio asc
limit 8", tenantParams);

            var escalasPendentes = await cn.QueryAsync<OperacaoEscalaPendenteDto>(@"
select es.id, m.nome as MedicoNome, h.nome_fantasia as HospitalNome, p.data_inicio as DataInicio, p.data_fim as DataFim, es.status
from plantaopro.escalas es
join plantaopro.medicos m on m.id=es.medico_id
join plantaopro.plantoes p on p.id=es.plantao_id
join plantaopro.hospitais h on h.id=p.hospital_id
where es.reg_status='A' and p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and lower(coalesce(es.status,'')) in ('solicitada','solicitado')
order by es.reg_date asc
limit 8", tenantParams);

            var pagamentosPendentes = await cn.QueryAsync<OperacaoPagamentoPendenteDto>(@"
select pg.id, m.nome as MedicoNome, h.nome_fantasia as HospitalNome, pg.valor_previsto as ValorPrevisto, pg.status, pg.data_prevista as DataPrevista
from plantaopro.pagamentos pg
join plantaopro.escalas es on es.id=pg.escala_id
join plantaopro.medicos m on m.id=es.medico_id
join plantaopro.plantoes p on p.id=es.plantao_id
join plantaopro.hospitais h on h.id=p.hospital_id
where pg.reg_status='A' and p.reg_status='A' and (@isAdminGlobal or p.cliente_id=@clienteId) and lower(coalesce(pg.status,'')) in ('pendente','atrasado')
order by pg.data_prevista asc nulls last
limit 8", tenantParams);

            var payload = new OperacaoResumoDto
            {
                TotalPlantoesHoje = resumo.TotalPlantoesHoje,
                TotalPlantoesAbertos = resumo.TotalPlantoesAbertos,
                TotalEscalasSolicitadas = resumo.TotalEscalasSolicitadas,
                TotalEscalasConfirmadasHoje = resumo.TotalEscalasConfirmadasHoje,
                TotalConflitos = resumo.TotalConflitos,
                TotalPagamentosPendentes = resumo.TotalPagamentosPendentes,
                ValorPendente = resumo.ValorPendente,
                NotificacoesNaoLidas = resumo.NotificacoesNaoLidas,
                PlantoesCriticos = plantoesCriticos?.ToArray() ?? Array.Empty<OperacaoPlantaoCriticoDto>(),
                EscalasPendentes = escalasPendentes?.ToArray() ?? Array.Empty<OperacaoEscalaPendenteDto>(),
                PagamentosPendentes = pagamentosPendentes?.ToArray() ?? Array.Empty<OperacaoPagamentoPendenteDto>()
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

public sealed class OperacaoResumoContadoresRow
{
    public long TotalPlantoesHoje { get; set; }
    public long TotalPlantoesAbertos { get; set; }
    public long TotalEscalasSolicitadas { get; set; }
    public long TotalEscalasConfirmadasHoje { get; set; }
    public long TotalConflitos { get; set; }
    public long TotalPagamentosPendentes { get; set; }
    public decimal ValorPendente { get; set; }
    public long NotificacoesNaoLidas { get; set; }
}
