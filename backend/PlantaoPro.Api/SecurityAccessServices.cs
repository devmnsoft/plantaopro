using System.Security.Claims;

namespace PlantaoPro.Api;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? ClienteId { get; }
    IReadOnlyCollection<string> Roles { get; }
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

    public Guid? UserId => ReadGuid("uid") ?? ReadGuid(ClaimTypes.NameIdentifier);
    public Guid? TenantId => ReadGuid("tenant_id") ?? ReadGuid("cliente_id");
    public Guid? ClienteId => ReadGuid("cliente_id") ?? TenantId;

    public IReadOnlyCollection<string> Roles => httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role)
        .Select(c => Normalize(c.Value))
        .Where(r => !string.IsNullOrWhiteSpace(r))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? Array.Empty<string>();

    public bool IsAuthenticated() => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;
    public bool IsGlobalAdmin() => HasRole(RolesConstants.AdministradorGlobal);
    public bool IsTenantAdmin() => HasRole(RolesConstants.Administrador) || HasRole(RolesConstants.AdministradorCliente) || HasRole(RolesConstants.Diretor);
    public bool IsPartner() => HasRole(RolesConstants.Parceiro);
    public bool IsDoctor() => HasRole(RolesConstants.Medico);
    public bool HasRole(string role) => Roles.Any(r => string.Equals(r, Normalize(role), StringComparison.OrdinalIgnoreCase));

    private Guid? ReadGuid(string claimType)
    {
        Guid value;
        var raw = httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
        return Guid.TryParse(raw, out value) ? value : null;
    }

    private static string Normalize(string? role) => (role ?? string.Empty).Trim().ToUpperInvariant();
}

public sealed class ModulePermissionService : IPermissionService, IModuleAccessService, ITenantAccessService
{
    private readonly ICurrentUserService currentUser;
    private readonly ILogger<ModulePermissionService> logger;

    public ModulePermissionService(ICurrentUserService currentUser, ILogger<ModulePermissionService> logger)
    {
        this.currentUser = currentUser;
        this.logger = logger;
    }

