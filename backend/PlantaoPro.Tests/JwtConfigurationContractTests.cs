using Xunit;

namespace PlantaoPro.Tests;

public class JwtConfigurationContractTests
{
    private static string Root()
    {
        var dir = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(dir, "README.md"))) dir = Directory.GetParent(dir)!.FullName;
        return dir;
    }

    private static string Read(string path) => File.ReadAllText(Path.Combine(Root(), path));

    [Fact]
    public void ProgramDeveValidarJwtSemExporSegredo()
    {
        var program = Read("backend/PlantaoPro.Api/Program.cs");
        Assert.Contains("jwt[\"Key\"]", program);
        Assert.Contains("jwt[\"Issuer\"]", program);
        Assert.Contains("jwt[\"Audience\"]", program);
        Assert.Contains("JwtConfigurationValidator.Validate(jwtKey, jwtIssuer, jwtAudience)", program);
        var validator = Read("backend/PlantaoPro.Api/Security/JwtConfigurationValidator.cs");
        Assert.Contains("MinimumKeyLength = 32", validator);
        Assert.Contains("Configure Jwt__Key com pelo menos 32 caracteres", validator);
        Assert.DoesNotContain("Jwt__Key=", program);
        Assert.DoesNotContain("Console.WriteLine(jwtKey", program);
    }

    [Fact]
    public void DocumentacaoCiEnvExampleDevemConfigurarJwtSeguro()
    {
        Assert.Contains("Jwt__Key", Read("README.md"));
        Assert.Contains("Jwt__Key", Read("docs/configuracao-jwt-local-ci-iis.md"));
        Assert.Contains("Jwt__Key: PLANTAOPRO_CI_JWT_KEY_2026_CHANGE_ME_64_CHARS", Read(".github/workflows/dotnet-ci.yml"));
        Assert.True(File.Exists(Path.Combine(Root(), ".env.example")));
        var envExample = Read(".env.example");
        Assert.Contains("PLANTAOPRO_JWT_KEY=PLANTAOPRO_LOCAL_DEV_JWT_KEY_2026_CHANGE_ME_64_CHARS", envExample);
        Assert.DoesNotContain("local-dev-jwt-key-change-me-with-32-chars", envExample);
    }

    [Fact]
    public void AppsettingsVersionadosNaoDevemConterChaveJwtReal()
    {
        var root = Root();
        var files = Directory.GetFiles(root, "appsettings.json", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("local-dev-jwt-key-change-me-with-32-chars", content);
            Assert.DoesNotContain("ci-demo-key-with-at-least-32-characters-change-me", content);
            Assert.DoesNotContain("CHANGE_ME_WITH" + "_32+_CHARS", content);
            Assert.DoesNotContain("PLANTAOPRO_CI_JWT_KEY_2026_CHANGE_ME_64_CHARS", content);
        }
    }
    [Fact]
    public void AppsettingsDaApiVersionadoDevePermanecerSemSegredosEFlagsInseguras()
    {
        var content = Read("backend/PlantaoPro.Api/appsettings.json");
        Assert.DoesNotContain("Password=" + "123456", content);
        Assert.DoesNotContain("Username=postgres;" + "Password=", content);
        Assert.DoesNotContain("CHANGE_ME_WITH" + "_32", content);
        Assert.DoesNotContain("\"AllowLegacyPostgresDatabase\": true", content);
        Assert.Contains("\"Default\": \"\"", content);
        Assert.Contains("\"Key\": \"\"", content);
    }

    [Fact]
    public void AppsettingsVersionadosNaoDevemConterSegredosOperacionaisConhecidos()
    {
        var root = Root();
        var files = Directory.GetFiles(root, "appsettings*.json", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("Password=" + "123456", content);
            Assert.DoesNotContain("Username=postgres;" + "Password=", content);
            Assert.DoesNotContain("CHANGE_ME_WITH" + "_32", content);
            if (Path.GetFileName(file).Equals("appsettings.json", StringComparison.OrdinalIgnoreCase))
            {
                Assert.DoesNotContain("\"AllowLegacyPostgresDatabase\": true", content);
            }
        }
    }

}
