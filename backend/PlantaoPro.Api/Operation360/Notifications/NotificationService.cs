using Dapper;
using Npgsql;
using PlantaoPro.Api.Operation360.Realtime;

namespace PlantaoPro.Api.Operation360.Notifications;

public sealed record NotificationDto(Guid Id, string Categoria, string Titulo, string Mensagem, string? Prioridade, string? OrigemTipo, Guid? OrigemId, string? DestinoUrl, bool Lida, DateTimeOffset CriadaEm, DateTimeOffset? ExpiraEm);
public sealed record NotificationPreferenceDto(string Categoria, string TipoEvento, bool InApp, bool Email, bool Push, bool Whatsapp, bool Ativo);
public sealed record NotificationReadResult(Guid Id, bool AlreadyRead);
public sealed record NotificationPreferencesRequest(IReadOnlyList<NotificationPreferenceDto> Preferences);

public interface INotificationRepository
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(Guid tenantId, Guid userId, bool unreadOnly, CancellationToken ct);
    Task<NotificationReadResult?> ReadAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct);
    Task<int> ReadAllAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<bool> DeleteAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct);
    Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task SavePreferencesAsync(Guid tenantId, Guid userId, IReadOnlyList<NotificationPreferenceDto> preferences, CancellationToken ct);
}

