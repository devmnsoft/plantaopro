using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class ProfessionalPortalService
{
    private readonly IConfiguration configuration;
    private readonly IAuditService audit;
    private readonly ILogger<ProfessionalPortalService> logger;

    public ProfessionalPortalService(IConfiguration configuration, IAuditService audit, ILogger<ProfessionalPortalService> logger)
    {
        this.configuration = configuration;
        this.audit = audit;
        this.logger = logger;
    }

    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));
    private static Task<ProfessionalContext?> ContextAsync(NpgsqlConnection cn, Guid uid) => cn.QueryFirstOrDefaultAsync<ProfessionalContext>(@"select m.id as ""MedicoId"",m.cliente_id as ""ClienteId"",coalesce(m.tenant_id,m.cliente_id) as ""TenantId""
from plantaopro.medicos m where m.usuario_id=@uid and m.reg_status='A' limit 1", new { uid });

    public async Task<ApiResponse<ProfessionalDashboardDto>> DashboardAsync(Guid uid)
    {
        try
        {
            await using var cn = Connection();
            var context = await ContextAsync(cn, uid);
            if (context is null) return ApiResponse<ProfessionalDashboardDto>.Fail("Profissional não vinculado ao usuário autenticado.", 404);
            var resumo = await cn.QueryFirstAsync<MedicoAreaResumoDto>(@"select coalesce(m.nome,'') as ""MedicoNome"",coalesce(m.crm,'') as ""Crm"",coalesce(m.uf_crm,'') as ""UfCrm"",
(select count(*) from plantaopro.plantoes p where p.cliente_id=@clienteId and p.reg_status='A' and lower(p.status)='aberto' and p.vagas_disponiveis>0) as ""PlantoesDisponiveis"",
(select count(*) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.medico_id=m.id and p.cliente_id=@clienteId and e.reg_status='A' and lower(e.status)='solicitado') as ""SolicitacoesPendentes"",
(select count(*) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.medico_id=m.id and p.cliente_id=@clienteId and e.reg_status='A' and lower(e.status)='confirmado') as ""EscalasConfirmadas"",
(select count(*) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.medico_id=m.id and p.cliente_id=@clienteId and e.reg_status='A' and lower(e.status)='realizado') as ""PlantoesRealizados"",
(select count(*) from plantaopro.pagamentos pg join plantaopro.plantoes p on p.id=pg.plantao_id where pg.medico_id=m.id and p.cliente_id=@clienteId and pg.reg_status='A' and lower(pg.status)='pendente') as ""PagamentosPendentes"",
(select coalesce(sum(pg.valor_previsto),0) from plantaopro.pagamentos pg join plantaopro.plantoes p on p.id=pg.plantao_id where pg.medico_id=m.id and p.cliente_id=@clienteId and pg.reg_status='A' and lower(pg.status)='pendente') as ""ValorPendente"",
(select count(*) from plantaopro.notificacoes n where n.usuario_id=@uid and n.reg_status='A' and not coalesce(n.lida,false)) as ""NotificacoesNaoLidas""
from plantaopro.medicos m where m.id=@medicoId", new { uid, context.MedicoId, context.ClienteId });
            var proximos = await cn.QueryAsync<MedicoEscalaDto>(EscalasSql + " and p.data_fim>=now() order by p.data_inicio limit 5", context);
            var convites = await cn.QueryAsync<PlantaoConviteDto>(@"select c.id as ""Id"",c.plantao_id as ""PlantaoId"",c.medico_id as ""MedicoId"",'' as ""MedicoNome"",c.status as ""Status"",coalesce(c.mensagem,'') as ""Mensagem"",c.data_envio as ""DataEnvio"",c.data_resposta as ""DataResposta"",coalesce(c.motivo_recusa,'') as ""MotivoRecusa"" from plantaopro.plantao_convites c join plantaopro.plantoes p on p.id=c.plantao_id where c.medico_id=@MedicoId and p.cliente_id=@ClienteId and c.reg_status='A' and upper(c.status) in ('ENVIADO','PENDENTE') order by c.data_envio desc limit 5", context);
            var notifications = await cn.QueryAsync<NotificacaoDto>("select id,titulo,mensagem,tipo,lida,reg_date as \"RegDate\" from plantaopro.notificacoes where usuario_id=@uid and reg_status='A' order by reg_date desc limit 5", new { uid });
            var finance = await cn.QueryFirstAsync<(decimal Previsto, decimal Aprovado, decimal Pago)>(@"select coalesce(sum(pg.valor_previsto),0),coalesce(sum(pg.valor_previsto) filter(where upper(pg.status) in ('APROVADO','PAGO')),0),coalesce(sum(pg.valor_pago) filter(where upper(pg.status)='PAGO'),0) from plantaopro.pagamentos pg join plantaopro.plantoes p on p.id=pg.plantao_id where pg.medico_id=@MedicoId and p.cliente_id=@ClienteId and pg.reg_status='A'", context);
            return ApiResponse<ProfessionalDashboardDto>.Ok(new(resumo, proximos, convites, notifications, finance.Previsto, finance.Aprovado, finance.Pago, 0, 0));
        }
        catch (Exception ex) { logger.LogError(ex, "Falha ao carregar Meu Dia profissional uid:{Uid}", uid); throw; }
    }

    public async Task<ApiResponse<IEnumerable<ProfessionalCheckInDto>>> CheckInsAsync(Guid uid)
    {
        await using var cn = Connection(); var context = await ContextAsync(cn, uid);
        if (context is null) return ApiResponse<IEnumerable<ProfessionalCheckInDto>>.Fail("Profissional não encontrado.", 404);
        var rows = await cn.QueryAsync<ProfessionalCheckInDto>(@"select e.id as ""EscalaId"",coalesce(h.nome_fantasia,'') as ""HospitalNome"",coalesce(s.nome,'') as ""EspecialidadeNome"",p.data_inicio as ""DataInicio"",p.data_fim as ""DataFim"",c.checkin_em as ""CheckInEm"",c.checkout_em as ""CheckOutEm"",(c.id is null and now()>=p.data_inicio-interval '2 hours') as ""PodeCheckIn"",(c.id is not null and c.checkout_em is null) as ""PodeCheckOut"" from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades s on s.id=p.especialidade_id left join medico_checkins c on c.escala_id=e.id and c.tenant_id=@TenantId where e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A' and lower(e.status) in ('confirmado','realizado') and p.data_fim>=now()-interval '24 hours' order by p.data_inicio", context);
        return ApiResponse<IEnumerable<ProfessionalCheckInDto>>.Ok(rows);
    }

    public async Task<ApiResponse<object>> RegisterPresenceAsync(Guid uid, Guid escalaId, bool checkout, string ip, string profile)
    {
        try
        {
            await using var cn = Connection(); var context = await ContextAsync(cn, uid);
            if (context is null || context.TenantId is null) return ApiResponse<object>.Fail("Contexto profissional inválido.", 403);
            var owns = await cn.ExecuteScalarAsync<bool>(@"select exists(select 1 from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.id=@escalaId and e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A')", new { escalaId, context.MedicoId, context.ClienteId });
            if (!owns) return ApiResponse<object>.Fail("Plantão não pertence ao profissional autenticado.", 403);
            int changed;
            if (checkout) changed = await cn.ExecuteAsync("update medico_checkins set checkout_em=now(),atualizado_em=now() where tenant_id=@TenantId and escala_id=@escalaId and medico_id=@MedicoId and checkout_em is null", new { context.TenantId, escalaId, context.MedicoId });
            else changed = await cn.ExecuteAsync("insert into medico_checkins(tenant_id,medico_id,escala_id,origem) values(@TenantId,@MedicoId,@escalaId,'WEB') on conflict(tenant_id,escala_id) do nothing", new { context.TenantId, context.MedicoId, escalaId });
            if (changed == 0) return ApiResponse<object>.Fail(checkout ? "Check-out exige check-in e não pode ser repetido." : "Check-in já registrado.", 409);
            await audit.RegistrarAsync(uid, context.ClienteId, "ESCALA", escalaId, checkout ? "CHECK_OUT" : "CHECK_IN", new { escalaId }, true, ip, profile);
            return ApiResponse<object>.Ok(new { escalaId }, checkout ? "Check-out registrado." : "Check-in registrado.");
        }
        catch (Exception ex) { logger.LogError(ex, "Falha no registro de presença uid:{Uid} escala:{EscalaId}", uid, escalaId); throw; }
    }

    private const string EscalasSql = @"select e.id as ""EscalaId"",e.plantao_id as ""PlantaoId"",coalesce(h.nome_fantasia,'') as ""HospitalNome"",coalesce(s.nome,'') as ""EspecialidadeNome"",p.data_inicio as ""DataInicio"",p.data_fim as ""DataFim"",p.valor as ""Valor"",e.status as ""Status"",e.justificativa as ""Justificativa"" from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades s on s.id=p.especialidade_id where e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A'";
    private sealed record ProfessionalContext(Guid MedicoId, Guid? ClienteId, Guid? TenantId);
}
