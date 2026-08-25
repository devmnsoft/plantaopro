using System.ComponentModel.DataAnnotations;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Tests;

public sealed class V2040FluxosPlantaoEscalaFormulariosTests
{
    [Fact]
    public void Plantao_ExigeEntidadesEHorarioValidos()
    {
        var model = new PlantaoFormViewModel
        {
            HospitalId = Guid.Empty,
            EspecialidadeId = Guid.Empty,
            DataInicio = new DateTime(2026, 8, 25, 12, 0, 0),
            DataFim = new DateTime(2026, 8, 25, 8, 0, 0),
            Tipo = "Presencial",
            Vagas = 1
        };

        var errors = Validate(model);

        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.HospitalId)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.EspecialidadeId)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.DataFim)));
    }

    [Fact]
    public void Substituicao_ExigeProfissionalEMotivoSelecionaveis()
    {
        var model = new SubstituicaoEscalaViewModel { Id = Guid.NewGuid() };
        var errors = Validate(model);
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.NovoMedicoId)));
        Assert.Contains(errors, x => x.MemberNames.Contains(nameof(model.Motivo)));
    }

    [Fact]
    public void Substituicao_OutroMotivoExigeDetalhamento()
    {
        var model = new SubstituicaoEscalaViewModel
        {
            Id = Guid.NewGuid(),
            NovoMedicoId = Guid.NewGuid(),
            Motivo = "OUTRO"
        };
        Assert.Contains(Validate(model), x => x.MemberNames.Contains(nameof(model.Detalhes)));
    }

    [Theory]
    [InlineData("texto livre")]
    [InlineData("INEXISTENTE")]
    public void Substituicao_RejeitaMotivoForaDoCatalogo(string motivo)
    {
        var model = new SubstituicaoEscalaViewModel { Id = Guid.NewGuid(), NovoMedicoId = Guid.NewGuid(), Motivo = motivo };
        Assert.Contains(Validate(model), x => x.MemberNames.Contains(nameof(model.Motivo)));
    }

    private static IReadOnlyList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }
}
