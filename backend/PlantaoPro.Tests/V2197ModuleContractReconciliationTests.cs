namespace PlantaoPro.Tests;

public sealed class V2197ModuleContractReconciliationTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, path));

    [Fact]
    public void Login_requires_canonical_active_contract_and_never_falls_back_to_legacy_code()
    {
        var auth = Read("backend/PlantaoPro.Api/Data.cs");
        Assert.Contains("join plantaopro.modulos_sistema ms on ms.id=tm.modulo_id", auth, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tm.habilitado=true", auth, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("upper(tm.status)='ATIVO'", auth, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("upper(coalesce(nullif(tm.codigo_modulo", auth, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("up.tenant_id is null or up.tenant_id=@tenantId", auth, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("isGlobal ? new[] { \"*\" }", auth);
    }

    [Fact]
    public void Corrective_migration_preserves_states_and_only_links_unambiguous_codes()
    {
        var sql = Read("database/migrations/2026_09_v2197_reconciliar_contratos_modulos.sql");
        Assert.Contains("m.quantidade=1", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CATALOGO_AMBIGUO", sql);
        Assert.Contains("contratos ativos duplicados", sql);
        Assert.Contains("upper(coalesce(status,''))='ATIVO'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drop table", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("truncate", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("using null", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Demo_contract_is_canonical_and_password_reset_is_explicitly_scoped()
    {
        var seed = Read("database/seeds/development/121_acesso_demo_local.sql");
        var reset = Read("database/seeds/development/121b_restaurar_senha_gestor_demo.sql");
        Assert.Contains("modulo_id,codigo,codigo_modulo,habilitado,status", seed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=v_id", reset, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gestor@santacasa-demo.example", reset);
        Assert.DoesNotContain("superadmin@mnsoft.example", reset);
        Assert.DoesNotContain("medico@santacasa-demo.example", reset);
    }

    [Fact]
    public void Infrastructure_failure_exposes_correlation_reference_without_sql_details()
    {
        var api = Read("backend/PlantaoPro.Api/Controllers/AuthController.cs");
        var web = Read("backend/PlantaoPro.Web/Controllers/AccountController.cs");
        Assert.Contains("Referência: {correlationId}", api);
        Assert.Contains("apiResult?.Message", web);
        Assert.DoesNotContain("PostgresException", api);
    }
}
