using Xunit;

public sealed class V112HomologationFlowTests
{
    [Fact]
    public void V112_Controller_Expose_Required_Functional_Endpoints()
    {
        var root = FindRoot();
        var controller = File.ReadAllText(Path.Combine(root, "backend", "PlantaoPro.Api", "Controllers", "V112HomologationController.cs"));
        foreach (var endpoint in new[] { "api/customers", "api/products", "api/inventory/entries", "api/orders/{id:guid}/confirm", "api/tasks/{id:guid}/complete", "api/billing/invoices/from-order/{orderId:guid}", "api/billing/titles/{titleId:guid}/fake-boleto", "api/outbox/{id:guid}/process", "api/templates/{id}/install", "api/demo/run-all", "api/homologation/status", "api/validation/worker/status" })
            Assert.Contains(endpoint, controller);
    }

    [Fact]
    public void V112_Smoke_Scripts_And_Docs_Are_Present()
    {
        var root = FindRoot();
        Assert.True(File.Exists(Path.Combine(root, "scripts", "smoke-test-v112.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "scripts", "smoke-test-v112.cmd")));
        Assert.True(File.Exists(Path.Combine(root, "docs", "v1.12-build-test-report.md")));
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "backend"))) dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }
}
