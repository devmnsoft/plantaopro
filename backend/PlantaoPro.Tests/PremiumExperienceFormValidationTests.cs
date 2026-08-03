using System.ComponentModel.DataAnnotations;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Tests;

public sealed class PremiumExperienceFormValidationTests
{
    [Fact]
    public void Plantao_rejects_empty_context_inverted_dates_and_invalid_quantity()
    {
        var model = new PlantaoFormViewModel
        {
            HospitalId = Guid.Empty,
            EspecialidadeId = Guid.Empty,
            DataInicio = new DateTime(2026, 8, 3, 18, 0, 0, DateTimeKind.Local),
            DataFim = new DateTime(2026, 8, 3, 6, 0, 0, DateTimeKind.Local),
            Valor = -1,
            Vagas = 0,
            Tipo = "Inválido"
        };

        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.HospitalId)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.EspecialidadeId)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.DataFim)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Valor)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Vagas)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Tipo)));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(24)]
    [InlineData(168)]
    public void Plantao_accepts_operational_periods_up_to_seven_days(int durationHours)
    {
        var start = new DateTime(2026, 8, 4, 7, 0, 0, DateTimeKind.Local);
        var model = new PlantaoFormViewModel
        {
            HospitalId = Guid.NewGuid(),
            EspecialidadeId = Guid.NewGuid(),
            DataInicio = start,
            DataFim = start.AddHours(durationHours),
            Valor = 1250.50m,
            Vagas = 2,
            Tipo = "Presencial"
        };

        Assert.Empty(Validate(model));
    }

    [Fact]
    public void Plantao_rejects_period_longer_than_seven_days()
    {
        var start = new DateTime(2026, 8, 4, 7, 0, 0, DateTimeKind.Local);
        var model = new PlantaoFormViewModel
        {
            HospitalId = Guid.NewGuid(),
            EspecialidadeId = Guid.NewGuid(),
            DataInicio = start,
            DataFim = start.AddDays(8),
            Valor = 100,
            Vagas = 1,
            Tipo = "Remoto"
        };

        Assert.Contains(Validate(model), result => result.MemberNames.Contains(nameof(model.DataFim)));
    }

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
