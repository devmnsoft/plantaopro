using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B21 — §27 linha 205 residual (parte 2): exibir as definições de KPI no PAINEL OPERACIONAL (Home/Dashboard).
///
/// R8-B9 fez o serviço de dashboard EMITIR Definicoes[] + TimeCriterion na API (Data.cs DashboardService);
/// R8-B20 exibiu-as no BI. Esta fatia fecha o mesmo RESIDUAL no painel principal: a Web consumia
/// /api/dashboard (HomeController) e o mirror DashboardDto DROPOU Definicoes/TimeCriterion (dívida B11),
/// então o usuário via os números sem a fórmula/fonte/período/fuso.
///
/// Mudança (só apresentação + espelho do DTO; sem nova consulta/número/regra clínica):
///   - Web ganha um mirror KpiDefinicaoDto (a Web MVC NÃO referencia o projeto PlantaoPro.Api);
///   - o mirror DashboardDto carrega Definicoes + TimeCriterion (parâmetros COM PADRÃO, sem quebrar as
///     construções de 13 argumentos e a expressão 'with' do filtro);
///   - Home/Dashboard renderiza um bloco "Como cada indicador é calculado" (detalhe, server-side, com
///     codificação HTML automática do Razor) a partir do payload já recebido; lista vazia =&gt; estado explícito.
///
/// Contrato anti-drift (leitura de fonte):
/// (1) o mirror Web DashboardDto carrega Definicoes + TimeCriterion (+ o espelho KpiDefinicaoDto existe);
/// (2) a view Home/Dashboard as renderiza (TimeCriterion + Label/Definicao por indicador + estado vazio);
/// (3) a fonte (Data.cs) continua emitindo Definicoes/TimeCriterion no dashboard.
/// </summary>
public sealed class R8B21DashboardDefinicoesContratoTests
{
    [Fact]
    public void Web_DashboardDto_CarregaDefinicoesETimeCriterion()
    {
        var vm = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Models", "ViewModels.cs");

        // Espelho Web do KpiDefinicao (a Web não referencia o projeto Api)...
        Assert.Contains("record KpiDefinicaoDto(string Codigo, string Label, string Definicao)", vm);
        // ...e o mirror DashboardDto agora carrega os campos (com padrão, p/ não quebrar c/13 args e o 'with').
        Assert.Contains("IReadOnlyList<KpiDefinicaoDto>? Definicoes = null", vm);
        Assert.Contains("string TimeCriterion = \"\"", vm);
    }

    [Fact]
    public void Web_DashboardView_RenderizaDefinicoes()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "Home", "Dashboard.cshtml");

        Assert.Contains("Como cada indicador é calculado", view);
        Assert.Contains("@indicadores.TimeCriterion", view);
        Assert.Contains("item.Label", view);
        Assert.Contains("item.Definicao", view);
        Assert.Contains("Sem definições informadas pela fonte", view);
    }

    [Fact]
    public void Api_Dashboard_EmiteDefinicoesEPopulaNoResultado()
    {
        var data = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Data.cs");

        // A fonte que a view consome continua emitindo Definicoes + TimeCriterion por KPI...
        Assert.Contains("ind.Definicoes = Definicoes()", data);
        Assert.Contains("ind.TimeCriterion =", data);
        Assert.Contains("new KpiDefinicao(\"TotalMedicos\"", data);
        Assert.Contains("new KpiDefinicao(\"ValorPendente\"", data);
    }
}
