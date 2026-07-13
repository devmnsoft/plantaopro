using Xunit;

namespace PlantaoPro.Tests;

public sealed class Saude360ReleaseCandidateContractTests
{
    [Fact]
    public void ApiSmokeEndpointsAreExposedThroughControllersAndWrappedAsApiResponse()
    {
        var controllers = Read("backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs") + Read("backend/PlantaoPro.Api/Controllers/Saude360SupportControllers.cs");
        foreach (var endpoint in new List<string>
        {
            "api/clinica-dashboard", "api/pacientes", "api/agendamentos", "api/painel-chamada", "api/triagens", "api/consultas", "api/cid", "api/prescricoes", "api/clinica-financeiro", "api/convenios", "api/planos-saude", "api/pendencias-clinicas"
        })
        {
            Assert.Contains(endpoint, controllers, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("ApiResponse<", controllers, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("throw new NotImplementedException", controllers, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WebReleaseCandidatePagesHaveControllersViewsHelpAndEmptyStateShell()
    {
        var webControllers = Read("backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs") + Read("backend/PlantaoPro.Web/Controllers/AjudaController.cs") + Read("backend/PlantaoPro.Web/Controllers/ManualController.cs");
        foreach (var controllerName in new List<string> { "ClinicaDashboardController", "PendenciasClinicasController", "PacientesController", "AgendamentosController", "PainelChamadaController", "TriagemController", "ConsultasController", "CidController", "PrescricoesController", "ClinicaFinanceiroController", "ConveniosController", "PlanosSaudeController" })
        {
            Assert.Contains(controllerName, webControllers, StringComparison.OrdinalIgnoreCase);
        }

        var moduleView = Read("backend/PlantaoPro.Web/Views/Saude360/Modulo.cshtml");
        Assert.Contains("_PageHelp", moduleView, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("_EmptyState", moduleView, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("toast", Read("backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseCandidateDocumentationIsPresent()
    {
        foreach (var path in new List<string>
        {
            "docs/homologacao/matriz-telas-rotas-saude360.md",
            "docs/homologacao/checklist-web-saude360.md",
            "docs/homologacao/qa-final-saude360.md",
            "docs/seguranca/checklist-lgpd-saude360.md",
            "docs/performance/otimizacoes-saude360.md",
            "docs/demo/roteiro-comercial-premium-saude360.md",
            "docs/demo/checklist-pre-demo.md",
            "docs/release/release-candidate-premium-saude360.md",
            "docs/manual/README.md"
        })
        {
            Assert.True(File.Exists(Path.Combine(GetRepoRoot(), path)), path);
        }
    }

    private static string Read(string relativePath) { return File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath)); }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend", "PlantaoPro.sln"))) dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("Repo root not found.");
        return dir.FullName;
    }
}
