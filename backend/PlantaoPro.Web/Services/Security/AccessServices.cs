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
    IReadOnlyCollection<string> Roles();
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
    bool CanAccessAdminArea();
    bool CanAccessClientPortal();
    bool CanAccessPartnerPortal();
    bool CanAccessMedicalArea();
    bool CanAccessFinancialArea();
}

public interface IModuleAccessService
{
    bool CanAccessModule(string moduleCode);
    bool IsModuleEnabled(string moduleCode);
    bool IsFeatureEnabled(string featureCode);
    bool CanAccessFeature(string featureCode);
}

public interface ITenantAccessService
{
    bool CanAccessTenant(Guid tenantId);
    bool CanAccessCliente(Guid clienteId);
    bool CanSwitchTenant();
    Task RegistrarTrocaContextoAsync(Guid tenantId, string motivo, CancellationToken cancellationToken = default);
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

    public IReadOnlyCollection<string> Roles()
    {
        return User.FindAll(ClaimTypes.Role)
            .Select(c => Normalize(c.Value))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool IsAuthenticated() => User.Identity?.IsAuthenticated == true;
    public bool IsGlobalAdmin() => HasRole(RolesConstants.AdministradorGlobal);
    public bool IsTenantAdmin() => HasRole(RolesConstants.Administrador) || HasRole(RolesConstants.AdministradorCliente) || HasRole(RolesConstants.Diretor);
    public bool IsPartner() => HasRole(RolesConstants.Parceiro);
    public bool IsDoctor() => HasRole(RolesConstants.Medico);
    public bool HasRole(string role) => Roles().Any(r => string.Equals(r, Normalize(role), StringComparison.OrdinalIgnoreCase));

    private Guid? ReadGuid(string claimType)
    {
        Guid value;
        var raw = User.FindFirst(claimType)?.Value;
        return Guid.TryParse(raw, out value) ? value : null;
    }

    private static string Normalize(string? role) => (role ?? string.Empty).Trim().ToUpperInvariant();
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
        if (!currentUser.IsAuthenticated()) return false;
        if (currentUser.IsGlobalAdmin()) return true;

        var moduleCode = Normalize(module);
        var actionCode = Normalize(action);

        if (moduleCode == "AJUDA" || moduleCode == "LGPD" || moduleCode == "CONTA" || moduleCode == "TREINAMENTO") return true;
        if (IsSaude360(moduleCode) && currentUser.HasRole(RolesConstants.AdministradorClinica)) return true;

        if (currentUser.IsTenantAdmin())
        {
            return moduleCode != "ADMIN_SAAS" && moduleCode != "BILLING_GLOBAL" && moduleCode != "OBSERVABILIDADE_GLOBAL" && moduleCode != "PARCEIRO" && moduleCode != "MARKETPLACE";
        }

        if (currentUser.HasRole(RolesConstants.Coordenacao) || currentUser.HasRole(RolesConstants.Coordenador) || currentUser.HasRole(RolesConstants.Operador))
        {
            return moduleCode == "DASHBOARD" || moduleCode == "PLANTOES" || moduleCode == "ESCALAS" || moduleCode == "CONVITES" || moduleCode == "CENTRAL_ESCALA" || moduleCode == "MEDICOS" || moduleCode == "HOSPITAIS" || moduleCode == "ESPECIALIDADES" || moduleCode == "AGENDA" || moduleCode == "COMUNICACAO" || moduleCode == "PAINEL_CHAMADA" || moduleCode == "AGENDAMENTO" || moduleCode == "PACIENTES";
        }

        if (currentUser.HasRole(RolesConstants.Financeiro))
        {
            return moduleCode == "FINANCEIRO" || moduleCode == "PAGAMENTOS" || moduleCode == "RELATORIOS" || moduleCode == "FATURAS" || moduleCode == "BILLING" || moduleCode == "FINANCEIRO_CLINICA";
        }

        if (currentUser.HasRole(RolesConstants.Medico))
        {
            return moduleCode == "MEDICO_AREA" || moduleCode == "MINHA_AGENDA" || moduleCode == "CONVITES" || moduleCode == "PAGAMENTOS" || moduleCode == "PAGAMENTOS_PROPRIOS" || moduleCode == "DISPONIBILIDADE" || moduleCode == "SUBSTITUICOES" || moduleCode == "CONSULTAS" || moduleCode == "PRESCRICAO" || moduleCode == "CID";
        }

        if (currentUser.HasRole(RolesConstants.Hospital))
        {
            return moduleCode == "HOSPITAL_AREA" || moduleCode == "PLANTOES" || moduleCode == "ESCALAS" || moduleCode == "AGENDA";
        }

        if (currentUser.HasRole(RolesConstants.Parceiro))
        {
            return moduleCode == "PARCEIRO" || moduleCode == "LEADS" || moduleCode == "PROPOSTAS" || moduleCode == "COMISSOES" || moduleCode == "REPASSES" || moduleCode == "MATERIAIS";
        }

        if (currentUser.HasRole(RolesConstants.Suporte))
        {
            return moduleCode == "SUPORTE" || moduleCode == "AJUDA" || moduleCode == "AUDITORIA" || moduleCode == "OBSERVABILIDADE" || moduleCode == "OBSERVABILIDADE_GLOBAL" || moduleCode == "ADMIN_SAAS";
        }

        if (currentUser.HasRole(RolesConstants.Auditor))
        {
            return actionCode == "VER" || moduleCode == "AUDITORIA" || moduleCode == "RELATORIOS" || moduleCode == "LGPD" || moduleCode == "OBSERVABILIDADE_GLOBAL";
        }

        if (currentUser.HasRole(RolesConstants.Comercial))
        {
            return moduleCode == "COMERCIAL" || moduleCode == "PROPOSTAS" || moduleCode == "PLANOS" || moduleCode == "MARKETPLACE";
        }

        if (currentUser.HasRole(RolesConstants.CustomerSuccess))
        {
            return moduleCode == "CUSTOMER_SUCCESS" || moduleCode == "ONBOARDING" || moduleCode == "CLIENTES" || moduleCode == "JORNADA" || moduleCode == "SUPORTE";
        }

        if (currentUser.HasRole(RolesConstants.Recepcao)) return moduleCode == "PAINEL_CHAMADA" || moduleCode == "AGENDAMENTO" || moduleCode == "PACIENTES";
        if (currentUser.HasRole(RolesConstants.Triagem)) return moduleCode == "TRIAGEM";
        if (currentUser.HasRole(RolesConstants.FinanceiroClinica)) return moduleCode == "FINANCEIRO_CLINICA";
        if (currentUser.HasRole(RolesConstants.FaturamentoConvenio)) return moduleCode == "CONVENIOS" || moduleCode == "PLANOS_SAUDE";
        return false;
    }

