namespace PlantaoPro.Tests;

public class FaturamentoSaasFunctionalContractTests
{
    [Fact]
    public void FaturamentoSaasApi_DeveAuditarTransacionarERegistrarEventosDeCobranca()
    {
        var raiz = EncontrarRaizRepositorio();
        var controller = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Api", "Controllers", "SaasCommercialController.cs"));

        Assert.Contains("BeginTransactionAsync", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RegistrarEventoCobrancaAsync", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RegistrarAlertaFinanceiroAsync", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ResolverAlertasFinanceirosAsync", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COBRANCA_NOTIFICADA", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FATURA_CONTESTADA", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CONTESTACAO_RESOLVIDA", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FATURA_PAGA", controller, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FaturamentoSaasWeb_DeveExporAcoesReaisComAntiForgeryEModais()
    {
        var raiz = EncontrarRaizRepositorio();
        var controller = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Controllers", "FaturamentoSaasController.cs"));
        var detalhe = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "FaturamentoSaas", "Details.cshtml"));

        foreach (var action in new[] { "MarcarPaga", "Cancelar", "Contestar", "ResolverContestacao", "Notificar" })
        {
            Assert.Contains("IActionResult> " + action, controller, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("asp-action=\"" + action + "\"", detalhe, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("@Html.AntiForgeryToken()", detalhe, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-bs-toggle=\"modal\"", detalhe, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("confirm(", detalhe, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert(", detalhe, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"#\"", detalhe, StringComparison.OrdinalIgnoreCase);
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git"))) diretorio = diretorio.Parent;
        if (diretorio is null) throw new InvalidOperationException("Raiz do repositório não encontrada para testes de contrato.");
        return diretorio.FullName;
    }
}
