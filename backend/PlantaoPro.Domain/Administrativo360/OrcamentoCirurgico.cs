namespace PlantaoPro.Domain.Administrativo360;

public static class OrcamentoCirurgicoRegras
{
    public static decimal CalcularTotalItem(decimal quantidade, decimal precoUnitario, decimal desconto)
    {
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade), "Quantidade do item deve ser positiva.");
        if (precoUnitario < 0)
            throw new ArgumentOutOfRangeException(nameof(precoUnitario), "Preço unitário não pode ser negativo.");
        if (desconto < 0)
            throw new ArgumentOutOfRangeException(nameof(desconto), "Desconto não pode ser negativo.");

        var subtotal = decimal.Round(quantidade * precoUnitario, 4, MidpointRounding.AwayFromZero);
        if (desconto > subtotal)
            throw new InvalidOperationException("Desconto não pode superar o valor total do item.");

        return decimal.Round(subtotal - desconto, 4, MidpointRounding.AwayFromZero);
    }

    public static (decimal TotalProdutos, decimal TotalGeral) CalcularTotais(IEnumerable<decimal> totaisItens, decimal descontoGeral)
    {
        if (descontoGeral < 0)
            throw new ArgumentOutOfRangeException(nameof(descontoGeral), "Desconto geral não pode ser negativo.");

        var totalProdutos = decimal.Round(totaisItens.Sum(), 4, MidpointRounding.AwayFromZero);
        if (descontoGeral > totalProdutos)
            throw new InvalidOperationException("Desconto geral não pode superar a soma dos produtos.");

        var totalGeral = decimal.Round(totalProdutos - descontoGeral, 4, MidpointRounding.AwayFromZero);
        return (totalProdutos, totalGeral);
    }

    public static void ValidarDataCirurgiaEValidadeLote(DateOnly dataPrevistaCirurgia, DateOnly? validadeLote)
    {
        if (validadeLote.HasValue && validadeLote.Value < dataPrevistaCirurgia)
        {
            throw new InvalidOperationException($"Lote com validade {validadeLote.Value:yyyy-MM-dd} não é elegível para cirurgia prevista em {dataPrevistaCirurgia:yyyy-MM-dd}. Material vencerá antes do procedimento.");
        }
    }

    public static bool PodeEditar(string situacao) =>
        string.Equals(situacao, "RASCUNHO", StringComparison.OrdinalIgnoreCase);

    public static bool PodeAprovar(string situacao) =>
        string.Equals(situacao, "RASCUNHO", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(situacao, "ENVIADO", StringComparison.OrdinalIgnoreCase);

    public static bool PodeReservar(string situacao) =>
        string.Equals(situacao, "APROVADO", StringComparison.OrdinalIgnoreCase);
}
