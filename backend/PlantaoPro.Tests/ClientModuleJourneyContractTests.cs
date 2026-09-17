namespace PlantaoPro.Tests;

public sealed class ClientModuleJourneyContractTests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, relative));

    [Fact]
    public void Catalogo_DeveSepararContratoSolicitacaoEOfertaPersistida()
    {
        var service = Read("backend/PlantaoPro.Api/ModuleContractingService.cs");
        var view = Read("backend/PlantaoPro.Web/Views/MinhaAssinatura/Modulos.cshtml");

        Assert.Contains("SOLICITACAO_PENDENTE", service);
        Assert.Contains("tm.preco_contratado", service);
        Assert.Contains("m.periodicidade", service);
        Assert.Contains("Solicitações em análise", view);
        Assert.Contains("ausência de preço não significa gratuidade", view);
        Assert.Contains("não ativa funcionalidades imediatamente", view);
    }

    [Fact]
    public void Confirmacao_DeveRevalidarCondicoesESerializarSolicitacoesDoTenant()
    {
        var service = Read("backend/PlantaoPro.Api/ModuleContractingService.cs");

        Assert.Contains("ReviewAsync(request, ct)", service);
        Assert.Contains("FixedEquals(review.ConditionsVersion", service);
        Assert.Contains("pg_advisory_xact_lock", service);
        Assert.Contains("Já existe solicitação pendente", service);
        Assert.Contains("conflictingContracts.Any()", service);
        Assert.Contains("do nothing", service);
    }

    [Fact]
    public void Portal_NaoDeveAceitarPrecoTenantOuStatusDoNavegador()
    {
        var controller = Read("backend/PlantaoPro.Web/Controllers/MinhaAssinaturaController.cs");
        var model = Read("backend/PlantaoPro.Web/Models/ClientModulesViewModel.cs");

        Assert.Contains("ClientModuleConfirmInput input", controller);
        Assert.DoesNotContain("public decimal? Preco { get; set; }", model.Split("public sealed class ClientModuleConfirmInput", StringSplitOptions.None)[1].Split("public sealed class ClientModuleReviewViewModel", StringSplitOptions.None)[0]);
        Assert.DoesNotContain("TenantId", model.Split("public sealed class ClientModuleConfirmInput", StringSplitOptions.None)[1].Split("public sealed class ClientModuleReviewViewModel", StringSplitOptions.None)[0]);
        Assert.DoesNotContain("Status", model.Split("public sealed class ClientModuleConfirmInput", StringSplitOptions.None)[1].Split("public sealed class ClientModuleReviewViewModel", StringSplitOptions.None)[0]);
    }

    [Fact]
    public void LoginEValidadorDeRotasPermanecemProtegidos()
    {
        var globalForm = Read("backend/PlantaoPro.Web/wwwroot/js/form-experience.js");
        var login = Read("backend/PlantaoPro.Web/wwwroot/js/auth-login.js");
        var routeTest = Read("backend/PlantaoPro.Tests/ApiRouteUniquenessIntegrationTests.cs");

        Assert.Contains("data-submit-loading=\"manual\"", globalForm);
        Assert.Contains("event.defaultPrevented", globalForm);
        Assert.Contains("event.defaultPrevented", login);
        Assert.Contains("ApiRouteStartupValidator.NormalizePath", routeTest);
        Assert.Contains("api/medicos/me/disponibilidade", routeTest, StringComparison.OrdinalIgnoreCase);
    }
}
