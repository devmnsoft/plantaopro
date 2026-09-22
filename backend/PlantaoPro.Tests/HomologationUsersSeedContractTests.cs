namespace PlantaoPro.Tests;

public sealed class HomologationUsersSeedContractTests
{
    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, relativePath));

    [Fact]
    public void Seed_DeveProvisionarCincoIdentidadesComBcryptETenantCompleto()
    {
        var seed = Read("database/seeds/development/122_usuarios_homologacao_plantaopro.sql");

        foreach (var login in new[]
        {
            "superadmin@plantaopro.local",
            "admin.clinica@plantaopro.local",
            "medico@plantaopro.local",
            "recepcao@plantaopro.local",
            "financeiro@plantaopro.local"
        })
            Assert.Contains(login, seed, StringComparison.Ordinal);

        Assert.Equal(5, System.Text.RegularExpressions.Regex.Matches(seed, @"\$2a\$11\$").Count);
        Assert.Contains("Clínica Modelo PlantãoPro", seed, StringComparison.Ordinal);
        Assert.Contains("Plano Homologação Saúde 360", seed, StringComparison.Ordinal);
        Assert.Contains("plantaopro.tenant_modulos", seed, StringComparison.Ordinal);
        Assert.Contains("plantaopro.hospitais", seed, StringComparison.Ordinal);
        Assert.Contains("plantaopro.especialidades", seed, StringComparison.Ordinal);
        Assert.Contains("plantaopro.medicos", seed, StringComparison.Ordinal);
    }

    [Fact]
    public void SenhasAbertas_DevemExistirSomenteNaDocumentacaoDeHomologacao()
    {
        var seed = Read("database/seeds/development/122_usuarios_homologacao_plantaopro.sql");
        var docs = Read("docs/usuarios-teste.md");

        foreach (var marker in new[] { "Super@", "Cliente@", "Medico@", "Recepcao@", "Financeiro@" })
        {
            Assert.DoesNotContain(marker, seed, StringComparison.Ordinal);
            Assert.Contains(marker, docs, StringComparison.Ordinal);
        }

        Assert.Contains("Não usar em produção", docs, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Login_DeveBloquearUsuarioETenantInativosEEmitirClaimsMinimas()
    {
        var auth = Read("backend/PlantaoPro.Api/Data.cs");

        Assert.Contains("USER_INACTIVE", auth, StringComparison.Ordinal);
        Assert.Contains("TENANT_INACTIVE", auth, StringComparison.Ordinal);
        Assert.Contains("BCrypt.Net.BCrypt.Verify", auth, StringComparison.Ordinal);
        Assert.Contains("is_global_admin", auth, StringComparison.Ordinal);
        Assert.Contains("tenant_name", auth, StringComparison.Ordinal);
        Assert.Contains("permission", auth, StringComparison.Ordinal);
        Assert.Contains("module", auth, StringComparison.Ordinal);
    }
}
