namespace PlantaoPro.Domain.Cobertura;

public sealed record CoverageCandidate(
    bool Active, bool Blocked, bool SpecialtyMatches, bool Available, bool HasScheduleConflict,
    bool RequiresValidCrm, bool HasValidCrm, bool SameCity, decimal ConfirmationRate, decimal AbsenceRate);

public sealed record CoverageScoreResult(bool Eligible, int Score, IReadOnlyList<string> Reasons, IReadOnlyList<string> Impediments);

public static class CoverageEligibilityService
{
    public static CoverageScoreResult Evaluate(CoverageCandidate candidate)
    {
        var impediments = new List<string>();
        if (!candidate.Active) impediments.Add("Médico inativo");
        if (candidate.Blocked) impediments.Add("Médico bloqueado");
        if (!candidate.SpecialtyMatches) impediments.Add("Especialidade incompatível");
        if (!candidate.Available) impediments.Add("Indisponibilidade no período");
        if (candidate.HasScheduleConflict) impediments.Add("Conflito de horário");
        if (candidate.RequiresValidCrm && !candidate.HasValidCrm) impediments.Add("CRM inválido ou ausente");
        if (impediments.Count > 0) return new(false, 0, Array.Empty<string>(), impediments);

        var reasons = new List<string> { "Especialidade compatível", "Disponível no período", "Sem conflito de horário" };
        var score = 55;
        if (candidate.SameCity) { score += 15; reasons.Add("Proximidade operacional"); }
        var confirmation = (int)Math.Round(Math.Clamp(candidate.ConfirmationRate, 0, 1) * 20);
        score += confirmation;
        if (confirmation >= 12) reasons.Add("Histórico positivo de confirmações");
        var absencePenalty = (int)Math.Round(Math.Clamp(candidate.AbsenceRate, 0, 1) * 25);
        score -= absencePenalty;
        if (absencePenalty > 0) reasons.Add($"Penalidade por ausências: -{absencePenalty}");
        return new(true, Math.Clamp(score, 0, 100), reasons, Array.Empty<string>());
    }
}
