namespace PlantaoPro.Tests;

public sealed class V116PersistenceContractTests
{
    private static string Root(){ var d=new DirectoryInfo(AppContext.BaseDirectory); while(d is not null && !Directory.Exists(Path.Combine(d.FullName,".git"))) d=d.Parent; return d?.FullName ?? throw new InvalidOperationException("Raiz não encontrada."); }
    private static string Read(string path)=>File.ReadAllText(Path.Combine(Root(), path));
    [Fact]
    public void ServicesV116UsamDapperPostgresTenantAuditoriaENaoDemoEmMemoria()
    {
        var source = Read("backend/PlantaoPro.Api/V116ConsolidacaoServices.cs");
        Assert.Contains("using Dapper", source);
        Assert.Contains("using Npgsql", source);
        Assert.Contains("ICurrentUserService", source);
        Assert.Contains("IAuditService", source);
        Assert.Contains("tenant_id = @TenantId", source);
        Assert.Contains("ApiResponse", source);
        Assert.DoesNotContain("Demo" + "TenantId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Seed" + "(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Task" + ".FromResult", source, StringComparison.Ordinal);
    }
}
