using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B24 — §27 linha 205 residual (parte 5): emitir EXIBIR as definições de KPI em ADMIN360 (Home Organização).
///
/// Os 4 indicadores organizacionais do índice Admin360 (Departamentos/Cargos/Colaboradores ativos/Contratos vigentes)
/// são computados pela API em Administrativo360Service.ResumoAsync (um único select c/ 4 count(*)), devolvidos por
/// api/administrativo360/resumo como Administrativo360ResumoDto e desserializados pela BFF Web em
/// Administrativo360ResumoViewModel (dentro de OrganizacaoIndexViewModel.Resumo). Como os demais painéis, Admin360 NÃO
/// emitia definições. Esta fatia EMITE (na API, fiel ao SQL real) E EXIBE (na Web) — cobrindo a tela Home do Admin360.
/// Sem nova consulta/número/regra.
///
/// Restante do Admin360 segue fora desta fatia (ver doc): Gestao/Dashboard (14 tiles), FluxoCaixa (8, já tem bloco
/// estático B4/J12 no view), telas de lista com totais somados no Razor, e telas de detalhe.
///
/// Contrato anti-drift (leitura de fonte):
/// (1) a API emite: Administrativo360ResumoDto carrega Definicoes+TimeCriterion (init) e ResumoAsync shapeia via 'with'
///     c/ texto fiel ao SQL (reg_status 'A', status 'ATIVO', status 'VIGENTE');
/// (2) a BFF Web carrega: Administrativo360ResumoViewModel tem Definicoes+TimeCriterion (espelho KpiDefinicaoDto);
/// (3) a view Index as renderiza (TimeCriterion + Label/Definicao por indicador + estado vazio).
/// </summary>
public sealed class R8B24Admin360OrgDefinicoesContratoTests
{
    [Fact]
    public void Api_ResumoOrganizacional_EmiteDefinicoesFieisAoSql()
    {
        var dto = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Administrativo360Contracts.cs");
        var svc = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "Administrativo360Service.cs");

        Assert.Contains("IReadOnlyList<KpiDefinicao>? Definicoes { get; init; }", dto);
        Assert.Contains("public string TimeCriterion { get; init; }", dto);

        // ResumoAsync anexa as definições ao resumo e o texto é fiel ao SQL real…
        Assert.Contains("with { Definicoes = Definicoes()", svc);
        Assert.Contains("reg_status 'A'", svc);
        Assert.Contains("status 'ATIVO'", svc);
        Assert.Contains("status 'VIGENTE'", svc);
    }

    [Fact]
    public void Web_ViewModelAdmin360Org_CarregaDefinicoesETimeCriterion()
    {
        var vm = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Models", "Administrativo360Models.cs");
        Assert.Contains("IReadOnlyList<KpiDefinicaoDto>? Definicoes", vm);
        Assert.Contains("public string TimeCriterion", vm);
    }

    [Fact]
    public void Web_ViewAdmin360Index_RenderizaDefinicoes()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Index.cshtml");
        Assert.Contains("Como cada indicador é calculado", view);
        Assert.Contains("@Model.Resumo.TimeCriterion", view);
        Assert.Contains("item.Label", view);
        Assert.Contains("item.Definicao", view);
        Assert.Contains("Sem definições informadas pela fonte", view);
    }
}
