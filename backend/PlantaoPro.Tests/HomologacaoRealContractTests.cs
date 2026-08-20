namespace PlantaoPro.Tests;

public class HomologacaoRealContractTests
{
    [Fact]
    public void Solution_DeveConterProjetoDeTestes()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var solution = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.sln"));
        Assert.Contains("PlantaoPro.Tests", solution, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PlantaoPro.Tests\\PlantaoPro.Tests.csproj", solution, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Views_NaoDevemUsarRecursosRazorPagesOuDynamicOuHrefVazio()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var views = Directory.EnumerateFiles(Path.Combine(RepositoryPathResolver.WebRoot, "Views"), "*.cshtml", SearchOption.AllDirectories);
        foreach (var file in views)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotMatch(@"(?im)^\s*@page(?:\s|$)", content);
            Assert.DoesNotMatch(@"\basp-page(?:\s|=)", content);
            Assert.DoesNotContain("@model" + " dynamic", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("href=\"#\"", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void JavascriptProprio_NaoDeveUsarAlertOuConfirmNativo()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var arquivos = Directory.EnumerateFiles(Path.Combine(RepositoryPathResolver.WebRoot, "wwwroot"), "*.js", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(raiz, "mobile", "PlantaoPro.App", "src"), "*.ts*", SearchOption.AllDirectories));
        foreach (var file in arquivos)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotMatch(@"(?i)(?<![\w.])(?:window\.)?(?:alert|confirm)\s*\(", content);
            Assert.DoesNotContain("Alert.alert", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ArtefatosHomologacaoDevemExistir()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var required = new[]
        {
            Path.Combine(".github", "workflows", "dotnet-ci.yml"),
            "docker-compose.yml",
            Path.Combine("scripts", "database", "apply-local-postgres.sh"),
            Path.Combine("scripts", "database", "apply-local-postgres.ps1"),
            Path.Combine("docs", "deploy", "execucao-local-postgresql.md"),
            Path.Combine("docs", "homologacao", "roteiro-qa-executavel.md"),
            Path.Combine("docs", "demo", "checklist-demo-plantao-pro.md"),
            Path.Combine("docs", "seguranca", "checklist-lgpd-auditoria.md")
        };
        foreach (var relative in required)
        {
            Assert.True(File.Exists(Path.Combine(raiz, relative)), $"Artefato obrigatório ausente: {relative}");
        }
    }

    [Fact]
    public void HealthEndpointsDevemExistirSemExporConnectionString()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var program = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Program.cs"));
        var health = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Controllers", "HealthController.cs"));
        Assert.Contains("MapGet(\"/\"", program, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Route(\"api/health\")]", health, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[HttpGet(\"db\")]", health, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", health, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppsettingsNaoDevemConterSegredosReais()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var files = Directory.EnumerateFiles(Path.Combine(raiz, "backend"), "appsettings*.json", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("eyJ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password=postgres", content, StringComparison.OrdinalIgnoreCase);
            Assert.True(content.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) || content.Contains("SUA_SENHA", StringComparison.OrdinalIgnoreCase) || content.Contains("ALTERAR_CHAVE", StringComparison.OrdinalIgnoreCase), $"Appsettings sem placeholder explícito: {file}");
        }
    }
}
