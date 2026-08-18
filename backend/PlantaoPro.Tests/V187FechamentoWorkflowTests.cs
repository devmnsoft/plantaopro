using PlantaoPro.Domain.Financeiro;

namespace PlantaoPro.Tests;

public sealed class V187FechamentoWorkflowTests
{
    [Theory]
    [InlineData(FechamentoStatus.Aberto, FechamentoStatus.EmConferencia)]
    [InlineData(FechamentoStatus.EmConferencia, FechamentoStatus.ComDivergencia)]
    [InlineData(FechamentoStatus.EmConferencia, FechamentoStatus.AguardandoAprovacao)]
    [InlineData(FechamentoStatus.AguardandoAprovacao, FechamentoStatus.Aprovado)]
    [InlineData(FechamentoStatus.Aprovado, FechamentoStatus.FinanceiroGerado)]
    public void Permite_transicoes_explicitas(string origem, string destino) => Assert.True(FechamentoStatus.PodeTransicionar(origem, destino));

    [Theory]
    [InlineData(FechamentoStatus.Aberto, FechamentoStatus.Aprovado)]
    [InlineData(FechamentoStatus.ComDivergencia, FechamentoStatus.Aprovado)]
    [InlineData(FechamentoStatus.FinanceiroGerado, FechamentoStatus.Aprovado)]
    public void Rejeita_saltos_de_workflow(string origem, string destino) => Assert.False(FechamentoStatus.PodeTransicionar(origem, destino));

    [Fact]
    public void Calcula_valor_com_a_mesma_regra_do_pagamento_medico() => Assert.Equal(1200m, PlantaoPaymentCalculator.Calcular(1200m, new DateTime(2026,8,18,7,0,0), new DateTime(2026,8,18,19,0,0)));
}
