namespace PlantaoPro.Domain.Escalas;

public enum EscalaEstado
{
    Solicitada, Confirmada, Recusada, Cancelada, Substituida, Realizada, Ausente, EmFechamento, Fechada
}

public sealed record EscalaTransitionResult(bool Allowed, string? BlockReason);

public static class EscalaStateMachine
{
    private static readonly IReadOnlyDictionary<EscalaEstado, EscalaEstado[]> Transitions =
        new Dictionary<EscalaEstado, EscalaEstado[]>
        {
            [EscalaEstado.Solicitada] = new[] { EscalaEstado.Confirmada, EscalaEstado.Recusada, EscalaEstado.Cancelada },
            [EscalaEstado.Confirmada] = new[] { EscalaEstado.Cancelada, EscalaEstado.Substituida, EscalaEstado.Realizada, EscalaEstado.Ausente },
            [EscalaEstado.Substituida] = new[] { EscalaEstado.Confirmada, EscalaEstado.Cancelada },
            [EscalaEstado.Realizada] = new[] { EscalaEstado.EmFechamento },
            [EscalaEstado.Ausente] = new[] { EscalaEstado.EmFechamento },
            [EscalaEstado.EmFechamento] = new[] { EscalaEstado.Fechada, EscalaEstado.Realizada, EscalaEstado.Ausente },
            [EscalaEstado.Fechada] = new[] { EscalaEstado.EmFechamento }
        };

    public static EscalaTransitionResult Validate(EscalaEstado current, EscalaEstado target, string? reason, bool canReopen = false)
    {
        if (current == EscalaEstado.Fechada && (!canReopen || target != EscalaEstado.EmFechamento))
            return new(false, "Escala fechada exige permissão de reabertura.");
        if (RequiresReason(target) && string.IsNullOrWhiteSpace(reason))
            return new(false, "Informe o motivo para esta transição.");
        return Transitions.TryGetValue(current, out var targets) && targets.Contains(target)
            ? new(true, null)
            : new(false, $"Transição de {current} para {target} não é permitida.");
    }

    private static bool RequiresReason(EscalaEstado target) =>
        target is EscalaEstado.Recusada or EscalaEstado.Cancelada or EscalaEstado.Substituida or EscalaEstado.Ausente;
}

public static class ConvitePolicy
{
    public static bool IsEligibleForAcceptance(string status, DateTimeOffset? expiresAt, DateTimeOffset now) =>
        (string.Equals(status, "ENVIADO", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(status, "PENDENTE", StringComparison.OrdinalIgnoreCase)) &&
        (!expiresAt.HasValue || expiresAt.Value > now);

    public static bool IntervalsConflict(DateTimeOffset firstStart, DateTimeOffset firstEnd, DateTimeOffset secondStart, DateTimeOffset secondEnd)
    {
        if (firstEnd <= firstStart) throw new ArgumentOutOfRangeException(nameof(firstEnd));
        if (secondEnd <= secondStart) throw new ArgumentOutOfRangeException(nameof(secondEnd));
        return firstStart < secondEnd && firstEnd > secondStart;
    }
}

public static class SubstitutionPolicy
{
    public static EscalaTransitionResult ValidateRequest(EscalaEstado current, Guid currentDoctorId, Guid replacementDoctorId, string? reason)
    {
        if (current != EscalaEstado.Confirmada)
            return new(false, "Somente uma escala confirmada pode solicitar substituição.");
        if (currentDoctorId == replacementDoctorId)
            return new(false, "O substituto deve ser outro médico.");
        if (string.IsNullOrWhiteSpace(reason))
            return new(false, "Informe o motivo da substituição.");
        return new(true, null);
    }
}
