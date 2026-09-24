using PlantaoPro.Domain.Escalas;
using PlantaoPro.Domain.Financeiro;
using PlantaoPro.Domain.Plantoes;

namespace PlantaoPro.Tests;

public sealed class M3OperationalDomainTests
{
    [Theory]
    [InlineData(RemunerationMode.ValorTotalPlantao, 1200, 6, 1, 1200)]
    [InlineData(RemunerationMode.ValorPorHora, 100, 6, 1, 600)]
    [InlineData(RemunerationMode.ValorBase12H, 1200, 6, 1, 600)]
    [InlineData(RemunerationMode.ValorFixoPorEscala, 500, 24, 2, 1000)]
    public void Remuneracao_respeita_a_modalidade_sem_divisao_por_doze_universal(
        RemunerationMode mode, decimal value, decimal hours, int schedules, decimal expected)
        => Assert.Equal(expected, RemunerationCalculator.Calculate(mode, value, hours, schedules));

    [Fact]
    public void Memoria_de_calculo_preserva_modalidade_base_duracao_versao_e_resultado()
    {
        var start = new DateTimeOffset(2026, 9, 23, 19, 0, 0, TimeSpan.FromHours(-3));
        var memory = RemunerationCalculator.CalculateWithMemory(
            RemunerationMode.ValorPorHora, 125.50m, start, start.AddHours(12));

        Assert.Equal(RemunerationMode.ValorPorHora, memory.Mode);
        Assert.Equal(125.50m, memory.ConfiguredValue);
        Assert.Equal(12m, memory.Hours);
        Assert.Equal(RemunerationCalculator.RuleVersion, memory.RuleVersion);
        Assert.Equal(1506m, memory.Amount);
    }

    [Fact]
    public void Plantao_que_atravessa_meia_noite_mantem_duracao_e_offset()
    {
        var start = new DateTimeOffset(2026, 9, 23, 19, 0, 0, TimeSpan.FromHours(-3));
        var end = new DateTimeOffset(2026, 9, 24, 7, 0, 0, TimeSpan.FromHours(-3));

        var memory = RemunerationCalculator.CalculateWithMemory(
            RemunerationMode.ValorBase12H, 1200m, start, end);

        Assert.Equal(12m, memory.Hours);
        Assert.Equal(1200m, memory.Amount);
    }

    [Theory]
    [InlineData("PENDENTE", 1, true)]
    [InlineData("ENVIADO", 1, true)]
    [InlineData("ACEITO", 1, false)]
    [InlineData("PENDENTE", -1, false)]
    public void Convite_so_pode_ser_aceito_quando_pendente_e_na_validade(string status, int expiryHours, bool expected)
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Equal(expected, ConvitePolicy.IsEligibleForAcceptance(status, now.AddHours(expiryHours), now));
    }

    [Fact]
    public void Intervalos_adjacentes_nao_conflitam_mas_sobrepostos_conflitam()
    {
        var start = new DateTimeOffset(2026, 9, 23, 7, 0, 0, TimeSpan.Zero);
        Assert.False(ConvitePolicy.IntervalsConflict(start, start.AddHours(6), start.AddHours(6), start.AddHours(12)));
        Assert.True(ConvitePolicy.IntervalsConflict(start, start.AddHours(6), start.AddHours(5), start.AddHours(12)));
    }

    [Fact]
    public void Solicitacao_de_substituicao_nao_muda_a_responsabilidade_atual()
    {
        var currentDoctor = Guid.NewGuid();
        var result = SubstitutionPolicy.ValidateRequest(
            EscalaEstado.Confirmada, currentDoctor, Guid.NewGuid(), "Impedimento documentado");

        Assert.True(result.Allowed);
        Assert.True(EscalaStateMachine.Validate(EscalaEstado.Confirmada, EscalaEstado.Substituida, "Troca efetivada").Allowed);
    }

    [Fact]
    public void Capacidade_nunca_pode_ficar_abaixo_da_ocupacao()
    {
        Assert.Equal(1, PlantaoVacancyCalculator.Available(3, 2));
        Assert.Throws<InvalidOperationException>(() => PlantaoVacancyCalculator.Available(1, 2));
    }
}
