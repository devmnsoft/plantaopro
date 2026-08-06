using System.Text.Json;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class V1450ExecutiveCommercialContractTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "database")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { RepositoryRoot() }.Concat(parts).ToArray()));

    [Fact]
    public void SchemaV1450_ContemEstruturasOperacionaisEComerciaisObrigatorias()
    {
        var schema = Read("database", "schema", "310_v1450_design_system_executivo_operacao_comercial.sql");
        var tables = new[]
        {
            "agenda_evento_participantes", "agenda_evento_conflitos", "medico_checkins",
            "medico_disponibilidade_regras", "onboarding_etapas_execucao", "relatorio_modelos",
            "relatorio_execucoes", "superadmin_cliente_riscos", "white_label_temas",
            "white_label_historico", "ajuda_contextual_topicos", "operacao_assistida_runbooks",
            "notificacao_agrupamentos_v145", "user_saved_dashboards"
        };

        foreach (var table in tables)
            Assert.Contains($"CREATE TABLE IF NOT EXISTS {table}", schema, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("ck_white_label_tema_ativo_legivel", schema);
        Assert.Contains("ck_medico_checkout_ordem", schema);
        Assert.Contains("uq_saved_dashboard_padrao", schema);
    }

    [Fact]
    public void Manifesto_InstalaSchemaV1450ComoFonteCanonicaObrigatoria()
    {
        using var document = JsonDocument.Parse(Read("database", "install-manifest.json"));
        Assert.Equal("v1.45.0", document.RootElement.GetProperty("schemaVersion").GetString());
        var json = document.RootElement.GetRawText();
        Assert.Contains("database/schema/310_v1450_design_system_executivo_operacao_comercial.sql", json);
        Assert.Contains("CANONICAL_PRODUCT", json);
    }

    [Fact]
    public void Agenda_UsaAcaoDeImpressaoDesacopladaEAcessivel()
    {
        var view = Read("backend", "PlantaoPro.Web", "Views", "Agenda", "Index.cshtml");
        var script = Read("backend", "PlantaoPro.Web", "wwwroot", "js", "agenda-operacional.js");

        Assert.DoesNotContain("onclick=\"window.print()\"", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-agenda-print", view);
        Assert.Contains("aria-describedby", view);
        Assert.Contains("addEventListener('click'", script);
        Assert.Contains("window.print()", script);
    }

    [Fact]
    public void AgendaAvancada_ExpoeBffEApiComIsolamentoDeTenant()
    {
        var bff = Read("backend", "PlantaoPro.Web", "Controllers", "AgendaBffController.cs");
        var api = Read("backend", "PlantaoPro.Api", "Controllers", "AgendaOperacionalController.cs");

        foreach (var route in new[] { "eventos", "conflitos", "medicos", "hospitais" })
            Assert.Contains($"HttpGet(\"{route}\")", bff);

        Assert.Contains("HttpPost(\"eventos\")", api);
        Assert.Contains("HttpPut(\"eventos/{id:guid}\")", api);
        Assert.Contains("HttpPost(\"eventos/{id:guid}/resolver-conflito\")", api);
        Assert.Contains("cliente_id=@clienteId", api);
        Assert.Contains("GetClienteId()", api);
        Assert.Contains("string.IsNullOrWhiteSpace(request.Resolucao)", api);
    }

    [Fact]
    public void AgendaAvancada_OfereceVisoesOperacionaisReais()
    {
        var controller = Read("backend", "PlantaoPro.Web", "Controllers", "AgendaController.cs");
        var view = Read("backend", "PlantaoPro.Web", "Views", "Agenda", "Index.cshtml");

        foreach (var route in new[] { "/agenda/calendario", "/agenda/medicos", "/agenda/hospitais" })
            Assert.Contains(route, controller);
        foreach (var label in new[] { "Timeline", "Calendário", "Por médico", "Por hospital", "Conflitos" })
            Assert.Contains(label, view);
    }
}
