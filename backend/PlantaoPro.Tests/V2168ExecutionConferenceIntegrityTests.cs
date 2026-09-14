namespace PlantaoPro.Tests;

public sealed class V2168ExecutionConferenceIntegrityTests
{
    private static string Read(string path) => RepositoryPathResolver.ReadRepositoryFile(path.Split('/'));

    [Fact]
    public void Dapper_read_models_use_provider_compatible_properties_and_explicit_aliases()
    {
        var source = Read("backend/PlantaoPro.Api/ExecutionConferenceService.cs");
        Assert.Contains("x.presenca_id as \"\"PresenceId\"\"", source);
        Assert.Contains("private sealed class DecisionRow", source);
        Assert.Contains("public long VersaoPresencaBase", source);
        Assert.Contains("public DateTimeOffset? InicioProposto", source);
    }

    [Fact]
    public void Normal_and_correction_decisions_lock_and_check_versions()
    {
        var source = Read("backend/PlantaoPro.Api/ExecutionConferenceService.cs");
        Assert.Contains("ApprovePresenceAsync", source);
        Assert.Contains("for update", source);
        Assert.Contains("presence.Versao != correction.VersaoPresencaBase", source);
        Assert.Contains("correctionChanged != 1 || presenceChanged != 1", source);
        Assert.Contains("effectiveEnd < effectiveStart", source);
    }

    [Fact]
    public void Replay_does_not_recalculate_processed_statuses()
    {
        var schema = Read("database/schema/400_v2167_execucao_conferencia.sql");
        var upgrade = Read("database/schema/410_v2168_conferencia_integridade.sql");
        Assert.Contains("not exists(select 1 from medico_presenca_historico", schema);
        Assert.DoesNotContain("status_conferencia=case", upgrade);
        Assert.Contains("Nunca recalcula status", upgrade);
    }

    [Fact]
    public void Professional_interface_has_history_navigation_and_unique_help_ids()
    {
        var view = Read("backend/PlantaoPro.Web/Views/MinhaAgenda/Presencas.cshtml");
        Assert.Contains("Acompanhar conferência</a>", view);
        Assert.Contains("correction-help-", view);
        Assert.DoesNotContain("setTimeout", view);
        Assert.Contains("resolvedOptions().timeZone", view);
    }
}
