using Dapper;
using Npgsql;
using PlantaoPro.Api.Contracts.Notifications;
using PlantaoPro.Api.Operation360.Realtime;

namespace PlantaoPro.Api.Operation360.Notifications;

public sealed record NotificationDto(Guid Id, string Categoria, string Titulo, string Mensagem, string Prioridade,
    string Status, string? OrigemTipo, Guid? OrigemId, string? DestinoUrl, bool Lida,
    DateTimeOffset CriadaEm, DateTimeOffset? ExpiraEm);
public sealed record NotificationReadResult(Guid Id, bool AlreadyRead);
public sealed record NotificationFilter(string? Tipo, string? Modulo, string? Prioridade, string? Status,
    DateTimeOffset? De, DateTimeOffset? Ate, int Limit = 100);
public sealed record DispatchNotification(Guid TenantId, Guid UsuarioId, string Categoria, string TipoEvento,
    string Titulo, string Mensagem, string Prioridade, string OrigemTipo, Guid OrigemId, string? DestinoUrl);

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(Guid tenantId, Guid userId, NotificationFilter filter, CancellationToken ct);
    Task<NotificationReadResult?> ReadAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct);
    Task<int> ReadAllAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<bool> SetStatusAsync(Guid tenantId, Guid userId, Guid id, string status, CancellationToken ct);
    Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task SavePreferencesAsync(Guid tenantId, Guid userId, IReadOnlyList<NotificationPreferenceDto> preferences, CancellationToken ct);
    Task<bool> DispatchAsync(DispatchNotification notification, CancellationToken ct);
}

public sealed class NotificationRepository : INotificationRepository
{
    private readonly string cs;
    public NotificationRepository(IConfiguration cfg) => cs = cfg.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
    private NpgsqlConnection Cn() => new(cs);

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Guid tenantId, Guid userId, NotificationFilter filter, CancellationToken ct)
    {
        await using var cn = Cn();
        var sql = @"select n.id,n.categoria,n.titulo,n.descricao as Mensagem,n.prioridade,
case when a.status is not null then a.status when rs.id is not null then 'LIDA' else 'NAO_LIDA' end as Status,
n.origem_tipo as OrigemTipo,n.origem_id as OrigemId,n.url as DestinoUrl,(rs.id is not null) as Lida,n.criado_em as CriadaEm,n.expira_em as ExpiraEm
from plantaopro.notifications n
join plantaopro.notification_recipients nr on nr.notification_id=n.id and nr.usuario_id=@userId
left join plantaopro.notification_read_states rs on rs.notification_id=n.id and rs.usuario_id=@userId
left join lateral (select status from plantaopro.notification_actions where notification_id=n.id and usuario_id=@userId order by criado_em desc limit 1) a on true
where n.tenant_id=@tenantId and n.reg_status='A' and (n.expira_em is null or n.expira_em>now())
and (@tipo is null or n.tipo_evento=@tipo) and (@modulo is null or n.categoria=@modulo)
and (@prioridade is null or n.prioridade=@prioridade) and (@de is null or n.criado_em>=@de) and (@ate is null or n.criado_em<=@ate)
and (@status is null or (@status='NAO_LIDA' and rs.id is null and a.status is null) or (@status='LIDA' and rs.id is not null and a.status is null) or a.status=@status)
order by case n.prioridade when 'CRITICA' then 1 when 'ALTA' then 2 when 'MEDIA' then 3 else 4 end,n.criado_em desc limit @limit";
        return (await cn.QueryAsync<NotificationDto>(new CommandDefinition(sql, new { tenantId, userId, tipo=Normalize(filter.Tipo), modulo=Normalize(filter.Modulo), prioridade=Normalize(filter.Prioridade), status=Normalize(filter.Status), filter.De, filter.Ate, limit=Math.Clamp(filter.Limit,1,200) }, cancellationToken: ct))).AsList();
    }

    public async Task<NotificationReadResult?> ReadAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct)
    {
        await using var cn = Cn();
        if (!await CanAccess(cn, tenantId, userId, id, ct)) return null;
        var inserted = await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.notification_read_states(id,notification_id,usuario_id,lida_em) values(gen_random_uuid(),@id,@userId,now()) on conflict(notification_id,usuario_id) do nothing", new { userId, id }, cancellationToken: ct));
        return new NotificationReadResult(id, inserted == 0);
    }

    public async Task<int> ReadAllAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        await using var cn = Cn();
        return await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.notification_read_states(id,notification_id,usuario_id,lida_em) select gen_random_uuid(),n.id,@userId,now() from plantaopro.notifications n join plantaopro.notification_recipients r on r.notification_id=n.id and r.usuario_id=@userId where n.tenant_id=@tenantId and n.reg_status='A' on conflict(notification_id,usuario_id) do nothing", new { tenantId, userId }, cancellationToken: ct));
    }

    public async Task<bool> SetStatusAsync(Guid tenantId, Guid userId, Guid id, string status, CancellationToken ct)
    {
        await using var cn = Cn();
        if (!await CanAccess(cn, tenantId, userId, id, ct)) return false;
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.notification_actions(id,notification_id,tenant_id,usuario_id,status,criado_em) values(gen_random_uuid(),@id,@tenantId,@userId,@status,now())", new { id, tenantId, userId, status }, cancellationToken: ct));
        return true;
    }

    public async Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        await using var cn = Cn();
        return (await cn.QueryAsync<NotificationPreferenceDto>(new CommandDefinition("select categoria,tipo_evento as TipoEvento,in_app as InApp,email,push,whatsapp,ativo from plantaopro.notification_preferences where tenant_id=@tenantId and usuario_id=@userId order by categoria,tipo_evento", new { tenantId, userId }, cancellationToken: ct))).AsList();
    }

    public async Task SavePreferencesAsync(Guid tenantId, Guid userId, IReadOnlyList<NotificationPreferenceDto> prefs, CancellationToken ct)
    {
        await using var cn = Cn(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        foreach (var p in prefs)
            await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.notification_preferences(id,tenant_id,usuario_id,categoria,tipo_evento,in_app,email,push,whatsapp,ativo) values(gen_random_uuid(),@tenantId,@userId,@Categoria,@TipoEvento,true,@Email,@Push,@Whatsapp,@Ativo) on conflict(tenant_id,usuario_id,categoria,tipo_evento) do update set in_app=true,email=excluded.email,push=excluded.push,whatsapp=excluded.whatsapp,ativo=excluded.ativo,atualizado_em=now()", new { tenantId, userId, p.Categoria, p.TipoEvento, p.Email, p.Push, p.Whatsapp, p.Ativo }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);
    }

    public async Task<bool> DispatchAsync(DispatchNotification n, CancellationToken ct)
    {
        await using var cn = Cn(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct);
        var enabled = await cn.ExecuteScalarAsync<bool?>(new CommandDefinition("select ativo and in_app from plantaopro.notification_preferences where tenant_id=@TenantId and usuario_id=@UsuarioId and categoria=@Categoria and tipo_evento=@TipoEvento", n, tx, cancellationToken: ct)) ?? true;
        if (!enabled) return false;
        var id = Guid.NewGuid();
        var inserted = await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.notifications(id,tenant_id,categoria,tipo_evento,titulo,descricao,prioridade,origem_tipo,origem_id,url,criado_em,reg_status) values(@id,@TenantId,@Categoria,@TipoEvento,@Titulo,@Mensagem,@Prioridade,@OrigemTipo,@OrigemId,@DestinoUrl,now(),'A') on conflict(tenant_id,usuario_id_dedupe,origem_tipo,origem_id,tipo_evento) do nothing", new { id, n.TenantId, n.Categoria, n.TipoEvento, n.Titulo, n.Mensagem, n.Prioridade, n.OrigemTipo, n.OrigemId, n.DestinoUrl, usuario_id_dedupe=n.UsuarioId }, tx, cancellationToken: ct));
        if (inserted == 0) return false;
        await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.notification_recipients(notification_id,usuario_id) values(@id,@UsuarioId)", new { id, n.UsuarioId }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct); return true;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    private static Task<bool> CanAccess(NpgsqlConnection cn, Guid tenantId, Guid userId, Guid id, CancellationToken ct) => cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.notifications n join plantaopro.notification_recipients r on r.notification_id=n.id and r.usuario_id=@userId where n.id=@id and n.tenant_id=@tenantId and n.reg_status='A')", new { tenantId, userId, id }, cancellationToken: ct));
}

