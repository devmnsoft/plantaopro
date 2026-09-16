namespace PlantaoPro.Tests;

public sealed class ProfessionalAgendaJourneyContractTests
{
    private static string Root => RepositoryPathResolver.ResolveRepositoryRoot();
    [Fact]
    public void Agenda_is_scoped_to_authenticated_professional_and_selected_client()
    {
        var source = Read("backend/PlantaoPro.Api/ProfessionalPortalService.cs");
        Assert.Contains("e.medico_id=@MedicoId and p.cliente_id=@ClienteId", source);
        Assert.Contains("end.DayNumber - start.DayNumber > 366", source);
        Assert.Contains("p.data_inicio is null", source);
        Assert.Contains("p.data_fim is null", source);
    }

    [Fact]
    public void Confirmation_revalidates_ownership_and_uses_canonical_transition_service()
    {
        var source = Read("backend/PlantaoPro.Api/ProfessionalPortalService.cs");
        Assert.Contains("Vínculo profissional inválido ou revogado", source);
        Assert.Contains("var authorized=await cn.ExecuteScalarAsync<bool>", source);
        Assert.Contains("escalaService.ConfirmarAsync", source);
        Assert.Contains("Plantão confirmado", source);
    }

    [Fact]
    public void Detail_keeps_confirmation_execution_and_finance_semantically_separate()
    {
        var view = Read("backend/PlantaoPro.Web/Views/MinhaAgenda/Detalhe.cshtml");
        Assert.Contains("<h2>Participação</h2>", view);
        Assert.Contains("<h2>Execução</h2>", view);
        Assert.Contains("<h2>Financeiro</h2>", view);
        Assert.Contains("não comprovam apuração ou pagamento", view);
    }

    private static string Read(string relativePath)
    {
        return File.ReadAllText(Path.Combine(Root, relativePath));
    }
}
