namespace PlantaoPro.Tests;

public class V2153CockpitSaasContractTests
{
    [Fact]
    public void AdminSaas_DeveUsarCockpitRealComBillingModulosAlertasEGuia()
    {
        var controller = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Controllers", "CommercialDemoWebController.cs"));
        var view = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "AdminSaas", "Index.cshtml"));
        var model = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Models", "SaasWebViewModels.cs"));

        Assert.Contains("public sealed class AdminSaasController : BaseWebController", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/saas-dashboard/resumo", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/faturamento-saas/resumo", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/saas-dashboard/alertas", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("api/modulos", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AdminSaasCockpitViewModel", model, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cockpit global", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Módulos mais contratados", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Como usar esta tela", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Login_DeClienteBloqueado_DeveAutenticarComStatusEAuditarBloqueioOperacional()
    {
        var apiAuth = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Data.cs"));
        var apiModels = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Models.cs"));
        var webAccount = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Controllers", "AccountController.cs"));

        Assert.DoesNotContain("TENANT_INACTIVE", apiAuth, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SUCCESS_TENANT_BLOCKED", apiAuth, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Login realizado com restrição operacional", apiAuth, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cliente_status", apiAuth, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ClienteStatus", apiModels, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cliente_status", webAccount, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WebGuard_DeveExigirModuloContratadoEBloquearOperacaoDeClienteSuspenso()
    {
        var guard = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Services", "Security", "SaasRouteGuardFilter.cs"));
        var accessDenied = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Account", "AccessDenied.cshtml"));

        Assert.Contains("IModuleAccessService", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modules.IsModuleEnabled(module)", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BlockedTenantAllowedControllers", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CLIENTE_BLOQUEADO", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MODULO_NAO_CONTRATADO", guard, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ver minhas faturas", accessDenied, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Solicitar contratação", accessDenied, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FaturamentoSaasApi_DeveIsolarFaturasPorClienteEManterGeracaoGlobal()
    {
        var controller = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "SaasCommercialController.cs"));

        Assert.Contains("ICurrentUserService _currentUser", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BillingScope", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@global or f.cliente_id=@scopedClienteId", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("@global or f.cliente_id=@clienteId", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("if (!_currentUser.IsGlobalAdmin()) return Forbid();", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("faturas/{id:guid}/marcar-paga", controller, StringComparison.OrdinalIgnoreCase);
    }
}