    public bool HasPermission(string module, string action)
    {
        if (!currentUser.IsAuthenticated()) return false;
        if (currentUser.IsGlobalAdmin()) return true;

        var code = Normalize(module);
        var actionCode = Normalize(action);

        if (code == "AJUDA" || code == "LGPD" || code == "CONTA") return true;

        var saude360Recepcao = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SAUDE360_PAINEL", "SAUDE360_AGENDAMENTO", "SAUDE360_PACIENTES" };
        var saude360Triagem = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SAUDE360_TRIAGEM" };
        var saude360Medico = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SAUDE360_CONSULTAS", "SAUDE360_PRESCRICAO", "SAUDE360_CID" };
        var saude360Financeiro = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SAUDE360_FINANCEIRO" };
        var saude360Convenios = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SAUDE360_CONVENIOS", "SAUDE360_PLANOS_SAUDE" };
        if (currentUser.HasRole(RolesConstants.AdministradorClinica)) return code.StartsWith("SAUDE360_", StringComparison.OrdinalIgnoreCase) || code != "ADMIN_SAAS";
        if (currentUser.HasRole(RolesConstants.Recepcao) && saude360Recepcao.Contains(code)) return true;
        if (currentUser.HasRole(RolesConstants.Triagem) && saude360Triagem.Contains(code)) return true;
        if (currentUser.HasRole(RolesConstants.FinanceiroClinica) && saude360Financeiro.Contains(code)) return true;
        if (currentUser.HasRole(RolesConstants.FaturamentoConvenio) && saude360Convenios.Contains(code)) return true;

        if (code == "SEGURANCA") return currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Suporte) || currentUser.HasRole(RolesConstants.Auditor);
        if (currentUser.IsTenantAdmin()) return code != "ADMIN_SAAS" && code != "BILLING_GLOBAL" && code != "OBSERVABILIDADE_GLOBAL" && code != "PARCEIRO";
        if (currentUser.HasRole(RolesConstants.Coordenacao) || currentUser.HasRole(RolesConstants.Coordenador) || currentUser.HasRole(RolesConstants.Operador)) return code == "DASHBOARD" || code == "PLANTOES" || code == "ESCALAS" || code == "CONVITES" || code == "CENTRAL_ESCALA" || code == "MEDICOS" || code == "HOSPITAIS" || code == "ESPECIALIDADES" || code == "AGENDA";
        if (currentUser.HasRole(RolesConstants.Financeiro)) return saude360Financeiro.Contains(code) || code == "FINANCEIRO" || code == "PAGAMENTOS" || code == "RELATORIOS" || code == "FATURAS" || code == "BILLING";
        if (currentUser.HasRole(RolesConstants.Medico)) return saude360Medico.Contains(code) || code == "MEDICO_AREA" || code == "MINHA_AGENDA" || code == "CONVITES" || code == "PAGAMENTOS" || code == "PAGAMENTOS_PROPRIOS" || code == "DISPONIBILIDADE" || code == "SUBSTITUICOES";
        if (currentUser.HasRole(RolesConstants.Hospital)) return code == "HOSPITAL_AREA" || code == "PLANTOES" || code == "ESCALAS" || code == "AGENDA";
        if (currentUser.HasRole(RolesConstants.Parceiro)) return code == "PARCEIRO" || code == "LEADS" || code == "PROPOSTAS" || code == "COMISSOES" || code == "REPASSES" || code == "MATERIAIS";
        if (currentUser.HasRole(RolesConstants.Suporte)) return code == "SUPORTE" || code == "AJUDA" || code == "AUDITORIA" || code == "OBSERVABILIDADE";
        if (currentUser.HasRole(RolesConstants.Auditor)) return actionCode == "VER" || code == "AUDITORIA" || code == "RELATORIOS" || code == "LGPD";
        if (currentUser.HasRole(RolesConstants.Comercial)) return code == "COMERCIAL" || code == "PROPOSTAS" || code == "PLANOS" || code == "MARKETPLACE";
        if (currentUser.HasRole(RolesConstants.CustomerSuccess)) return code == "CUSTOMER_SUCCESS" || code == "ONBOARDING" || code == "CLIENTES" || code == "JORNADA" || code == "SUPORTE";
        return false;
    }

    public bool CanManageSaas() => currentUser.IsGlobalAdmin();
    public bool CanAccessModule(string moduleCode) => IsModuleEnabled(moduleCode) && HasPermission(moduleCode, "VER");
    public bool CanAccessFeature(string featureCode) => IsFeatureEnabled(featureCode) && HasPermission(featureCode, "USAR");
    public bool IsModuleEnabled(string moduleCode) => currentUser.IsGlobalAdmin() || !string.Equals((moduleCode ?? string.Empty).Trim(), "BI_AVANCADO", StringComparison.OrdinalIgnoreCase);
    public bool IsFeatureEnabled(string featureCode) => currentUser.IsGlobalAdmin() || !string.Equals((featureCode ?? string.Empty).Trim(), "WHITE_LABEL_AVANCADO", StringComparison.OrdinalIgnoreCase);
    public bool CanAccessTenant(Guid tenantId) => currentUser.IsGlobalAdmin() || (currentUser.TenantId.HasValue && currentUser.TenantId.Value == tenantId);
    public bool CanAccessCliente(Guid clienteId) => currentUser.IsGlobalAdmin() || (currentUser.ClienteId.HasValue && currentUser.ClienteId.Value == clienteId);
    public bool CanSwitchTenant() => currentUser.IsGlobalAdmin();
    public bool CanManageUsers() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanManageBilling() => currentUser.IsGlobalAdmin() || currentUser.HasRole(RolesConstants.Financeiro) || currentUser.IsTenantAdmin();
    public bool CanManageWhiteLabel() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanViewSensitiveData() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Auditor);
    public bool CanAccessAdminArea() => currentUser.IsGlobalAdmin();
    public bool CanAccessClientPortal() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanAccessPartnerPortal() => currentUser.IsGlobalAdmin() || currentUser.IsPartner();
    public bool CanAccessMedicalArea() => currentUser.IsGlobalAdmin() || currentUser.IsDoctor() || currentUser.HasRole(RolesConstants.Triagem) || currentUser.HasRole(RolesConstants.Recepcao) || currentUser.HasRole(RolesConstants.AdministradorClinica);
    public bool CanAccessFinancialArea() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Financeiro) || currentUser.HasRole(RolesConstants.FinanceiroClinica) || currentUser.HasRole(RolesConstants.FaturamentoConvenio) || currentUser.HasRole(RolesConstants.AdministradorClinica);

    public Task RegistrarTrocaContextoAsync(Guid tenantId, string motivo, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Troca de contexto API. Usuario:{UsuarioId} TenantDestino:{TenantId} Motivo:{Motivo} Permitido:{Permitido}", currentUser.UserId, tenantId, motivo, CanSwitchTenant());
        return Task.CompletedTask;
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
}
