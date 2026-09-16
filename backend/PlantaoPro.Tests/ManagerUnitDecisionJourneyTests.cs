namespace PlantaoPro.Tests;

public sealed class ManagerUnitDecisionJourneyTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void SubstitutionDecisionIsExplicitVersionedAndConditional()
    {
        var service = Read("backend/PlantaoPro.Api/Fase4OperationalServices.cs");
        Assert.Contains("IsolationLevel.Serializable", service);
        Assert.Contains("status='SOLICITADA' and versao=@versaoEsperada", service);
        Assert.Contains("if (changed != 1)", service);
        Assert.Contains("A solicitação já foi decidida", service);
        Assert.Contains("InserirHistoricoSubstituicaoAsync", service);
    }

    [Fact]
    public void DecisionRevalidatesPermissionAndUnitAfterPageWasOpened()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/Fase4OperationalController.cs");
        Assert.Contains("ValidarPermissaoAsync", controller);
        Assert.Contains("PodeAcessarHospitalAsync", controller);
        Assert.Contains("ObterSubstituicaoAsync(id, ctx.ClienteId)", controller);
    }

    [Fact]
    public void ManagerQueueUsesRealFlowsAndPreservesReturnContext()
    {
        var controller = Read("backend/PlantaoPro.Web/Controllers/CentralEscalaController.cs");
        var queue = Read("backend/PlantaoPro.Web/Views/CentralEscala/Substituicoes.cshtml");
        Assert.Contains("api/substituicoes", controller);
        Assert.Contains("returnUrl", controller);
        Assert.Contains("data de envio", queue);
        Assert.Contains("ConferenciaExecucao", queue);
        Assert.DoesNotContain("href=\"#\"", queue);
    }
}
