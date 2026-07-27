namespace PlantaoPro.Domain.Plantoes;

public enum PlantaoState
{
    Rascunho, Aberto, EmEscala, Preenchido, EmAndamento, Realizado, Encerrado, Cancelado
}

public static class PlantaoStateMachine
{
    public static bool CanTransition(PlantaoState current, PlantaoState next) => (current, next) switch
    {
        (PlantaoState.Rascunho, PlantaoState.Aberto or PlantaoState.Cancelado) => true,
        (PlantaoState.Aberto, PlantaoState.EmEscala or PlantaoState.Preenchido or PlantaoState.Cancelado) => true,
        (PlantaoState.EmEscala, PlantaoState.Preenchido or PlantaoState.Cancelado) => true,
        (PlantaoState.Preenchido, PlantaoState.EmAndamento or PlantaoState.Cancelado) => true,
        (PlantaoState.EmAndamento, PlantaoState.Realizado) => true,
        (PlantaoState.Realizado, PlantaoState.Encerrado) => true,
        _ => false
    };

    public static void EnsureTransition(PlantaoState current, PlantaoState next)
    {
        if (!CanTransition(current, next))
            throw new InvalidOperationException($"Transição de {current} para {next} não é permitida.");
    }
}

public static class PlantaoVacancyCalculator
{
    public static int Available(int totalVacancies, int activeConfirmedSchedules)
    {
        if (totalVacancies < 1) throw new ArgumentOutOfRangeException(nameof(totalVacancies));
        if (activeConfirmedSchedules < 0) throw new ArgumentOutOfRangeException(nameof(activeConfirmedSchedules));
        if (totalVacancies < activeConfirmedSchedules)
            throw new InvalidOperationException("O total de vagas não pode ser inferior às escalas confirmadas.");
        return totalVacancies - activeConfirmedSchedules;
    }
}
