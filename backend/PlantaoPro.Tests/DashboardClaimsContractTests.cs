namespace PlantaoPro.Tests;

public sealed class DashboardClaimsContractTests
{
    private static string Root(){ var d=new DirectoryInfo(AppContext.BaseDirectory); while(d is not null && !Directory.Exists(Path.Combine(d.FullName,".git"))) d=d.Parent; return d?.FullName ?? throw new InvalidOperationException("Raiz não encontrada."); }
    [Fact]
    public void DashboardNaoDeveUsarClaimsFirstOuGuidParseDireto()
    {
        var source = File.ReadAllText(Path.Combine(Root(), "backend/PlantaoPro.Api/Controllers/DashboardController.cs"));
        Assert.DoesNotContain("Claims" + ".First", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Guid.Parse" + "(User.Claims", source, StringComparison.Ordinal);
        Assert.Contains("User.FindFirst(\"uid\")", source, StringComparison.Ordinal);
        Assert.Contains("Guid.TryParse", source, StringComparison.Ordinal);
        Assert.Contains("Unauthorized(ApiResponse<string>.Fail", source, StringComparison.Ordinal);
    }
}
