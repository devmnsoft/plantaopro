namespace PlantaoPro.Domain.Financeiro;

public enum RemunerationMode
{
    ValorTotalPlantao,
    ValorPorHora,
    ValorBase12H,
    ValorFixoPorEscala
}

public static class RemunerationCalculator
{
    public const string RuleVersion = "M3-2026-09";

    public static decimal Calculate(RemunerationMode mode, decimal configuredValue, decimal hours, int schedules = 1)
    {
        if (configuredValue < 0) throw new ArgumentOutOfRangeException(nameof(configuredValue));
        if (hours <= 0) throw new ArgumentOutOfRangeException(nameof(hours));
        if (schedules < 1) throw new ArgumentOutOfRangeException(nameof(schedules));

        var amount = mode switch
        {
            RemunerationMode.ValorTotalPlantao => configuredValue,
            RemunerationMode.ValorPorHora => configuredValue * hours,
            RemunerationMode.ValorBase12H => configuredValue / 12m * hours,
            RemunerationMode.ValorFixoPorEscala => configuredValue * schedules,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public static RemunerationMemory CalculateWithMemory(
        RemunerationMode mode,
        decimal configuredValue,
        DateTimeOffset start,
        DateTimeOffset end,
        int schedules = 1)
    {
        if (end <= start) throw new ArgumentOutOfRangeException(nameof(end), "O término deve ser posterior ao início.");

        var hours = decimal.Round((decimal)(end - start).TotalHours, 4, MidpointRounding.AwayFromZero);
        var amount = Calculate(mode, configuredValue, hours, schedules);
        return new RemunerationMemory(mode, configuredValue, hours, schedules, RuleVersion, amount);
    }
}

public sealed record RemunerationMemory(
    RemunerationMode Mode,
    decimal ConfiguredValue,
    decimal Hours,
    int Schedules,
    string RuleVersion,
    decimal Amount);
