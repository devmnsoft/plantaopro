using System.Security.Cryptography;
using System.Text;

namespace PlantaoPro.Domain.Administrativo360;

public enum CondicaoEstoque { Quarentena, Liberado, Bloqueado, Reprovado, Vencido }
public enum SituacaoPedido { Rascunho, Aprovado, Parcial, Recebido, Cancelado }
public enum SituacaoInventario { Aberto, Contagem, Revisao, Aprovado, Cancelado }
public enum SituacaoOrcamento { Rascunho, Enviado, Aprovado, Rejeitado, Expirado, Cancelado }
public enum SituacaoReserva { Ativa, Consumida, Cancelada }

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

    public static void ValidarTransferencia(Guid origemId, Guid destinoId, decimal quantidade, string motivo)
    {
        if (origemId == Guid.Empty || destinoId == Guid.Empty || origemId == destinoId)
            throw new ArgumentException("Origem e destino devem ser locais distintos e válidos.");
        if (quantidade <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantidade), "Quantidade a transferir deve ser positiva.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Motivo da transferência é obrigatório.", nameof(motivo));
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

public static class InventarioRegras
{
    public static void ValidarAbertura(Guid localId, string escopo)
    {
        if (localId == Guid.Empty) throw new ArgumentException("Local é obrigatório.", nameof(localId));
        if (string.IsNullOrWhiteSpace(escopo)) throw new ArgumentException("Escopo da contagem é obrigatório.", nameof(escopo));
    }

    public static void ValidarContagem(decimal quantidade)
    {
        if (quantidade < 0) throw new ArgumentOutOfRangeException(nameof(quantidade), "Quantidade contada não pode ser negativa.");
    }

    public static void ValidarAprovacao(string justificativa)
    {
        if (string.IsNullOrWhiteSpace(justificativa))
            throw new ArgumentException("Aprovação com ajuste exige justificativa.", nameof(justificativa));
    }
}

public static class IdempotenciaHelper
{
    public static string CalcularHash(string operacao, params object?[] partes)
    {
        var sb = new StringBuilder(operacao);
        foreach (var p in partes)
        {
            sb.Append(':');
            if (p is null)
            {
                sb.Append("null");
            }
            else if (p is decimal m)
            {
                sb.Append(m.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (p is DateOnly d)
            {
                sb.Append(d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (p is DateTime dt)
            {
                sb.Append(dt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (p is DateTimeOffset dto)
            {
                sb.Append(dto.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (p is Guid g)
            {
                sb.Append(g.ToString("D"));
            }
            else
            {
                sb.Append(p.ToString()?.Trim() ?? "null");
            }
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
