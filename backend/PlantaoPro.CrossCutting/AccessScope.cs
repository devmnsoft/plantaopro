namespace PlantaoPro.CrossCutting.Security;

public static class AccessScopes
{
    public const string Global = "GLOBAL";
    public const string Tenant = "TENANT";
    public const string Hybrid = "HYBRID";
}

public sealed record RoleDefinition(
    string Code,
    string Name,
    string Scope,
    int Priority,
    bool RequiresTenant,
    bool CanSwitchTenant,
    bool CanImpersonate,
    string DefaultRoute);

public interface IRoleCatalog
{
    IReadOnlyCollection<RoleDefinition> Roles { get; }
    string? Normalize(string? role);
    RoleDefinition? Find(string? role);
    bool IsGlobal(string? role);
    bool RequiresTenant(string? role);
}

public sealed class RoleCatalog : IRoleCatalog
{
    private static readonly RoleDefinition[] Definitions =
    {
        new("ADMINISTRADOR_GLOBAL", "Administrador Global", AccessScopes.Global, 1000, false, true, true, "/AdminSaas"),
        new("SUPORTE", "Suporte", AccessScopes.Global, 900, false, true, true, "/AdminSaas"),
        new("AUDITOR", "Auditor", AccessScopes.Global, 800, false, true, false, "/Auditoria"),
        new("ADMINISTRADOR_CLIENTE", "Administrador Cliente", AccessScopes.Tenant, 700, true, false, false, "/ClientePortal"),
        new("ADMINISTRADOR", "Administrador", AccessScopes.Tenant, 650, true, false, false, "/ClientePortal"),
        new("DIRETOR", "Diretor", AccessScopes.Tenant, 600, true, false, false, "/ClientePortal"),
        new("COORDENADOR", "Coordenador", AccessScopes.Tenant, 550, true, false, false, "/CentralEscala"),
        new("COORDENACAO", "Coordenação", AccessScopes.Tenant, 540, true, false, false, "/CentralEscala"),
        new("FINANCEIRO", "Financeiro", AccessScopes.Tenant, 500, true, false, false, "/Financeiro"),
        new("OPERADOR", "Operador", AccessScopes.Tenant, 450, true, false, false, "/CentralEscala"),
        new("MEDICO", "Médico", AccessScopes.Tenant, 400, true, false, false, "/MedicoArea"),
        new("HOSPITAL", "Hospital", AccessScopes.Tenant, 350, true, false, false, "/HospitalArea"),
        new("RECEPCAO", "Recepção", AccessScopes.Tenant, 300, true, false, false, "/CentralAtendimento"),
        new("TRIAGEM", "Triagem", AccessScopes.Tenant, 250, true, false, false, "/Triagem"),
        new("PARCEIRO", "Parceiro", AccessScopes.Tenant, 200, true, false, false, "/ParceiroPortal"),
        new("COMERCIAL", "Comercial", AccessScopes.Global, 150, false, false, false, "/Comercial"),
        new("CUSTOMER_SUCCESS", "Customer Success", AccessScopes.Global, 140, false, true, false, "/CustomerSuccess")
    };

    public IReadOnlyCollection<RoleDefinition> Roles => Definitions;

    public string? Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return null;
        var value = role.Trim().ToUpperInvariant()
            .Replace("Á", "A").Replace("À", "A").Replace("Â", "A").Replace("Ã", "A")
            .Replace("É", "E").Replace("Ê", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O").Replace("Ô", "O").Replace("Õ", "O")
            .Replace("Ú", "U")
            .Replace("Ç", "C");
        return value switch
        {
            "ADMIN" or "ADMINISTRADOR" => "ADMINISTRADOR",
            "ADMIN_CLIENTE" or "ADMINISTRADOR_CLIENTE" => "ADMINISTRADOR_CLIENTE",
            _ => value
        };
    }

    public RoleDefinition? Find(string? role)
    {
        var normalized = Normalize(role);
        return string.IsNullOrWhiteSpace(normalized) ? null : Definitions.FirstOrDefault(r => r.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsGlobal(string? role)
    {
        var scope = Find(role)?.Scope;
        return string.Equals(scope, AccessScopes.Global, StringComparison.OrdinalIgnoreCase) || string.Equals(scope, AccessScopes.Hybrid, StringComparison.OrdinalIgnoreCase);
    }
    public bool RequiresTenant(string? role) => Find(role)?.RequiresTenant == true;
}

public interface IPrimaryRoleResolver { string Resolve(IEnumerable<string> roles); }
public sealed class PrimaryRoleResolver : IPrimaryRoleResolver
{
    private readonly IRoleCatalog _roleCatalog;

    public PrimaryRoleResolver(IRoleCatalog roleCatalog)
    {
        _roleCatalog = roleCatalog;
    }

    public string Resolve(IEnumerable<string> roles) => roles.Select(_roleCatalog.Find).OfType<RoleDefinition>().OrderByDescending(r => r.Priority).Select(r => r.Code).FirstOrDefault() ?? _roleCatalog.Normalize(roles.FirstOrDefault()) ?? "USUARIO";
}

public interface IAccessScopeResolver { string Resolve(IEnumerable<string> roles, bool tenantContextSelected = false); }
public sealed class AccessScopeResolver : IAccessScopeResolver
{
    private readonly IRoleCatalog _roleCatalog;

    public AccessScopeResolver(IRoleCatalog roleCatalog)
    {
        _roleCatalog = roleCatalog;
    }

    public string Resolve(IEnumerable<string> roles, bool tenantContextSelected = false)
    {
        var definitions = roles.Select(_roleCatalog.Find).OfType<RoleDefinition>().ToArray();
        var hasGlobal = definitions.Any(r => r.Scope == AccessScopes.Global || r.Scope == AccessScopes.Hybrid);
        var hasTenant = definitions.Any(r => r.RequiresTenant || r.Scope == AccessScopes.Tenant);
        if (tenantContextSelected && hasGlobal) return AccessScopes.Hybrid;
        if (hasGlobal && hasTenant) return AccessScopes.Global;
        if (hasGlobal) return AccessScopes.Global;
        return AccessScopes.Tenant;
    }
}

public interface ITenantContextResolver { bool RequiresTenant(IEnumerable<string> roles, Guid? tenantId); bool IsTenantSelected(Guid? tenantId); }
public sealed class TenantContextResolver : ITenantContextResolver
{
    private readonly IRoleCatalog _roleCatalog;

    public TenantContextResolver(IRoleCatalog roleCatalog)
    {
        _roleCatalog = roleCatalog;
    }

    public bool RequiresTenant(IEnumerable<string> roles, Guid? tenantId)
    {
        var normalizedRoles = roles.Select(_roleCatalog.Normalize).Where(r => !string.IsNullOrWhiteSpace(r)).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var hasGlobalAccess = normalizedRoles.Any(role => _roleCatalog.IsGlobal(role));
        var requiresTenant = !hasGlobalAccess && normalizedRoles.Any(role => _roleCatalog.RequiresTenant(role));
        return requiresTenant && !tenantId.HasValue;
    }
    public bool IsTenantSelected(Guid? tenantId) => tenantId.HasValue;
}
