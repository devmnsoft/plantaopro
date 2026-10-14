using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B22 — §27 linha 205 residual (parte 3): emitir EXIBIR as definições de KPI em MEU DIA.
///
/// Os indicadores de Meu Dia (Pendências/Críticas/Hoje/Atrasadas) são computados pela API em
/// ProductivityActionService.SummaryAsync (um único summarySql), devolvidos por api/produtividade/meu-dia
/// como o campo 'summary' (ProductivitySummaryDto) e desserializados pela BFF Web em
/// ProductivitySummaryViewModel. Diferente de BI/Dashboard, Meu Dia NÃO emitia definições (fora do escopo
/// B9). Esta fatia EMITE (na API, fiel ao SQL real) E EXIBE (na Web) — fechando o residual §27-205 para o
/// painel diário mais usado. Sem nova consulta/número/regra: só metadado descrevendo o cálculo existente.
///
/// Contrato anti-drift (leitura de fonte):
/// (1) a API emite: ProductivitySummaryDto carrega Definicoes+TimeCriterion (init) e a controller shapeia o
///     resumo via 'with' c/ texto fiel ao SQL (Priority='CRITICA', DueAt<now(), now() servidor);
/// (2) a BFF Web carrega: ProductivitySummaryViewModel tem Definicoes+TimeCriterion (espelho KpiDefinicaoDto);
/// (3) a view Meu Dia as renderiza (TimeCriterion + Label/Definicao por indicador + estado vazio).
/// </summary>
public sealed class R8B22MeuDiaDefinicoesContratoTests
{
    [Fact]
    public void Api_ResumoProdutividade_EmiteDefinicoesFieisAoSql()
    {
        var model = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "ProductivityActionModels.cs");
        var ctrl = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Controllers", "ProductivityActionController.cs");

        Assert.Contains("IReadOnlyList<KpiDefinicao>? Definicoes { get; init; }", model);
        Assert.Contains("public string TimeCriterion { get; init; }", model);

        // a controller anexa as definições ao resumo e o texto é fiel ao SQL real…
        Assert.Contains("with { Definicoes = DefinicoesMeuDia()", ctrl);
        Assert.Contains("Priority='CRITICA'", ctrl);
        Assert.Contains("DueAt < now()", ctrl);
    }

    [Fact]
    public void Web_ViewModelMeuDia_CarregaDefinicoesETimeCriterion()
    {
        var vm = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Models", "ProductivityViewModels.cs");
        Assert.Contains("IReadOnlyList<KpiDefinicaoDto>? Definicoes", vm);
        Assert.Contains("public string TimeCriterion", vm);
    }

    [Fact]
    public void Web_ViewMeuDia_RenderizaDefinicoes()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "MeuDia", "Index.cshtml");
        Assert.Contains("Como cada indicador é calculado", view);
        Assert.Contains("@Model.Summary.TimeCriterion", view);
        Assert.Contains("item.Label", view);
        Assert.Contains("item.Definicao", view);
        Assert.Contains("Sem definições informadas pela fonte", view);
    }
}
