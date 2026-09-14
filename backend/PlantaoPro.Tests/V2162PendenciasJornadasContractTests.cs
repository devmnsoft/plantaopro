namespace PlantaoPro.Tests;

public sealed class V2162PendenciasJornadasContractTests
{
    private static string Api(string file) => File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, file));

    [Fact]
    public void Central_DeveDerivarAsSeisCategoriasDasOrigensEIsolarTenant()
    {
        var source = Api("ProductivityActionServices.cs");
        foreach (var origin in new[] { "cobertura_convites", "plantaopro.escalas", "plantaopro.pagamentos pg", "pagamento_contestacoes", "plantaopro.agendamentos", "plantaopro.consultas" })
            Assert.Contains(origin, source, StringComparison.Ordinal);
        Assert.True(source.Split("tenant_id=@tenantId", StringSplitOptions.None).Length >= 7);
        Assert.DoesNotContain("Plantão sem cobertura", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Central_DeveUsarEstadoDaOrigemResumoIntegralEAcaoCanonica()
    {
        var source = Api("ProductivityActionServices.cs");
        Assert.Contains("with derived as (", source, StringComparison.Ordinal);
        Assert.Contains("count(*) filter", source, StringComparison.Ordinal);
        Assert.Contains("FindActiveAsync(Tenant,User,key", source, StringComparison.Ordinal);
        Assert.Contains("PRODUCTIVITY_ITEM_SNOOZE", source, StringComparison.Ordinal);
        Assert.DoesNotContain("concluir", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tela_DeveDistinguirErroEVazioEExplicarOrigemResponsavelEData()
    {
        var root = Directory.GetParent(RepositoryPathResolver.ApiRoot)!.FullName;
        var view = File.ReadAllText(Path.Combine(root, "PlantaoPro.Web", "Views", "Pendencias", "Index.cshtml"));
        Assert.Contains("Não foi possível carregar as pendências", view, StringComparison.Ordinal);
        Assert.Contains("Nenhuma ação nesta visão", view, StringComparison.Ordinal);
        foreach (var label in new[] { "Motivo:", "Origem:", "Responsável:", "Data relevante:" }) Assert.Contains(label, view, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"#\"", view, StringComparison.Ordinal);
    }
}
