using System.Security.Claims;

namespace PlantaoPro.Api;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? ClienteId { get; }
    string[] Roles { get; }
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

    public Guid? UserId => ReadGuid("uid") ?? ReadGuid(ClaimTypes.NameIdentifier);
    public Guid? TenantId => ReadGuid("tenant_id") ?? ReadGuid("cliente_id");
    public Guid? ClienteId => ReadGuid("cliente_id") ?? TenantId;
    public string[] Roles => httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
    public bool IsAuthenticated() => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    public bool IsGlobalAdmin() => HasRole(RolesConstants.AdministradorGlobal);
    public bool IsTenantAdmin() => HasRole(RolesConstants.Administrador) || HasRole(RolesConstants.AdministradorCliente) || HasRole(RolesConstants.Diretor);
    public bool IsPartner() => HasRole(RolesConstants.Parceiro);
    public bool IsDoctor() => HasRole(RolesConstants.Medico);
    public bool HasRole(string role) => Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

    private Guid? ReadGuid(string claimType)
    {
        Guid value;
        var raw = httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
        return Guid.TryParse(raw, out value) ? value : null;
    }
}

public sealed class ModulePermissionService : IPermissionService, IModuleAccessService, ITenantAccessService
{
    private readonly ICurrentUserService currentUser;

    public ModulePermissionService(ICurrentUserService currentUser)
    {
        this.currentUser = currentUser;
    }

    public bool HasPermission(string module, string action)
    {
        if (currentUser.IsGlobalAdmin()) return true;
        var code = (module ?? string.Empty).Trim().ToUpperInvariant();
        if (currentUser.IsTenantAdmin()) return code != "ADMIN_SAAS" && code != "BILLING_GLOBAL" && code != "OBSERVABILIDADE_GLOBAL";
        if (currentUser.HasRole(RolesConstants.Coordenacao) || currentUser.HasRole(RolesConstants.Coordenador) || currentUser.HasRole(RolesConstants.Operador)) return code is "PLANTOES" or "ESCALAS" or "CONVITES" or "CENTRAL_ESCALA" or "MEDICOS";
        if (currentUser.HasRole(RolesConstants.Financeiro)) return code is "FINANCEIRO" or "RELATORIOS" or "FATURAS";
        if (currentUser.HasRole(RolesConstants.Medico)) return code is "MEDICO_AREA" or "AGENDA" or "CONVITES" or "PAGAMENTOS";
        if (currentUser.HasRole(RolesConstants.Hospital)) return code is "PLANTOES" or "ESCALAS";
        if (currentUser.HasRole(RolesConstants.Parceiro)) return code is "PARCEIRO" or "PROPOSTAS" or "COMISSOES";
        if (currentUser.HasRole(RolesConstants.Suporte)) return code is "SUPORTE" or "CHAMADOS";
        if (currentUser.HasRole(RolesConstants.Auditor)) return code is "AUDITORIA" or "RELATORIOS";
        if (currentUser.HasRole(RolesConstants.Comercial)) return code is "COMERCIAL" or "PROPOSTAS" or "PLANOS";
        if (currentUser.HasRole(RolesConstants.CustomerSuccess)) return code is "CUSTOMER_SUCCESS" or "ONBOARDING" or "CLIENTES";
        return false;
    }

    public bool CanManageSaas() => currentUser.IsGlobalAdmin();
    public bool CanAccessModule(string moduleCode) => HasPermission(moduleCode, "VER");
    public bool CanAccessFeature(string featureCode) => HasPermission(featureCode, "USAR");
    public bool CanAccessTenant(Guid tenantId) => currentUser.IsGlobalAdmin() || (currentUser.TenantId.HasValue && currentUser.TenantId.Value == tenantId);
    public bool CanManageUsers() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanManageBilling() => currentUser.IsGlobalAdmin() || currentUser.HasRole(RolesConstants.Financeiro) || currentUser.IsTenantAdmin();
    public bool CanManageWhiteLabel() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanViewSensitiveData() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Auditor);
}
