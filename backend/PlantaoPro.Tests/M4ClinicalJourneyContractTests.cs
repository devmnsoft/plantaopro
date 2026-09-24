using Xunit;

namespace PlantaoPro.Tests;

public sealed class M4ClinicalJourneyContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void ClinicalRequests_DoNotExecuteSchemaDdlAtRuntime()
    {
        var service = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");
        var readiness = Read("backend/PlantaoPro.Api/Saude360ClinicalSchema.cs");

        Assert.DoesNotContain("create table", service, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alter table", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pg_catalog.pg_tables", readiness);
        Assert.Contains("Aplique as migrations pendentes", readiness);
    }

    [Fact]
    public void ConvenioPlans_AreFilteredByRouteAndTenant()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs");
        var service = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");

        Assert.Contains("ListarAsync(\"convenioPlanos\", convenioId: id)", controller);
        Assert.Contains("(@convenioId is null or convenio_id = @convenioId)", service);
        Assert.Contains("(@isGlobal or (@tenantId is not null and cliente_id = @tenantId))", service);
    }

    [Fact]
    public void Documents_StartExplicitlyUnsigned()
    {
        var models = Read("backend/PlantaoPro.Api/Clinical/LongitudinalModels.cs");
        Assert.Contains("InitialStatus => \"NAO_ASSINADO\"", models);
        Assert.DoesNotContain("InitialStatus => \"ASSINADO\"", models);
    }
}
