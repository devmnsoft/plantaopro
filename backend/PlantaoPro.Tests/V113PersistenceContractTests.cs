namespace PlantaoPro.Tests;

public sealed class V113PersistenceContractTests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Controller_DeveUsarServicoAuthApiResponseESemEstadoEmMemoria()
    {
        var controller = Read("PlantaoPro.Api/Controllers/V112HomologationController.cs");
        Assert.Contains("[Authorize]", controller);
        Assert.Contains("V113OperationalService", controller);
        Assert.Contains("ApiResponse<", controller);
        Assert.Contains("api/v113/customers", controller);
        Assert.Contains("api/v113/billing/titles/{titleId:guid}/demo-boleto", controller);
        Assert.DoesNotContain("" + "Concurrent" + "Dictionary", controller);
        Assert.DoesNotContain("static readonly", controller);
        Assert.DoesNotContain("" + "Fake" + "Boleto", controller);
        Assert.DoesNotContain("= " + "[]", controller);
        Assert.DoesNotContain("return " + "[]", controller);
    }

    [Fact]
    public void ServiceMigrationSeedSmokeECI_DeveExistir()
    {
        var service = Read("PlantaoPro.Api/V113OperationalService.cs");
        Assert.Contains("class V113OperationalService", service);
        Assert.Contains("Dapper", service);
        Assert.Contains("Npgsql", service);
        Assert.Contains("IAuditService", service);
        Assert.Contains("ICurrentUserService", service);
        Assert.Contains("v113_outbox_eventos", service);
        Assert.Contains("DEMO_BOLETO_CREATED", service);
        Assert.Contains("Boleto demonstrativo sem valor financeiro real.", service);
        Assert.True(File.Exists(Path.Combine(Root, "../database/migrations/2026_v113_operacional_real.sql")));
        Assert.True(File.Exists(Path.Combine(Root, "../database/seeds/2026_demo_v113_operacional.sql")));
        Assert.True(File.Exists(Path.Combine(Root, "../scripts/smoke-test-v113.sh")));
        Assert.True(File.Exists(Path.Combine(Root, "../scripts/smoke-test-v113.ps1")));
        Assert.Contains("runtime-e2e-v113", File.ReadAllText(Path.Combine(Root, "../.github/workflows/dotnet-ci.yml")));
    }

    [Fact]
    public void Migration_DeveCriarTabelasOperacionaisV113()
    {
        var migration = File.ReadAllText(Path.Combine(Root, "../database/migrations/2026_v113_operacional_real.sql"));
        foreach (var table in new[] { "v113_clientes", "v113_produtos", "v113_estoque_movimentos", "v113_pedidos", "v113_pedido_itens", "v113_tarefas", "v113_faturas", "v113_titulos", "v113_outbox_eventos", "v113_outbox_logs", "v113_templates", "v113_template_instalacoes", "v113_jornada_acoes", "v113_atividades", "v113_auditoria" })
        {
            Assert.Contains(table, migration);
        }
        Assert.Contains("add column if not exists cliente_id", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("add column if not exists tenant_id", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create index if not exists", migration, StringComparison.OrdinalIgnoreCase);
    }
}
