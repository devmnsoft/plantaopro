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

        var subtotal = Math.Max(0m, (quantidadeConsumida * precoUnitario) - descontoItem);
        var totalCusto = quantidadeConsumida * custoUnitario;
        return (subtotal, totalCusto);
    }
}

public static class VendaRegras
{
    public static IReadOnlyList<(int Parcela, DateOnly Vencimento, decimal Valor)> GerarParcelas(
        decimal totalLiquido, int quantidadeParcelas, DateOnly dataEmissao)
    {
        if (totalLiquido <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalLiquido), "Total líquido da venda deve ser positivo para gerar títulos.");
        if (quantidadeParcelas <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidadeParcelas), "Quantidade de parcelas deve ser pelo menos 1.");

        var parcelas = new List<(int Parcela, DateOnly Vencimento, decimal Valor)>();
        var valorBase = Math.Round(totalLiquido / quantidadeParcelas, 2);
        var somaBase = valorBase * quantidadeParcelas;
        var residuo = totalLiquido - somaBase;

        for (int i = 1; i <= quantidadeParcelas; i++)
        {
            var vencimento = dataEmissao.AddMonths(i);
            var valor = valorBase;
            if (i == quantidadeParcelas)
            {
                // Resíduo de centavos alocado na última parcela
                valor += residuo;
            }
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