public sealed class NotificationRepository : INotificationRepository
{
    private readonly string cs; public NotificationRepository(IConfiguration cfg) => cs = cfg.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
    private NpgsqlConnection Cn() => new(cs);
    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Guid tenantId, Guid userId, bool unreadOnly, CancellationToken ct) { await using var cn = Cn(); var sql = @"select n.id,n.categoria,n.titulo,n.descricao as Mensagem,null::text as Prioridade,null::text as OrigemTipo,null::uuid as OrigemId,n.url as DestinoUrl,(rs.id is not null) as Lida,n.criado_em as CriadaEm,n.expira_em as ExpiraEm from plantaopro.notifications n join plantaopro.notification_recipients nr on nr.notification_id=n.id and nr.usuario_id=@userId left join plantaopro.notification_read_states rs on rs.notification_id=n.id and rs.usuario_id=@userId where n.tenant_id=@tenantId and n.reg_status='A' and (n.expira_em is null or n.expira_em>now()) and (not @unreadOnly or rs.id is null) order by n.criado_em desc limit 100"; return (await cn.QueryAsync<NotificationDto>(new CommandDefinition(sql, new { tenantId, userId, unreadOnly }, cancellationToken: ct))).AsList(); }
    public async Task<NotificationReadResult?> ReadAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct) { await using var cn = Cn(); var exists = await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.notifications n join plantaopro.notification_recipients r on r.notification_id=n.id and r.usuario_id=@userId where n.id=@id and n.tenant_id=@tenantId and n.reg_status='A')", new { tenantId, userId, id }, cancellationToken: ct)); if (!exists) return null; var inserted = await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.notification_read_states(id,notification_id,usuario_id,lida_em) values(gen_random_uuid(),@id,@userId,now()) on conflict(notification_id,usuario_id) do nothing", new { userId, id }, cancellationToken: ct)); return new NotificationReadResult(id, inserted == 0); }
    public async Task<int> ReadAllAsync(Guid tenantId, Guid userId, CancellationToken ct) { await using var cn = Cn(); return await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.notification_read_states(id,notification_id,usuario_id,lida_em) select gen_random_uuid(),n.id,@userId,now() from plantaopro.notifications n join plantaopro.notification_recipients r on r.notification_id=n.id and r.usuario_id=@userId where n.tenant_id=@tenantId and n.reg_status='A' on conflict(notification_id,usuario_id) do nothing", new { tenantId, userId }, cancellationToken: ct)); }
    public async Task<bool> DeleteAsync(Guid tenantId, Guid userId, Guid id, CancellationToken ct) { await using var cn = Cn(); return await cn.ExecuteAsync(new CommandDefinition("delete from plantaopro.notification_recipients r using plantaopro.notifications n where r.notification_id=n.id and n.id=@id and n.tenant_id=@tenantId and r.usuario_id=@userId", new { tenantId, userId, id }, cancellationToken: ct)) > 0; }
    public async Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(Guid tenantId, Guid userId, CancellationToken ct) { await using var cn = Cn(); return (await cn.QueryAsync<NotificationPreferenceDto>(new CommandDefinition("select categoria,tipo_evento as TipoEvento,in_app as InApp,email,push,whatsapp,ativo from plantaopro.notification_preferences where tenant_id=@tenantId and usuario_id=@userId order by categoria", new { tenantId, userId }, cancellationToken: ct))).AsList(); }
    public async Task SavePreferencesAsync(Guid tenantId, Guid userId, IReadOnlyList<NotificationPreferenceDto> prefs, CancellationToken ct) { await using var cn = Cn(); await cn.OpenAsync(ct); await using var tx = await cn.BeginTransactionAsync(ct); foreach (var p in prefs) await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.notification_preferences(id,tenant_id,usuario_id,categoria,tipo_evento,in_app,email,push,whatsapp,ativo) values(gen_random_uuid(),@tenantId,@userId,@Categoria,@TipoEvento,@InApp,@Email,@Push,@Whatsapp,@Ativo) on conflict(tenant_id,usuario_id,categoria,tipo_evento) do update set in_app=excluded.in_app,email=excluded.email,push=excluded.push,whatsapp=excluded.whatsapp,ativo=excluded.ativo,atualizado_em=now()", new { tenantId, userId, p.Categoria, p.TipoEvento, p.InApp, p.Email, p.Push, p.Whatsapp, p.Ativo }, tx, cancellationToken: ct)); await tx.CommitAsync(ct); }
}

public interface IOperationNotificationService { Task<IReadOnlyList<NotificationDto>> ListAsync(bool unread, CancellationToken ct); Task<NotificationReadResult?> ReadAsync(Guid id, CancellationToken ct); Task<int> ReadAllAsync(CancellationToken ct); Task<bool> DeleteAsync(Guid id, CancellationToken ct); Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(CancellationToken ct); Task SavePreferencesAsync(NotificationPreferencesRequest request, CancellationToken ct); }
public sealed class OperationNotificationService : IOperationNotificationService
{
    private readonly INotificationRepository repo; private readonly ICurrentUserService user; private readonly IOperationRealtimePublisher realtime;
    public OperationNotificationService(INotificationRepository repo, ICurrentUserService user, IOperationRealtimePublisher realtime) { this.repo=repo; this.user=user; this.realtime=realtime; }
    private Guid Tenant => user.TenantId ?? throw new UnauthorizedAccessException(); private Guid User => user.UserId ?? throw new UnauthorizedAccessException();
    public Task<IReadOnlyList<NotificationDto>> ListAsync(bool unread, CancellationToken ct) => repo.ListAsync(Tenant, User, unread, ct);
    public async Task<NotificationReadResult?> ReadAsync(Guid id, CancellationToken ct) { var result=await repo.ReadAsync(Tenant,User,id,ct); if(result is not null && !result.AlreadyRead) await realtime.PublishNotificationAsync(Tenant,User,"NotificacaoLida",new { id },ct); return result; }
    public Task<int> ReadAllAsync(CancellationToken ct) => repo.ReadAllAsync(Tenant,User,ct); public Task<bool> DeleteAsync(Guid id,CancellationToken ct)=>repo.DeleteAsync(Tenant,User,id,ct); public Task<IReadOnlyList<NotificationPreferenceDto>> PreferencesAsync(CancellationToken ct)=>repo.PreferencesAsync(Tenant,User,ct);
    public Task SavePreferencesAsync(NotificationPreferencesRequest request,CancellationToken ct) { var allowed=new HashSet<string>{"OPERACAO","ESCALA","CLINICA","FINANCEIRO","SEGURANCA","SISTEMA"}; if(request.Preferences.Any(x=>!allowed.Contains(x.Categoria))) throw new ArgumentException("Categoria de notificação inválida."); return repo.SavePreferencesAsync(Tenant,User,request.Preferences,ct); }
}
