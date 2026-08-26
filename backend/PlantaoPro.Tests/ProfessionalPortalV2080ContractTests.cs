namespace PlantaoPro.Tests;

public sealed class ProfessionalPortalV2080ContractTests
{
    private static string Root => RepositoryPathResolver.ResolveRepositoryRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Portal_exposes_dashboard_finance_and_presence_without_manual_ids()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/MedicoAreaController.cs");
        var view = Read("backend/PlantaoPro.Web/Views/MinhaAgenda/Index.cshtml");
        Assert.Contains("[HttpGet(\"meu-dia\")]", controller);
        Assert.Contains("[HttpGet(\"presencas\")]", controller);
        Assert.Contains("Meus pagamentos", view);
        Assert.Contains("Informar disponibilidade", view);
        Assert.DoesNotContain("name=\"medicoId\"", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Critical_queries_scope_professional_and_tenant_and_presence_is_idempotent()
    {
        var service = Read("backend/PlantaoPro.Api/ProfessionalPortalService.cs");
        Assert.Contains("e.medico_id=@MedicoId", service);
        Assert.Contains("p.cliente_id=@ClienteId", service);
        Assert.Contains("on conflict(tenant_id,escala_id) do nothing", service);
        Assert.Contains("checkout_em is null", service);
        Assert.Contains("audit.RegistrarAsync", service);
    }

    [Fact]
    public void Mobile_layout_uses_cards_and_visible_focus()
    {
        var css = Read("backend/PlantaoPro.Web/wwwroot/css/pages/professional-portal.css");
        var opportunities = Read("backend/PlantaoPro.Web/Views/MinhaAgenda/PlantoesDisponiveis.cshtml");
        Assert.Contains("@media(max-width:800px)", css);
        Assert.Contains(":focus-visible", css);
        Assert.DoesNotContain("<table", opportunities, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"#\"", opportunities, StringComparison.OrdinalIgnoreCase);
    }
}
