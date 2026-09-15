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
    public void Central_DeveIntegrarConferenciaCorrecoesEOcorrenciasSemCopiarEstado()
    {
        var source = Api("ProductivityActionServices.cs");
        foreach (var origin in new[] { "medico_checkins", "medico_presenca_correcoes", "ocorrencias_operacionais" })
            Assert.Contains(origin, source, StringComparison.Ordinal);
        foreach (var state in new[] { "status_conferencia", "x.status='PENDENTE'", "o.situacao not in ('RESOLVIDA','CANCELADA')" })
            Assert.Contains(state, source, StringComparison.Ordinal);
        Assert.Contains("CreatedAt,Key", source, StringComparison.Ordinal);
        Assert.Contains("not @mine or OwnerId=@userId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Central_DeveUsarAcoesObjetivasEAjudarSemConfundirLeituraComResolucao()
    {
        var root = Directory.GetParent(RepositoryPathResolver.ApiRoot)!.FullName;
        var controller = Api(Path.Combine("Controllers", "ProductivityActionController.cs"));
        var view = File.ReadAllText(Path.Combine(root, "PlantaoPro.Web", "Views", "Pendencias", "Index.cshtml"));
        foreach (var label in new[] { "Confirmar plantão", "Revisar correção", "Atribuir responsável", "Conferir execução" })
            Assert.Contains(label, controller, StringComparison.Ordinal);
        Assert.Contains("ler uma notificação não o resolve", view, StringComparison.Ordinal);
        Assert.Contains("Sem prazo definido", view, StringComparison.Ordinal);
        Assert.Contains("name=\"periodFrom\"", view, StringComparison.Ordinal);
        Assert.Contains("name=\"mine\"", view, StringComparison.Ordinal);
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
        foreach (var label in new[] { "Motivo:", "Origem:", "Responsabilidade:", "Sem prazo definido" }) Assert.Contains(label, view, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"#\"", view, StringComparison.Ordinal);
    }
}
