using Microsoft.AspNetCore.Authorization;
using PlantaoPro.Web.Controllers;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Tests;

public sealed class V2169ConferenceDecisionTests
{
    [Fact]
    public void Entry_group_matches_api_and_excludes_non_decision_roles()
    {
        const string expected = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,DIRETOR,COORDENACAO,COORDENADOR,OPERADOR";
        Assert.Equal(expected, RolesConstants.EscalasGestao);
        Assert.DoesNotContain("HOSPITAL", RolesConstants.EscalasGestao);
        Assert.DoesNotContain("AUDITOR", RolesConstants.EscalasGestao);
        var authorize = Assert.Single(typeof(ConferenciaExecucaoController).GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>());
        Assert.Equal(expected, authorize.Roles);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ExecutionConferenceDecision.RecusarCorrecao)]
    [InlineData(ExecutionConferenceDecision.AprovarCorrecao)]
    public void Normal_presence_never_accepts_omitted_or_correction_decision(ExecutionConferenceDecision? decision)
    {
        var errors = ExecutionConferenceDecisionValidator.Validate(Guid.NewGuid(), null, 0, 1, decision, "Conferência operacional");
        Assert.Contains(errors, error => error.Field == "decisao");
    }

    [Fact]
    public void Correction_rejects_incompatible_ids_versions_and_normal_approval()
    {
        var errors = ExecutionConferenceDecisionValidator.Validate(Guid.Empty, Guid.NewGuid(), 0, 0,
            ExecutionConferenceDecision.AprovarExecucao, "ok");
        Assert.Contains(errors, error => error.Field == "presencaId");
        Assert.Contains(errors, error => error.Field == "versao");
        Assert.Contains(errors, error => error.Field == "decisao");
        Assert.Contains(errors, error => error.Field == "justificativa");
    }
}
