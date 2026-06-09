using System.Security.Claims;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Services.Security;

public interface ICurrentUserService
{
    ClaimsPrincipal User { get; }
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? ClienteId { get; }
    string UserName { get; }
    string[] Roles();
    bool IsAuthenticated();
    bool IsGlobalAdmin();
    bool IsTenantAdmin();
    bool IsPartner();
    bool IsDoctor();
    bool HasRole(string role);
}

public interface IPermissionService
{
    bool HasPermission(string module, string action);
    bool CanManageSaas();
    bool CanManageUsers();
    bool CanManageBilling();
    bool CanManageWhiteLabel();
    bool CanViewSensitiveData();
}

public interface IModuleAccessService
{
    bool CanAccessModule(string moduleCode);
    bool CanAccessFeature(string featureCode);
}

public interface ITenantAccessService
{
    bool CanAccessTenant(Guid tenantId);
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
    public Guid? UserId => ReadGuid("uid") ?? ReadGuid(ClaimTypes.NameIdentifier);
    public Guid? TenantId => ReadGuid("tenant_id") ?? ReadGuid("cliente_id");
    public Guid? ClienteId => ReadGuid("cliente_id") ?? TenantId;
    public string UserName => User.Identity?.Name ?? string.Empty;
    public string[] Roles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    public bool IsAuthenticated() => User.Identity?.IsAuthenticated == true;
    public bool IsGlobalAdmin() => User.IsGlobalAdmin();
    public bool IsTenantAdmin() => User.IsTenantAdmin();
    public bool IsPartner() => HasRole(RolesConstants.Parceiro);
    public bool IsDoctor() => HasRole(RolesConstants.Medico);
    public bool HasRole(string role) => User.IsInRole(role);

    private Guid? ReadGuid(string claimType)
    {
        Guid value;
        var raw = User.FindFirst(claimType)?.Value;
        return Guid.TryParse(raw, out value) ? value : null;
    }
}

public sealed class PermissionService : IPermissionService
{
    private readonly ICurrentUserService currentUser;

    public PermissionService(ICurrentUserService currentUser)
    {
        this.currentUser = currentUser;
    }

    public bool HasPermission(string module, string action)
    {
        if (currentUser.IsGlobalAdmin()) return true;
        var moduleCode = (module ?? string.Empty).Trim().ToUpperInvariant();
        var actionCode = (action ?? string.Empty).Trim().ToUpperInvariant();

        if (currentUser.IsTenantAdmin()) return moduleCode != "ADMIN_SAAS" && moduleCode != "BILLING_GLOBAL" && moduleCode != "OBSERVABILIDADE_GLOBAL";
        if (currentUser.HasRole(RolesConstants.Coordenacao) || currentUser.HasRole(RolesConstants.Coordenador) || currentUser.HasRole(RolesConstants.Operador)) return moduleCode is "PLANTOES" or "ESCALAS" or "CONVITES" or "CENTRAL_ESCALA" or "MEDICOS" or "AGENDA";
        if (currentUser.HasRole(RolesConstants.Financeiro)) return moduleCode is "FINANCEIRO" or "RELATORIOS" or "FATURAS";
        if (currentUser.HasRole(RolesConstants.Medico)) return moduleCode is "MEDICO_AREA" or "MINHA_AGENDA" or "CONVITES" or "PAGAMENTOS";
        if (currentUser.HasRole(RolesConstants.Hospital)) return moduleCode is "PLANTOES" or "ESCALAS" or "AGENDA";
        if (currentUser.HasRole(RolesConstants.Parceiro)) return moduleCode is "PARCEIRO" or "PROPOSTAS" or "COMERCIAL";
        if (currentUser.HasRole(RolesConstants.Suporte)) return moduleCode is "SUPORTE" or "AJUDA" or "AUDITORIA";
        if (currentUser.HasRole(RolesConstants.Auditor)) return actionCode == "VER" || moduleCode is "AUDITORIA" or "RELATORIOS";
        if (currentUser.HasRole(RolesConstants.Comercial)) return moduleCode is "COMERCIAL" or "PROPOSTAS" or "PLANOS";
        if (currentUser.HasRole(RolesConstants.CustomerSuccess)) return moduleCode is "CUSTOMER_SUCCESS" or "ONBOARDING" or "CLIENTES";
        return false;
    }

    public bool CanManageSaas() => currentUser.IsGlobalAdmin();
    public bool CanManageUsers() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanManageBilling() => currentUser.IsGlobalAdmin() || currentUser.HasRole(RolesConstants.Financeiro) || currentUser.IsTenantAdmin();
    public bool CanManageWhiteLabel() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanViewSensitiveData() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Auditor);
}

public sealed class ModuleAccessService : IModuleAccessService
{
    private readonly IPermissionService permissions;

    public ModuleAccessService(IPermissionService permissions)
    {
        this.permissions = permissions;
    }

    public bool CanAccessModule(string moduleCode) => permissions.HasPermission(moduleCode, "VER");
    public bool CanAccessFeature(string featureCode) => permissions.HasPermission(featureCode, "USAR");
}

public sealed class TenantAccessService : ITenantAccessService
{
    private readonly ICurrentUserService currentUser;

    public TenantAccessService(ICurrentUserService currentUser)
    {
        this.currentUser = currentUser;
    }

    public bool CanAccessTenant(Guid tenantId) => currentUser.IsGlobalAdmin() || (currentUser.TenantId.HasValue && currentUser.TenantId.Value == tenantId);
}
