using PlantaoPro.Domain.Escalas;
using PlantaoPro.Domain.Cobertura;

namespace PlantaoPro.Tests;

public sealed class V1410OperationalCycleTests
{
    [Fact]
    public void Requested_shift_can_only_follow_official_paths()
    {
        Assert.True(EscalaStateMachine.Validate(EscalaEstado.Solicitada, EscalaEstado.Confirmada, null).Allowed);
        Assert.False(EscalaStateMachine.Validate(EscalaEstado.Solicitada, EscalaEstado.Realizada, null).Allowed);
    }

    [Theory]
    [InlineData(EscalaEstado.Recusada)]
    [InlineData(EscalaEstado.Cancelada)]
    [InlineData(EscalaEstado.Substituida)]
    [InlineData(EscalaEstado.Ausente)]
    public void Critical_transition_requires_reason(EscalaEstado target)
    {
        var current = target == EscalaEstado.Recusada ? EscalaEstado.Solicitada : EscalaEstado.Confirmada;
        var result = EscalaStateMachine.Validate(current, target, "  ");
        Assert.False(result.Allowed);
        Assert.Contains("motivo", result.BlockReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Closed_shift_requires_elevated_reopen_permission()
    {
        Assert.False(EscalaStateMachine.Validate(EscalaEstado.Fechada, EscalaEstado.EmFechamento, "correção").Allowed);
        Assert.True(EscalaStateMachine.Validate(EscalaEstado.Fechada, EscalaEstado.EmFechamento, "correção", true).Allowed);
    }

    [Fact]
    public void Coverage_score_is_explainable_and_rewards_operational_proximity()
    {
        var result = CoverageEligibilityService.Evaluate(new(true, false, true, true, false, true, true, true, .9m, 0m));
        Assert.True(result.Eligible);
        Assert.Equal(88, result.Score);
        Assert.Contains("Proximidade operacional", result.Reasons);
        Assert.Contains("Histórico positivo de confirmações", result.Reasons);
    }

    [Theory]
    [InlineData(false, false, true, true, false, true, "Médico inativo")]
    [InlineData(true, true, true, true, false, true, "Médico bloqueado")]
    [InlineData(true, false, false, true, false, true, "Especialidade incompatível")]
    [InlineData(true, false, true, false, false, true, "Indisponibilidade no período")]
    [InlineData(true, false, true, true, true, true, "Conflito de horário")]
    [InlineData(true, false, true, true, false, false, "CRM inválido ou ausente")]
    public void Ineligible_doctor_is_never_ranked(bool active, bool blocked, bool specialty, bool available, bool conflict, bool crm, string expected)
    {
        var result = CoverageEligibilityService.Evaluate(new(active, blocked, specialty, available, conflict, true, crm, false, 1m, 0m));
        Assert.False(result.Eligible);
        Assert.Equal(0, result.Score);
        Assert.Contains(expected, result.Impediments);
    }
}
