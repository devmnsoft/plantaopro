using PlantaoPro.Web.Services;

namespace PlantaoPro.Tests;

public sealed class R4C11BreadcrumbTrailTests
{
    private static BreadcrumbService NewService() => new(new FeatureCatalogService());

    [Fact]
    public void Feature_Trail_Keeps_Friendly_Root_And_No_Controller_Leak()
    {
        var service = NewService();

        var trail = service.ResolveTrail("Agendamentos", "CheckIn", "Check-in");

        Assert.Equal(new[] { "Início", "Atendimento", "Check-in" }, trail.Select(t => t.Label).ToArray());
        Assert.Equal("/Home/Dashboard", trail[0].Url);
        Assert.All(trail.Skip(1), t => Assert.Null(t.Url));
        Assert.DoesNotContain("Agendamentos", trail.Select(t => t.Label));
    }

    [Fact]
    public void Area_Trail_Adds_Prefix_And_Title_Without_Raw_Controller()
    {
        var service = NewService();

        var trail = service.ResolveTrail("Administrativo360", "Estoque", "Estoque e Lotes");

        Assert.Equal(new[] { "Início", "Administrativo 360", "Estoque e Lotes" }, trail.Select(t => t.Label).ToArray());
        Assert.Equal("/Home/Dashboard", trail[0].Url);
        Assert.All(trail.Skip(1), t => Assert.Null(t.Url));
        Assert.DoesNotContain("Administrativo360", trail.Select(t => t.Label));
    }

    [Fact]
    public void Dynamic_Leaf_Wins_For_Detail_Pages()
    {
        var service = NewService();

        var trail = service.ResolveTrail(
            "Administrativo360", "CotacoesDetalhe", "Detalhes da cotação",
            breadcrumbLabel: "Cotações", dynamicLeaf: "COT-2026-0001");

        Assert.Equal("COT-2026-0001", trail[^1].Label);
        Assert.DoesNotContain("Detalhes da cotação", trail.Select(t => t.Label));
        Assert.DoesNotContain("Cotações", trail.Select(t => t.Label));
    }

    [Fact]
    public void Breadcrumb_Label_Is_PREFERRED_Over_Title()
    {
        var service = NewService();

        var trail = service.ResolveTrail("Administrativo360", "Recebimentos", "Título longo e redundante", breadcrumbLabel: "Recebimentos");

        Assert.Equal("Recebimentos", trail[^1].Label);
    }

    [Fact]
    public void Homologacao_Area_Uses_Web_Controller_Key()
    {
        var service = NewService();

        var trail = service.ResolveTrail("V112Web", "Dashboard", "Dashboard");

        Assert.Equal(new[] { "Início", "Homologação", "Dashboard" }, trail.Select(t => t.Label).ToArray());
        Assert.DoesNotContain("V112Web", trail.Select(t => t.Label));
    }

    [Fact]
    public void Unknown_Controller_Falls_Back_To_Inicio_And_Title()
    {
        var service = NewService();

        var trail = service.ResolveTrail("AlgumArea", "Index", "Página de exemplo");

        Assert.Equal(new[] { "Início", "Página de exemplo" }, trail.Select(t => t.Label).ToArray());
    }
}
