using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R5-E13: contrato da rota de escrita de unidades de atendimento (clinica_unidades_atendimento).
/// A jornada SaaS so pode concluir ONB_SD_UNIDADE por dado persistido via rota real; estas
/// assercoes travam as quatro camadas (servico SQL, controller API, guard Web + BFF e avaliador
/// dual-key da jornada) para que a etapa nunca volte a depender de clique ficticio.
/// </summary>
public sealed class OnboardingE13UnidadesContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Service_PersistsUnidadeWithEscopoDuplo()
    {
        var service = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");

        Assert.Contains("{ \"unidadesAtendimento\", \"clinica_unidades_atendimento\" }", service);
        Assert.Contains("insert into plantaopro.clinica_unidades_atendimento(id,cliente_id,tenant_id,nome,status,created_by)", service);
        // Atualizacao escopada pelo kernel (nunca atravessa tenant) e validacao de nome obrigator io.
        Assert.Contains("if (key == \"unidadesAtendimento\") return \"update plantaopro.\"", service);
        Assert.Contains("key == \"unidadesAtendimento\") && string.IsNullOrWhiteSpace(r.Nome)", service);
    }

    [Fact]
    public void ApiRoute_UnidadesExisteComGateDeContrato()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs");
        var gate = controller.IndexOf("[Route(\"api/unidades-atendimento\")]", StringComparison.Ordinal);

        Assert.True(gate > 0);
        var attrs = controller.Substring(Math.Max(0, gate - 400), 400);
        Assert.Contains("[Saude360Module]", attrs);
        Assert.Contains("RolesConstants.Saude360Assistencial", attrs);
    }

    [Fact]
    public void WebGuard_UnidadesMapeadoParaModuloContratado()
    {
        var guard = Read("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs");
        var bff = Read("backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs");

        // Mapeamento explicito evita CATALOGO_NAO_CONFIGURADO; SAUDE360 e o modulo grosso ja no
        // contrato do tenant e nos grants canonicos (v2334) enquanto UNIDADES fino espera decisao.
        Assert.Contains("[\"ClinicaUnidades\"] = \"SAUDE360\"", guard);
        Assert.Contains("class ClinicaUnidadesController : Saude360WebControllerBase", bff);
        Assert.Contains("\"api/unidades-atendimento\"", bff);
        foreach (var view in new[] { "Index", "Create", "Edit" })
        {
            Assert.True(File.Exists(Path.Combine(Root, "backend/PlantaoPro.Web/Views/ClinicaUnidades", view + ".cshtml")),
                "View ClinicaUnidades/" + view + ".cshtml ausente - ModuloAsync/Formulario nao renderiza sem ela.");
        }
    }

    [Fact]
    public void Avaliador_UnidadeAceitaEscopoClienteETenant()
    {
        var avaliador = Read("backend/PlantaoPro.Api/OnboardingJornadaService.cs");
        var caseStart = avaliador.IndexOf("case \"PRIMEIRA_UNIDADE_SAUDE\":", StringComparison.Ordinal);

        Assert.True(caseStart > 0);
        var caso = avaliador.Substring(caseStart, 500);
        // Mesma regra da homologacao D11: kernel grava cliente_id; negar por tenant_id NULL seria falso negativo.
        Assert.Contains("(ua.tenant_id=@tenantId or ua.cliente_id=@clienteId)", caso);
    }
}
