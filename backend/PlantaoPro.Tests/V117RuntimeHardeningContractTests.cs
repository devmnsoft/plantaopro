namespace PlantaoPro.Tests;

public sealed class V117RuntimeHardeningContractTests
{
    private static string Root(){ var d=new DirectoryInfo(AppContext.BaseDirectory); while(d is not null && !Directory.Exists(Path.Combine(d.FullName,".git"))) d=d.Parent; return d?.FullName ?? throw new InvalidOperationException("Raiz não encontrada."); }
    private static string Read(string path)=>File.ReadAllText(Path.Combine(Root(), path));
    [Fact]
    public void RuntimeV117DocumentaSeedControladoSmokeECi()
    {
        Assert.Contains("DevelopmentSeed:Enabled", Read("backend/PlantaoPro.Api/Program.cs"));
        Assert.True(File.Exists(Path.Combine(Root(), "scripts/smoke-test-v117.sh")));
        var ci = Read(".github/workflows/dotnet-ci.yml");
        Assert.Contains("runtime-e2e-v116", ci);
        Assert.Contains("runtime-e2e-v117", ci);
        Assert.Contains("smoke-test-v117.sh", ci);
        Assert.Contains("ASPNETCORE_ENVIRONMENT: Development", ci);
    }
    [Fact]
    public void DocumentacaoV117Existe()
    {
        foreach (var path in new[] { "docs/v1.17-hardening-runtime.md", "docs/v1.17-persistencia-v116.md", "docs/v1.17-smoke-bloqueante.md", "docs/v1.17-rotas-legadas.md", "docs/v1.17-qa-navegada-perfil.md", "docs/homologacao/v117-smoke-result.md", "docs/produto/matriz-status-funcional-plantao-pro.md" })
        {
            Assert.True(File.Exists(Path.Combine(Root(), path)), path);
        }
    }
}
