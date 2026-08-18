using PlantaoPro.Api.Clinical;

namespace PlantaoPro.Tests;

public sealed class V185BusinessActionsContractTests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, relative));

    [Fact]
    public void Consultation_finalization_has_typed_response_real_value_gate_and_safe_ui()
    {
        var service = Read("backend/PlantaoPro.Api/Clinical/ConsultaApplicationService.cs");
        var controller = Read("backend/PlantaoPro.Api/Controllers/ConsultasWorkspaceController.cs");
        var script = Read("backend/PlantaoPro.Web/wwwroot/js/clinical-workspace.js");

        Assert.Contains("{id:guid}/finalizar", controller);
        Assert.Contains("ApiResponse<FinalizarConsultaResponse>", service);
        Assert.Contains("liquido <= 0", service);
        Assert.Contains("returning id", service);
        Assert.Contains("result.podeAbrirFaturamento && result.financeiroId", script);
        Assert.DoesNotContain("innerHTML", script);
    }

    [Fact]
    public void Operational_actions_use_persisted_state_transitions_and_required_reasons()
    {
        var escalas = Read("backend/PlantaoPro.Api/Controllers/EscalasController.cs");
        var plantoes = Read("backend/PlantaoPro.Api/Controllers/PlantoesController.cs");
        var data = Read("backend/PlantaoPro.Api/Data.cs");

        Assert.Contains("escalas/{id:guid}/confirmar", escalas);
        Assert.Contains("escalas/{id:guid}/presenca", escalas);
        Assert.Contains("escalas/{id:guid}/ausencia", escalas);
        Assert.Contains("O motivo da ausência é obrigatório.", data);
        Assert.Contains("O motivo do cancelamento é obrigatório.", plantoes);
        Assert.Contains("await tx.CommitAsync()", data);
    }

    [Fact]
    public void Billing_calculator_rejects_invented_or_invalid_amounts()
    {
        Assert.Equal(125m, AtendimentoBillingService.CalcularValorLiquido(100m, 5m, 30m));
        Assert.Throws<ArgumentOutOfRangeException>(() => AtendimentoBillingService.CalcularValorLiquido(-1m, 0m, 0m));
        Assert.Throws<InvalidOperationException>(() => AtendimentoBillingService.CalcularValorLiquido(10m, 20m, 0m));
    }

    [Fact]
    public void Generic_business_action_client_handles_expected_http_errors_without_fake_success()
    {
        var script = Read("backend/PlantaoPro.Web/wwwroot/js/components/business-actions.js");
        foreach (var status in new[] { "400", "401", "403", "404", "409", "422" }) Assert.Contains($"[{status},", script);
        Assert.Contains("response.ok", script);
        Assert.Contains("result.success === false", script);
        Assert.DoesNotContain("innerHTML", script);
        Assert.DoesNotContain("alert(", script);
        Assert.DoesNotContain("confirm(", script);
    }
}
