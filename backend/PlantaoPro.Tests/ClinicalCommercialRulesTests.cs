using PlantaoPro.Domain.Financeiro;

namespace PlantaoPro.Tests;

public sealed class ClinicalCommercialRulesTests
{
    [Fact]
    public void Recebimento_exige_dados_e_bloqueia_duplicidade_e_caixa_fechado()
    {
        Assert.True(ClinicalCommercialRules.PodeReceber(100m, DateTime.UtcNow, "PIX", false, false));
        Assert.False(ClinicalCommercialRules.PodeReceber(100m, DateTime.UtcNow, "PIX", true, false));
        Assert.False(ClinicalCommercialRules.PodeReceber(100m, DateTime.UtcNow, "PIX", false, true));
        Assert.False(ClinicalCommercialRules.PodeReceber(-1m, DateTime.UtcNow, "PIX", false, false));
    }

    [Fact]
    public void Desconto_nao_supera_valor_ou_limite()
    {
        Assert.True(ClinicalCommercialRules.DescontoValido(100m, 10m, 15m));
        Assert.False(ClinicalCommercialRules.DescontoValido(100m, 16m, 15m));
        Assert.False(ClinicalCommercialRules.DescontoValido(100m, 101m, 150m));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("não")]
    public void Estorno_cancelamento_e_glosa_exigem_justificativa_util(string? justificativa)
    {
        Assert.False(ClinicalCommercialRules.JustificativaValida(justificativa));
    }

    [Fact]
    public void Convenio_suspenso_ou_contrato_vencido_nao_autoriza()
    {
        var hoje = new DateOnly(2026, 9, 22);
        Assert.False(ClinicalCommercialRules.PodeAutorizarConvenio("SUSPENSO", hoje.AddDays(1), hoje));
        Assert.False(ClinicalCommercialRules.PodeAutorizarConvenio("ATIVO", hoje.AddDays(-1), hoje));
        Assert.True(ClinicalCommercialRules.PodeAutorizarConvenio("ATIVO", hoje, hoje));
    }

    [Fact]
    public void Autorizacao_negada_bloqueia_faturamento()
    {
        Assert.False(ClinicalCommercialRules.PodeFaturarConvenio("NEGADA"));
        Assert.True(ClinicalCommercialRules.PodeFaturarConvenio("APROVADA"));
    }

    [Fact]
    public void Plano_inativo_ou_vencido_nao_pode_ser_usado()
    {
        var hoje = new DateOnly(2026, 9, 22);
        Assert.False(ClinicalCommercialRules.PlanoPodeSerUsado("INATIVO", hoje.AddDays(1), hoje));
        Assert.False(ClinicalCommercialRules.PlanoPodeSerUsado("ATIVO", hoje.AddDays(-1), hoje));
        Assert.True(ClinicalCommercialRules.PlanoPodeSerUsado("ATIVO", hoje, hoje));
    }

    [Fact]
    public void Repasse_exige_atendimento_finalizado_evento_financeiro_e_unicidade()
    {
        Assert.True(ClinicalCommercialRules.PodeGerarRepasse("FINALIZADA", false, true, false, false));
        Assert.False(ClinicalCommercialRules.PodeGerarRepasse("FINALIZADA", false, false, false, false));
        Assert.True(ClinicalCommercialRules.PodeGerarRepasse("FINALIZADA", true, false, true, false));
        Assert.False(ClinicalCommercialRules.PodeGerarRepasse("FINALIZADA", true, true, true, true));
    }

    [Fact]
    public void Medico_visualiza_somente_repasse_proprio()
    {
        var medico = Guid.NewGuid();
        Assert.True(ClinicalCommercialRules.PodeVisualizarRepasse(false, false, medico, medico));
        Assert.False(ClinicalCommercialRules.PodeVisualizarRepasse(false, false, medico, Guid.NewGuid()));
        Assert.True(ClinicalCommercialRules.PodeVisualizarRepasse(false, true, Guid.Empty, Guid.NewGuid()));
    }
}
