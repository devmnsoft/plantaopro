using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class PosRcUxFinalQaContractTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void WebDashboardsPremium_DevemConsumirApiRealSemNumerosFalsos()
    {
        var controller = Read("backend/PlantaoPro.Web/Controllers/DashboardsPremiumController.cs");
        Assert.Contains("api/dashboards/", controller);
        Assert.Contains("ReadApiResponseAsync<DashboardPremiumApiViewModel>", controller);
        var view = Read("backend/PlantaoPro.Web/Views/DashboardsPremium/Perfil.cshtml");
        Assert.Contains("Fonte:", view);
        Assert.Contains("Nenhum número demonstrativo", view);
        Assert.DoesNotContain("i * 7", view);
    }

    [Fact]
    public void AgendaClinicaPremium_NaoDeveUsarPacienteDemoPorPadrao()
    {
        var controller = Read("backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs");
        Assert.Contains("AgendaPremiumAsync", controller);
        Assert.Contains("api/agendamentos/calendario", controller);
        Assert.Contains("api/agendamentos/medico/{medicoId}", controller);
        var view = Read("backend/PlantaoPro.Web/Views/Agendamentos/AgendaPremium.cshtml");
        Assert.DoesNotContain("Paciente demo", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-bs-toggle=\"modal\"", view);
        Assert.Contains("Chamar no painel", view);
        Assert.Contains("Enviar para triagem", view);
    }

    [Fact]
    public void SmokeMobileDocsEPadroes_DevemCobrirHomologacaoFinal()
    {
        var smoke = Read("scripts/smoke/smoke-api.sh");
        foreach (var endpoint in new[] { "/api/operacao-inteligente/resumo", "/api/dashboards/admin-cliente", "/api/dashboards/coordenacao", "/api/dashboards/medico", "/api/dashboards/financeiro", "/api/dashboards/saude360" })
            Assert.Contains(endpoint, smoke);
        Assert.DoesNotContain("echo ${TOKEN}", smoke);
        Assert.DoesNotContain("Alert.alert", Read("mobile/PlantaoPro.App/src/navigation/AppNavigator.tsx"));
        Assert.Contains("EXPO_PUBLIC_API_BASE_URL", Read("mobile/PlantaoPro.App/src/services/api.ts"));
        Assert.True(File.Exists(Path.Combine(Root, "docs/homologacao/qa-saude360-ponta-a-ponta.md")));
        Assert.True(File.Exists(Path.Combine(Root, "docs/homologacao/qa-plantoes-escalas-financeiro.md")));
        Assert.True(File.Exists(Path.Combine(Root, "docs/mobile/qa-mobile-medico.md")));
    }

    [Fact]
    public void MenusNaoDevemApontarParaPadroesMvcProibidos()
    {
        var files = Directory.EnumerateFiles(Path.Combine(Root, "backend", "PlantaoPro.Web"), "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(Root, "mobile"), "*.tsx", SearchOption.AllDirectories))
            .Where(f => !f.Contains("node_modules"));
        var pattern = new Regex("@" + "page|asp-" + "page|@model " + "dynamic|href=\"" + "#\"|alert" + "\\(|confirm" + "\\(|= " + "\\[\\]|return " + "\\[");
        foreach (var file in files)
            Assert.False(pattern.IsMatch(File.ReadAllText(file)), file);
    }
}
