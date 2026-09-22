namespace PlantaoPro.Tests;

public class GenericClientOperationalContractTests
{
    [Fact]
    public void Onboarding_MustCreateGenericTenant_WithRequiredModulesAndCanonicalRoles()
    {
        var onboardingService = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "OnboardingService.cs"));

        // Generates dedicated tenant_id and isolates records
        Assert.Contains("var tenantId = Guid.NewGuid();", onboardingService);
        Assert.Contains("var clienteId = Guid.NewGuid();", onboardingService);
        Assert.Contains("INSERT INTO plantaopro.tenants", onboardingService);
        Assert.Contains("INSERT INTO plantaopro.clientes", onboardingService);
        Assert.Contains("INSERT INTO plantaopro.unidades", onboardingService);
        Assert.Contains("INSERT INTO plantaopro.hospitais", onboardingService);

        // Explicitly contracts initial operational modules: ESCALAS, EXECUCAO, CONFERENCIA
        Assert.Contains("\"ESCALAS\"", onboardingService);
        Assert.Contains("\"EXECUCAO\"", onboardingService);
        Assert.Contains("\"CONFERENCIA\"", onboardingService);
        Assert.Contains("INSERT INTO plantaopro.tenant_modulos", onboardingService);

        // Assigns canonical profiles ADMINISTRADOR_CLIENTE and ADMINISTRADOR
        Assert.Contains("\"ADMINISTRADOR_CLIENTE\"", onboardingService);
        Assert.Contains("\"ADMINISTRADOR\"", onboardingService);

        // Password must be hashed via BCrypt
        Assert.Contains("PasswordHashService.Hash", onboardingService);
    }

    [Fact]
    public void PlantaoAndEscala_MustEnforceTenantIsolation()
    {
        var plantoesController = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "PlantoesController.cs"));
        var escalasController = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "EscalasController.cs"));
        var dataServices = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Data.cs"));

        // PlantoesController passes tenant context from ICurrentUserService
        Assert.Contains("ICurrentUserService currentUser", plantoesController);
        Assert.Contains("service.GetAllAsync(filter, currentUser.TenantId, currentUser.ClienteId)", plantoesController);
        Assert.Contains("service.GetByIdAsync(id, currentUser.TenantId, currentUser.ClienteId)", plantoesController);
        Assert.Contains("service.CreateAsync(req, uid", plantoesController);

        // EscalasController passes tenant, client and doctor identity
        Assert.Contains("ICurrentUserService currentUser", escalasController);
        Assert.Contains("service.ListarAsync(f, currentUser.TenantId, currentUser.ClienteId, currentUser.UserId, currentUser.IsDoctor())", escalasController);
        Assert.Contains("service.GetByIdAsync(id, currentUser.TenantId, currentUser.ClienteId)", escalasController);

        // Data queries apply strict tenant isolation
        Assert.Contains("(p.tenant_id = @tenantId or p.cliente_id = @tenantId)", dataServices);
        Assert.Contains("(e.tenant_id = @tenantId or e.cliente_id = @tenantId or pl.tenant_id = @tenantId or pl.cliente_id = @tenantId)", dataServices);
    }

    [Fact]
    public void AlertEngine_MustEvaluateOperationalRulesAndPlatformHealth()
    {
        var alertService = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Operation360", "Notifications", "AlertRuleService.cs"));
        var notificacoesController = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "NotificacoesController.cs"));

        // Interface definition
        Assert.Contains("Task<int> EvaluateAsync(Guid tenantId, CancellationToken ct);", alertService);
        Assert.Contains("Task<int> EvaluatePlatformAsync(Guid globalAdminUserId, CancellationToken ct);", alertService);

        // Operational rules evaluation
        Assert.Contains("PLANTAO_SEM_PROFISSIONAL", alertService);
        Assert.Contains("CHECKIN_PENDENTE", alertService);
        Assert.Contains("PLANTAO_AGUARDANDO_CONFIRMACAO", alertService);
        Assert.Contains("OCORRENCIA_PLANTAO", alertService);
        Assert.Contains("FECHAMENTO_FINANCEIRO_PENDENTE", alertService);

        // Realtime notification broadcast
        Assert.Contains("IOperationRealtimePublisher realtime", alertService);
        Assert.Contains("PublishNotificationAsync", alertService);

        // Platform-level evaluation for Superadmin without tenant mixing
        Assert.Contains("EvaluatePlatformAsync", notificacoesController);
        Assert.Contains("user.IsGlobalAdmin() && !tenant.HasValue", notificacoesController);
    }

    [Fact]
    public void UI_MustUseNeutralCopy_AndNeverCoupleWithSantaCasa()
    {
        var meuDiaView = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "MeuDia", "Index.cshtml"));
        var commandCenterView = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "CommandCenter", "Index.cshtml"));

        // No hardcoded "Santa Casa" in operational dashboard views
        Assert.DoesNotContain("Santa Casa", meuDiaView, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Santa Casa", commandCenterView, StringComparison.OrdinalIgnoreCase);

        // Doctor links must navigate to MinhaAgenda journeys, not management-only controllers
        Assert.Contains("asp-controller=\"MinhaAgenda\" asp-action=\"Index\"", meuDiaView);
        Assert.Contains("asp-controller=\"MinhaAgenda\" asp-action=\"Presencas\"", meuDiaView);
        Assert.Contains("asp-controller=\"MinhaAgenda\" asp-action=\"MeusPagamentos\"", meuDiaView);

        // Contextual alert banners must be present
        Assert.Contains("pp-alert-banner", meuDiaView);
        Assert.Contains("pp-alert-banner", commandCenterView);
    }

    [Fact]
    public void CriticalActions_MustRequireConfirmationModals()
    {
        var escalasDetails = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Escalas", "Details.cshtml"));
        var presencas = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "MinhaAgenda", "Presencas.cshtml"));
        var conferencia = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "ConferenciaExecucao", "Index.cshtml"));

        // Irreversible actions use accessible modal attributes (data-confirm="true")
        Assert.Contains("data-confirm=\"true\"", escalasDetails);
        Assert.Contains("asp-action=\"Cancelar\"", escalasDetails);
        Assert.Contains("asp-action=\"Recusar\"", escalasDetails);

        Assert.Contains("data-confirm=\"true\"", presencas);
        Assert.Contains("asp-action=\"CancelarCorrecao\"", presencas);

        Assert.Contains("data-confirm=\"true\"", conferencia);
        Assert.Contains("value=\"RecusarCorrecao\"", conferencia);
    }

    [Fact]
    public void CanonicalRoles_MustMatchProductSpecification()
    {
        var apiRoles = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "RolesConstants.cs"));
        var webRoles = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Security", "RolesConstants.cs"));

        string[] requiredRoles =
        {
            "ADMINISTRADOR_GLOBAL",
            "ADMINISTRADOR_CLIENTE",
            "ADMINISTRADOR",
            "DIRETOR",
            "COORDENACAO",
            "OPERADOR",
            "MEDICO",
            "FINANCEIRO",
            "HOSPITAL",
            "SUPORTE",
            "AUDITOR"
        };

        foreach (var role in requiredRoles)
        {
            Assert.Contains($"\"{role}\"", apiRoles);
            Assert.Contains($"\"{role}\"", webRoles);
        }
    }
}
