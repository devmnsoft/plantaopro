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
    private readonly EscalaService escalaService;
    private readonly UsuarioContextService usuarioContext;

    public ProfessionalPortalService(IConfiguration configuration, IAuditService audit, ILogger<ProfessionalPortalService> logger, EscalaService escalaService, UsuarioContextService usuarioContext)
    {
        this.configuration = configuration;
        this.audit = audit;
        this.logger = logger;
        this.escalaService = escalaService;
        this.usuarioContext = usuarioContext;
    }

    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));
    private Task<ProfessionalContext?> ContextAsync(NpgsqlConnection cn, Guid uid)
    {
        var selectedClientId = usuarioContext.GetClienteId();
        if (!selectedClientId.HasValue) return Task.FromResult<ProfessionalContext?>(null);
        return cn.QueryFirstOrDefaultAsync<ProfessionalContext>(@"select m.id as ""MedicoId"",m.cliente_id as ""ClienteId"",coalesce(m.tenant_id,m.cliente_id) as ""TenantId""
from plantaopro.medicos m where m.usuario_id=@uid and m.cliente_id=@selectedClientId and m.reg_status='A' limit 1", new { uid, selectedClientId });
    }

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
        var rows = await cn.QueryAsync<ProfessionalCheckInDto>(@"select e.id as ""EscalaId"",coalesce(h.nome_fantasia,'') as ""HospitalNome"",coalesce(s.nome,'') as ""EspecialidadeNome"",p.data_inicio as ""DataInicio"",p.data_fim as ""DataFim"",c.checkin_em as ""CheckInEm"",c.checkout_em as ""CheckOutEm"",c.checkin_recebido_em as ""CheckInRecebidoEm"",c.checkout_recebido_em as ""CheckOutRecebidoEm"",x.id as ""CorrecaoId"",x.versao as ""VersaoCorrecao"",x.status as ""StatusCorrecao"",x.inicio_proposto_em as ""InicioPropostoEm"",x.fim_proposto_em as ""FimPropostoEm"",c.inicio_aprovado_em as ""InicioAprovadoEm"",c.fim_aprovado_em as ""FimAprovadoEm"",coalesce(c.status_conferencia,'CONFIRMADO') as ""StatusConferencia"",c.timezone_contexto as ""TimezoneContexto"",coalesce(c.versao,0) as ""Versao"",(c.id is null and now()>=p.data_inicio-interval '2 hours') as ""PodeCheckIn"",(c.id is not null and c.checkout_em is null) as ""PodeCheckOut"" from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades s on s.id=p.especialidade_id left join medico_checkins c on c.escala_id=e.id and c.tenant_id=@TenantId left join lateral(select id,versao,status,inicio_proposto_em,fim_proposto_em from medico_presenca_correcoes where tenant_id=@TenantId and presenca_id=c.id and status='PENDENTE' order by solicitado_em desc limit 1)x on true where e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A' and lower(e.status) in ('confirmado','realizado') and p.data_fim>=now()-interval '365 days' order by p.data_inicio desc", context);
        return ApiResponse<IEnumerable<ProfessionalCheckInDto>>.Ok(rows);
    }

    public async Task<ApiResponse<IEnumerable<ProfessionalShiftDto>>> AgendaAsync(Guid uid, DateOnly start, DateOnly end, string? unit, string? status)
    {
        if (start == default || end == default || end < start || end.DayNumber - start.DayNumber > 366)
            return ApiResponse<IEnumerable<ProfessionalShiftDto>>.Fail("Informe um período válido de até 366 dias.", 422);
        await using var cn = Connection();
        var context = await ContextAsync(cn, uid);
        if (context is null || !context.ClienteId.HasValue) return ApiResponse<IEnumerable<ProfessionalShiftDto>>.Fail("Vínculo profissional inválido.", 403);
        var rows = await cn.QueryAsync<ProfessionalShiftDto>(@"select e.id as ""EscalaId"",e.plantao_id as ""PlantaoId"",coalesce(h.nome_fantasia,'') as ""HospitalNome"",coalesce(s.nome,'') as ""EspecialidadeNome"",p.data_inicio as ""DataInicio"",p.data_fim as ""DataFim"",coalesce(p.valor,0) as ""Valor"",e.status as ""Status"",e.justificativa as ""Justificativa"",(select he.reg_date from plantaopro.historico_escala he where he.escala_id=e.id and lower(he.status_novo)='confirmado' order by he.reg_date desc limit 1) as ""ConfirmadoEm"",(lower(e.status)='solicitado' and p.data_inicio>now() and p.reg_status='A') as ""PodeConfirmar""
from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades s on s.id=p.especialidade_id
where e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A' and (p.data_inicio is null or p.data_inicio < @endAt) and (p.data_fim is null or p.data_fim >= @startAt) and (@unit is null or h.nome_fantasia ilike '%'||@unit||'%') and (@status is null or lower(e.status)=lower(@status)) order by p.data_inicio nulls last", new { context.MedicoId, context.ClienteId, startAt=start.ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc), endAt=end.AddDays(1).ToDateTime(TimeOnly.MinValue,DateTimeKind.Utc), unit=string.IsNullOrWhiteSpace(unit)?null:unit.Trim(), status=string.IsNullOrWhiteSpace(status)?null:status.Trim() });
        return ApiResponse<IEnumerable<ProfessionalShiftDto>>.Ok(rows);
    }

    public async Task<ApiResponse<ProfessionalShiftDetailDto>> ShiftDetailAsync(Guid uid, Guid shiftId)
    {
        await using var cn=Connection(); var context=await ContextAsync(cn,uid);
        if(context is null||!context.ClienteId.HasValue)return ApiResponse<ProfessionalShiftDetailDto>.Fail("Vínculo profissional inválido.",403);
        var shift=await cn.QueryFirstOrDefaultAsync<ProfessionalShiftDto>(@"select e.id as ""EscalaId"",e.plantao_id as ""PlantaoId"",coalesce(h.nome_fantasia,'') as ""HospitalNome"",coalesce(s.nome,'') as ""EspecialidadeNome"",p.data_inicio as ""DataInicio"",p.data_fim as ""DataFim"",coalesce(p.valor,0) as ""Valor"",e.status as ""Status"",e.justificativa as ""Justificativa"",(select he.reg_date from plantaopro.historico_escala he where he.escala_id=e.id and lower(he.status_novo)='confirmado' order by he.reg_date desc limit 1) as ""ConfirmadoEm"",(lower(e.status)='solicitado' and p.data_inicio>now() and p.reg_status='A') as ""PodeConfirmar"" from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades s on s.id=p.especialidade_id where e.id=@shiftId and e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A'",new{shiftId,context.MedicoId,context.ClienteId});
        if(shift is null)return ApiResponse<ProfessionalShiftDetailDto>.Fail("Plantão não encontrado no contexto profissional atual.",404);
        var execution=(await CheckInsAsync(uid)).Data?.FirstOrDefault(x=>x.EscalaId==shiftId);
        var finance=await cn.QueryFirstOrDefaultAsync<MedicoPagamentoDto>(@"select pg.id as ""PagamentoId"",h.nome_fantasia as ""HospitalNome"",s.nome as ""EspecialidadeNome"",p.data_inicio as ""DataPlantao"",pg.valor_previsto as ""ValorPrevisto"",pg.valor_pago as ""ValorPago"",pg.status as ""Status"",pg.data_prevista as ""DataPrevista"",pg.data_pagamento as ""DataPagamento"",pg.forma_pagamento as ""FormaPagamento"",coalesce(pg.valor_apurado,pg.valor_previsto) as ""ValorApurado"",pg.valor_aprovado as ""ValorAprovado"",greatest(coalesce(pg.valor_aprovado,pg.valor_apurado,pg.valor_previsto)-coalesce(pg.valor_pago,0),0) as ""Saldo"",coalesce(pg.horas_referencia,0) as ""HorasReferencia"",coalesce(pg.valor_hora,0) as ""ValorHora"",pg.escala_id as ""EscalaId"",null::uuid as ""FechamentoId"",null::text as ""FechamentoStatus"",null::text as ""ContestacaoStatus"" from plantaopro.pagamentos pg join plantaopro.plantoes p on p.id=pg.plantao_id join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades s on s.id=p.especialidade_id where pg.escala_id=@shiftId and pg.medico_id=@MedicoId and pg.cliente_id=@ClienteId and pg.reg_status='A' order by pg.reg_date desc limit 1",new{shiftId,context.MedicoId,context.ClienteId});
        var history=new List<ProfessionalShiftEventDto>(); if(shift.ConfirmadoEm.HasValue)history.Add(new(shift.ConfirmadoEm.Value,"Presença futura confirmada","Profissional"));
        return ApiResponse<ProfessionalShiftDetailDto>.Ok(new(shift,execution,finance,history));
    }

    public async Task<ApiResponse<object>> ConfirmShiftAsync(Guid uid,Guid shiftId,string ip,CancellationToken ct)
    {
        await using var cn=Connection();await cn.OpenAsync(ct);var context=await ContextAsync(cn,uid);
        if(context is null||!context.ClienteId.HasValue||!context.TenantId.HasValue)return ApiResponse<object>.Fail("Vínculo profissional inválido ou revogado.",403);
        var authorized=await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"select exists(select 1 from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.id=@shiftId and e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A' and p.reg_status='A')",new{shiftId,context.MedicoId,context.ClienteId},cancellationToken:ct));
        if(!authorized)return ApiResponse<object>.Fail("Plantão não encontrado no contexto profissional atual.",404);
        var result=await escalaService.ConfirmarAsync(shiftId,null,uid,ip,"portal-profissional");
        return result.Success?ApiResponse<object>.Ok(new{shiftId},"Plantão confirmado"):ApiResponse<object>.Fail(result.Message,result.StatusCode==200?409:result.StatusCode);
    }

    public async Task<ApiResponse<object>> RegisterPresenceAsync(Guid uid, Guid escalaId, bool checkout, RegistrarPresencaRequest? request, string ip, string profile)
    {
        try
        {
            await using var cn = Connection(); await cn.OpenAsync(); await using var tx=await cn.BeginTransactionAsync(); var context = await ContextAsync(cn, uid);
            if (context is null || context.TenantId is null) return ApiResponse<object>.Fail("Contexto profissional inválido.", 403);
            var state = await cn.QueryFirstOrDefaultAsync<string>(@"select lower(e.status) from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where e.id=@escalaId and e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A' and p.reg_status='A' for update", new { escalaId, context.MedicoId, context.ClienteId },tx);
            if (state is null) return ApiResponse<object>.Fail("Plantão não pertence ao profissional autenticado ou o vínculo foi revogado.", 403);
            if (state is not ("confirmado" or "realizado")) return ApiResponse<object>.Fail("O estado atual do plantão não permite registrar execução.",409);
            int changed;
            var declared=request?.HorarioDeclarado?.ToUniversalTime(); var timezone=request?.Timezone?.Trim();
            if (string.IsNullOrWhiteSpace(timezone) || !TimeZoneInfo.TryFindSystemTimeZoneById(timezone, out _)) return ApiResponse<object>.Fail("Fuso horário inválido. Atualize a página e tente novamente.",422);
            if (checkout) changed = await cn.ExecuteAsync("update medico_checkins set checkout_em=now(),checkout_recebido_em=now(),checkout_declarado_em=@declared,timezone_contexto=coalesce(timezone_contexto,@timezone),status_conferencia=case when status_conferencia='CORRECAO_PENDENTE' then status_conferencia else 'PENDENTE' end,versao=versao+1,atualizado_em=now() where tenant_id=@TenantId and escala_id=@escalaId and medico_id=@MedicoId and checkout_em is null and now()>=checkin_em", new { context.TenantId, escalaId, context.MedicoId,declared,timezone },tx);
            else changed = await cn.ExecuteAsync("insert into medico_checkins(tenant_id,medico_id,escala_id,origem,checkin_em,checkin_recebido_em,checkin_declarado_em,timezone_contexto,status_conferencia) values(@TenantId,@MedicoId,@escalaId,'WEB',now(),now(),@declared,@timezone,'REGISTRO_INCOMPLETO') on conflict(tenant_id,escala_id) do nothing", new { context.TenantId, context.MedicoId, escalaId,declared,timezone },tx);
            if (changed == 0) return ApiResponse<object>.Fail(checkout ? "Check-out exige check-in e não pode ser repetido." : "Check-in já registrado.", 409);
            await tx.CommitAsync();
            await audit.RegistrarAsync(uid, context.ClienteId, "ESCALA", escalaId, checkout ? "CHECK_OUT" : "CHECK_IN", new { escalaId }, true, ip, profile);
            return ApiResponse<object>.Ok(new { escalaId }, checkout ? "Check-out registrado." : "Check-in registrado.");
        }
        catch (Exception ex) { logger.LogError(ex, "Falha no registro de presença uid:{Uid} escala:{EscalaId}", uid, escalaId); throw; }
    }

    public async Task<ApiResponse<object>> RequestCorrectionAsync(Guid uid,Guid escalaId,SolicitarCorrecaoPresencaRequest request,CancellationToken ct)
    {
        var justification=request.Justificativa?.Trim();
        if(justification is not { Length: >= 3 and <= 1000 }) return ApiResponse<object>.Fail("A justificativa deve conter entre 3 e 1000 caracteres.",422);
        await using var cn=Connection();await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);var context=await ContextAsync(cn,uid);
        if(context is null||!context.TenantId.HasValue||!context.ClienteId.HasValue)return ApiResponse<object>.Fail("Vínculo profissional inválido.",403);
        var presence=await cn.QueryFirstOrDefaultAsync<PresenceRow>(new CommandDefinition("select c.id,c.checkin_em as CheckInEm,c.checkout_em as CheckOutEm,c.versao from medico_checkins c join plantaopro.escalas e on e.id=c.escala_id and e.medico_id=c.medico_id and e.reg_status='A' join plantaopro.plantoes p on p.id=e.plantao_id and p.cliente_id=@ClienteId where c.tenant_id=@TenantId and c.escala_id=@escalaId and c.medico_id=@MedicoId for update",new{context.ClienteId,context.TenantId,context.MedicoId,escalaId},tx,cancellationToken:ct));
        if(presence is null)return ApiResponse<object>.Fail("Registro de execução não encontrado.",404);if(presence.Versao!=request.Versao)return ApiResponse<object>.Fail("O registro mudou. Atualize os dados antes de solicitar a correção.",409);
        var effectiveStart=request.InicioPropostoEm?.ToUniversalTime()??presence.CheckInEm;
        var effectiveEnd=request.FimPropostoEm?.ToUniversalTime()??presence.CheckOutEm;
        if(!effectiveEnd.HasValue||effectiveEnd<effectiveStart)return ApiResponse<object>.Fail("O intervalo efetivo proposto é inválido.",422);
        if(effectiveStart==presence.CheckInEm&&effectiveEnd==presence.CheckOutEm)return ApiResponse<object>.Fail("Informe ao menos uma alteração efetiva de horário.",422);
        var id=Guid.NewGuid();try{var changed=await cn.ExecuteAsync(new CommandDefinition(@"insert into medico_presenca_correcoes(id,tenant_id,cliente_id,presenca_id,escala_id,medico_id,inicio_original_em,fim_original_em,inicio_proposto_em,fim_proposto_em,justificativa,solicitado_por,versao_presenca_base) values(@id,@TenantId,@ClienteId,@PresenceId,@escalaId,@MedicoId,@CheckInEm,@CheckOutEm,@inicio,@fim,@justification,@uid,@baseVersion);update medico_checkins set status_conferencia='CORRECAO_PENDENTE',versao=versao+1,atualizado_em=now() where id=@PresenceId and versao=@Versao and status_conferencia in ('PENDENTE','REGISTRO_INCOMPLETO')",new{id,context.TenantId,context.ClienteId,PresenceId=presence.Id,escalaId,context.MedicoId,presence.CheckInEm,presence.CheckOutEm,inicio=request.InicioPropostoEm?.ToUniversalTime(),fim=request.FimPropostoEm?.ToUniversalTime(),justification,uid,presence.Versao,baseVersion=presence.Versao+1},tx,cancellationToken:ct));if(changed!=2){await tx.RollbackAsync(ct);return ApiResponse<object>.Fail("O registro mudou durante a solicitação. Nenhuma correção foi criada.",409);}await tx.CommitAsync(ct);return ApiResponse<object>.Ok(new{id},"Correção enviada para conferência.");}catch(PostgresException ex)when(ex.SqlState==PostgresErrorCodes.UniqueViolation){await tx.RollbackAsync(ct);return ApiResponse<object>.Fail("Já existe uma correção pendente.",409);}
    }

    public async Task<ApiResponse<object>> CancelCorrectionAsync(Guid uid, Guid escalaId, Guid correctionId,
        CancelarCorrecaoPresencaRequest request, CancellationToken ct)
    {
        if (escalaId == Guid.Empty || correctionId == Guid.Empty || request.VersaoCorrecao <= 0 || request.VersaoPresenca <= 0)
            return ApiResponse<object>.Fail("Identificador ou versão inválida.", 422);
        await using var cn = Connection(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        var context = await ContextAsync(cn, uid);
        if (context is null || !context.TenantId.HasValue || !context.ClienteId.HasValue)
            return ApiResponse<object>.Fail("Vínculo profissional inválido.", 403);
        var row = await cn.QueryFirstOrDefaultAsync<PendingCorrectionRow>(new CommandDefinition(@"
select x.id as ""Id"",x.presenca_id as ""PresencaId"",c.checkout_em as ""CheckoutEm""
from medico_presenca_correcoes x
join medico_checkins c on c.id=x.presenca_id and c.tenant_id=x.tenant_id and c.escala_id=x.escala_id
join plantaopro.escalas e on e.id=x.escala_id and e.medico_id=x.medico_id and e.reg_status='A'
join plantaopro.plantoes p on p.id=e.plantao_id and p.cliente_id=@ClienteId and p.reg_status='A'
where x.id=@correctionId and x.escala_id=@escalaId and x.medico_id=@MedicoId
 and x.tenant_id=@TenantId and x.solicitado_por=@uid and x.status='PENDENTE'
 and x.versao=@VersaoCorrecao and c.versao=@VersaoPresenca and c.status_conferencia='CORRECAO_PENDENTE'
for update of x,c", new { correctionId, escalaId, context.MedicoId, context.TenantId, context.ClienteId,
                uid, request.VersaoCorrecao, request.VersaoPresenca }, tx, cancellationToken: ct));
        if (row is null)
            return ApiResponse<object>.Fail("Este registro foi atualizado. Revise os dados antes de decidir.", 409);
        var presenceStatus = row.CheckoutEm.HasValue ? "PENDENTE" : "REGISTRO_INCOMPLETO";
        var correctionChanged = await cn.ExecuteAsync(new CommandDefinition(@"
update medico_presenca_correcoes set status='CANCELADA',versao=versao+1
where id=@correctionId and tenant_id=@TenantId and status='PENDENTE' and versao=@VersaoCorrecao", new {
            correctionId, context.TenantId, request.VersaoCorrecao }, tx, cancellationToken: ct));
        var presenceChanged = await cn.ExecuteAsync(new CommandDefinition(@"
update medico_checkins set status_conferencia=@presenceStatus,versao=versao+1,atualizado_em=now()
where id=@PresencaId and tenant_id=@TenantId and status_conferencia='CORRECAO_PENDENTE' and versao=@VersaoPresenca", new {
            row.PresencaId, context.TenantId, request.VersaoPresenca, presenceStatus }, tx, cancellationToken: ct));
        if (correctionChanged != 1 || presenceChanged != 1)
            return ApiResponse<object>.Fail("Este registro foi atualizado. Revise os dados antes de decidir.", 409);
        await cn.ExecuteAsync(new CommandDefinition(@"
insert into medico_presenca_historico(tenant_id,cliente_id,presenca_id,correcao_id,evento,valores_anteriores,valores_posteriores,justificativa,executado_por)
values(@TenantId,@ClienteId,@PresencaId,@correctionId,'CORRECAO_CANCELADA',jsonb_build_object('status','PENDENTE'),jsonb_build_object('status',@presenceStatus),'Cancelada pelo solicitante',@uid)", new {
            context.TenantId, context.ClienteId, row.PresencaId, correctionId, presenceStatus, uid }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
        return ApiResponse<object>.Ok(new { correctionId, status = "CANCELADA" }, "Solicitação cancelada; os horários registrados foram preservados.");
    }

    private const string EscalasSql = @"select e.id as ""EscalaId"",e.plantao_id as ""PlantaoId"",coalesce(h.nome_fantasia,'') as ""HospitalNome"",coalesce(s.nome,'') as ""EspecialidadeNome"",p.data_inicio as ""DataInicio"",p.data_fim as ""DataFim"",p.valor as ""Valor"",e.status as ""Status"",e.justificativa as ""Justificativa"" from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades s on s.id=p.especialidade_id where e.medico_id=@MedicoId and p.cliente_id=@ClienteId and e.reg_status='A'";
    private sealed record ProfessionalContext(Guid MedicoId, Guid? ClienteId, Guid? TenantId);
    private sealed record PresenceRow(Guid Id,DateTimeOffset CheckInEm,DateTimeOffset? CheckOutEm,long Versao);
    private sealed record PendingCorrectionRow(Guid Id, Guid PresencaId, DateTimeOffset? CheckoutEm);
}
