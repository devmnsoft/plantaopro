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

        // R6-BlocoA item 1 (complemento): ÚNICO caminho de decisão — módulo efetivo
        // (claim emitido no login pela função canônica; o guard Web reconfirma por request
        // com a verificação live) + permissão por ação do usuário (grants de perfil).
        // Os fallbacks por papel pré-v2149 foram removidos: todo emissor atual de sessão
        // (login Data.cs, SessionClaimsBuilder, TestSigninController) carrega sempre
        // access_catalog_version=v2149, e os conjuntos papel->módulo antigos não
        // distinguem contratação de permissão — a raiz das 11 resoluções paralelas
        // documentadas no adendo D.3 do inventário.
        if (CommonModules.Contains(moduleCode)) return true;
        var isCoreAdministration = TenantAdministrationModules.Contains(moduleCode) && currentUser.IsTenantAdmin();
        var moduleEnabled = isCoreAdministration || HasAccessClaim("module", moduleCode);
        if (!moduleEnabled) return false;

        var requested = $"{moduleCode}.{actionCode}";
        return isCoreAdministration || HasAccessClaim("permission", requested) || HasAccessClaim("permission", $"{moduleCode}.*");
    }

    public bool CanManageSaas() => currentUser.IsGlobalAdmin();
    public bool CanManageUsers() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanManageBilling() => currentUser.IsGlobalAdmin() || currentUser.HasRole(RolesConstants.Financeiro) || currentUser.IsTenantAdmin();
    public bool CanManageWhiteLabel() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanViewSensitiveData() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Auditor);
    public bool CanAccessAdminArea() => currentUser.IsGlobalAdmin();
    public bool CanAccessClientPortal() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin();
    public bool CanAccessPartnerPortal() => currentUser.IsGlobalAdmin() || currentUser.IsPartner();
    public bool CanAccessMedicalArea() => currentUser.IsGlobalAdmin() || currentUser.IsDoctor() || currentUser.HasRole(RolesConstants.Triagem) || currentUser.HasRole(RolesConstants.Recepcao) || currentUser.HasRole(RolesConstants.AdministradorClinica);
    public bool CanAccessFinancialArea() => currentUser.IsGlobalAdmin() || currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Financeiro) || currentUser.HasRole(RolesConstants.FinanceiroClinica) || currentUser.HasRole(RolesConstants.FaturamentoConvenio) || currentUser.HasRole(RolesConstants.AdministradorClinica);

    private bool HasAccessClaim(string claimType, string expected)
    {
        IEnumerable<string> values;
        if (string.Equals(claimType, "permission", StringComparison.OrdinalIgnoreCase))
        {
            // P8: sessões novas trazem a claim combinada "permissions" (cookie <= ~4 KB);
            // cookies emitidos antes do fix ainda levam claims individuais "permission" —
            // mantemos o fallback até esses tickets expirarem.
            var combined = currentUser.User.FindFirst("permissions")?.Value;
            values = string.IsNullOrWhiteSpace(combined)
                ? currentUser.User.FindAll("permission").Select(claim => claim.Value)
                : combined.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        else
        {
            values = currentUser.User.FindAll(claimType).Select(claim => claim.Value);
        }
        return values
            .Select(value => NormalizeAccessCode(value))
            .Any(value => value == "*" || string.Equals(value, NormalizeAccessCode(expected), StringComparison.OrdinalIgnoreCase));
    }

    private static readonly HashSet<string> CommonModules = new(StringComparer.OrdinalIgnoreCase)
    {
        "MEU_DIA", "AJUDA", "LGPD", "CONTA", "TREINAMENTO"
    };

    private static readonly HashSet<string> TenantAdministrationModules = new(StringComparer.OrdinalIgnoreCase)
    {
        // R5-D9: ONBOARDING e fluxo core do SaaS (a jornada so materializa etapas de
        // modulo com assinatura canonica vigente); sem entrada propria em modulos_sistema,
        // o gestor de um tenant correto ficaria preso em MODULO_NAO_CONTRATADO.
        "USUARIOS", "PERFIS", "PERMISSOES", "CONFIGURACOES", "SEGURANCA", "ASSINATURAS", "CLIENTE_PORTAL", "ONBOARDING"
    };

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static string NormalizeAccessCode(string? value) => Normalize(value).Replace(':', '.');
}

