using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

public class RuntimeRealDataContractTests
{
    private static string Read(params string[] parts) => RepositoryPathResolver.ReadRepositoryFile(parts);

    [Fact]
    public void Web_NaoDeveTerAgendamentosControllerDuplicado()
    {
        var files = Directory.GetFiles(RepositoryPathResolver.WebRoot, "*.cs", SearchOption.AllDirectories);
        var count = files.Sum(f => Regex.Matches(File.ReadAllText(f), "class\\s+AgendamentosController").Count);
        Assert.Equal(1, count);
    }

    [Fact]
    public void OperacaoInteligenteWeb_NaoUsaDemoComoCaminhoPadrao()
    {
        var controller = RepositoryPathResolver.ReadSourceContaining(
            Path.Combine(RepositoryPathResolver.WebRoot, "Controllers"),
            "OperacaoInteligenteController");
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
        var controller = RepositoryPathResolver.ReadSourceContaining(
            Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers"),
            "DashboardsController");
        foreach (var route in new[] { "admin-global", "admin-cliente", "coordenacao", "medico", "financeiro", "saude360" })
        {
            Assert.Contains("[HttpGet(\"" + route + "\")]", controller);
        }
        Assert.Contains("ApiResponse<DashboardPremiumDto>", controller);
    }
}
