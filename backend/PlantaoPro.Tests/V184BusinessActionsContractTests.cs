namespace PlantaoPro.Tests;

public sealed class V184BusinessActionsContractTests
{
    [Fact]
    public void Triage_finalize_is_endpoint_backed_and_only_enabled_for_persisted_records()
    {
        var root = RepositoryPathResolver.RepoRoot;
        var controller = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs"));
        var webController = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs"));
        var view = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Web/Views/Saude360/Formulario.cshtml"));

        Assert.Contains("{id:guid}/finalizar-tipado", controller);
        Assert.Contains("FinalizarTriagemAsync", webController);
        Assert.Contains("asp-action=\"Finalizar\"", view);
        Assert.Contains("Salve a triagem para obter um identificador real", view);
        Assert.DoesNotContain("A API atual salva a triagem, mas não expõe", view);
    }
}
