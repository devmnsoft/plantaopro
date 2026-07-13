using Xunit;

namespace PlantaoPro.Tests;

public class V116ConsolidacaoOperacionalContractTests
{
    private static string Root()
    {
        var dir = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(dir, "README.md"))) dir = Directory.GetParent(dir)!.FullName;
        return dir;
    }
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root(), path));
    [Fact]
    public void ArtefatosV116DevemExistir()
    {
        var root = Root();
        foreach (var path in new[] { "database/migrations/2026_v116_consolidacao_operacional_final.sql", "database/seeds/2026_demo_v116_consolidacao_operacional.sql", "backend/PlantaoPro.Api/V116ConvenioService.cs", "backend/PlantaoPro.Api/V116LoteFaturamentoService.cs", "backend/PlantaoPro.Api/V116CaixaService.cs", "backend/PlantaoPro.Api/V116TimelineService.cs", "backend/PlantaoPro.Api/V116NotificacaoOperacionalService.cs", "backend/PlantaoPro.Api/V116RelatorioExecutivoService.cs", "backend/PlantaoPro.Api/Controllers/V116ConsolidacaoController.cs", "scripts/smoke-test-v116.sh", "scripts/smoke-test-v116.ps1", "docs/v1.16-consolidacao-operacional-final.md" }) Assert.True(File.Exists(Path.Combine(root, path)), path);
    }
    [Fact]
    public void MigrationSeedControllerDevemCobrirFluxos()
    {
        var sql = Read("database/migrations/2026_v116_consolidacao_operacional_final.sql") + Read("database/seeds/2026_demo_v116_consolidacao_operacional.sql");
        foreach (var termo in new[] { "v116_convenio_autorizacoes", "v116_convenio_guias", "v116_faturamento_lotes", "v116_caixas", "v116_timelines", "v116_notificacoes_operacionais", "v116_relatorios_execucoes", "DEPENDENTE_CONFIGURACAO" }) Assert.Contains(termo, sql);
        var controller = Read("backend/PlantaoPro.Api/Controllers/V116ConsolidacaoController.cs");
        foreach (var rota in new[] { "convenios/autorizacoes", "convenios/guias", "faturamento/lotes", "caixa/status", "caixa/receber", "timelines", "notificacoes-operacionais", "relatorios" }) Assert.Contains(rota, controller);
        Assert.Contains("[Authorize]", controller); Assert.Contains("ApiResponse<", controller);
    }
    [Fact]
    public void NaoDeveConterPadroesProibidosV116()
    {
        var combined = Read("backend/PlantaoPro.Api/Controllers/V116ConsolidacaoController.cs") + Read("backend/PlantaoPro.Api/V116ConsolidacaoServices.cs");
        foreach (var forbidden in new[] { "@" + "page", "asp-" + "page", "@model " + "dynamic", "href=\"" + "#\"", "al" + "ert(", "con" + "firm(", "= " + "[]", "return " + "[" }) Assert.DoesNotContain(forbidden, combined);
    }
}
