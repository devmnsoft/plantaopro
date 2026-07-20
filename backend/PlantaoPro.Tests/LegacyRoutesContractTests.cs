namespace PlantaoPro.Tests;

public sealed class LegacyRoutesContractTests
{
    private static string Root(){ var d=new DirectoryInfo(AppContext.BaseDirectory); while(d is not null && !Directory.Exists(Path.Combine(d.FullName,".git"))) d=d.Parent; return d?.FullName ?? throw new InvalidOperationException("Raiz não encontrada."); }
    [Fact]
    public void V112HomologationMantemContratoV113SemRotasOperacionaisNaoVersionadas()
    {
        var source = File.ReadAllText(Path.Combine(Root(), "backend/PlantaoPro.Api/Controllers/V112HomologationController.cs"));
        foreach (var route in new[] { "api/customers", "api/products", "api/orders", "api/billing", "api/templates", "api/outbox", "api/tasks" })
        {
            Assert.DoesNotContain("\"" + route, source, StringComparison.Ordinal);
        }
        Assert.Contains("api/v113/customers", source, StringComparison.Ordinal);
        Assert.Contains("api/v113/dashboard", source, StringComparison.Ordinal);
        Assert.Contains("api/v112/dashboard", source, StringComparison.Ordinal);
    }
}
