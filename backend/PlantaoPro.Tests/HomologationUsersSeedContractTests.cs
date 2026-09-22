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
        Assert.Contains("PasswordHashService.Verify", auth, StringComparison.Ordinal);
        Assert.Contains("is_global_admin", auth, StringComparison.Ordinal);
        Assert.Contains("tenant_name", auth, StringComparison.Ordinal);
        Assert.Contains("permission", auth, StringComparison.Ordinal);
        Assert.Contains("module", auth, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Super@123456", "$2a$11$KZ80jdGp.ymLQ/E6zk8vluf6o4/.Ur2cEKxjD4Hp4jx5YETf7OaUG")]
    [InlineData("Cliente@123456", "$2a$11$4jafymzm6xqC48JdaVE3GuH0Dy2evtr/dqT7sKDqUe92OwpbaLbX2")]
    [InlineData("Medico@123456", "$2a$11$EIMmoQs8gPeaCFShI4.ACeL2WdJFeFulTrXL4JjBKFgJLCmqc11q2")]
    [InlineData("Recepcao@123456", "$2a$11$1biWFM2YemJyh9DaoRRjXe3K5UpzJdN.GYJMeglzoODIBi9BTW5yK")]
    [InlineData("Financeiro@123456", "$2a$11$9URjd.sZ/id/DeASc.y4a.s/fX3GbcINasNKsgRtMwi10u1/b7Ss2")]
    public void HashesDeHomologacao_DevemValidarComOMesmoServicoDaApi(string password, string hash)
    {
        Assert.True(PlantaoPro.Api.Security.PasswordHashService.Verify(password, hash));
        Assert.False(PlantaoPro.Api.Security.PasswordHashService.Verify(password + "-incorreta", hash));
    }

    [Fact]
    public void PasswordHashService_DeveGerarSaltEmbutidoERecusarHashMalformado()
    {
        var first = PlantaoPro.Api.Security.PasswordHashService.Hash("Senha-Forte@123");
        var second = PlantaoPro.Api.Security.PasswordHashService.Hash("Senha-Forte@123");

        Assert.NotEqual(first, second);
        Assert.StartsWith("$2", first, StringComparison.Ordinal);
        Assert.True(PlantaoPro.Api.Security.PasswordHashService.Verify("Senha-Forte@123", first));
        Assert.False(PlantaoPro.Api.Security.PasswordHashService.Verify("Senha-Forte@123", "hash-invalido"));
    }
}
