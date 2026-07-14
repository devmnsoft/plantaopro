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
        Assert.Contains("jwtKey.Length < 32", program);
        Assert.Contains("jwt[\"Issuer\"]", program);
        Assert.Contains("jwt[\"Audience\"]", program);
        Assert.Contains("Configure Jwt__Key com pelo menos 32 caracteres", program);
        Assert.DoesNotContain("Jwt__Key=", program);
        Assert.DoesNotContain("Console.WriteLine(jwtKey", program);
    }

    [Fact]
    public void DocumentacaoCiEnvExampleDevemConfigurarJwtSeguro()
    {
        Assert.Contains("Jwt__Key", Read("README.md"));
        Assert.Contains("Jwt__Key", Read("docs/configuracao-jwt-local-ci.md"));
        Assert.Contains("Jwt__Key: ci-demo-key-with-at-least-32-characters-change-me", Read(".github/workflows/dotnet-ci.yml"));
        Assert.True(File.Exists(Path.Combine(Root(), ".env.example")));
        var envExample = Read(".env.example");
        Assert.Contains("PLANTAOPRO_JWT_KEY=CHANGE_ME_LOCAL_DEV_JWT_KEY_32_CHARS_MINIMUM", envExample);
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
            Assert.DoesNotContain("CHANGE_ME_WITH_32+_CHARS", content);
        }
    }
}
