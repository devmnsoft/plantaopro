using PlantaoPro.Web.Services;

namespace PlantaoPro.Tests;

public sealed class V1220ProductCatalogContractTests
{
    [Fact]
    public void Catalog_Uses_Friendly_Unique_Routes()
    {
        var catalog = new FeatureCatalogService();

        Assert.All(catalog.Features, feature =>
        {
            Assert.False(string.IsNullOrWhiteSpace(feature.Name));
            Assert.False(string.IsNullOrWhiteSpace(feature.Permission));
            Assert.DoesNotContain("Controller", feature.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("api/", feature.Description, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(catalog.Features.Count, catalog.Features.Select(feature => $"{feature.Controller}/{feature.Action}").Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Navigation_Has_At_Most_Twelve_Items_Per_Profile()
    {
        var catalog = new FeatureCatalogService();

        Assert.All(catalog.Navigation.GroupBy(item => item.Profile), group => Assert.InRange(group.Count(), 1, 12));
    }

    [Fact]
    public void Page_Context_Does_Not_Leak_Controller_Name()
    {
        var service = new PageContextService(new FeatureCatalogService());

        var context = service.Resolve("Agendamentos", "CheckIn", "Clínica Central", "Paciente selecionado");

        Assert.Equal("Check-in", context.Title);
        Assert.DoesNotContain("Agendamentos", context.Breadcrumb);
        Assert.Equal("Clínica Central", context.TenantName);
    }
}
