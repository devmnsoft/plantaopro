using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using PlantaoPro.CrossCutting.Security;

namespace PlantaoPro.Web.Security;

/// <summary>
/// Dados imutáveis para construir uma sessão autenticada. Login e RefreshContext mapeiam a
/// resposta da API para este mesmo contexto, garantindo que os dois caminhos gerem o mesmo cookie.
/// </summary>
public sealed record SessionLoginContext(
    Guid UsuarioId,
    string? Nome,
    string? Email,
    IReadOnlyCollection<string>? Roles,
    IReadOnlyCollection<string>? Permissions,
    IReadOnlyCollection<string>? Modules,
    string? PrimaryRole,
    string? AccessScope,
    bool TenantContextSelected,
    string? ContextMode,
    Guid? ClienteId,
    string? ClienteNome,
    string? ClienteStatus,
    Guid? TenantId,
    string? TenantNome,
    string? SessionId,
    string Token);

public static class SessionClaimsBuilder
{
    /// <summary>
    /// Construtor único do principal/sessão compartilhado por Login e RefreshContext.
    /// Gera o conjunto completo e normalizado de claims (permissões e módulos com ponto como
    /// separador, além de <c>access_catalog_version</c>, <c>is_global_admin</c>, <c>sub</c>,
    /// <c>email</c>, <c>role</c>, <c>Perfil</c>, <c>roles</c>, <c>cliente_status</c>,
    /// <c>cliente</c>, <c>tenant</c>): o login inicial e qualquer atualização de contexto
    /// produzem um cookie idêntico para a mesma resposta da API.
    /// </summary>
    public static (ClaimsPrincipal Principal, string PrimaryRole, string AccessScope, string ContextMode, string[] NormalizedRoles) Build(
        SessionLoginContext ctx,
        IRoleCatalog roleCatalog,
        IPrimaryRoleResolver primaryRoleResolver,
        IAccessScopeResolver accessScopeResolver,
        string fallbackSessionId)
    {
        var normalizedRoles = (ctx.Roles ?? Array.Empty<string>())
            .Select(roleCatalog.Normalize)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var primaryRole = string.IsNullOrWhiteSpace(ctx.PrimaryRole)
            ? primaryRoleResolver.Resolve(normalizedRoles)
            : roleCatalog.Normalize(ctx.PrimaryRole) ?? primaryRoleResolver.Resolve(normalizedRoles);
        var accessScope = string.IsNullOrWhiteSpace(ctx.AccessScope)
            ? accessScopeResolver.Resolve(normalizedRoles, ctx.TenantContextSelected)
            : ctx.AccessScope;
        var contextMode = string.IsNullOrWhiteSpace(ctx.ContextMode)
            ? (ctx.TenantId.HasValue ? AccessScopes.Tenant : AccessScopes.Global)
            : ctx.ContextMode;
        var hasGlobalAccess = normalizedRoles.Any(role => roleCatalog.IsGlobal(role));

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, ctx.UsuarioId.ToString()),
            new Claim("sub", ctx.UsuarioId.ToString()),
            new Claim("uid", ctx.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(ctx.Nome) ? "Usuário PlantãoPro" : ctx.Nome),
            new Claim(ClaimTypes.Email, ctx.Email ?? string.Empty),
            new Claim("email", ctx.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, primaryRole),
            new Claim("role", primaryRole),
            new Claim("Perfil", primaryRole),
            new Claim("primary_role", primaryRole),
            new Claim("roles", string.Join(',', normalizedRoles)),
            new Claim("is_global_admin", hasGlobalAccess.ToString().ToLowerInvariant()),
            new Claim("access_scope", accessScope),
            new Claim("context_mode", contextMode),
            new Claim("session_id", string.IsNullOrWhiteSpace(ctx.SessionId) ? fallbackSessionId : ctx.SessionId),
            new Claim("jwt", ctx.Token),
            new Claim("access_catalog_version", "v2149")
        };

        claims.AddRange((ctx.Permissions ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => new Claim("permission", NormalizeAccessCode(value)))
            .DistinctBy(claim => claim.Value));
        claims.AddRange((ctx.Modules ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => new Claim("module", NormalizeAccessCode(value)))
            .DistinctBy(claim => claim.Value));

        if (ctx.ClienteId.HasValue)
        {
            var clienteId = ctx.ClienteId.Value.ToString();
            claims.Add(new Claim("cliente_id", clienteId));
            claims.Add(new Claim("cliente", ctx.ClienteNome ?? "Cliente PlantãoPro"));
            if (!string.IsNullOrWhiteSpace(ctx.ClienteStatus))
            {
                claims.Add(new Claim("cliente_status", ctx.ClienteStatus.Trim().ToUpperInvariant()));
            }
        }
        if (ctx.TenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", ctx.TenantId.Value.ToString()));
            claims.Add(new Claim("tenant", ctx.TenantNome ?? ctx.ClienteNome ?? "Tenant PlantãoPro"));
        }

        foreach (var role in normalizedRoles)
        {
            var normalizedRole = roleCatalog.Normalize(role);
            if (!string.IsNullOrWhiteSpace(normalizedRole) && !claims.Any(c => c.Type == ClaimTypes.Role && c.Value == normalizedRole))
            {
                claims.Add(new Claim(ClaimTypes.Role, normalizedRole));
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return (new ClaimsPrincipal(identity), primaryRole, accessScope, contextMode, normalizedRoles);
    }

    /// <summary>
    /// Normalização canônica de códigos de permissão/módulo: maiúsculas e ":" virando ".".
    /// O banco pode persistir os dois formatos; as claims e as comparações usam sempre ponto.
    /// </summary>
    public static string NormalizeAccessCode(string value) => value.Trim().ToUpperInvariant().Replace(':', '.');
}
