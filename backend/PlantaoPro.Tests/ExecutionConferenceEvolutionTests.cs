namespace PlantaoPro.Tests;

public sealed class ExecutionConferenceEvolutionTests
{
    private static string Read(string path) => RepositoryPathResolver.ReadRepositoryFile(path.Split('/'));

    [Fact]
    public void Conference_queue_includes_incomplete_presence_and_stable_pagination()
    {
        var source = Read("backend/PlantaoPro.Api/ExecutionConferenceService.cs");
        Assert.DoesNotContain("and c.checkout_em is not null", source);
        Assert.Contains("'REGISTRO_INCOMPLETO','PENDENTE','CORRECAO_PENDENTE'", source);
        Assert.Contains("c.id desc limit @limit offset @offset", source);
        Assert.Contains("@Divergencia", source);
    }

    [Fact]
    public void Conference_queue_only_exposes_actionable_correction_details()
    {
        var source = Read("backend/PlantaoPro.Api/ExecutionConferenceService.cs");
        Assert.Contains("case when x.status='PENDENTE' then x.id end", source);
        Assert.Contains("case when x.status='PENDENTE' then x.inicio_proposto_em end", source);
        Assert.Contains("case when x.status='PENDENTE' then x.fim_proposto_em end", source);
        Assert.Contains("case when x.status='PENDENTE' then coalesce(x.justificativa,'') else '' end", source);
        Assert.Contains("case when x.status='PENDENTE' then x.versao else c.versao end", source);
        Assert.Contains("case when x.status='PENDENTE' then x.solicitado_em", source);
    }

    [Fact]
    public void Pending_correction_can_only_be_cancelled_by_its_versioned_owner()
    {
        var source = Read("backend/PlantaoPro.Api/ProfessionalPortalService.cs");
        Assert.Contains("x.solicitado_por=@uid and x.status='PENDENTE'", source);
        Assert.Contains("c.versao=@VersaoPresenca", source);
        Assert.Contains("'CORRECAO_CANCELADA'", source);
        Assert.Contains("correctionChanged != 1 || presenceChanged != 1", source);
        Assert.Contains("Este registro foi atualizado. Revise os dados antes de decidir.", source);
    }

    [Fact]
    public void Forms_expose_explicit_actions_without_destructive_delete()
    {
        var professional = Read("backend/PlantaoPro.Web/Views/MinhaAgenda/Presencas.cshtml");
        var conference = Read("backend/PlantaoPro.Web/Views/ConferenciaExecucao/Index.cshtml");
        Assert.Contains("Cancelar solicitação", professional);
        Assert.Contains("Recebido pelo servidor", professional);
        Assert.Contains("Duração resultante", professional);
        Assert.Contains("name=\"divergencia\"", conference);
        Assert.Contains("REGISTRO_INCOMPLETO", conference);
        Assert.DoesNotContain("alert(", professional + conference);
        Assert.DoesNotContain("confirm(", professional + conference);
    }
}
