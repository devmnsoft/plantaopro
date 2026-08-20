namespace PlantaoPro.Tests;

public sealed class V186FinancialOperationalContractTests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, relative));

    [Fact]
    public void Payment_actions_have_canonical_routes_typed_contracts_and_persistence()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/PagamentosController.cs");
        var service = Read("backend/PlantaoPro.Api/Data.cs");
        var models = Read("backend/PlantaoPro.Api/Models.cs");
        Assert.Contains("{id:guid}/marcar-pago", controller);
        Assert.Contains("{id:guid}/contestar", controller);
        Assert.Contains("MarcarPagamentoPagoRequest", models);
        Assert.Contains("ContestarPagamentoRequest", models);
        Assert.Contains("PagamentoActionResponse", models);
        Assert.Contains("status='pago'", service);
        Assert.Contains("status='contestado'", service);
        Assert.Contains("await tx.CommitAsync()", service);
        Assert.Contains("for update", service);
    }

    [Fact]
    public void Payment_rules_preserve_real_value_and_require_reason()
    {
        var service = Read("backend/PlantaoPro.Api/Data.cs");
        var view = Read("backend/PlantaoPro.Web/Views/Financeiro/Details.cshtml");
        Assert.Contains("pg.ValorPrevisto <= 0", service);
        Assert.Contains("string.IsNullOrWhiteSpace(req.Motivo)", service);
        Assert.Contains("Somente pagamento pendente pode ser contestado", service);
        Assert.Contains("name=\"motivo\"", view);
        Assert.Contains("required", view);
    }

    [Fact]
    public void Unsupported_closing_and_financial_actions_remain_explicitly_disabled()
    {
        var closing = Read("backend/PlantaoPro.Web/Views/OperacaoPremium/Fechamentos.cshtml");
        Assert.Contains("disabled", closing);
        Assert.DoesNotContain("href=\"#\"", closing);
    }

    [Fact]
    public void Business_client_handles_contract_errors_without_unsafe_rendering()
    {
        var script = Read("backend/PlantaoPro.Web/wwwroot/js/components/business-actions.js");
        foreach (var status in new[] { "400", "401", "403", "404", "409", "422" }) Assert.Contains($"[{status},", script);
        Assert.DoesNotContain("innerHTML", script);
        Assert.DoesNotMatch(@"(?<![\w.])(?:window\.)?confirm\s*\(", script);
        Assert.Contains("url.origin !== window.location.origin", script);
        Assert.Contains("event.target instanceof Element", script);
        Assert.True(script.IndexOf("try {", StringComparison.Ordinal) < script.IndexOf("JSON.parse", StringComparison.Ordinal));
    }

    [Fact]
    public void Unsupported_canonical_routes_are_documented_instead_of_faked()
    {
        var controllers = Directory.GetFiles(Path.Combine(RepositoryPathResolver.RepoRoot, "backend/PlantaoPro.Api/Controllers"), "*.cs")
            .Select(File.ReadAllText);
        var source = string.Join('\n', controllers);
        foreach (var unsupportedRoute in new[] { "api/fechamentos", "{id:guid}/gerar-financeiro", "{id:guid}/gerar-pagamento" })
            Assert.DoesNotContain(unsupportedRoute, source);
        var paymentsController = Read("backend/PlantaoPro.Api/Controllers/PagamentosController.cs");
        Assert.DoesNotContain("{id:guid}/resolver-contestacao", paymentsController);

        var pending = Read("artifacts/ui-audit/v186-endpoints-pendentes.md");
        Assert.Contains("não existe agregado/repository real", pending);
        Assert.Contains("não há campos/workflow de resolução", pending);
    }
}
