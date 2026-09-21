using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using System.Text.Json;

namespace PlantaoPro.Api;

public sealed class ExecutionConferenceService
{
    private readonly IConfiguration configuration;
    private readonly ICurrentUserService current;
    private readonly PermissionGuardService permissionGuard;
    private readonly TenantGuardService tenantGuard;

    public ExecutionConferenceService(IConfiguration configuration, ICurrentUserService current,
        PermissionGuardService permissionGuard, TenantGuardService tenantGuard)
    {
        this.configuration = configuration;
        this.current = current;
        this.permissionGuard = permissionGuard;
        this.tenantGuard = tenantGuard;
    }

    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));

    private (Guid Tenant, Guid Cliente, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException(),
         current.ClienteId ?? throw new UnauthorizedAccessException(),
         current.UserId ?? throw new UnauthorizedAccessException());

    public async Task<ApiResponse<ExecutionConferencePageDto>> ListAsync(ExecutionConferenceFilter filter, CancellationToken ct)
    {
        var permission = await permissionGuard.ValidarPermissaoAsync(PermissionConstants.EscalasVer);
        if (!permission.Success) return ApiResponse<ExecutionConferencePageDto>.Fail(permission.Message, permission.StatusCode);
        if (filter.Inicio.HasValue != filter.Fim.HasValue || filter.Inicio > filter.Fim)
            return ApiResponse<ExecutionConferencePageDto>.Fail("Informe um período válido.", 422);

        var context = Context();
        if (filter.UnidadeId.HasValue && !await tenantGuard.PodeAcessarHospitalAsync(context.User, filter.UnidadeId.Value))
            return ApiResponse<ExecutionConferencePageDto>.Fail("Unidade indisponível no contexto atual.", 403);
        if (filter.ProfissionalId.HasValue && !await tenantGuard.PodeAcessarMedicoAsync(context.User, filter.ProfissionalId.Value))
            return ApiResponse<ExecutionConferencePageDto>.Fail("Profissional indisponível no contexto atual.", 403);
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 100);
        var status = (filter.Status ?? string.Empty).Trim().ToUpperInvariant();
        var args = new
        {
            context.Tenant,
            context.Cliente,
            inicio = filter.Inicio,
            fim = filter.Fim,
            filter.UnidadeId,
            filter.ProfissionalId,
            filter.Divergencia,
            status,
            limit = size,
            offset = (page - 1) * size
        };

        const string from = @"
from medico_checkins c
join plantaopro.escalas e on e.id=c.escala_id and e.medico_id=c.medico_id and e.reg_status='A'
join plantaopro.plantoes p on p.id=e.plantao_id and p.cliente_id=@Cliente and p.reg_status='A'
join plantaopro.medicos m on m.id=c.medico_id and m.reg_status='A'
join plantaopro.hospitais h on h.id=p.hospital_id and h.reg_status='A'
left join lateral (
    select x.* from medico_presenca_correcoes x
    where x.tenant_id=c.tenant_id and x.cliente_id=@Cliente and x.presenca_id=c.id
    order by x.solicitado_em desc limit 1
) x on true
where c.tenant_id=@Tenant
  and (@inicio is null or p.data_inicio::date>=@inicio)
  and (@fim is null or p.data_inicio::date<=@fim)
  and (@UnidadeId is null or p.hospital_id=@UnidadeId)
  and (@ProfissionalId is null or c.medico_id=@ProfissionalId)
  and (@Divergencia is null or @Divergencia=(x.id is not null or c.checkin_em<>p.data_inicio or c.checkout_em is distinct from p.data_fim))
  and (@status='' or c.status_conferencia=@status or x.status=@status)";

        const string select = @"
