using PlantaoPro.Domain.Cobertura;
using PlantaoPro.Domain.Financeiro;
using PlantaoPro.Domain.Plantoes;

namespace PlantaoPro.Tests;

public sealed class OperationalCoreRulesTests
{
    [Fact]
    public void VacancyEdition_PreservesConfirmedOccupancy()
    {
        Assert.Equal(2, PlantaoVacancyCalculator.Available(5, 3));
        Assert.Throws<InvalidOperationException>(() => PlantaoVacancyCalculator.Available(2, 3));
    }

    [Fact]
    public void RealizationAndClosure_AreSeparateTransitions()
    {
        Assert.True(PlantaoStateMachine.CanTransition(PlantaoState.EmAndamento, PlantaoState.Realizado));
        Assert.True(PlantaoStateMachine.CanTransition(PlantaoState.Realizado, PlantaoState.Encerrado));
        Assert.False(PlantaoStateMachine.CanTransition(PlantaoState.EmAndamento, PlantaoState.Encerrado));
    }

    [Theory]
    [InlineData(RemunerationMode.ValorTotalPlantao, 1200, 6, 1, 1200)]
    [InlineData(RemunerationMode.ValorPorHora, 100, 6, 1, 600)]
    [InlineData(RemunerationMode.ValorBase12H, 1200, 6, 1, 600)]
    [InlineData(RemunerationMode.ValorFixoPorEscala, 500, 6, 2, 1000)]
    public void Remuneration_IsExplicit(RemunerationMode mode, decimal value, decimal hours, int schedules, decimal expected)
        => Assert.Equal(expected, RemunerationCalculator.Calculate(mode, value, hours, schedules));

    [Fact]
    public void CoverageScore_IsExplainableAndBounded()
    {
        var result = CoveragePriorityCalculator.Calculate(new(
            TimeSpan.FromHours(1), 2, 2, true, .75m, 2, .6m, TimeSpan.FromHours(4)));
        Assert.Equal(94, result.Score);
        Assert.Equal("CRITICA", result.Level);
        Assert.Contains("Especialidade crítica", result.Reasons);
        Assert.NotEmpty(result.Reasons);
    }
}
