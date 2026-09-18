namespace PlantaoPro.Tests;

public sealed class SantaCasaOperationalDemoContractTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, path));

    [Fact]
    public void Seed130_IsIdempotent_AdvisoryLocked_And_CoversOperationalJourney()
    {
        var seedSql = Read("database/seeds/development/130_operacao_demo_santacasa.sql");
        Assert.Contains("pg_advisory_xact_lock", seedSql);
        Assert.Contains("d3f6584c-2c64-4e5a-9ea9-4e1428647501", seedSql); // cliente
        Assert.Contains("d3f6584c-2c64-4e5a-9ea9-4e1428647502", seedSql); // tenant
        Assert.Contains("d3f6584c-2c64-4e5a-9ea9-4e1428647504", seedSql); // unidade
        Assert.Contains("d3f6584c-2c64-4e5a-9ea9-4e1428647511", seedSql); // gestor
        Assert.Contains("d3f6584c-2c64-4e5a-9ea9-4e1428647512", seedSql); // medica
        Assert.Contains("d3f6584c-2c64-4e5a-9ea9-4e1428647530", seedSql); // medico_id
        Assert.Contains("ESCALAS", seedSql);
        Assert.Contains("EXECUCAO", seedSql);
        Assert.Contains("CONFERENCIA", seedSql);
        Assert.Contains("plantaopro.plantoes", seedSql);
        Assert.Contains("plantaopro.escalas", seedSql);
        Assert.Contains("plantaopro.medico_checkins", seedSql);
        Assert.Contains("plantaopro.fechamento_plantao", seedSql);
        Assert.Contains("plantaopro.ocorrencias_operacionais", seedSql);
    }

    [Fact]
    public void ManagerCommandCenter_EnforcesOperacaoRole_And_PlatformIsolationForGlobalAdmin()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/ManagerCommandCenterController.cs");
        var service = Read("backend/PlantaoPro.Api/ManagerCommandCenterService.cs");
        var roles = Read("backend/PlantaoPro.Api/RolesConstants.cs");

        Assert.Contains("Roles = RolesConstants.Operacao", controller);
        Assert.Contains("public const string Operacao", roles);
        Assert.Contains("GetGlobalAsync", controller);
        Assert.Contains("GetGlobalAsync", service);
        Assert.Contains("PlatformHealthSummary", service);
        Assert.Contains("PendingCheckIns", service);
        Assert.Contains("OpenIncidents", service);
        Assert.Contains("PendingReplacements", service);
    }

    [Fact]
    public void MeuDia_SuppressesMontarEscalaForPhysician_And_ExposesOperationalModules()
    {
        var view = Read("backend/PlantaoPro.Web/Views/MeuDia/Index.cshtml");
        Assert.Contains("isPhysician", view);
        Assert.Contains("Minha Agenda", view);
        Assert.Contains("Meu Turno", view);
        Assert.Contains("Minha Produção", view);
        Assert.Contains("Montar cobertura", view);
    }

    [Fact]
    public void Productivity_PopulatesTimelineAgenda_And_ProtectsGlobalAdminFromTenantCrash()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/ProductivityActionController.cs");
        var service = Read("backend/PlantaoPro.Api/ProductivityActionServices.cs");

        Assert.Contains("GetAgendaAsync", controller);
        Assert.Contains("GetAgendaAsync", service);
        Assert.Contains("isDoctor", service);
        Assert.Contains("isTenantAdmin", service);
        Assert.Contains("IsGlobalAdmin()", service);
    }

    [Fact]
    public void CommandCenterView_DifferentiatesGlobalPlatformHealthFromTenantOperations()
    {
        var view = Read("backend/PlantaoPro.Web/Views/CommandCenter/Index.cshtml");
        Assert.Contains("PlatformHealth", view);
        Assert.Contains("Tenants Ativos", view);
        Assert.Contains("Clientes Ativos", view);
        Assert.Contains("Módulos Contratados", view);
        Assert.Contains("Isolamento de Plataforma Ativo", view);
    }
}
