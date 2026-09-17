namespace PlantaoPro.Tests;

public sealed class CentralOperacionalJourneyContractTests
{
    private static string Root() => RepositoryPathResolver.ApiRoot;
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root() }.Concat(parts).ToArray()));

    [Fact]
    public void Central_DeveExporOrigemAcaoEIdentidadeEstavel()
    {
        var models = Read("Operation360", "WorkItems", "WorkItemModels.cs");
        var service = Read("Operation360", "WorkItems", "WorkItemService.cs");
        Assert.Contains("StableKey", models);
        Assert.Contains("OriginUrl", models);
        Assert.Contains("NextAction", models);
        Assert.Contains("GroupBy", service);
        Assert.Contains("CanAccessModule", service);
    }

    [Fact]
    public void Central_NaoDeveOferecerConclusaoGenericaDaPendencia()
    {
        var viewRoot = Path.Combine(Directory.GetParent(Root())!.FullName, "PlantaoPro.Web", "Views", "MinhaCentral");
        var view = File.ReadAllText(Path.Combine(viewRoot, "Index.cshtml"));
        var card = File.ReadAllText(Path.Combine(viewRoot, "_WorkItemCard.cshtml"));
        Assert.DoesNotContain("Mover para", view + card, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Concluir", view + card, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("registro de origem", card, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sem prazo definido", card, StringComparison.Ordinal);
    }

    [Fact]
    public void Central_DeveDistinguirContextoGlobalEIndisponibilidade()
    {
        var service = Read("Operation360", "WorkItems", "WorkItemService.cs");
        var webRoot = Path.Combine(Directory.GetParent(Root())!.FullName, "PlantaoPro.Web");
        var view = File.ReadAllText(Path.Combine(webRoot, "Views", "MinhaCentral", "Index.cshtml"));
        var webService = File.ReadAllText(Path.Combine(webRoot, "Services", "MinhaCentralWebService.cs"));
        Assert.Contains("GlobalView", service);
        Assert.Contains("Visão global MNSOFT", service);
        Assert.Contains("Sua Central não carregou", view);
        Assert.Contains("Não conseguimos carregar", webService);
    }
}
