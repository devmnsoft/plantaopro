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
}
