namespace PlantaoPro.Tests;

public class FechamentoHomologacaoFinalContractTests
{
    [Fact]
    public void LgpdWeb_DeveTerTelaDedicadaDeMinhaPrivacidadeETratamentoSeguroDeExportacao()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var controller = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Controllers", "LgpdController.cs"));
        var view = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Lgpd", "MinhaPrivacidade.cshtml"));

        Assert.Contains("public IActionResult MinhaPrivacidade() => View();", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Erro ao solicitar exportação LGPD via Web", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Central do titular", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("asp-action=\"Consentimentos\"", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("asp-action=\"ExportarDados\"", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("asp-action=\"Eventos\"", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Observabilidade_DeveLimitarConsultasEMapearStringsComCoalesce()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var controller = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "ObservabilidadeController.cs"));

        Assert.Contains("NormalizarLimit", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Math.Clamp(limit, 1, 100)", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("coalesce(endpoint,'')", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count(*)::bigint as Total", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("limit @limit", controller, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocumentacaoFinal_DeveConterChecklistsDeHomologacaoEDeploy()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var homologacao = File.ReadAllText(Path.Combine(raiz, "docs", "homologacao", "checklist-homologacao-final.md"));
        var deploy = File.ReadAllText(Path.Combine(raiz, "docs", "deploy", "checklist-deploy-homologacao.md"));

        Assert.Contains("Build API verde", homologacao, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sem binários, secrets, tokens", homologacao, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LGPD", homologacao, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mobile/API", homologacao, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ConnectionStrings:Default", deploy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Plano de rollback", deploy, StringComparison.OrdinalIgnoreCase);
    }
}
