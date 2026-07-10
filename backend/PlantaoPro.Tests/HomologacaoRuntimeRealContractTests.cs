using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class HomologacaoRuntimeRealContractTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void SmokeRuntime_DeveCobrirRotasPrincipaisApiEWeb()
    {
        var smoke = Read("scripts/smoke/smoke-api.sh");
        foreach (var route in new[]
        {
            "/api/health", "/api/health/db", "/api/usuarios/me", "/api/operacao-inteligente/resumo", "/api/dashboards/admin-cliente",
            "/api/agendamentos", "/api/triagens", "/api/consultas", "/api/cid", "/api/prescricoes", "/api/clinica-financeiro",
            "/api/convenios", "/api/planos-saude", "/api/plantoes", "/api/escalas", "/api/financeiro/pagamentos", "/api/notificacoes",
            "/Dashboard", "/DashboardsPremium/AdministradorCliente", "/OperacaoInteligente", "/Pacientes", "/Pacientes/Create", "/Agendamentos",
            "/Agendamentos/AgendaDia", "/Agendamentos/CheckIn", "/PainelChamada", "/Triagem", "/Consultas", "/Prescricoes", "/Cid",
            "/ClinicaFinanceiro", "/Convenios", "/PlanosSaude", "/Plantoes", "/Escalas", "/Financeiro", "/Notificacoes", "/Relatorios"
        })
        {
            Assert.Contains(route, smoke, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Web_DevePossuirCreateEditDetailsEAcoesCriticasDosModulosPrincipais()
    {
        var controllers = string.Join("\n", Directory.EnumerateFiles(Path.Combine(Root, "backend", "PlantaoPro.Web", "Controllers"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        foreach (var action in new[] { "Create()", "Edit(Guid id)", "Details(Guid id)", "AgendaDia", "CheckIn" })
        {
            Assert.Contains(action, controllers, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MenusControllersSegredosEMobile_DevemRespeitarContratosDeHomologacao()
    {
        var menu = Read("backend/PlantaoPro.Web/Services/Security/MenuBuilderService.cs");
        foreach (var route in new[] { "Dashboard", "Pacientes", "Agendamentos", "Triagem", "Consultas", "Prescricoes", "Cid", "ClinicaFinanceiro", "Convenios", "PlanosSaude", "Plantoes", "Escalas", "Financeiro", "Notificacoes", "Relatorios" })
        {
            Assert.Contains(route, menu, StringComparison.OrdinalIgnoreCase);
        }

        var controllerClasses = Directory.EnumerateFiles(Path.Combine(Root, "backend", "PlantaoPro.Web", "Controllers"), "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), @"class\s+(\w+Controller)").Cast<Match>().Select(match => match.Groups[1].Value))
            .GroupBy(name => name)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        Assert.Empty(controllerClasses);

        foreach (var appsettings in Directory.EnumerateFiles(Path.Combine(Root, "backend"), "appsettings*.json", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(appsettings);
            Assert.DoesNotContain("admin123", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("123456", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("eyJ", content, StringComparison.OrdinalIgnoreCase);
        }

        var mobileFiles = Directory.EnumerateFiles(Path.Combine(Root, "mobile", "PlantaoPro.App", "src"), "*.ts*", SearchOption.AllDirectories).Select(File.ReadAllText).ToArray();
        Assert.Contains("EXPO_PUBLIC_API_BASE_URL", Read("mobile/PlantaoPro.App/src/services/api.ts"), StringComparison.OrdinalIgnoreCase);
        Assert.All(mobileFiles, content => Assert.DoesNotContain("Alert.alert", content, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DocumentacaoDeEvidenciaRuntime_DeveExistir()
    {
        foreach (var doc in new[]
        {
            "docs/homologacao/evidencia-runtime-real.md",
            "docs/homologacao/resultado-smoke-runtime-real.md",
            "docs/homologacao/qa-cruds-runtime-real.md",
            "docs/homologacao/bugs-corrigidos-runtime-real.md",
            "docs/mobile/resultado-qa-mobile-runtime.md"
        })
        {
            Assert.True(File.Exists(Path.Combine(Root, doc)), doc);
            Assert.Contains("2026-07-09", Read(doc), StringComparison.OrdinalIgnoreCase);
        }
    }
}
