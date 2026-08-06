using Xunit;

namespace PlantaoPro.Tests;

public sealed class V1440ProdutoVendavelContractTests
{
    private static string Read(params string[] path) => RepositoryPathResolver.ReadRepositoryFile(path);

    [Fact]
    public void AgendaPremium_DeveExporTodasAsVisoesComDadosReais()
    {
        var controller = Read("backend", "PlantaoPro.Web", "Controllers", "AgendaController.cs");
        foreach (var route in new[] { "/agenda", "/agenda/dia", "/agenda/semana", "/agenda/mes", "/agenda/conflitos" })
            Assert.Contains(route, controller);
        Assert.Contains("api/plantoes?", controller);
        Assert.DoesNotContain("new PlantaoResumoDto", controller);
    }

    [Fact]
    public void AgendaPremium_DeveSerOrientadaAAcaoEResponsiva()
    {
        var view = Read("backend", "PlantaoPro.Web", "Views", "Agenda", "Index.cshtml");
        Assert.Contains("Operação em tempo real", view);
        Assert.Contains("Ver contexto", view);
        Assert.Contains("_EmptyState", view);
        var css = Read("backend", "PlantaoPro.Web", "wwwroot", "css", "plantao-premium.css");
        Assert.Contains("@media(max-width:767.98px)", css);
        Assert.Contains("@media print", css);
    }

    [Fact]
    public void SchemaV1440_DeveCobrirOperacaoMobileOnboardingRelatoriosENotificacoes()
    {
        var schema = Read("database", "schema", "300_v1440_produto_vendavel_design_mobile_operacao.sql");
        foreach (var table in new[] { "agenda_eventos_operacionais", "medico_registros_jornada", "onboarding_progresso", "relatorios_salvos_v144", "exportacoes_gerenciais", "notificacoes_mobile", "white_label_previews", "acoes_rapidas_auditoria" })
            Assert.Contains($"CREATE TABLE IF NOT EXISTS {table}", schema);
        Assert.Contains("uq_medico_jornada_escala_tipo", schema);
        Assert.Contains("ck_white_label_aplicacao_segura", schema);
    }
}
