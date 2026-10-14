using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B25 — §27 linha 205 residual (parte 6): emitir EXIBIR as definições de KPI em ADMIN360 (Gestão/Dashboard).
///
/// O dashboard gerencial Admin360 exibe 3 grupos de indicadores (Cotações 10 valores, Documentos XML 6,
/// Integrações 4) computados pela API em GestaoDashboardRepository.ObterDashboardAsync (cotacoesSql/xmlSql/
/// integracoesSql + fórmulas derivadas no servidor: taxa de sucesso e tempo médio). Como os demais painéis,
/// Admin360 NÃO emitia definições. Esta fatia EMITE (na camada Api, fiel ao SQL real) E EXIBE (na Web).
///
/// Detalhe arquitetural: o DTO de dashboard vive em Application (que só referencia Domain), enquanto KpiDefinicao
/// está em Api. Para respeitar a camadas, a emissão usa um record de resposta da CAMADA API (DashboardAdm360Resposta)
/// que carrega os campos do DTO + Definicoes/TimeCriterion — mesmo padrão da B22 (definições no controller Api).
///
/// Contrato anti-drift (leitura de fonte):
/// (1) a API emite: controller monta DashboardAdm360Resposta com Definicoes (IReadOnlyList<KpiDefinicao>) c/ texto
///     fiel ao SQL ('VENCEDORA', prazo_resposta <= now(), EXTRACT(EPOCH, status_integracao 'CONFIGURADA');
/// (2) a BFF Web carrega: DashboardAdm360ViewModel tem Definicoes+TimeCriterion (espelho KpiDefinicaoDto);
/// (3) a view Gestão/Dashboard as renderiza (TimeCriterion + Label/Definicao por indicador + estado vazio).
/// </summary>
public sealed class R8B25GestaoDashboardDefinicoesContratoTests
{
    [Fact]
    public void Api_GestaoDashboard_EmiteDefinicoesFieisAoSql()
    {
        var ctrl = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Controllers", "Adm360GestaoController.cs");

        Assert.Contains("public sealed record DashboardAdm360Resposta(", ctrl);
        Assert.Contains("IReadOnlyList<KpiDefinicao> Definicoes", ctrl);
        Assert.Contains("new DashboardAdm360Resposta(", ctrl);

        // o texto é fiel ao SQL real…
        Assert.Contains("'VENCEDORA'", ctrl);
        Assert.Contains("prazo_resposta <= now()", ctrl);
        Assert.Contains("EXTRACT(EPOCH", ctrl);
        Assert.Contains("status_integracao 'CONFIGURADA'", ctrl);
    }

    [Fact]
    public void Web_ViewModelGestaoDashboard_CarregaDefinicoesETimeCriterion()
    {
        var vm = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Models", "Administrativo360CotacoesXmlModels.cs");
        Assert.Contains("IReadOnlyList<KpiDefinicaoDto>? Definicoes", vm);
        Assert.Contains("public string TimeCriterion", vm);
    }

    [Fact]
    public void Web_ViewGestaoDashboard_RenderizaDefinicoes()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Gestao", "Dashboard.cshtml");
        Assert.Contains("Como cada indicador é calculado", view);
        Assert.Contains("@Model.Dashboard.TimeCriterion", view);
        Assert.Contains("item.Label", view);
        Assert.Contains("item.Definicao", view);
        Assert.Contains("Sem definições informadas pela fonte", view);
    }
}
