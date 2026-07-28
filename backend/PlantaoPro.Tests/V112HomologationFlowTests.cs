namespace PlantaoPro.Tests;

public sealed class V112HomologationFlowTests
{
    [Fact]
    public void V112_Controller_Expose_Required_Functional_Endpoints()
    {
        var controller = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "V112HomologationController.cs"));
        foreach (var endpoint in new[] { "api/customers", "api/products", "api/inventory/entries", "api/orders/{id:guid}/confirm", "api/tasks/{id:guid}/complete", "api/billing/invoices/from-order/{orderId:guid}", "api/billing/titles/{titleId:guid}/fake-boleto", "api/outbox/{id:guid}/process", "api/templates/{id}/install", "api/demo/run-all", "api/homologation/status", "api/validation/worker/status" })
            Assert.Contains(endpoint, controller);
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
