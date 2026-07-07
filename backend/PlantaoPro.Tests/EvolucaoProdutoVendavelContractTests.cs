using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class EvolucaoProdutoVendavelContractTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void OperacaoInteligente_DeveTerControllerWebApiEndpointEServicoDeterministico()
    {
        Assert.Contains("class OperacaoInteligenteController", Read("backend/PlantaoPro.Web/Controllers/OperacaoInteligenteController.cs"));
        var api = Read("backend/PlantaoPro.Api/Controllers/OperacaoInteligenteController.cs");
        Assert.Contains("api/operacao-inteligente", api);
        Assert.Contains("resumo", api);
        Assert.Contains("ApiResponse<OperacaoInteligenteResumoDto>", api);
        var service = Read("backend/PlantaoPro.Api/OperacaoRecomendacaoService.cs");
        Assert.Contains("OperacaoRecomendacaoService", service);
        Assert.Contains("Convide médicos recomendados", service);
        Assert.DoesNotContain("OpenAI", service, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DashboardsPorPerfil_EPrimeirosPassos_DeveExistir()
    {
        var controller = Read("backend/PlantaoPro.Web/Controllers/DashboardsPremiumController.cs");
        foreach (var perfil in new[] { "AdminGlobal", "AdministradorCliente", "Coordenacao", "Medico", "Financeiro", "Saude360" })
            Assert.Contains(perfil, controller);
        var primeirosPassos = Read("backend/PlantaoPro.Web/Views/PrimeirosPassos/Index.cshtml");
        foreach (var texto in new[] { "Admin Cliente", "Recepção", "Triagem", "Médico", "Financeiro", "Configurar unidade", "Fazer check-in" })
            Assert.Contains(texto, primeirosPassos);
    }

    [Fact]
    public void AgendaVisual_Relatorios_DemoPremium_ESeeds_DeveExistir()
    {
        var agenda = Read("backend/PlantaoPro.Web/Controllers/AgendamentosController.cs");
        foreach (var action in new[] { "Calendario", "AgendaDia", "AgendaMedico", "CheckIn" }) Assert.Contains(action, agenda);
        var relatorios = Read("backend/PlantaoPro.Web/Views/Relatorios/Index.cshtml");
        Assert.Contains("Plantões por período", relatorios);
        Assert.Contains("Exportação futura bloqueada", relatorios);
        Assert.Contains("LGPD", relatorios);
        Assert.Contains("O PlantãoPro organiza a operação médica de ponta a ponta", Read("docs/demo/roteiro-demo-comercial-premium.md"));
        Assert.Contains("plantao_pro_demo_premium", Read("database/seeds/2026_demo_comercial_premium.sql"));
    }

    [Fact]
    public void MobileMedico_DeveTerTelasMinimasEApiBaseUrlPublica()
    {
        var nav = Read("mobile/PlantaoPro.App/src/navigation/AppNavigator.tsx");
        foreach (var tela in new[] { "Início", "Plantões", "Convites", "Escalas", "Pagamentos", "Notificações", "Disponibilidade", "Preferências", "Perfil" })
            Assert.Contains(tela, nav);
        Assert.Contains("EXPO_PUBLIC_API_BASE_URL", Read("mobile/PlantaoPro.App/src/services/api.ts"));
        Assert.DoesNotContain("console.log(token", Read("mobile/PlantaoPro.App/src/services/api.ts"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Repositorio_NaoDeveConterPadroesProibidosOuSegredosObvios()
    {
        var files = Directory.EnumerateFiles(Path.Combine(Root, "backend"), "*.*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(Root, "mobile"), "*.*", SearchOption.AllDirectories))
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !f.Contains("node_modules"));
        var pattern = string.Join("|", "@" + "page", "asp-" + "page", "@model " + "dynamic", "href=\"" + "#\"", "alert" + "\\(", "confirm" + "\\(", "= " + "\\[\\]", "return " + "\\[\\]");
        var forbidden = new Regex(pattern, RegexOptions.Compiled);
        foreach (var file in files)
            Assert.False(forbidden.IsMatch(File.ReadAllText(file)), file);
        foreach (var appsettings in Directory.EnumerateFiles(Path.Combine(Root, "backend"), "appsettings*.json", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(appsettings);
            Assert.DoesNotContain("senha-real", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("jwt-key-real", content, StringComparison.OrdinalIgnoreCase);
        }
    }
}
