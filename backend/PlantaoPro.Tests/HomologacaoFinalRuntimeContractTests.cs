namespace PlantaoPro.Tests;

public class HomologacaoFinalRuntimeContractTests
{
    [Fact]
    public void WorkflowDotnetCi_DeveDiagnosticarRestaurarBuildarETestarSolutionNet10()
    {
        var raiz = EncontrarRaizRepositorio();
        var workflow = File.ReadAllText(Path.Combine(raiz, ".github", "workflows", "dotnet-ci.yml"));

        Assert.Contains("actions/checkout@v4", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("actions/setup-dotnet@v4", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet-version: '10.0.x'", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet-quality: 'preview'", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet --info", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet restore backend/PlantaoPro.sln", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet build backend/PlantaoPro.sln -c Release", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SmokeApi_DeveValidarHealthSwaggerLoginEEndpointAutenticadoSemLogarToken()
    {
        var raiz = EncontrarRaizRepositorio();
        var shell = File.ReadAllText(Path.Combine(raiz, "scripts", "smoke", "smoke-api.sh"));
        var powershell = File.ReadAllText(Path.Combine(raiz, "scripts", "smoke", "smoke-api.ps1"));

        foreach (var content in new[] { shell, powershell })
        {
            Assert.Contains("/api/health", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/api/health/db", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/swagger", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/api/auth/login", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/api/usuarios/me", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("echo $TOKEN", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Write-Host $token", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DocumentacaoFinal_DeveRegistrarBloqueiosReaisEComandosReproduziveis()
    {
        var raiz = EncontrarRaizRepositorio();
        var pendencias = File.ReadAllText(Path.Combine(raiz, "docs", "homologacao", "pendencias-reais-pos-auditoria.md"));
        var smokeWeb = File.ReadAllText(Path.Combine(raiz, "docs", "homologacao", "smoke-web.md"));
        var qaMobile = File.ReadAllText(Path.Combine(raiz, "docs", "mobile", "qa-mobile.md"));

        Assert.Contains("dotnet não encontrado", pendencias, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker não encontrado", pendencias, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("psql não encontrado", pendencias, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/Account/Login", smokeWeb, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("npm run start", qaMobile, StringComparison.OrdinalIgnoreCase);
    }

    private static string EncontrarRaizRepositorio()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null && !Directory.Exists(Path.Combine(diretorio.FullName, ".git")))
        {
            diretorio = diretorio.Parent;
        }

        if (diretorio is null)
        {
            throw new InvalidOperationException("Raiz do repositório não encontrada.");
        }

        return diretorio.FullName;
    }
}
