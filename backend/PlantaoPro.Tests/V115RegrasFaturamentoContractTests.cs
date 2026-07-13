using Xunit;

namespace PlantaoPro.Tests;

public class V115RegrasFaturamentoContractTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(GetRoot(), path));
    private static string GetRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(dir, "README.md"))) dir = Directory.GetParent(dir)!.FullName;
        return dir;
    }

    [Fact]
    public void ArtefatosV115DevemExistir()
    {
        var root = GetRoot();
        Assert.True(File.Exists(Path.Combine(root, "database/migrations/2026_v115_regras_faturamento_repasses.sql")));
        Assert.True(File.Exists(Path.Combine(root, "database/seeds/2026_demo_v115_regras_faturamento.sql")));
        Assert.True(File.Exists(Path.Combine(root, "backend/PlantaoPro.Api/V115FaturamentoRegraService.cs")));
        Assert.True(File.Exists(Path.Combine(root, "backend/PlantaoPro.Api/V115RepasseMedicoService.cs")));
        Assert.True(File.Exists(Path.Combine(root, "backend/PlantaoPro.Api/V115GlosaService.cs")));
        Assert.True(File.Exists(Path.Combine(root, "backend/PlantaoPro.Api/Controllers/V115FaturamentoController.cs")));
        Assert.True(File.Exists(Path.Combine(root, "scripts/smoke-test-v115.sh")));
        Assert.True(File.Exists(Path.Combine(root, "docs/v1.15-regras-faturamento.md")));
    }

    [Fact]
    public void ControllerDeveExporEndpointsApiResponseAuthorize()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/V115FaturamentoController.cs");
        Assert.Contains("[Authorize]", controller);
        Assert.Contains("ApiResponse<", controller);
        foreach (var rota in new[] { "faturamento/regras", "faturamento/contas-receber", "gerar-conta-consulta", "gerar-conta-plantao", "faturamento/receber", "repasses-medicos", "contestar", "resolver", "glosas", "financeiro/alertas" }) Assert.Contains(rota, controller);
    }

    [Fact]
    public void RegrasNaoDevemUsarHardcodeDemonstrativoEmFluxoReal()
    {
        var service = Read("backend/PlantaoPro.Api/V114ProdutoService.cs") + Read("backend/PlantaoPro.Api/V115FaturamentoRegraService.cs") + Read("backend/PlantaoPro.Api/V115RepasseMedicoService.cs") + Read("backend/PlantaoPro.Api/V115GlosaService.cs");
        Assert.DoesNotContain("valor " + "* 0.7", service);
        Assert.DoesNotContain("valor " + "* 0.1", service);
        Assert.DoesNotContain("valor " + "= 100m", service);
        Assert.Contains("valorCalculado", service);
        Assert.Contains("REGRA_ATIVA", service);
    }

    [Fact]
    public void MobileEDocsCiDevemCobrirV115()
    {
        var mobile = Read("mobile/PlantaoPro.App/src/screens/financeiro/FinanceiroScreen.tsx") + Read("mobile/PlantaoPro.App/src/services/financeiroService.ts");
        Assert.Contains("repasses", mobile, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("contestarPagamentoV115", mobile);
        Assert.DoesNotContain("Alert.alert", mobile);
        var ci = Read(".github/workflows/dotnet-ci.yml");
        Assert.Contains("2026_v115_regras_faturamento_repasses.sql", ci);
        Assert.Contains("smoke-test-v115.sh", ci);
    }

    [Fact]
    public void NaoDeveConterPadroesProibidosNosArquivosV115()
    {
        var combined = Read("backend/PlantaoPro.Api/Controllers/V115FaturamentoController.cs") + Read("backend/PlantaoPro.Api/V115FaturamentoRegraService.cs") + Read("backend/PlantaoPro.Web/Controllers/V115FaturamentoWebControllers.cs");
        foreach (var forbidden in new[] { "@" + "page", "asp-" + "page", "@model " + "dynamic", "href=\"" + "#\"", "al" + "ert(", "con" + "firm(", "= " + "[]", "return " + "[" }) Assert.DoesNotContain(forbidden, combined);
    }
}
