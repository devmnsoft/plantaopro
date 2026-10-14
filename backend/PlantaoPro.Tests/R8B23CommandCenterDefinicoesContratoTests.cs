using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B23 — §27 linha 205 residual (parte 4): emitir EXIBIR as definições de KPI em COMMAND CENTER.
///
/// Os 8 indicadores operacionais do Command Center (Risco agora/Descobertos/Confirmação/Em Execução/Ocorrências/
/// Profissionais/Financeiro/Alertas) são computados pela API em ManagerCommandCenterService.GetAsync (SqlCommandCenter),
/// devolvidos por api/manager-command-center como 'summary' (CommandCenterSummary) e desserializados pela BFF Web em
/// ManagerSummary. Como Meu Dia, CC NÃO emitia definições (fora do escopo B9). Esta fatia EMITE (na API, fiel ao SQL
/// real) E EXIBE (na Web) — fechando mais uma tela do residual §27-205. Sem nova consulta/número/regra.
///
/// O branch GLOBAL (governança, métricas de plataforma) segue fora desta fatia (ver doc): exige emitir as próprias
/// definições em GetGlobalAsync.
///
/// Contrato anti-drift (leitura de fonte):
/// (1) a API emite: CommandCenterSummary carrega Definicoes+TimeCriterion (init) e GetAsync shapeia o resumo via 'with'
///     c/ texto fiel ao SQL (interval '6 hours', REGISTRO_INCOMPLETO, AGUARDANDO_APROVACAO);
/// (2) a BFF Web carrega: ManagerSummary tem Definicoes+TimeCriterion (espelho KpiDefinicaoDto);
/// (3) a view CommandCenter as renderiza (TimeCriterion + Label/Definicao por indicador + estado vazio).
/// </summary>
public sealed class R8B23CommandCenterDefinicoesContratoTests
{
    [Fact]
    public void Api_CommandCenter_EmiteDefinicoesFieisAoSql()
    {
        var svc = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "ManagerCommandCenterService.cs");

        Assert.Contains("IReadOnlyList<KpiDefinicao>? Definicoes { get; init; }", svc);
        Assert.Contains("public string TimeCriterion { get; init; }", svc);

        // GetAsync anexa as definições ao resumo e o texto é fiel ao SQL real…
        Assert.Contains("with { Definicoes = Definicoes()", svc);
        Assert.Contains("interval '6 hours'", svc);
        Assert.Contains("REGISTRO_INCOMPLETO", svc);
        Assert.Contains("AGUARDANDO_APROVACAO", svc);
    }

    [Fact]
    public void Web_ViewModelCommandCenter_CarregaDefinicoesETimeCriterion()
    {
        var vm = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Models", "ManagerCommandCenterViewModel.cs");
        Assert.Contains("IReadOnlyList<KpiDefinicaoDto>? Definicoes", vm);
        Assert.Contains("public string TimeCriterion", vm);
    }

    [Fact]
    public void Web_ViewCommandCenter_RenderizaDefinicoes()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "CommandCenter", "Index.cshtml");
        Assert.Contains("Como cada indicador é calculado", view);
        Assert.Contains("@Model.Summary.TimeCriterion", view);
        Assert.Contains("item.Label", view);
        Assert.Contains("item.Definicao", view);
        Assert.Contains("Sem definições informadas pela fonte", view);
    }
}
