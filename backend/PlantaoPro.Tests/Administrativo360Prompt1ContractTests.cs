namespace PlantaoPro.Tests;

public sealed class Administrativo360Prompt1ContractTests
{
 private static string Read(string path)=>File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,path));
 [Fact] public void Migration_IsTenantScopedAndPreventsCrossTenantReferences(){var sql=Read("database/migrations/2026_09_v2190_administrativo360_base.sql");Assert.Contains("tenant_id uuid NOT NULL",sql);Assert.Contains("adm360_validar_tenant",sql);Assert.Contains("ck_adm_contrato_vigencia",sql);}
 [Fact] public void Api_ProvidesPromptOneResources(){var controller=Read("backend/PlantaoPro.Api/Controllers/Administrativo360Controller.cs");Assert.Contains("api/administrativo360",controller);Assert.Contains("departamentos",controller);Assert.Contains("colaboradores",controller);Assert.Contains("contratos",controller);}
 [Fact] public void Demo_UsesPersistedLoginInsteadOfMock(){var seed=Read("database/seeds/development/140_administrativo360_demo.sql");Assert.Contains("gestor@santacasa-demo.example",seed);Assert.Contains("plantaopro.usuarios",seed);Assert.DoesNotContain("INSERT INTO plantaopro.usuarios",seed,StringComparison.OrdinalIgnoreCase);}
}
