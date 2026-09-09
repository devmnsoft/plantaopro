using PlantaoPro.Domain.Contratos;

namespace PlantaoPro.Tests;

public sealed class UnitPortalV2100Tests
{
    [Fact]
    public void Pricing_Uses_Most_Specific_Current_Rule()
    {
        var date=new DateOnly(2026,8,26);
        var rules=new[]{new ContractPriceRule(1000,0,0,date.AddDays(-1),date.AddDays(1)),new ContractPriceRule(1000,25,10,date.AddDays(-1),date.AddDays(1),date.DayOfWeek)};
        Assert.Equal(1125m,ContractPricingCalculator.Calculate(date,new TimeOnly(19,0),false,rules));
    }

    [Fact]
    public void Pricing_Rejects_Shift_Without_Current_Contract() => Assert.Throws<InvalidOperationException>(() =>
        ContractPricingCalculator.Calculate(new DateOnly(2026,8,26),new TimeOnly(8,0),false,new[]{new ContractPriceRule(100,0,0,new DateOnly(2025,1,1),new DateOnly(2025,12,31))}));

    [Fact]
    public void Coverage_Reports_Gap_And_Sla()
    {
        var result=ContractCoverageCalculator.Calculate(10,12,11,8,TimeSpan.FromMinutes(50),TimeSpan.FromMinutes(30));
        Assert.Equal(3,result.Uncovered); Assert.False(result.SlaMet);
    }

    
}
