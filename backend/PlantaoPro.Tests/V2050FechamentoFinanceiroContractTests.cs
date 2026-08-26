namespace PlantaoPro.Tests;

public sealed class V2050FechamentoFinanceiroContractTests
{
    private static string Read(string path)=>File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,path));

    [Fact]
    public void Apuracao_filtra_no_tenant_e_nao_expoe_campos_de_id()
    {
        var service=Read("backend/PlantaoPro.Api/Fechamentos/FechamentoOperacionalService.cs");
        var view=Read("backend/PlantaoPro.Web/Views/OperacaoPremium/Fechamentos.cshtml");
        Assert.Contains("f.tenant_id=@Tenant and f.cliente_id=@Cliente",service);
        Assert.Contains("@UnidadeId is null",service);
        Assert.Contains("@Profissional=''",service);
        Assert.Contains("type=\"date\"",view);
        Assert.Contains("<select class=\"form-select\" id=\"unidadeId\"",view);
        Assert.DoesNotContain("Digite o ID",view,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Aprovacao_rejeicao_e_financeiro_possuem_transicoes_e_idempotencia()
    {
        var service=Read("backend/PlantaoPro.Api/Fechamentos/FechamentoOperacionalService.cs");
        var workflow=Read("backend/PlantaoPro.Domain/Financeiro/FechamentoWorkflow.cs");
        Assert.Contains("FechamentoStatus.AguardandoAprovacao",service);
        Assert.Contains("Selecione um motivo de rejeição válido",service);
        Assert.Contains("select id from plantaopro.pagamentos where escala_id=@EscalaId",service);
        Assert.Contains("Rejeitado",workflow);
    }

    [Fact]
    public void Exportacao_csv_respeita_filtros_tenant_e_auditoria()
    {
        var service=Read("backend/PlantaoPro.Api/Fechamentos/FechamentoOperacionalService.cs");
        var controller=Read("backend/PlantaoPro.Api/Controllers/FechamentosController.cs");
        Assert.Contains("ExportarCsvAsync",service);
        Assert.Contains("Relatório financeiro CSV exportado",service);
        Assert.Contains("exportar.csv",controller);
        Assert.Contains("Authorize(Roles",controller);
    }
}
