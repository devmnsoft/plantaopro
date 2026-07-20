namespace PlantaoPro.Tests;

public sealed class SmokeBlockingContractTests
{
    private static string Root(){ var d=new DirectoryInfo(AppContext.BaseDirectory); while(d is not null && !Directory.Exists(Path.Combine(d.FullName,".git"))) d=d.Parent; return d?.FullName ?? throw new InvalidOperationException("Raiz não encontrada."); }
    [Fact]
    public void SmokeV116EV117FalhamPara000404500ETemJsonPassedFailed()
    {
        foreach (var script in new[] { "scripts/smoke-test-v116.sh", "scripts/smoke-test-v117.sh" })
        {
            var source = File.ReadAllText(Path.Combine(Root(), script));
            Assert.Contains("set -euo pipefail", source);
            Assert.Contains("FAILED", source);
            Assert.Contains("PASSED", source);
            Assert.Contains("000", source);
            Assert.DoesNotContain("|| true", source, StringComparison.Ordinal);
            Assert.DoesNotContain("SCRIPT_EXECUTADO", source, StringComparison.Ordinal);
        }
    }
}
