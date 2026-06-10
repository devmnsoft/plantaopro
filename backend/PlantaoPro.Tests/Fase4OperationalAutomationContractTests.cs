using Xunit;

namespace PlantaoPro.Tests;

public sealed class Fase4OperationalAutomationContractTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(GetRepoRoot(), path));

    [Fact]
    public void Fase4MigrationContainsOperationalAutomationTables()
    {
        var sql = Read("database/migrations/2026_plantao_pro_automacao_operacional_fase4.sql");
        Assert.Contains("create table if not exists plantaopro.medico_disponibilidades", sql);
        Assert.Contains("create table if not exists plantaopro.escala_sugestoes", sql);
        Assert.Contains("create table if not exists plantaopro.substituicoes_plantao", sql);
        Assert.Contains("create table if not exists plantaopro.notificacao_regras", sql);
        Assert.Contains("create table if not exists plantaopro.pendencias_operacionais", sql);
        Assert.Contains("create table if not exists plantaopro.relatorios_exportacoes", sql);
        Assert.Contains("create table if not exists plantaopro.conversas", sql);
        Assert.Contains("create table if not exists plantaopro.mensagem_leituras", sql);
        Assert.Contains("add column if not exists updated_by", sql);
        Assert.Contains("create index if not exists", sql);
        Assert.DoesNotContain("add constraint if not exists", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiExposesFase4OperationalEndpoints()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/Fase4OperationalController.cs");
        Assert.Contains("api/medicos/me/disponibilidade", controller);
        Assert.Contains("api/medicos/me/indisponibilidade", controller);
        Assert.Contains("api/medicos/me/preferencias", controller);
        Assert.Contains("medicos-disponiveis", controller);
        Assert.Contains("gerar-sugestoes", controller);
        Assert.Contains("convidar-sugeridos", controller);
        Assert.Contains("api/substituicoes", controller);
        Assert.Contains("api/pendencias", controller);
        Assert.Contains("api/relatorios", controller);
    }

    [Fact]
    public void ComunicacaoApiSupportsOperationalConversationLifecycleUsedByWeb()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/ComunicacaoController.cs");
        var detalhes = Read("backend/PlantaoPro.Api/Controllers/Fase4ComunicacaoNotificacoesController.cs");
        var models = Read("backend/PlantaoPro.Api/Models.cs");

        Assert.Contains("MensagemInicial", models);
        Assert.Contains("pageSize", controller);
        Assert.Contains("search", controller);
        Assert.Contains("api/comunicacao/conversas", Read("backend/PlantaoPro.Web/Controllers/ComunicacaoController.cs"));
        Assert.Contains("mensagens/{id:guid}/lida", controller);
        Assert.Contains("ComunicacaoConversaDetalheDto", detalhes);
        Assert.Contains("Participantes", detalhes);
        Assert.Contains("MinhaMensagem", detalhes);
    }

    [Fact]
    public void ServicesImplementDeterministicSuggestionAndAuditedOperationalFlows()
    {
        var service = Read("backend/PlantaoPro.Api/Fase4OperationalServices.cs");
        Assert.Contains("AplicarScore", service);
        Assert.Contains("Indisponível no período", service);
        Assert.Contains("Conflito de agenda", service);
        Assert.Contains("CriarPendenciaInternaAsync", service);
        Assert.Contains("audit.LogAsync", service);
        Assert.Contains("relatorios_exportacoes", service);
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend", "PlantaoPro.sln")))
        {
            dir = dir.Parent;
        }
        if (dir is null) throw new InvalidOperationException("Repo root not found.");
        return dir.FullName;
    }
}
