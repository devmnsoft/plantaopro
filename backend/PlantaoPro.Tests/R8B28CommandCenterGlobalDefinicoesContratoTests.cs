using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B28 — §27 linha 205 residual (parte final): emitir EXIBIR as definições no COMMAND CENTER GLOBAL.
///
/// O branch global (governança/plataforma) de ManagerCommandCenterService.GetGlobalAsync exibe métricas de
/// plataforma (tenants ativos, clientes ativos, módulos contratados) em vez dos 8 indicadores operacionais.
/// B23 cobriu apenas o branch TENANT (GetAsync); o global seguia sem emissão. Esta fatia EMITE (na API, fiel
/// ao SQL real de GetGlobalAsync — contagens globais, sem tenant/período) E EXIBE (na Web, no branch isGlobal).
/// </summary>
public sealed class R8B28CommandCenterGlobalDefinicoesContratoTests
{
    [Fact]
    public void Api_CommandCenterGlobal_EmiteDefinicoesDaPlataformaFieisAoSql()
    {
        var svc = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Api", "ManagerCommandCenterService.cs");

        Assert.Contains("with { Definicoes = DefinicoesPlataforma(), TimeCriterion = TempoCriticoPlataforma }", svc);
        Assert.Contains("private static IReadOnlyList<KpiDefinicao> DefinicoesPlataforma()", svc);
        // fiel ao SQL real…
        Assert.Contains("reg_status = 'A' e status = 'ATIVO'", svc);
        Assert.Contains("tenant_modulos com status = 'ATIVO'", svc);
    }

    [Fact]
    public void Web_ViewModelCommandCenter_CarregaDefinicoesETimeCriterion()
    {
        var vm = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Models", "ManagerCommandCenterViewModel.cs");
        Assert.Contains("IReadOnlyList<KpiDefinicaoDto>? Definicoes", vm);
        Assert.Contains("public string TimeCriterion", vm);
    }

    [Fact]
    public void Web_ViewCommandCenterGlobal_RenderizaDefinicoesDaPlataforma()
    {
        var view = RepositoryPathResolver.ReadRepositoryFile("backend", "PlantaoPro.Web", "Views", "CommandCenter", "Index.cshtml");
        Assert.Contains("Como cada indicador da plataforma é calculado", view);
        Assert.Contains("@Model.Summary.TimeCriterion", view);
    }
}
