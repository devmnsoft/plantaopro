using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

public interface IPageContextService
{
    PageContextViewModel Resolve(string controller, string action, string? tenantName = null, string? currentRecord = null);
}

public sealed class PageContextService : IPageContextService
{
    private readonly IFeatureCatalogService catalog;

    public PageContextService(IFeatureCatalogService catalog) => this.catalog = catalog;

    public PageContextViewModel Resolve(string controller, string action, string? tenantName = null, string? currentRecord = null)
    {
        var page = catalog.FindPage(controller, action);
        if (page is null)
        {
            return new PageContextViewModel("Página", "Consulte as informações e ações disponíveis.",
                new List<string> { "Início" }, string.Empty, string.Empty, "Voltar", tenantName, currentRecord);
        }

        return new PageContextViewModel(page.Title, page.Description, page.Breadcrumb, page.JourneyStep,
            page.PrimaryActionLabel, page.SecondaryActionLabel, tenantName, currentRecord);
    }
}
