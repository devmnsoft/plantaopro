using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B27 — §27 linha 205 residual: emitir EXIBIR as definições de KPI em ADMIN360 (Fluxo de Caixa).
///
/// O painel de Fluxo de Caixa exibe 8 indicadores consolidados (saldos, entradas/saídas realizadas,
/// previsões AR/AP e vencidos em aberto) computados pela API em CaixaRepository.FluxoCaixaAsync
/// (SQL real + fórmulas derivadas no servidor: abertura/fechamento). Antes, as definições eram um
/// bloco ESTÁTICO no .cshtml. Esta fatia EMITE (na camada Api, fiel ao SQL real) E EXIBE (na Web),
/// substituindo o bloco estático.
///
/// Detalhe arquitetural: o DTO vive em Application (que só referencia Domain); a emissão usa um record de
/// resposta da CAMADA API (FluxoCaixaResposta) que mantém os mesmos campos de topo de FluxoCaixaDto +
/// Definicoes/TimeCriterion para a BFF mapear 1:1 por nome (STJ) no FluxoCaixaViewModel — mesmo padrão da B25.
/// </summary>
public sealed class R8B27FluxoCaixaDefinicoesContratoTests
{
    [Fact]
    public void Api_FluxoCaixa_EmiteDefinicoesFieisAoSql()
    {
        var ctrl = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Controllers", "Adm360CaixaController.cs");

        Assert.Contains("public sealed record FluxoCaixaResposta(", ctrl);
        Assert.Contains("IReadOnlyList<KpiDefinicao> Definicoes", ctrl);
        Assert.Contains("new FluxoCaixaResposta(", ctrl);

        // o texto é fiel ao SQL real…
        Assert.Contains("situacao IN ('ABERTO','PARCIAL')", ctrl);
        Assert.Contains("'PENDENTE_APROVACAO','APROVADO','PARCIAL'", ctrl);
        Assert.Contains("data_movimento < @inicio", ctrl);
        Assert.Contains("adm360_movimentos_financeiros", ctrl);
    }

    [Fact]
    public void Web_ViewModelFluxoCaixa_CarregaDefinicoesETimeCriterion()
    {
        var vm = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Models", "Administrativo360Models.cs");
        Assert.Contains("public sealed record FluxoCaixaViewModel(", vm);
        Assert.Contains("definições dos 8 indicadores de Fluxo de Caixa", vm);
        Assert.Contains("IReadOnlyList<KpiDefinicaoDto>? Definicoes { get; init; }", vm);
        Assert.Contains("public string TimeCriterion { get; init; } = \"\";", vm);
    }

    [Fact]
    public void Web_ViewFluxoCaixa_RenderizaDefinicoesESubstituiBlocoEstatico()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "Administrativo360", "FluxoCaixa.cshtml");
        Assert.Contains("Como cada indicador é calculado", view);
        Assert.Contains("@f.TimeCriterion", view);
        Assert.Contains("item.Label", view);
        Assert.Contains("item.Definicao", view);
        Assert.Contains("Sem definições informadas pela fonte", view);
    }
}
