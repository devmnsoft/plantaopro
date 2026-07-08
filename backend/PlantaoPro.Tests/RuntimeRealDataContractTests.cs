using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

public class RuntimeRealDataContractTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    [Fact]
    public void Web_NaoDeveTerAgendamentosControllerDuplicado()
    {
        var files = Directory.GetFiles(Path.Combine(Root, "backend", "PlantaoPro.Web"), "*.cs", SearchOption.AllDirectories);
        var count = files.Sum(f => Regex.Matches(File.ReadAllText(f), "class\\s+AgendamentosController").Count);
        Assert.Equal(1, count);
    }

    [Fact]
    public void OperacaoInteligenteWeb_NaoUsaDemoComoCaminhoPadrao()
    {
        var controller = Read("backend", "PlantaoPro.Web", "Controllers", "OperacaoInteligenteController.cs");
        Assert.Contains("api/operacao-inteligente/resumo", controller);
        Assert.Contains("DemoMode", controller);
        Assert.DoesNotContain("var model = OperacaoInteligenteViewModel.Demo()", controller);
    }

    [Fact]
    public void OperacaoRecomendacaoService_UsaDapperPostgreSqlEDadosReais()
    {
        var service = Read("backend", "PlantaoPro.Api", "OperacaoRecomendacaoService.cs");
        Assert.Contains("using Dapper", service);
        Assert.Contains("NpgsqlConnection", service);
        Assert.DoesNotContain("tenant-demo", service);
        Assert.DoesNotContain("demo-", service);
    }

    [Fact]
    public void DashboardsPremium_PossuemEndpointsApiReais()
    {
        var controller = Read("backend", "PlantaoPro.Api", "Controllers", "DashboardsController.cs");
        foreach (var route in new[] { "admin-global", "admin-cliente", "coordenacao", "medico", "financeiro", "saude360" })
        {
            Assert.Contains("[HttpGet(\"" + route + "\")]", controller);
        }
        Assert.Contains("ApiResponse<DashboardPremiumDto>", controller);
    }
}
