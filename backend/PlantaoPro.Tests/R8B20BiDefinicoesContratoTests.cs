using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B20 — §27 linha 205 residual: "Web não exibe Definicoes" (definição e rastreabilidade de KPIs).
///
/// R8-B9 ESTENDEU o padrão Operacional às calculadoras para que cada KPI EMITA sua definição
/// (Codigo/Label/Definicao) + TimeCriterion na API (BI resumo, dashboard, etc.). Esta fatia fecha o
/// RESIDUAL registrado em B9/B11: a view do BI JÁ recebe essas definições no payload de
/// /api/bi/resumo-executivo (data.definicoes / data.timeCriterion), mas NUNCA as renderizava.
///
/// Mudança (só presentation, sem nova consulta/número/regra clínica): Bi/Index ganha um bloco
/// colapsável "Como cada indicador é calculado" que popula do payload (timeCriterion + uma linha por
/// indicador, escapada). Em falha, as definições exibem estado de indisponibilidade — o mesmo critério
/// "Zero ≠ informação indisponível" da B19, aplicado às definições.
///
/// Contrato anti-drift (leitura de fonte):
/// (1) a view renderiza as definições (markup + JS lê d.definicoes/d.timeCriterion, escapa e lista por
///     indicador, com estado "indisponíveis" no catch);
/// (2) a fonte que a view consome continua emitindo (BiServices popula row.Definicoes + row.TimeCriterion
///     com KpiDefinicao por KPI) e o DTO carrega os campos;
/// (3) o record KpiDefinicao tem a forma Codigo/Label/Definicao.
/// </summary>
public sealed class R8B20BiDefinicoesContratoTests
{
    [Fact]
    public void Bi_View_RenderizaDefinicoesDosIndicadores()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "Bi", "Index.cshtml");

        // Markup do bloco de definições + campo de critério de tempo existem…
        Assert.Contains("<details", view);
        Assert.Contains("id=\"bi-definicoes\"", view);
        Assert.Contains("id=\"bi-timecriterion\"", view);

        // …e o JS popula do payload (d.definicoes / d.timeCriterion), escapando e listando por indicador…
        Assert.Contains("d.definicoes", view);
        Assert.Contains("d.timeCriterion", view);
        Assert.Contains("esc(x.label)", view);
        Assert.Contains("esc(x.definicao)", view);

        // …e distingue falha: em erro as definições ficam "indisponíveis" (não silenciosas/vazias).
        Assert.Contains("Definições indisponíveis", view);
    }

    [Fact]
    public void Bi_Api_EmiteDefinicoesEPopulaNoResumo()
    {
        var svc = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "BiServices.cs");
        var dto = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Models.cs");

        // A fonte que a view consome: população explícita de Definicoes + TimeCriterion, por KPI…
        Assert.Contains("row.Definicoes = Definicoes()", svc);
        Assert.Contains("row.TimeCriterion =", svc);
        Assert.Contains("new KpiDefinicao(\"TotalClientesAtivos\"", svc);
        Assert.Contains("new KpiDefinicao(\"PercentualCobertura\"", svc);

        // …e o contrato DTO → payload carrega ambos os campos.
        Assert.Contains("public IReadOnlyList<KpiDefinicao> Definicoes", dto);
        Assert.Contains("public string TimeCriterion", dto);
    }

    [Fact]
    public void KpiDefinicao_Record_TemCodigoLabelEDefinicao()
    {
        var kpi = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "KpiDefinicao.cs");
        Assert.Contains("record KpiDefinicao(string Codigo, string Label, string Definicao)", kpi);
    }
}
