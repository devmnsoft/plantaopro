namespace PlantaoPro.Api;

public static class AgendamentoStateMachine
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Transitions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["AGENDADO"] = new HashSet<string>(new[] { "CONFIRMADO", "CANCELADO", "REAGENDADO", "FALTOU" }, StringComparer.OrdinalIgnoreCase),
            ["CONFIRMADO"] = new HashSet<string>(new[] { "CHECKIN_REALIZADO", "CANCELADO", "REAGENDADO", "FALTOU" }, StringComparer.OrdinalIgnoreCase),
            ["CHECKIN_REALIZADO"] = new HashSet<string>(new[] { "EM_TRIAGEM", "AGUARDANDO_CONSULTA", "CANCELADO" }, StringComparer.OrdinalIgnoreCase),
            ["EM_TRIAGEM"] = new HashSet<string>(new[] { "AGUARDANDO_CONSULTA", "CANCELADO" }, StringComparer.OrdinalIgnoreCase),
            ["AGUARDANDO_CONSULTA"] = new HashSet<string>(new[] { "EM_ATENDIMENTO", "CANCELADO", "FALTOU" }, StringComparer.OrdinalIgnoreCase),
            ["EM_ATENDIMENTO"] = new HashSet<string>(new[] { "ATENDIDO", "CANCELADO" }, StringComparer.OrdinalIgnoreCase)
        };

    public static bool PodeTransicionar(string atual, string destino)
    {
        return !string.IsNullOrWhiteSpace(atual) && !string.IsNullOrWhiteSpace(destino)
            && Transitions.TryGetValue(atual, out var allowed) && allowed.Contains(destino);
    }
}

public static class ClinicalMeasurements
{
    public static decimal? CalcularImc(decimal? pesoKg, decimal? alturaMetros)
    {
        if (!pesoKg.HasValue || !alturaMetros.HasValue || pesoKg <= 0 || alturaMetros <= 0)
            return null;
        return Math.Round(pesoKg.Value / (alturaMetros.Value * alturaMetros.Value), 2, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<string> Validar(TriagemUpdateRequest request, bool finalizar)
    {
        var errors = new List<string>();
        ValidateRange(request.PressaoSistolica, 50, 300, "Pressão sistólica", errors);
        ValidateRange(request.PressaoDiastolica, 30, 200, "Pressão diastólica", errors);
        ValidateRange(request.FrequenciaCardiaca, 20, 250, "Frequência cardíaca", errors);
        ValidateRange(request.Temperatura, 25, 45, "Temperatura", errors);
        ValidateRange(request.Saturacao, 50, 100, "Saturação", errors);
        if (finalizar && string.IsNullOrWhiteSpace(request.ClassificacaoRisco)) errors.Add("A classificação de risco é obrigatória.");
        return errors;
    }

    private static void ValidateRange(decimal? value, decimal min, decimal max, string field, List<string> errors)
    {
        if (value.HasValue && (value < min || value > max)) errors.Add(field + " fora do limite clínico plausível.");
    }
}

public sealed class TriagemUpdateRequest
{
    public Guid AtendimentoId { get; set; }
    public Guid PacienteId { get; set; }
    public string QueixaPrincipal { get; set; } = string.Empty;
    public decimal? PressaoSistolica { get; set; }
    public decimal? PressaoDiastolica { get; set; }
    public decimal? FrequenciaCardiaca { get; set; }
    public decimal? FrequenciaRespiratoria { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? Saturacao { get; set; }
    public decimal? Glicemia { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public string AlergiasRelatadas { get; set; } = string.Empty;
    public string MedicamentosEmUso { get; set; } = string.Empty;
    public string ClassificacaoRisco { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
}
