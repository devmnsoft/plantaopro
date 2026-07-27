namespace PlantaoPro.Domain.Cobertura;

public sealed record CoveragePriorityInput(
    TimeSpan TimeUntilStart,
    int TotalVacancies,
    int UncoveredVacancies,
    bool CriticalSpecialty,
    decimal RejectionRate,
    int Conflicts,
    decimal HospitalRiskRate,
    TimeSpan TenantSla);

public sealed record CoveragePriority(int Score, string Level, IReadOnlyList<string> Reasons);

public static class CoveragePriorityCalculator
{
    public static CoveragePriority Calculate(CoveragePriorityInput input)
    {
        if (input.TotalVacancies < 1) throw new ArgumentOutOfRangeException(nameof(input.TotalVacancies));
        if (input.UncoveredVacancies < 0 || input.UncoveredVacancies > input.TotalVacancies)
            throw new ArgumentOutOfRangeException(nameof(input.UncoveredVacancies));
        if (input.RejectionRate is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(input.RejectionRate));
        if (input.HospitalRiskRate is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(input.HospitalRiskRate));

        var reasons = new List<string>();
        var score = 0m;
        if (input.TimeUntilStart <= input.TenantSla) { score += 30; reasons.Add("Início dentro do SLA do tenant"); }
        if (input.TimeUntilStart <= TimeSpan.Zero) { score += 15; reasons.Add("Plantão já iniciado"); }
        var uncoveredRate = (decimal)input.UncoveredVacancies / input.TotalVacancies;
        score += uncoveredRate * 25;
        if (input.UncoveredVacancies > 0) reasons.Add($"{input.UncoveredVacancies} vaga(s) descoberta(s)");
        if (input.CriticalSpecialty) { score += 15; reasons.Add("Especialidade crítica"); }
        score += input.RejectionRate * 10;
        if (input.RejectionRate >= .5m) reasons.Add("Taxa elevada de recusas");
        score += Math.Min(input.Conflicts * 5, 10);
        if (input.Conflicts > 0) reasons.Add("Conflitos de horário ativos");
        score += input.HospitalRiskRate * 10;
        if (input.HospitalRiskRate >= .5m) reasons.Add("Histórico de risco do hospital");

        var normalized = Math.Clamp(decimal.ToInt32(decimal.Round(score)), 0, 100);
        var level = normalized switch { >= 80 => "CRITICA", >= 60 => "ALTA", >= 35 => "MEDIA", _ => "BAIXA" };
        return new CoveragePriority(normalized, level, reasons);
    }
}
