namespace PlantaoPro.Tests;

public sealed class V112HomologationFlowTests
{
    [Fact]
    public void V112_Controller_Expose_Required_Functional_Endpoints()
    {
        var controller = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "V112HomologationController.cs"));
        foreach (var endpoint in new[] { "api/v113/customers", "api/v113/products", "api/v113/inventory/movements", "api/v113/orders/{id:guid}/confirm", "api/v113/tasks/{id:guid}/complete", "api/v113/billing/invoices/from-order/{orderId:guid}", "api/v113/billing/titles/{titleId:guid}/demo-boleto", "api/v113/outbox/{id:guid}/process", "api/v113/templates/{id}/install", "api/v113/homologation/status", "api/v113/validation/worker/status" })
            Assert.Contains(endpoint, controller);
        var decisions = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, "docs", "releases", "v1.24.3", "contract-decisions.md"));
        Assert.Contains("api/customers", decisions);
        Assert.Contains("api/clientes", decisions);
    }

    [Fact]
    public void V112_Smoke_Scripts_And_Docs_Are_Present()
    {
        var root = RepositoryPathResolver.RepoRoot;
        Assert.True(File.Exists(Path.Combine(root, "scripts", "smoke-test-v112.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "scripts", "smoke-test-v112.cmd")));
        Assert.True(File.Exists(Path.Combine(root, "docs", "v1.12-build-test-report.md")));
    }
}
