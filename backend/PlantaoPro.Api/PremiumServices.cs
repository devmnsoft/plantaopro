using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class PermissionService
{
    private readonly IAuditService audit;

    public PermissionService(IAuditService audit)
    {
        this.audit = audit;
    }

    public async Task<ApiResponse<IEnumerable<RolePermissionDto>>> ListRolePermissionsAsync(Guid userId, string? ip, string? ua)
    {
        var rolePermissions = new[]
        {
            new RolePermissionDto(RolesConstants.Administrador, new[]{"dashboard:read","usuarios:manage","auditoria:read","plantoes:manage","escalas:manage","financeiro:manage"}),
            new RolePermissionDto(RolesConstants.Coordenacao, new[]{"dashboard:read","plantoes:manage","escalas:manage","medicos:read"}),
            new RolePermissionDto(RolesConstants.Operador, new[]{"dashboard:read","plantoes:manage","escalas:manage"}),
            new RolePermissionDto(RolesConstants.Financeiro, new[]{"dashboard:financeiro","pagamentos:manage","relatorios:financeiro"}),
            new RolePermissionDto(RolesConstants.Medico, new[]{"agenda:read","plantoes:request","pagamentos:read"}),
            new RolePermissionDto(RolesConstants.Hospital, new[]{"hospital:read","plantoes:publish","escalas:read"})
        };

        await audit.LogAsync(userId, "READ_PERMISSION_MATRIX", "perfis", null, "Consulta da matriz de permissões", ip: ip, userAgent: ua);
        return ApiResponse<IEnumerable<RolePermissionDto>>.Ok(rolePermissions);
    }
}

public sealed class NotificationPreferenceService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;

    public NotificationPreferenceService(IConfiguration cfg, IAuditService audit)
    {
        this.cfg = cfg;
        this.audit = audit;
    }

    public async Task<ApiResponse<IEnumerable<NotificationPreferenceDto>>> GetAsync(Guid userId)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<NotificationPreferenceDto>(@"select tipo as Tipo, in_app as InApp, email as Email
from plantaopro.usuario_notificacao_preferencias where usuario_id=@userId order by tipo", new { userId });
        return ApiResponse<IEnumerable<NotificationPreferenceDto>>.Ok(rows);
    }

    public async Task<ApiResponse<string>> UpsertAsync(Guid userId, UpsertNotificationPreferenceRequest request, string? ip, string? ua)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync(@"insert into plantaopro.usuario_notificacao_preferencias(usuario_id,tipo,in_app,email,reg_date,reg_status)
values(@userId,@tipo,@inApp,@email,now(),'A')
on conflict(usuario_id,tipo) do update set in_app=excluded.in_app,email=excluded.email,reg_update=now()", new
        {
            userId,
            tipo = request.Tipo.Trim().ToUpperInvariant(),
            request.InApp,
            request.Email
        });
        await audit.LogAsync(userId, "UPSERT_NOTIFICATION_PREF", "usuario_notificacao_preferencias", userId, $"Preferência atualizada: {request.Tipo}", valorNovo: System.Text.Json.JsonSerializer.Serialize(request), ip: ip, userAgent: ua);
        return ApiResponse<string>.Ok("Preferência atualizada.");
    }
}