public sealed class ModuleAccessService : IModuleAccessService
{
    private readonly IPermissionService permissions;
    private readonly ICurrentUserService currentUser;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor;

    public ModuleAccessService(IPermissionService permissions, ICurrentUserService currentUser, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        this.permissions = permissions;
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
    }

    public bool CanAccessModule(string moduleCode) => IsModuleEnabled(moduleCode) && permissions.HasPermission(moduleCode, "VER");
    public bool CanAccessFeature(string featureCode) => IsFeatureEnabled(featureCode) && permissions.HasPermission(featureCode, "USAR");
    public bool IsModuleEnabled(string moduleCode)
    {
        if (currentUser.IsGlobalAdmin()) return true;
        var normalized = Normalize(moduleCode);
        if (CoreModules.Contains(normalized)) return true;

        // R6-BlocoA item 1 (complemento): o ramo pré-v2149 ("tudo liberado menos
        // BI_AVANCADO") foi removido — não existe emissor atual de sessão sem o claim
        // access_catalog_version=v2149, e o bypass por ausência de catálogo conflitava
        // com a regra de não autorizar sem decisão de contratação válida.

        // R6-BlocoA item 1: verificação LIVE via função canônica. O guard (IAsyncActionFilter)
        // popula HttpContext.Items[CacheKey] com o conjunto efetivo por request (authoritative
        // quando disponível). Assim suspensão/expiração/revogação e restrição per-capacidade
        // refletem na hora, sem depender apenas de claims emitidos no login. Se a verificação
        // já ocorreu e FALHOU (valor null), degradamos aos claims do login (mesma fonte canônica
        // emitida no login) com janela documentada; sem estado de resolução (chamada fora do
        // guard) também cai nos claims.
        var items = httpContextAccessor.HttpContext?.Items;
        if (items != null && items.TryGetValue(EffectiveModuleResolver.CacheKey, out var resolved)
            && resolved is IReadOnlySet<string> set)
        {
            return set.Contains("*") || set.Contains(normalized);
        }

        return currentUser.User.FindAll("module")
            .Select(claim => Normalize(claim.Value))
            .Any(value => value == "*" || string.Equals(value, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public bool IsFeatureEnabled(string featureCode) => IsModuleEnabled(featureCode);

    /// <summary>Módulos que dispensam verificação de contratação (core/comum do tenant). Usado
    /// pelo guard para pular a resolução live sem custo nesses acessos.</summary>
    public static bool IsCoreOrCommonModule(string moduleCode) => CoreModules.Contains(Normalize(moduleCode));

    /// <summary>O usuário tem um claim de módulo para <paramref name="moduleCode"/> (ou "*")?
    /// O guard usa isto para só disparar a verificação LIVE quando o módulo foi contratado no
    /// login (e pode ter sido revogado/suspento desde então). Módulo nunca contratado nem
    /// precisa de consulta de rede: segue negado pelos claims.</summary>
    public static bool HasModuleClaim(System.Security.Claims.ClaimsPrincipal user, string moduleCode)
    {
        var normalized = Normalize(moduleCode);
        return user.FindAll("module").Select(c => Normalize(c.Value)).Any(v => v == "*" || v == normalized);
    }

    private static readonly HashSet<string> CoreModules = new(StringComparer.OrdinalIgnoreCase)
    {
        // R5-D9: ver TenantAdministrationModules - jornada de onboarding e core do tenant.
        "MEU_DIA", "AJUDA", "LGPD", "CONTA", "TREINAMENTO", "USUARIOS", "PERFIS",
        "PERMISSOES", "CONFIGURACOES", "SEGURANCA", "ASSINATURAS", "CLIENTE_PORTAL", "ONBOARDING"
    };

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
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
