namespace PlantaoPro.Domain.Escalas;

public sealed record ScheduleCandidate(Guid ProfessionalId, string Name, bool SpecialtyMatches,
    bool Available, bool HasConflict, decimal ConfirmationRate, int RecentHours,
    bool PreferredUnit, decimal? Cost, bool Blocked, bool Active, bool Authorized);

public sealed record ScheduleSuggestion(Guid ProfessionalId, string Name, int Score,
    IReadOnlyList<string> Reasons, IReadOnlyList<string> Alerts, bool Eligible);

/// <summary>Deterministic and explainable ranking. It deliberately has no external AI dependency.</summary>
public static class SmartScheduleScoring
{
    public static IReadOnlyList<ScheduleSuggestion> Rank(IEnumerable<ScheduleCandidate> candidates)
        => candidates.Select(Score).OrderByDescending(x => x.Eligible).ThenByDescending(x => x.Score)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();

    public static ScheduleSuggestion Score(ScheduleCandidate candidate)
    {
        var reasons = new List<string>(); var alerts = new List<string>(); var score = 0;
        if (candidate.SpecialtyMatches) { score += 35; reasons.Add("Especialidade compatível"); } else alerts.Add("Especialidade incompatível");
        if (candidate.Available) { score += 20; reasons.Add("Disponibilidade declarada"); } else alerts.Add("Sem disponibilidade declarada");
        if (candidate.HasConflict) alerts.Add("Conflito de horário"); else { score += 15; reasons.Add("Sem conflito de horário"); }
        var reliability = (int)Math.Round(Math.Clamp(candidate.ConfirmationRate, 0m, 1m) * 15m);
        score += reliability; if (reliability > 0) reasons.Add($"Histórico de confirmação: {candidate.ConfirmationRate:P0}");
        if (candidate.PreferredUnit) { score += 8; reasons.Add("Unidade preferencial"); }
        if (candidate.RecentHours <= 36) { score += 7; reasons.Add("Carga recente equilibrada"); } else alerts.Add("Carga horária recente elevada");
        if (candidate.Cost.HasValue) reasons.Add("Custo disponível para decisão do gestor");
        if (candidate.Blocked) alerts.Add("Profissional bloqueado ou indisponível");
        if (!candidate.Active) alerts.Add("Perfil inativo");
        if (!candidate.Authorized) alerts.Add("Perfil sem autorização operacional");
        var eligible = candidate.SpecialtyMatches && candidate.Available && !candidate.HasConflict && !candidate.Blocked && candidate.Active && candidate.Authorized;
        return new(candidate.ProfessionalId, candidate.Name, Math.Clamp(score, 0, 100), reasons, alerts, eligible);
    }
}

public static class ShiftRiskCalculator
{
    public static int Calculate(DateTimeOffset startsAt, int openSlots, bool confirmationPending, bool checkInPending, int openIncidents, DateTimeOffset now)
    {
        var score = openSlots > 0 ? 35 : 0;
        if (openSlots > 0 && startsAt <= now.AddHours(6)) score += 30;
        if (confirmationPending) score += 15;
        if (checkInPending) score += 10;
        score += Math.Min(openIncidents * 10, 20);
        return Math.Clamp(score, 0, 100);
    }
}
