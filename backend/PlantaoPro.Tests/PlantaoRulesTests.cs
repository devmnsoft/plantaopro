using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Tests;

public class PlantaoRulesTests
{
    private readonly PlantaoRegraService _service = new();

    [Fact]
    public void ValidarCriacao_DeveFalhar_QuandoDataFimMenorOuIgualInicio()
    {
        var req = new CreatePlantaoRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(-1), 100, 1, "Presencial", null);
        var resp = _service.ValidarCriacao(req);
        Assert.False(resp.Success);
    }

    [Fact]
    public void ValidarCriacao_DeveFalhar_QuandoVagasZero()
    {
        var req = new CreatePlantaoRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(12), 100, 0, "Presencial", null);
        var resp = _service.ValidarCriacao(req);
        Assert.False(resp.Success);
    }

    [Fact]
    public void ValidarCriacao_DeveFalhar_QuandoValorNegativo()
    {
        var req = new CreatePlantaoRequest(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(12), -1, 1, "Presencial", null);
        var resp = _service.ValidarCriacao(req);
        Assert.False(resp.Success);
    }

    [Fact]
    public void Transicao_DevePermitirFluxoEsperado()
    {
        var transicao = new PlantaoTransicaoService();
        Assert.True(transicao.PodeTransicionar(PlantaoStatus.Rascunho, PlantaoStatus.Aberto));
        Assert.False(transicao.PodeTransicionar(PlantaoStatus.Realizado, PlantaoStatus.Aberto));
    }
}
