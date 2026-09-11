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

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData(" 52998224725 ", "52998224725")]
    public void Paciente_NormalizaCpfValido(string input, string normalized)
    {
        Assert.True(PacienteDocumentRules.IsValidCpf(input));
        Assert.Equal(normalized, PacienteDocumentRules.NormalizeCpf(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("111.111.111-11")]
    [InlineData("52998224724")]
    public void Paciente_RejeitaCpfInvalidoQuandoInformado(string input) =>
        Assert.False(PacienteDocumentRules.IsValidCpf(input));

    [Theory]
    [InlineData("confirmar", "CONFIRMADO")]
    [InlineData("checkin", "CHECKIN_REALIZADO")]
    [InlineData("marcar-falta", "FALTOU")]
    public void Agendamento_MapeiaAcoesParaTransicao(string action, string expected)
    {
        Assert.True(AgendamentoStateMachine.TryGetTarget(action, out var target));
        Assert.Equal(expected, target);
    }
}
