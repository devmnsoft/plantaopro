namespace PlantaoPro.Domain.Contratos;

public sealed record ContractPriceRule(decimal BaseValue, decimal SurchargePercent, decimal DiscountPercent,
    DateOnly ValidFrom, DateOnly ValidTo, DayOfWeek? DayOfWeek = null, TimeOnly? StartsAt = null,
    TimeOnly? EndsAt = null, bool? Holiday = null);

public static class ContractPricingCalculator
{
    public static decimal Calculate(DateOnly shiftDate, TimeOnly shiftTime, bool holiday, IEnumerable<ContractPriceRule> rules)
    {
        var rule = rules.Where(x => shiftDate >= x.ValidFrom && shiftDate <= x.ValidTo)
            .Where(x => x.DayOfWeek is null || x.DayOfWeek == shiftDate.DayOfWeek)
            .Where(x => x.Holiday is null || x.Holiday == holiday)
            .Where(x => x.StartsAt is null || shiftTime >= x.StartsAt)
            .Where(x => x.EndsAt is null || shiftTime < x.EndsAt)
            .OrderByDescending(x => x.Holiday.HasValue).ThenByDescending(x => x.DayOfWeek.HasValue)
            .FirstOrDefault() ?? throw new InvalidOperationException("Não há regra contratual vigente para o plantão.");
        return decimal.Round(rule.BaseValue * (1 + rule.SurchargePercent / 100) * (1 - rule.DiscountPercent / 100), 2);
    }
}
