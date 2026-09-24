namespace PlantaoPro.Domain.Administrativo360;

public enum CondicaoEstoque { Quarentena, Liberado, Bloqueado, Reprovado, Vencido }
public enum SituacaoPedido { Rascunho, Aprovado, Parcial, Recebido, Cancelado }
public enum SituacaoInventario { Aberto, Contagem, Revisao, Aprovado, Cancelado }

public static class Estoque
{
    public static decimal Disponivel(decimal fisicoLiberado, decimal reservado, bool vencido, bool bloqueado)
    {
        if (fisicoLiberado < 0 || reservado < 0) throw new ArgumentOutOfRangeException(nameof(fisicoLiberado));
        if (reservado > fisicoLiberado) throw new InvalidOperationException("Reservas não podem superar o saldo físico liberado.");
        return vencido || bloqueado ? 0 : fisicoLiberado - reservado;
    }

    public static void ValidarQuantidade(decimal quantidade, string campo = "Quantidade")
    {
        if (quantidade <= 0) throw new ArgumentOutOfRangeException(campo, $"{campo} deve ser positiva.");
    }
}

public static class Inspecao
{
    public static void ValidarDecisao(decimal pendente, decimal aprovada, decimal reprovada, string? justificativa)
    {
        if (aprovada < 0 || reprovada < 0 || aprovada + reprovada <= 0 || aprovada + reprovada > pendente)
            throw new InvalidOperationException("A decisão deve ser positiva e não pode exceder o saldo pendente da inspeção.");
        if (reprovada > 0 && string.IsNullOrWhiteSpace(justificativa))
            throw new InvalidOperationException("A reprovação exige justificativa.");
    }
}
