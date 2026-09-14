using Xunit;

namespace PlantaoPro.Tests;

public sealed class V2165CentralClientesContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void CentralGlobal_UsaPaginacaoFiltrosAgregacoesEAutorizacaoNoServidor()
    {
        var service = Read("backend/PlantaoPro.Api/SaasServices.cs");
        var controller = Read("backend/PlantaoPro.Api/Controllers/ClientesController.cs");
        Assert.Contains("ListCentralAsync(string? search, string? status, int page, int pageSize)", service);
        Assert.Contains("limit @take offset @skip", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count(*) filter", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Authorize(Roles = RolesConstants.AdministradorGlobal)]", controller);
        Assert.Contains("[HttpGet(\"central\")]", controller);
        Assert.DoesNotContain("RepositoryPathResolver." + "Root", Read("backend/PlantaoPro.Tests/V2160ClinicalJourneyContractTests.cs"));
    }

    [Fact]
    public void SituacaoAdministrativa_ExigeMotivoBloqueiaLinhaERegistraAtorClienteResultado()
    {
        var service = Read("backend/PlantaoPro.Api/SaasServices.cs");
        Assert.Contains("for update", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Informe o motivo e o alcance", service);
        Assert.Contains("audit.RegistrarAsync(actorId, id", service);
        Assert.Contains("resultado = \"SUCESSO\"", service);
    }

    [Fact]
    public void Interface_MantemContextoExplicitoTotaisDoFiltroEAbasDoDetalhe()
    {
        var list = Read("backend/PlantaoPro.Web/Views/Clientes/Index.cshtml");
        var details = Read("backend/PlantaoPro.Web/Views/Clientes/Details.cshtml");
        Assert.Contains("Modo global", list);
        Assert.Contains("Totais do conjunto filtrado", list);
        Assert.Contains("Ausência de telemetria", list);
        foreach (var tab in new[] { "Visão geral", "Organização e unidades", "Módulos e contratos", "Equipe e perfis", "Cobranças", "Histórico" })
            Assert.Contains(tab, details);
        Assert.DoesNotContain("alert(", list);
        Assert.DoesNotContain("confirm(", list);
        Assert.DoesNotContain("href=\"#\"", list);
    }
}
