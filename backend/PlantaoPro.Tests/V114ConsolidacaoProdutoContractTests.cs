using Xunit;

namespace PlantaoPro.Tests;

public sealed class V114ConsolidacaoProdutoContractTests
{
    private static readonly string Root = FindRoot();
    private static string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));
    private static bool Exists(string relative) => File.Exists(Path.Combine(Root, relative));

    [Fact]
    public void ApiV114_DeveExporServiceControllerRotasProduto()
    {
        var service = Read("backend/PlantaoPro.Api/V114ProdutoService.cs");
        var controller = Read("backend/PlantaoPro.Api/Controllers/V114ProdutoController.cs");
        Assert.Contains("class V114ProdutoService", service);
        Assert.Contains("class V114ProdutoController", controller);
        foreach (var rota in new[] { "api/v114", "dashboard", "operacao/central", "itens-faturaveis", "faturamento/contas-receber", "faturamento/titulos", "demo-boleto", "jornada/progresso", "templates-operacionais" })
        {
            Assert.Contains(rota, controller);
        }
        Assert.Contains("ApiResponse<", controller);
        Assert.Contains("[Authorize]", controller);
    }

    [Fact]
    public void WebV114_DeveTerTelasMenuFavoritosTimelinesEJornada()
    {
        Assert.True(Exists("backend/PlantaoPro.Web/Controllers/V114ProdutoWebControllers.cs"));
        Assert.True(Exists("backend/PlantaoPro.Web/Views/V114/Produto.cshtml"));
        Assert.True(Exists("backend/PlantaoPro.Web/Views/V114/Form.cshtml"));
        var web = Read("backend/PlantaoPro.Web/Controllers/V114ProdutoWebControllers.cs");
        Assert.Contains("ItensFaturaveisController", web);
        Assert.Contains("FaturamentoClinicoController", web);
        Assert.Contains("JornadaController", web);
        Assert.Contains("FavoritosController", web);
        Assert.Contains("HistoricoAcoesController", web);
        var menu = Read("backend/PlantaoPro.Web/Services/Security/MenuBuilderService.cs");
        Assert.Contains("Jornada", menu);
        Assert.Contains("ItensFaturaveis", menu);
        Assert.Contains("FaturamentoClinico", menu);
    }

    [Fact]
    public void SmokeCiDocsEMobileV114_DevemExistir()
    {
        Assert.True(Exists("scripts/smoke-test-v114.sh"));
        Assert.True(Exists("scripts/smoke-test-v114.ps1"));
        Assert.Contains("smoke-test-v114.sh", Read(".github/workflows/dotnet-ci.yml"));
        Assert.True(Exists("docs/v1.14-mapeamento-dominio-produto.md"));
        Assert.True(Exists("docs/v1.14-consolidacao-produto.md"));
        Assert.True(Exists("docs/v1.14-rbac-lgpd.md"));
        Assert.True(Exists("docs/homologacao/v114-smoke-result.md"));
        Assert.Contains("EXPO_PUBLIC_API_BASE_URL", Read("mobile/PlantaoPro.App/src/services/api.ts"));
        Assert.Contains("v114/mobile/medico/dashboard", Read("mobile/PlantaoPro.App/src/services/v114Service.ts"));
    }

    [Fact]
    public void ContratosNaoDevemReintroduzirEstadoGenericoOuCSharp12()
    {
        var apiController = Read("backend/PlantaoPro.Api/Controllers/V114ProdutoController.cs");
        Assert.DoesNotContain("V112" + "Store", apiController);
        Assert.DoesNotContain("Fake" + "Boleto", apiController);
        Assert.DoesNotContain("Concurrent" + "Dictionary", apiController);
        Assert.DoesNotContain("= " + "[]", apiController);
        Assert.DoesNotContain("return " + "[", apiController);
        Assert.DoesNotContain("href=\"#\"", Read("backend/PlantaoPro.Web/Views/V114/Produto.cshtml"));
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "README.md"))) dir = dir.Parent;
        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
