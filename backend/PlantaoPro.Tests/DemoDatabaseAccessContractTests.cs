namespace PlantaoPro.Tests;

public sealed class DemoDatabaseAccessContractTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, path));

    [Fact]
    public void Provisioning_is_explicit_development_only_and_database_pinned()
    {
        var program = Read("backend/PlantaoPro.Api/Program.cs");
        var seed = Read("backend/PlantaoPro.Api/DevelopmentSeed.cs");
        Assert.Contains("--provision-demo", program);
        Assert.DoesNotContain("DevelopmentSeed:Enabled", program);
        Assert.Contains("env.IsDevelopment()", seed);
        Assert.Contains("DemoSeed:Enabled", seed);
        Assert.Contains("DemoSeed:DevelopmentDatabase", seed);
        Assert.Contains("current_database()", seed);
        Assert.Contains("BeginTransactionAsync", seed);
        Assert.Contains("pg_advisory_xact_lock", seed);
    }

    [Fact]
    public void Demo_identities_use_canonical_tables_and_bcrypt_without_plaintext_fallback()
    {
        var seed = Read("backend/PlantaoPro.Api/DevelopmentSeed.cs");
        var auth = Read("backend/PlantaoPro.Api/Data.cs");
        Assert.Contains("plantaopro.usuarios", seed);
        Assert.Contains("plantaopro.usuarios_perfis", seed);
        Assert.Contains("plantaopro.perfis", seed);
        Assert.Contains("plantaopro.clientes", seed);
        Assert.Contains("plantaopro.tenant_modulos", seed);
        Assert.Contains("BCrypt.Net.BCrypt.HashPassword", seed);
        Assert.Contains("BCrypt.Net.BCrypt.Verify", auth);
        Assert.DoesNotContain("string.Equals(req.Senha, candidate.SenhaHash", auth);
        Assert.DoesNotContain("MnSoft!Demo2026", seed);
        Assert.DoesNotContain("SantaCasa!Demo2026", seed);
        Assert.Contains("DemoSeed:SuperAdminPassword", seed);
        Assert.Contains("DemoSeed:ManagerPassword", seed);
    }

    [Fact]
    public void Provisioning_is_idempotent_and_password_reset_is_separate_and_revokes_sessions()
    {
        var seed = Read("backend/PlantaoPro.Api/DevelopmentSeed.cs");
        var program = Read("backend/PlantaoPro.Api/Program.cs");
        Assert.Contains("on conflict(id) do nothing", seed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("where not exists", seed, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("else if (reset)", seed);
        Assert.Contains("--reset-demo-passwords", program);
        Assert.Contains("update plantaopro.auth_sessoes", seed);
        Assert.Contains("Já existe um administrador global", seed);
    }

    [Fact]
    public void Login_keeps_antiforgery_accessibility_and_does_not_publish_credentials()
    {
        var view = Read("backend/PlantaoPro.Web/Views/Account/Login.cshtml");
        var layout = Read("backend/PlantaoPro.Web/Views/Shared/_AuthLayout.cshtml");
        Assert.Contains("AntiForgeryToken", view);
        Assert.Contains("autocomplete=\"username\"", view);
        Assert.Contains("autocomplete=\"current-password\"", view);
        Assert.Contains("data-password-toggle", view);
        Assert.Contains("DemoSeed:Enabled", layout);
        Assert.DoesNotContain("MnSoft!Demo2026", view);
        Assert.DoesNotContain("SantaCasa!Demo2026", view);
    }
}
