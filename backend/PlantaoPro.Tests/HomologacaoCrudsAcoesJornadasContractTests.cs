using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class HomologacaoCrudsAcoesJornadasContractTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void ControllersWebPrincipais_DevemExistirComActionsDeCrudERotasDeMenu()
    {
        var saude = Read("backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs");
        var controllers = string.Join("\n", Directory.EnumerateFiles(Path.Combine(Root, "backend", "PlantaoPro.Web", "Controllers"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        foreach (var controller in new[] { "DashboardController", "PacientesController", "AgendamentosController", "PainelChamadaController", "TriagemController", "ConsultasController", "PrescricoesController", "CidController", "ClinicaFinanceiroController", "ConveniosController", "PlanosSaudeController" })
            Assert.Contains("class " + controller, controllers);

        foreach (var action in new[] { "Index()", "Create()", "Edit(Guid id)", "Details(Guid id)" })
            Assert.Contains(action, saude);

        var menu = Read("backend/PlantaoPro.Web/Services/Security/MenuBuilderService.cs");
        foreach (var route in new[] { "Pacientes", "Agendamentos", "CheckIn", "PainelChamada", "Triagem", "Consultas", "Prescricoes", "Cid", "ClinicaFinanceiro", "Convenios", "PlanosSaude", "Plantoes", "Escalas", "Notificacoes", "Relatorios", "Ajuda", "Manual" })
            Assert.Contains(route, menu);
    }

    [Fact]
    public void ApiESmoke_DevemCobrirRotasCriticasDaHomologacaoFuncional()
    {
        var smoke = Read("scripts/smoke/smoke-api.sh");
        foreach (var endpoint in new[]
        {
            "/api/health", "/api/health/db", "/api/usuarios/me", "/api/operacao-inteligente/resumo", "/api/dashboards/admin-cliente",
            "/api/agendamentos", "/api/triagens", "/api/consultas", "/api/cid", "/api/prescricoes", "/api/clinica-financeiro",
            "/api/convenios", "/api/planos-saude", "/api/plantoes", "/api/escalas", "/api/financeiro/pagamentos", "/api/notificacoes",
            "/Dashboard", "/DashboardsPremium/AdministradorCliente", "/OperacaoInteligente", "/Pacientes", "/Pacientes/Create", "/Agendamentos",
            "/Agendamentos/AgendaDia", "/Agendamentos/CheckIn", "/PainelChamada", "/Triagem", "/Consultas", "/Prescricoes", "/Cid",
            "/ClinicaFinanceiro", "/Convenios", "/PlanosSaude", "/Plantoes", "/Escalas", "/Financeiro", "/Notificacoes", "/Relatorios"
        })
            Assert.Contains(endpoint, smoke);
        Assert.DoesNotContain("echo ${TOKEN}", smoke);
    }

    [Fact]
    public void Repositorio_NaoDeveConterPadroesProibidosSegredosOuControllerDuplicado()
    {
        var webControllers = Directory.EnumerateFiles(Path.Combine(Root, "backend", "PlantaoPro.Web", "Controllers"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText).ToArray();
        Assert.Single(webControllers.SelectMany(c => Regex.Matches(c, "class AgendamentosController").Cast<Match>()));

        var files = Directory.EnumerateFiles(Path.Combine(Root, "backend"), "*.*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(Root, "mobile"), "*.*", SearchOption.AllDirectories))
            .Where(f => !f.Contains("node_modules") && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".cshtml") || f.EndsWith(".tsx") || f.EndsWith(".ts") || f.EndsWith(".json"));
        var pattern = new Regex("@" + "page|asp-" + "page|@model " + "dynamic|href=\"" + "#\"|alert" + "\\(|confirm" + "\\(|= " + "\\[\\]|return " + "\\[");
        foreach (var file in files)
            Assert.False(pattern.IsMatch(File.ReadAllText(file)), file);

        foreach (var appsettings in Directory.EnumerateFiles(Path.Combine(Root, "backend"), "appsettings*.json", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(appsettings);
            Assert.DoesNotContain("admin123", content);
            Assert.DoesNotContain("123456", content);
            Assert.DoesNotContain("JwtSecret", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MobileEDocumentacao_DevemEstarPreparadosParaAcoesReaisEQA()
    {
        Assert.Contains("EXPO_PUBLIC_API_BASE_URL", Read("mobile/PlantaoPro.App/src/services/api.ts"));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(Root, "mobile", "PlantaoPro.App", "src"), "*.tsx", SearchOption.AllDirectories))
            Assert.DoesNotContain("Alert.alert", File.ReadAllText(file));
        foreach (var doc in new[]
        {
            "docs/produto/matriz-status-funcional-plantao-pro.md", "docs/homologacao/qa-final-funcional.md", "docs/homologacao/qa-cruds-telas-funcionais.md",
            "docs/homologacao/qa-saude360-ponta-a-ponta.md", "docs/homologacao/qa-plantoes-escalas-financeiro.md", "docs/mobile/qa-mobile-medico.md",
            "docs/demo/roteiro-demo-comercial-premium.md", "docs/homologacao/pendencias-reais-pos-auditoria.md"
        })
            Assert.True(File.Exists(Path.Combine(Root, doc)), doc);
    }
}
