namespace PlantaoPro.Tests;

public sealed class V188LongitudinalClinicalContractTests
{
    private static readonly string Root = FindRoot();
    [Fact]
    public void Migration_IsIdempotentTenantScopedAndHasClinicalAggregates()
    {
        var sql=File.ReadAllText(Path.Combine(Root,"database/migrations/2026_08_v188_prontuario_longitudinal.sql"));
        foreach(var table in new[]{"paciente_problemas","paciente_alergias","paciente_medicamentos_uso","solicitacoes_exames","solicitacao_exame_itens","resultados_exames","anexos_clinicos","encaminhamentos_clinicos","documentos_clinicos"}) Assert.Contains($"CREATE TABLE IF NOT EXISTS plantaopro.{table}",sql,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tenant_id",sql); Assert.DoesNotContain("ADD CONSTRAINT IF NOT EXISTS",sql,StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void Api_ExposesTypedLongitudinalFlowsAndCentralAccess()
    {
        var controller=File.ReadAllText(Path.Combine(Root,"backend/PlantaoPro.Api/Controllers/LongitudinalControllers.cs"));
        var service=File.ReadAllText(Path.Combine(Root,"backend/PlantaoPro.Api/Clinical/LongitudinalService.cs"));
        foreach(var route in new[]{"prontuario","timeline","problemas","alergias","medicamentos","exames","encaminhamentos"}) Assert.Contains(route,controller,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IClinicalAccessService",service); Assert.Contains("FINANCEIRO",service); Assert.Contains("AUDITOR_CLINICO",service); Assert.Contains("PRONTUARIO_VISUALIZAR",service);
    }
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"README.md")))d=d.Parent;return d?.FullName??throw new InvalidOperationException("Root não encontrado.");}
}