public interface IOperationNotificationService
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(NotificationFilter filter, CancellationToken ct);
    Task<NotificationReadResult?> ReadAsync(Guid id, CancellationToken ct); Task<int> ReadAllAsync(CancellationToken ct);
    Task<bool> SetStatusAsync(Guid id, string status, CancellationToken ct);
    Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(CancellationToken ct);
    Task SavePreferencesAsync(NotificationPreferencesRequest request, CancellationToken ct);
}

public sealed class OperationNotificationService : IOperationNotificationService
{
    private static readonly HashSet<string> Categories = new(StringComparer.OrdinalIgnoreCase) { "OPERACAO", "ESCALA", "CLINICA", "FINANCEIRO", "SEGURANCA", "SISTEMA" };
    private readonly INotificationRepository repo; private readonly ICurrentUserService user; private readonly IOperationRealtimePublisher realtime; private readonly ILogger<OperationNotificationService> logger;
    public OperationNotificationService(INotificationRepository repo, ICurrentUserService user, IOperationRealtimePublisher realtime, ILogger<OperationNotificationService> logger) { this.repo=repo; this.user=user; this.realtime=realtime; this.logger=logger; }
    private Guid Tenant => user.TenantId ?? throw new UnauthorizedAccessException(); private Guid User => user.UserId ?? throw new UnauthorizedAccessException();
    public Task<IReadOnlyList<NotificationDto>> ListAsync(NotificationFilter filter, CancellationToken ct) => repo.ListAsync(Tenant, User, filter, ct);
    public async Task<NotificationReadResult?> ReadAsync(Guid id, CancellationToken ct) { var result=await repo.ReadAsync(Tenant,User,id,ct); if(result is not null && !result.AlreadyRead) await realtime.PublishNotificationAsync(Tenant,User,"NotificacaoLida",new { id },ct); return result; }
    public Task<int> ReadAllAsync(CancellationToken ct) => repo.ReadAllAsync(Tenant,User,ct);
    public async Task<bool> SetStatusAsync(Guid id,string status,CancellationToken ct) { if(status is not ("ARQUIVADA" or "RESOLVIDA")) throw new ArgumentException("Status inválido."); var result=await repo.SetStatusAsync(Tenant,User,id,status,ct); logger.LogInformation("Notificação {NotificationId} alterada para {Status} pelo usuário {UserId} no tenant {TenantId}",id,status,User,Tenant); return result; }
    public Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(CancellationToken ct)=>repo.PreferencesAsync(Tenant,User,ct);
    public Task SavePreferencesAsync(NotificationPreferencesRequest request,CancellationToken ct) { if(request.Preferences.Any(x=>!Categories.Contains(x.Categoria))) throw new ArgumentException("Categoria de notificação inválida."); return repo.SavePreferencesAsync(Tenant,User,request.Preferences,ct); }
}