    private static bool IsSaude360(string moduleCode) => moduleCode == "PAINEL_CHAMADA" || moduleCode == "AGENDAMENTO" || moduleCode == "PACIENTES" || moduleCode == "TRIAGEM" || moduleCode == "CONSULTAS" || moduleCode == "CID" || moduleCode == "PRESCRICAO" || moduleCode == "FINANCEIRO_CLINICA" || moduleCode == "CONVENIOS" || moduleCode == "PLANOS_SAUDE";

    public bool CanManageSaas() => currentUser.IsGlobalAdmin();
    public bool CanManageUsers() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanManageBilling() => currentUser.IsGlobalAdmin() || currentUser.HasRole(RolesConstants.Financeiro) || currentUser.IsTenantAdmin();
    public bool CanManageWhiteLabel() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanViewSensitiveData() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Auditor);
    public bool CanAccessAdminArea() => currentUser.IsGlobalAdmin();
    public bool CanAccessClientPortal() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanAccessPartnerPortal() => currentUser.IsGlobalAdmin() || currentUser.IsPartner();
    public bool CanAccessMedicalArea() => currentUser.IsGlobalAdmin() || currentUser.IsDoctor();
    public bool CanAccessFinancialArea() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Financeiro);

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
}

public sealed class ModuleAccessService : IModuleAccessService
{
    private readonly IPermissionService permissions;
    private readonly ICurrentUserService currentUser;

    public ModuleAccessService(IPermissionService permissions, ICurrentUserService currentUser)
    {
        this.permissions = permissions;
        this.currentUser = currentUser;
    }

    public bool CanAccessModule(string moduleCode) => IsModuleEnabled(moduleCode) && permissions.HasPermission(moduleCode, "VER");
    public bool CanAccessFeature(string featureCode) => IsFeatureEnabled(featureCode) && permissions.HasPermission(featureCode, "USAR");
    public bool IsModuleEnabled(string moduleCode) => currentUser.IsGlobalAdmin() || !string.Equals((moduleCode ?? string.Empty).Trim(), "BI_AVANCADO", StringComparison.OrdinalIgnoreCase);
    public bool IsFeatureEnabled(string featureCode) => currentUser.IsGlobalAdmin() || !string.Equals((featureCode ?? string.Empty).Trim(), "WHITE_LABEL_AVANCADO", StringComparison.OrdinalIgnoreCase);
}

public sealed class TenantAccessService : ITenantAccessService
{
    private readonly ICurrentUserService currentUser;
    private readonly ILogger<TenantAccessService> logger;

    public TenantAccessService(ICurrentUserService currentUser, ILogger<TenantAccessService> logger)
    {
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public bool CanAccessTenant(Guid tenantId) => currentUser.IsGlobalAdmin() || (currentUser.TenantId.HasValue && currentUser.TenantId.Value == tenantId);
    public bool CanAccessCliente(Guid clienteId) => currentUser.IsGlobalAdmin() || (currentUser.ClienteId.HasValue && currentUser.ClienteId.Value == clienteId);
    public bool CanSwitchTenant() => currentUser.IsGlobalAdmin();

    public Task RegistrarTrocaContextoAsync(Guid tenantId, string motivo, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Troca de contexto solicitada. Usuario:{UsuarioId} TenantDestino:{TenantId} Motivo:{Motivo} Permitido:{Permitido}", currentUser.UserId, tenantId, motivo, CanSwitchTenant());
        return Task.CompletedTask;
    }
}
