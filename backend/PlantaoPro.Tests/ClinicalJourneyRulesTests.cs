using PlantaoPro.Api;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class ClinicalJourneyRulesTests
{
    [Theory]
    [InlineData("AGENDADO", "CONFIRMADO")]
    [InlineData("CONFIRMADO", "CHECKIN_REALIZADO")]
    [InlineData("EM_TRIAGEM", "AGUARDANDO_CONSULTA")]
    [InlineData("EM_ATENDIMENTO", "ATENDIDO")]
    public void Agendamento_PermiteTransicoesOperacionais(string atual, string destino)
    {
        Assert.True(AgendamentoStateMachine.PodeTransicionar(atual, destino));
    }

    [Theory]
    [InlineData("AGENDADO", "ATENDIDO")]
    [InlineData("CANCELADO", "CONFIRMADO")]
    [InlineData("ATENDIDO", "EM_ATENDIMENTO")]
    public void Agendamento_BloqueiaSaltosEEstadosTerminais(string atual, string destino)
    {
        Assert.False(AgendamentoStateMachine.PodeTransicionar(atual, destino));
    }

    [Fact]
    public void Triagem_CalculaImcComArredondamentoClinico()
    {
        Assert.Equal(22.86m, ClinicalMeasurements.CalcularImc(70m, 1.75m));
    }

    [Fact]
    public void Triagem_FinalizacaoExigeClassificacaoEValidaSinais()
    {
        var request = new TriagemUpdateRequest { Temperatura = 50m, Saturacao = 30m };
        var errors = ClinicalMeasurements.Validar(request, true);
        Assert.Equal(3, errors.Count);
    }
}

public sealed class ConsultaStateMachineV1270Tests
{
    [Theory]
    [InlineData(ConsultaStatus.AGUARDANDO, ConsultaStatus.EM_ATENDIMENTO)]
    [InlineData(ConsultaStatus.EM_ATENDIMENTO, ConsultaStatus.RASCUNHO)]
    [InlineData(ConsultaStatus.RASCUNHO, ConsultaStatus.FINALIZADA)]
    [InlineData(ConsultaStatus.FINALIZADA, ConsultaStatus.RETORNO_SOLICITADO)]
    public void Permite_transicoes_clinicas_previstas(ConsultaStatus origem, ConsultaStatus destino)
        => Assert.True(ConsultaStateMachine.PodeTransicionar(origem, destino));

    [Fact]
    public void Bloqueia_edicao_comum_de_consulta_finalizada()
        => Assert.False(ConsultaStateMachine.PodeTransicionar(ConsultaStatus.FINALIZADA, ConsultaStatus.RASCUNHO));
}