select case when x.status='PENDENTE' then x.id end as ""CorrecaoId"",c.id as ""PresencaId"",c.escala_id as ""EscalaId"",
 c.medico_id as ""MedicoId"",m.nome as ""Profissional"",
 coalesce(h.nome_fantasia,h.razao_social) as ""Unidade"",
 p.data_inicio as ""InicioPrevisto"",p.data_fim as ""FimPrevisto"",
 c.checkin_em as ""InicioRegistrado"",c.checkout_em as ""FimRegistrado"",
 case when x.status='PENDENTE' then x.inicio_proposto_em end as ""InicioProposto"",
 case when x.status='PENDENTE' then x.fim_proposto_em end as ""FimProposto"",
 c.inicio_aprovado_em as ""InicioAprovado"",c.fim_aprovado_em as ""FimAprovado"",
 c.status_conferencia as ""Status"",
 case when x.status='PENDENTE' then coalesce(x.justificativa,'') else '' end as ""Justificativa"",
 case when x.status='PENDENTE' then x.versao else c.versao end as ""Versao"",c.versao as ""VersaoPresenca"",
 case when x.status='PENDENTE' then x.solicitado_em
      else coalesce(c.checkout_em,c.checkin_em) end as ""SolicitadoEm"" ";

        await using var connection = Connection();
        var rows = (await connection.QueryAsync<ConferenceRow>(new CommandDefinition(
            select + from + @" order by case when x.status='PENDENTE' then x.solicitado_em
                                        else coalesce(c.checkout_em,c.checkin_em) end desc,
                                  c.id desc limit @limit offset @offset",
            args,
            cancellationToken: ct))).AsList();
        var items = rows.Select(row => row.ToDto()).ToArray();
        var counts = await connection.QuerySingleAsync<ConferenceCounts>(new CommandDefinition(@"
select count(*) as ""Total"",
 count(*) filter(where c.status_conferencia in ('REGISTRO_INCOMPLETO','PENDENTE','CORRECAO_PENDENTE')) as ""Pendentes"",
 count(*) filter(where c.status_conferencia='APROVADA') as ""Aprovadas"",
 count(*) filter(where x.status='RECUSADA') as ""Recusadas"" " + from, args, cancellationToken: ct));

        return ApiResponse<ExecutionConferencePageDto>.Ok(
            new(items, counts.Total, counts.Pendentes, counts.Aprovadas, counts.Recusadas, page, size));
    }

    public Task<ApiResponse<object>> DecideAsync(Guid id, DecideExecutionCorrectionRequest request, CancellationToken ct) =>
        DecideCorrectionAsync(id, request, ct);

    public async Task<ApiResponse<object>> ApprovePresenceAsync(Guid presenceId, DecideExecutionPresenceRequest request, CancellationToken ct)
    {
        var permission = await permissionGuard.ValidarPermissaoAsync(PermissionConstants.EscalasConfirmar);
        if (!permission.Success) return ApiResponse<object>.Fail(permission.Message, permission.StatusCode);
        if (presenceId == Guid.Empty || request.Versao <= 0)
            return ApiResponse<object>.Fail("Identificador ou versão inválida.", 422);
        var reason = ValidateReason(request.Justificativa);
        if (reason is null)
            return ApiResponse<object>.Fail("A justificativa deve conter entre 3 e 1000 caracteres.", 422);

        var context = Context();
        await using var connection = Connection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var presence = await LockPresenceAsync(connection, transaction, presenceId, context, ct);
        if (presence is null)
            return ApiResponse<object>.Fail("Execução não encontrada no contexto atual.", 404);
        if (!await tenantGuard.PodeAcessarHospitalAsync(context.User, presence.HospitalId))
            return ApiResponse<object>.Fail("Você não possui vínculo com a unidade desta execução.", 403);
        if (presence.Versao != request.Versao || presence.Status != "PENDENTE")
            return ApiResponse<object>.Fail("Este registro foi atualizado. Revise os dados antes de decidir.", 409);
        if (!presence.FimRegistrado.HasValue)
            return ApiResponse<object>.Fail("Registre a saída antes da conferência.", 422);

        var consolidated = await IsConsolidatedAsync(connection, transaction, presence.EscalaId, context.Tenant, ct);
        var newStatus = consolidated ? "AJUSTE_POS_APURACAO" : "APROVADA";
        var changed = await connection.ExecuteAsync(new CommandDefinition(@"
update medico_checkins set inicio_aprovado_em=case when @consolidated then inicio_aprovado_em else checkin_em end,
 fim_aprovado_em=case when @consolidated then fim_aprovado_em else checkout_em end,
 status_conferencia=@newStatus,versao=versao+1,atualizado_em=now()
where id=@presenceId and tenant_id=@Tenant and versao=@Versao and status_conferencia='PENDENTE'",
            new { presenceId, context.Tenant, presence.Versao, consolidated, newStatus }, transaction, cancellationToken: ct));
        if (changed != 1)
            return ApiResponse<object>.Fail("Este registro foi atualizado. Revise os dados antes de decidir.", 409);

        await InsertHistoryAsync(connection, transaction, context, presenceId, null,
            consolidated ? "CONFERENCIA_APOS_APURACAO" : "EXECUCAO_APROVADA",
            new { presence.InicioRegistrado, presence.FimRegistrado },
            new { presence.InicioRegistrado, presence.FimRegistrado, status = newStatus, pendenciaFinanceira = consolidated },
            reason, ct);
        await transaction.CommitAsync(ct);
        var message = consolidated
            ? "Conferência registrada como pendência pós-apuração; nenhum valor financeiro foi alterado."
            : "Execução aprovada operacionalmente; o pagamento continua no fluxo financeiro existente.";
        return ApiResponse<object>.Ok(new { presenceId, status = newStatus, consolidated }, message);
    }

    private async Task<ApiResponse<object>> DecideCorrectionAsync(Guid id, DecideExecutionCorrectionRequest request, CancellationToken ct)
    {
        var requiredPermission = request.Aprovar ? PermissionConstants.EscalasConfirmar : PermissionConstants.EscalasRecusar;
        var permission = await permissionGuard.ValidarPermissaoAsync(requiredPermission);
        if (!permission.Success) return ApiResponse<object>.Fail(permission.Message, permission.StatusCode);
        if (id == Guid.Empty || request.PresencaId == Guid.Empty || request.Versao <= 0)
            return ApiResponse<object>.Fail("Identificador ou versão inválida.", 422);
        var reason = ValidateReason(request.Justificativa);
        if (reason is null)
            return ApiResponse<object>.Fail("A justificativa deve conter entre 3 e 1000 caracteres.", 422);

        var context = Context();
        await using var connection = Connection();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var correction = await connection.QueryFirstOrDefaultAsync<DecisionRow>(new CommandDefinition(@"
select x.id as ""Id"",x.presenca_id as ""PresenceId"",x.solicitado_por as ""SolicitadoPor"",
 x.status as ""Status"",x.versao as ""Versao"",x.versao_presenca_base as ""VersaoPresencaBase"",
 x.inicio_original_em as ""InicioOriginal"",x.fim_original_em as ""FimOriginal"",
 x.inicio_proposto_em as ""InicioProposto"",x.fim_proposto_em as ""FimProposto""
from medico_presenca_correcoes x
where x.id=@id and x.tenant_id=@Tenant and x.cliente_id=@Cliente for update",
            new { id, context.Tenant, context.Cliente }, transaction, cancellationToken: ct));
        if (correction is null)
            return ApiResponse<object>.Fail("Correção não encontrada no contexto atual.", 404);
        if (correction.PresenceId != request.PresencaId)
            return ApiResponse<object>.Fail("A correção e a presença não pertencem ao mesmo registro.", 422);
        if (correction.SolicitadoPor == context.User)
            return ApiResponse<object>.Fail("O solicitante não pode decidir a própria correção.", 403);
        if (correction.Status != "PENDENTE" || correction.Versao != request.Versao)
            return ApiResponse<object>.Fail("Este registro foi atualizado. Revise os dados antes de decidir.", 409);

        var presence = await LockPresenceAsync(connection, transaction, correction.PresenceId, context, ct);
        if (presence is not null && !await tenantGuard.PodeAcessarHospitalAsync(context.User, presence.HospitalId))
            return ApiResponse<object>.Fail("Você não possui vínculo com a unidade desta execução.", 403);
        if (presence is null || presence.Status != "CORRECAO_PENDENTE" || presence.Versao != correction.VersaoPresencaBase)
            return ApiResponse<object>.Fail("Este registro foi atualizado. Revise os dados antes de decidir.", 409);

        var effectiveStart = correction.InicioProposto ?? presence.InicioRegistrado;
        var effectiveEnd = correction.FimProposto ?? presence.FimRegistrado;
        if (!effectiveEnd.HasValue || effectiveEnd < effectiveStart)
            return ApiResponse<object>.Fail("O intervalo efetivo da correção é inválido.", 422);

        var consolidated = await IsConsolidatedAsync(connection, transaction, presence.EscalaId, context.Tenant, ct);
        var correctionStatus = request.Aprovar ? "APROVADA" : "RECUSADA";
        var presenceStatus = request.Aprovar ? (consolidated ? "AJUSTE_POS_APURACAO" : "APROVADA") : "PENDENTE";
        var correctionChanged = await connection.ExecuteAsync(new CommandDefinition(@"
update medico_presenca_correcoes set status=@correctionStatus,decidido_por=@User,decidido_em=now(),
 justificativa_decisao=@reason,inicio_aprovado_em=case when @approve then @effectiveStart end,
 fim_aprovado_em=case when @approve then @effectiveEnd end,versao=versao+1
where id=@id and tenant_id=@Tenant and versao=@Versao and status='PENDENTE'",
            new { id, context.Tenant, context.User, reason, approve = request.Aprovar, effectiveStart, effectiveEnd, correction.Versao, correctionStatus }, transaction, cancellationToken: ct));
        var presenceChanged = await connection.ExecuteAsync(new CommandDefinition(@"
update medico_checkins set
 inicio_aprovado_em=case when @approve and not @consolidated then @effectiveStart else inicio_aprovado_em end,
 fim_aprovado_em=case when @approve and not @consolidated then @effectiveEnd else fim_aprovado_em end,
 status_conferencia=@presenceStatus,versao=versao+1,atualizado_em=now()
where id=@presenceId and tenant_id=@Tenant and versao=@presenceVersion and status_conferencia='CORRECAO_PENDENTE'",
            new { presenceId = correction.PresenceId, context.Tenant, presenceVersion = presence.Versao, approve = request.Aprovar, consolidated, effectiveStart, effectiveEnd, presenceStatus }, transaction, cancellationToken: ct));
        if (correctionChanged != 1 || presenceChanged != 1)
            return ApiResponse<object>.Fail("Este registro foi atualizado. Revise os dados antes de decidir.", 409);

        var after = request.Aprovar
            ? new { Inicio = effectiveStart, Fim = effectiveEnd, status = presenceStatus, pendenciaFinanceira = consolidated }
            : new { Inicio = presence.InicioRegistrado, Fim = presence.FimRegistrado, status = presenceStatus, pendenciaFinanceira = false };
        await InsertHistoryAsync(connection, transaction, context, correction.PresenceId, id,
            request.Aprovar ? "CORRECAO_APROVADA" : "CORRECAO_RECUSADA",
            new { correction.InicioOriginal, correction.FimOriginal }, after, reason, ct);
        await transaction.CommitAsync(ct);

        var message = consolidated && request.Aprovar
            ? "Correção registrada como pendência pós-apuração; os valores consolidados foram preservados."
            : "Decisão registrada.";
        return ApiResponse<object>.Ok(new { id, status = correctionStatus, consolidated }, message);
    }

    private static string? ValidateReason(string? value)
    {
        var reason = value?.Trim();
        return reason is { Length: >= 3 and <= 1000 } ? reason : null;
    }

    private static Task<PresenceDecisionRow?> LockPresenceAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Guid presenceId, (Guid Tenant, Guid Cliente, Guid User) context, CancellationToken ct) =>
        connection.QueryFirstOrDefaultAsync<PresenceDecisionRow>(new CommandDefinition(@"
select c.id as ""Id"",c.escala_id as ""EscalaId"",p.hospital_id as ""HospitalId"",c.checkin_em as ""InicioRegistrado"",
 c.checkout_em as ""FimRegistrado"",c.status_conferencia as ""Status"",c.versao as ""Versao""
from medico_checkins c
join plantaopro.escalas e on e.id=c.escala_id and e.reg_status='A'
join plantaopro.plantoes p on p.id=e.plantao_id and p.cliente_id=@Cliente and p.reg_status='A'
where c.id=@presenceId and c.tenant_id=@Tenant for update",
            new { presenceId, context.Tenant, context.Cliente }, transaction, cancellationToken: ct));

    private static Task<bool> IsConsolidatedAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        Guid escalaId, Guid tenant, CancellationToken ct) =>
        connection.ExecuteScalarAsync<bool>(new CommandDefinition(@"
select exists(
 select 1 from plantaopro.financeiro_pagamento_origem o
 join plantaopro.pagamentos pg on pg.id=o.pagamento_id
 where o.tenant_id=@tenant and o.escala_id=@escalaId and lower(pg.status) in ('pago','aprovado')
) ", new { tenant, escalaId }, transaction, cancellationToken: ct));

    private static Task<int> InsertHistoryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        (Guid Tenant, Guid Cliente, Guid User) context, Guid presenceId, Guid? correctionId, string eventName,
        object before, object after, string reason, CancellationToken ct) =>
        connection.ExecuteAsync(new CommandDefinition(@"
insert into medico_presenca_historico
 (tenant_id,cliente_id,presenca_id,correcao_id,evento,valores_anteriores,valores_posteriores,justificativa,executado_por)
values (@Tenant,@Cliente,@presenceId,@correctionId,@eventName,@before::jsonb,@after::jsonb,@reason,@User)",
            new { context.Tenant, context.Cliente, context.User, presenceId, correctionId, eventName,
                before = JsonSerializer.Serialize(before), after = JsonSerializer.Serialize(after), reason }, transaction, cancellationToken: ct));

    private sealed class ConferenceCounts
    {
        public long Total { get; set; }
        public long Pendentes { get; set; }
        public long Aprovadas { get; set; }
        public long Recusadas { get; set; }
    }

    private sealed class ConferenceRow
    {
        public Guid? CorrecaoId { get; set; }
        public Guid PresencaId { get; set; }
        public Guid EscalaId { get; set; }
        public Guid HospitalId { get; set; }
        public Guid MedicoId { get; set; }
        public string Profissional { get; set; } = string.Empty;
        public string Unidade { get; set; } = string.Empty;
        public DateTimeOffset InicioPrevisto { get; set; }
        public DateTimeOffset FimPrevisto { get; set; }
        public DateTimeOffset? InicioRegistrado { get; set; }
        public DateTimeOffset? FimRegistrado { get; set; }
        public DateTimeOffset? InicioProposto { get; set; }
        public DateTimeOffset? FimProposto { get; set; }
        public DateTimeOffset? InicioAprovado { get; set; }
        public DateTimeOffset? FimAprovado { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Justificativa { get; set; } = string.Empty;
        public long Versao { get; set; }
        public long VersaoPresenca { get; set; }
        public DateTimeOffset SolicitadoEm { get; set; }

        public ExecutionConferenceItemDto ToDto() => new(CorrecaoId, PresencaId, EscalaId, MedicoId,
            Profissional, Unidade, InicioPrevisto, FimPrevisto, InicioRegistrado, FimRegistrado,
            InicioProposto, FimProposto, InicioAprovado, FimAprovado, Status, Justificativa,
            Versao, VersaoPresenca, SolicitadoEm);
    }

    private sealed class DecisionRow
    {
        public Guid Id { get; set; }
        public Guid PresenceId { get; set; }
        public Guid SolicitadoPor { get; set; }
        public string Status { get; set; } = string.Empty;
        public long Versao { get; set; }
        public long VersaoPresencaBase { get; set; }
        public DateTimeOffset? InicioOriginal { get; set; }
        public DateTimeOffset? FimOriginal { get; set; }
        public DateTimeOffset? InicioProposto { get; set; }
        public DateTimeOffset? FimProposto { get; set; }
    }

    private sealed class PresenceDecisionRow
    {
        public Guid Id { get; set; }
        public Guid EscalaId { get; set; }
        public Guid HospitalId { get; set; }
        public DateTimeOffset InicioRegistrado { get; set; }
        public DateTimeOffset? FimRegistrado { get; set; }
        public string Status { get; set; } = string.Empty;
        public long Versao { get; set; }
    }
}
