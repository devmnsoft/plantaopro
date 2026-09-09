using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;

namespace PlantaoPro.Api;

public sealed class AuthenticationSessionRow
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime? ExpiraEm { get; set; }
    public DateTime? RevogadaEm { get; set; }
    public string RegStatus { get; set; } = string.Empty;
    public string UsuarioRegStatus { get; set; } = string.Empty;
    public string UsuarioStatus { get; set; } = string.Empty;
    public string TenantStatus { get; set; } = string.Empty;
}

public static class AuthenticationSessionState
{
    public static bool IsUsable(AuthenticationSessionRow? session, Guid expectedUserId, DateTime utcNow)
    {
        if (session is null || session.Id == Guid.Empty || session.UsuarioId != expectedUserId) return false;
        if (!string.Equals(session.RegStatus, "A", StringComparison.OrdinalIgnoreCase) || session.RevogadaEm.HasValue) return false;
        if (session.ExpiraEm.HasValue && session.ExpiraEm.Value <= utcNow) return false;
        if (!string.Equals(session.UsuarioRegStatus, "A", StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(session.UsuarioStatus, "ATIVO", StringComparison.OrdinalIgnoreCase)) return false;
        return string.Equals(session.TenantStatus, "ATIVO", StringComparison.OrdinalIgnoreCase);
    }
}

public interface IAuthenticationSessionService
{
    Task CreateAsync(Guid sessionId, Guid userId, Guid? tenantId, Guid? clienteId, DateTime expiresAtUtc, string? ip, string? userAgent, CancellationToken ct = default);
    Task<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken ct = default);
}

public sealed class AuthenticationSessionService : IAuthenticationSessionService
{
    private readonly string connectionString;

    public AuthenticationSessionService(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
    }

    public async Task CreateAsync(Guid sessionId, Guid userId, Guid? tenantId, Guid? clienteId, DateTime expiresAtUtc, string? ip, string? userAgent, CancellationToken ct = default)
    {
        const string sql = @"insert into plantaopro.auth_sessoes
    (id, tenant_id, cliente_id, usuario_id, dispositivo_nome, ip_mascarado, user_agent_sanitizado, iniciado_em, ultimo_uso_em, expira_em, reg_status, reg_date)
values
    (@sessionId, @tenantId, @clienteId, @userId, @device, @maskedIp, @safeUserAgent, now(), now(), @expiresAtUtc, 'A', now())";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            sessionId,
            tenantId,
            clienteId,
            userId,
            device = DeviceName(userAgent),
            maskedIp = Fingerprint(ip),
            safeUserAgent = Sanitize(userAgent, 500),
            expiresAtUtc
        }, cancellationToken: ct));
    }

    public async Task<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        if (!TryGuid(principal, "uid", out var userId) && !TryGuid(principal, ClaimTypes.NameIdentifier, out userId)) return false;
        if (!TryGuid(principal, "session_id", out var sessionId)) return false;

        const string sql = @"select s.id as ""Id"", s.usuario_id as ""UsuarioId"", s.expira_em as ""ExpiraEm"", s.revogada_em as ""RevogadaEm"",
       coalesce(s.reg_status,'') as ""RegStatus"", coalesce(u.reg_status,'') as ""UsuarioRegStatus"",
       coalesce(u.status,'ATIVO') as ""UsuarioStatus"",
       case
         when exists(
           select 1 from plantaopro.usuarios_perfis up
           join plantaopro.perfis p on p.id=up.perfil_id and p.reg_status='A' and coalesce(p.status,'ATIVO')='ATIVO'
           where up.usuario_id=u.id and up.reg_status='A'
             and upper(coalesce(p.codigo,p.nome)) in ('ADMIN_GLOBAL','ADMINISTRADOR_GLOBAL','SUPER_ADMIN','SUPER_ADMINISTRADOR')) then 'ATIVO'
         when coalesce(s.tenant_id,s.cliente_id,u.tenant_id,u.cliente_id) is null then 'ATIVO'
         else coalesce(c.status,'INATIVO')
       end as ""TenantStatus""
from plantaopro.auth_sessoes s
join plantaopro.usuarios u on u.id=s.usuario_id
left join plantaopro.clientes c on c.id=coalesce(s.tenant_id,s.cliente_id,u.tenant_id,u.cliente_id) and c.reg_status='A'
where s.id=@sessionId and s.usuario_id=@userId";

        await using var connection = new NpgsqlConnection(connectionString);
        var session = await connection.QuerySingleOrDefaultAsync<AuthenticationSessionRow>(
            new CommandDefinition(sql, new { sessionId, userId }, cancellationToken: ct));
        if (!AuthenticationSessionState.IsUsable(session, userId, DateTime.UtcNow)) return false;

        await connection.ExecuteAsync(new CommandDefinition(
            "update plantaopro.auth_sessoes set ultimo_uso_em=now(),reg_update=now() where id=@sessionId and (ultimo_uso_em is null or ultimo_uso_em < now() - interval '1 minute')",
            new { sessionId }, cancellationToken: ct));
        return true;
    }

    private static bool TryGuid(ClaimsPrincipal principal, string claimType, out Guid value) =>
        Guid.TryParse(principal.FindFirstValue(claimType), out value);

    private static string DeviceName(string? userAgent)
    {
        var safe = Sanitize(userAgent, 120);
        return string.IsNullOrWhiteSpace(safe) ? "Dispositivo não identificado" : safe;
    }

    private static string? Fingerprint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return "sha256:" + Convert.ToHexString(hash).Substring(0, 16);
    }

    private static string? Sanitize(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var safe = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return safe.Length <= maximumLength ? safe : safe.Substring(0, maximumLength);
    }
}
