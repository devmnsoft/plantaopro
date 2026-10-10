using System.Security.Claims;

namespace PlantaoPro.Api;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
    Guid? ClienteId { get; }
    Guid? SessionId { get; }
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
    public Guid? SessionId => ReadGuid("session_id");

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
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<ModulePermissionService> logger;

    public ModulePermissionService(ICurrentUserService currentUser, IHttpContextAccessor httpContextAccessor, ILogger<ModulePermissionService> logger)
    {
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public bool HasPermission(string module, string action)
    {
        if (!currentUser.IsAuthenticated()) return false;
        if (currentUser.IsGlobalAdmin()) return true;

        var code = Normalize(module);
        var actionCode = Normalize(action);

        // R6-BlocoA item 1 (complemento): ÚNICO caminho de decisão — claim de módulo
        // efetivo (função canônica) + permissão por ação do usuário. Os fallbacks por
        // papel pré-v2149 foram removidos: todo JWT emitido pela API (Data.cs) carrega
        // access_catalog_version=v2149, e os conjuntos papel->módulo não distinguem
        // contratação de permissão (adendo D.3 do inventário).
        if (CommonModules.Contains(code)) return true;
        var tenantAdministration = TenantAdministrationModules.Contains(code) && currentUser.IsTenantAdmin();
        if (!tenantAdministration && !HasClaim("module", code)) return false;
        return tenantAdministration || HasClaim("permission", $"{code}.{actionCode}") || HasClaim("permission", $"{code}.*");
    }

    public bool CanManageSaas() => currentUser.IsGlobalAdmin();
    public bool CanAccessModule(string moduleCode) => IsModuleEnabled(moduleCode) && HasPermission(moduleCode, "VER");
    public bool CanAccessFeature(string featureCode) => IsFeatureEnabled(featureCode) && HasPermission(featureCode, "USAR");
    public bool IsModuleEnabled(string moduleCode)
    {
        if (currentUser.IsGlobalAdmin()) return true;
        var code = Normalize(moduleCode);
        if (CommonModules.Contains(code) || TenantAdministrationModules.Contains(code)) return true;
        // R6-BlocoA item 1 (complemento): ramo pré-v2149 removido — todos os JWTs atuais
        // carregam access_catalog_version=v2149 (Data.cs emite o claim sempre).
        return HasClaim("module", code);
    }
    public bool IsFeatureEnabled(string featureCode) => IsModuleEnabled(featureCode);
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

    private bool HasClaim(string type, string value)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        if (principal is null) return false;
        var expected = NormalizeAccessCode(value);
        return principal.FindAll(type).Select(claim => NormalizeAccessCode(claim.Value))
            .Any(candidate => candidate == "*" || string.Equals(candidate, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly HashSet<string> CommonModules = new(StringComparer.OrdinalIgnoreCase) { "MEU_DIA", "AJUDA", "LGPD", "CONTA", "TREINAMENTO" };
    private static readonly HashSet<string> TenantAdministrationModules = new(StringComparer.OrdinalIgnoreCase) { "USUARIOS", "PERFIS", "PERMISSOES", "CONFIGURACOES", "SEGURANCA", "ASSINATURAS", "CLIENTE_PORTAL", "ONBOARDING" }; // R5-D9: ONBOARDING = fluxo core do tenant (jornada so materializa etapas de modulo com assinatura vigente)
    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static string NormalizeAccessCode(string? value) => Normalize(value).Replace(':', '.');
}
