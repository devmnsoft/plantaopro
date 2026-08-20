namespace PlantaoPro.Domain.Financeiro;

public static class FechamentoStatus
{
    public const string Aberto = "ABERTO";
    public const string EmConferencia = "EM_CONFERENCIA";
    public const string ComDivergencia = "COM_DIVERGENCIA";
    public const string AguardandoAprovacao = "AGUARDANDO_APROVACAO";
    public const string Aprovado = "APROVADO";
    public const string Devolvido = "DEVOLVIDO";
    public const string FinanceiroGerado = "FINANCEIRO_GERADO";
    public const string Concluido = "CONCLUIDO";

    private static readonly IReadOnlyDictionary<string, string[]> Transicoes = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        [Aberto] = new[] { EmConferencia },
        [EmConferencia] = new[] { ComDivergencia, AguardandoAprovacao },
        [ComDivergencia] = new[] { EmConferencia },
        [AguardandoAprovacao] = new[] { Aprovado, Devolvido },
        [Devolvido] = new[] { EmConferencia },
        [Aprovado] = new[] { FinanceiroGerado },
        [FinanceiroGerado] = new[] { Concluido }
    };

    public static bool PodeTransicionar(string atual, string destino) =>
        Transicoes.TryGetValue(atual, out var destinos) && destinos.Contains(destino, StringComparer.Ordinal);
}

public static class PlantaoPaymentCalculator
{
    public static decimal Calcular(decimal valorPlantao, DateTime inicio, DateTime fim)
    {
        if (valorPlantao <= 0 || fim <= inicio) return 0;
        var horas = Math.Max(1m, (decimal)(fim - inicio).TotalHours);
        return Math.Round((valorPlantao / 12m) * horas, 2, MidpointRounding.AwayFromZero);
    }
}
