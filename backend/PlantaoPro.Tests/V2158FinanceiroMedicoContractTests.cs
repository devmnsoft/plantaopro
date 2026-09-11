namespace PlantaoPro.Tests;

public sealed class V2158FinanceiroMedicoContractTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, path));

    [Fact]
    public void Fechamento_gera_obrigacao_aprovada_com_tenant_e_snapshot()
    {
        var service = Read("backend/PlantaoPro.Api/Fechamentos/FechamentoOperacionalService.cs");
        Assert.Contains("tenant_id,cliente_id", service);
        Assert.Contains("valor_previsto,valor_apurado,valor_aprovado", service);
        Assert.Contains("'aprovado'", service);
        Assert.Contains("parametros_apuracao", service);
    }

    [Fact]
    public void Baixa_integral_e_contestacao_preservam_invariantes()
    {
        var service = Read("backend/PlantaoPro.Api/Data.cs");
        Assert.Contains("for update", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Somente obrigação aprovada", service);
        Assert.Contains("req.ValorPago != pg.ValorAprovado", service);
        Assert.Contains("CONTESTACAO_ABERTA", service);
        Assert.DoesNotContain("set status='contestado'", service, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_protege_snapshot_referencia_e_quitacao()
    {
        var sql = Read("database/schema/360_v2158_financeiro_medico_conferencia.sql");
        Assert.Contains("versao bigint", sql);
        Assert.Contains("valor_pago=valor_aprovado", sql);
        Assert.Contains("ux_v2158_pagamento_referencia_manual", sql);
        Assert.Contains("origem_pagamento", sql);
    }
}
