namespace PlantaoPro.Domain.Administrativo360;

public static class ValorizacaoRegras
{
    public static void ValidarElegibilidadeVale(string situacaoVale, string situacaoFinanceira)
    {
        if (!string.Equals(situacaoVale, "RECONCILIADO", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Apenas vales na situação 'RECONCILIADO' podem ser valorizados. Situação atual: '{situacaoVale}'.");

        if (string.Equals(situacaoFinanceira, "VALORIZADO", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Este vale já foi valorizado anteriormente.");
    }

    public static void ValidarConsumo(decimal quantidadeConsumida, decimal precoUnitario)
    {
        if (quantidadeConsumida <= 0)
            throw new InvalidOperationException("O item não possui consumo registrado para valorização comercial.");
        if (precoUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(precoUnitario), "Preço unitário não pode ser negativo.");
    }

    public static (decimal Subtotal, decimal TotalCusto) CalcularTotaisItem(
        decimal quantidadeConsumida, decimal precoUnitario, decimal descontoItem, decimal custoUnitario)
    {
        ValidarConsumo(quantidadeConsumida, precoUnitario);
        if (descontoItem < 0) throw new ArgumentOutOfRangeException(nameof(descontoItem), "Desconto não pode ser negativo.");
        if (custoUnitario < 0) throw new ArgumentOutOfRangeException(nameof(custoUnitario), "Custo não pode ser negativo.");

        var subtotalBruto = quantidadeConsumida * precoUnitario;
        if (descontoItem > subtotalBruto)
            throw new InvalidOperationException($"Desconto do item ({descontoItem:C2}) não pode ser maior que o subtotal bruto ({subtotalBruto:C2}).");

        var subtotal = subtotalBruto - descontoItem;
        var totalCusto = quantidadeConsumida * custoUnitario;
        return (subtotal, totalCusto);
    }

    public static decimal CalcularDescontoProporcional(decimal subtotalItem, decimal totalOrcamento, decimal descontoOrcamento)
    {
        if (descontoOrcamento < 0) throw new ArgumentOutOfRangeException(nameof(descontoOrcamento), "Desconto não pode ser negativo.");
        if (descontoOrcamento > totalOrcamento) throw new InvalidOperationException("Desconto do orçamento não pode ser superior ao total do orçamento.");
        if (totalOrcamento <= 0) return 0m;

        decimal proporcao = subtotalItem / totalOrcamento;
        return Math.Round(descontoOrcamento * proporcao, 2);
    }

    public static void ValidarDesconto(decimal totalBruto, decimal desconto)
    {
        if (desconto < 0)
            throw new ArgumentOutOfRangeException(nameof(desconto), "Desconto não pode ser negativo.");
        if (desconto > totalBruto)
            throw new InvalidOperationException($"Desconto ({desconto:C2}) não pode ser superior ao total bruto ({totalBruto:C2}).");
    }
}

public static class VendaRegras
{
    public static IReadOnlyList<(int Parcela, DateOnly Vencimento, decimal Valor)> GerarParcelas(
        decimal totalLiquido, int quantidadeParcelas, DateOnly dataEmissao)
    {
        if (totalLiquido <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalLiquido), "Total líquido da venda deve ser positivo para gerar títulos.");
        if (quantidadeParcelas <= 0 || quantidadeParcelas > 60)
            throw new ArgumentOutOfRangeException(nameof(quantidadeParcelas), "Quantidade de parcelas deve estar entre 1 e 60.");

        long totalCentavos = (long)Math.Round(totalLiquido * 100m, MidpointRounding.AwayFromZero);
        if (totalCentavos < quantidadeParcelas)
            throw new InvalidOperationException($"Não é possível dividir R$ {totalLiquido:N2} em {quantidadeParcelas} parcelas pois o valor mínimo por parcela é R$ 0,01.");

        long centavosBase = totalCentavos / quantidadeParcelas;
        long restoCentavos = totalCentavos % quantidadeParcelas;

        var parcelas = new List<(int Parcela, DateOnly Vencimento, decimal Valor)>();
        for (int i = 1; i <= quantidadeParcelas; i++)
        {
            var vencimento = dataEmissao.AddMonths(i);
            long centavosParcela = centavosBase + (i <= restoCentavos ? 1 : 0);
            decimal valor = centavosParcela / 100m;
            parcelas.Add((i, vencimento, valor));
        }

        return parcelas;
    }
}

public static class ComissaoRegras
{
    public static decimal CalcularComissaoApropriada(decimal baseAcumulada, decimal valorBaixa, decimal percentual, decimal comissaoJaApropriada)
    {
        if (percentual < 0 || percentual > 100)
            throw new ArgumentOutOfRangeException(nameof(percentual), "Percentual de comissão deve estar entre 0 e 100.");
        if (valorBaixa <= 0)
            throw new ArgumentOutOfRangeException(nameof(valorBaixa), "Valor da baixa para comissão deve ser positivo.");

        var novaBaseTotal = baseAcumulada + valorBaixa;
        var comissaoTotalEsperada = Math.Round(novaBaseTotal * (percentual / 100m), 2);
        var comissaoParcela = comissaoTotalEsperada - comissaoJaApropriada;
        return Math.Max(0m, comissaoParcela);
    }

    public static decimal CalcularEstornoComissao(decimal valorEstornado, decimal percentual)
    {
        if (percentual < 0 || percentual > 100)
            throw new ArgumentOutOfRangeException(nameof(percentual), "Percentual de comissão deve estar entre 0 e 100.");
        if (valorEstornado <= 0)
            throw new ArgumentOutOfRangeException(nameof(valorEstornado), "Valor estornado deve ser positivo.");

        return Math.Round(valorEstornado * (percentual / 100m), 2);
    }
}

public static class TituloRegras
{
    public static void ValidarBaixa(decimal saldoAberto, decimal valorRecebido)
    {
        if (valorRecebido <= 0)
            throw new ArgumentOutOfRangeException(nameof(valorRecebido), "Valor recebido deve ser positivo.");
        if (valorRecebido > saldoAberto)
            throw new InvalidOperationException($"Valor recebido ({valorRecebido:C2}) excede o saldo em aberto do título ({saldoAberto:C2}).");
    }

    public static string DefinirNovaSituacao(decimal saldoRestante) =>
        saldoRestante <= 0 ? "QUITADO" : "PARCIAL";

    public static void ValidarEstorno(bool baixaEstornada, decimal valorEstorno, decimal valorBaixa)
    {
        if (baixaEstornada)
            throw new InvalidOperationException("Esta baixa já foi estornada anteriormente.");
        if (valorEstorno <= 0)
            throw new ArgumentOutOfRangeException(nameof(valorEstorno), "Valor do estorno deve ser positivo.");
        if (valorEstorno > valorBaixa)
            throw new InvalidOperationException("Valor de estorno não pode exceder o valor original da baixa.");
    }
}

public static class ContasPagarRegras
{
    public static void ValidarAprovacao(string situacaoAtual)
    {
        if (!string.Equals(situacaoAtual, "PENDENTE_APROVACAO", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Apenas títulos na situação 'PENDENTE_APROVACAO' podem ser aprovados. Situação atual: '{situacaoAtual}'.");
    }

    public static void ValidarPagamento(string situacaoAtual, decimal saldoAberto, decimal valorPago)
    {
        if (string.Equals(situacaoAtual, "CANCELADO", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(situacaoAtual, "PAGO", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(situacaoAtual, "PENDENTE_APROVACAO", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"O título a pagar está na situação '{situacaoAtual}' e não pode receber pagamentos.");
        }

        if (valorPago <= 0)
            throw new ArgumentOutOfRangeException(nameof(valorPago), "Valor pago deve ser positivo.");

        if (valorPago > saldoAberto)
            throw new InvalidOperationException($"Valor pago ({valorPago:C2}) excede o saldo em aberto do título ({saldoAberto:C2}).");
    }

    public static string DefinirNovaSituacao(decimal saldoRestante) =>
        saldoRestante <= 0 ? "PAGO" : "PARCIAL";

    public static string DefinirNovaSituacaoAposPagamento(decimal saldoRestante) =>
        DefinirNovaSituacao(saldoRestante);

    public static void ValidarEstorno(bool pagamentoEstornado, decimal valorEstorno, decimal valorPago)
    {
        if (pagamentoEstornado)
            throw new InvalidOperationException("Este pagamento já foi estornado anteriormente.");
        if (valorEstorno <= 0)
            throw new ArgumentOutOfRangeException(nameof(valorEstorno), "Valor do estorno deve ser positivo.");
        if (valorEstorno > valorPago)
            throw new InvalidOperationException("Valor de estorno não pode exceder o valor original do pagamento.");
    }
}

public static class CaixaRegras
{
    public static void ValidarPeriodo(DateOnly inicio, DateOnly fim)
    {
        if (inicio > fim)
            throw new ArgumentException($"Data inicial ({inicio:dd/MM/yyyy}) não pode ser maior que a data final ({fim:dd/MM/yyyy}).");
    }

    public static void ValidarSaldoSuficiente(decimal saldoAtual, decimal valorSaida, bool permitirSaldoNegativo = false)
    {
        if (!permitirSaldoNegativo && saldoAtual < valorSaida)
            throw new InvalidOperationException($"Saldo insuficiente na conta financeira para realizar a saída de {valorSaida:C2}. Saldo atual disponível: {saldoAtual:C2}.");
    }

    public static void ValidarFechamento(decimal saldoCalculado, decimal saldoConferido, string? justificativa)
    {
        decimal diferenca = Math.Abs(saldoConferido - saldoCalculado);
        if (diferenca > 0m && string.IsNullOrWhiteSpace(justificativa))
            throw new ArgumentException($"Existe uma diferença de {diferenca:C2} entre o saldo conferido e o calculado. A justificativa é obrigatória.");
    }

    public static void ValidarDataBloqueioFechamento(DateOnly dataMovimento, DateOnly? dataUltimoFechamento)
    {
        if (dataUltimoFechamento.HasValue && dataMovimento <= dataUltimoFechamento.Value)
            throw new InvalidOperationException($"Lançamento retroativo bloqueado: a conta financeira já está fechada até a data {dataUltimoFechamento.Value:dd/MM/yyyy}.");
    }
}
