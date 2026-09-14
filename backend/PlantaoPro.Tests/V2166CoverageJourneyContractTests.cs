namespace PlantaoPro.Tests;

public sealed class V2166CoverageJourneyContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void AceiteDeConvite_ETransacionalIdempotenteETenantScoped()
    {
        var service = Read("backend/PlantaoPro.Api/Data.cs");
        Assert.Contains("AceitarConviteAsync", service);
        Assert.Contains("pg_advisory_xact_lock", service);
        Assert.Contains("for update", service, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("p.cliente_id=@clienteId", service);
        Assert.Contains("Convite já aceito; escala confirmada anteriormente", service);
        Assert.Contains("vagas_disponiveis=vagas_disponiveis-1", service);
        Assert.Contains("@inicio < p.data_fim and @fim > p.data_inicio", service);
    }

    [Fact]
    public void ConviteOperacional_TemValidadeENaoEConviteDeVinculo()
    {
        var invitation = Read("backend/PlantaoPro.Api/BusinessRulesServices.cs");
        Assert.Contains("plantao_convites", invitation);
        Assert.Contains("now()+interval '24 hours'", invitation);
        Assert.DoesNotContain("usuarios_convites", invitation);
    }

    [Fact]
    public void RegressaoClinica_UsaResolverSemExporCampoRoot()
    {
        var test = Read("backend/PlantaoPro.Tests/V2160ClinicalJourneyContractTests.cs");
        Assert.Contains("private static readonly string Root = RepositoryPathResolver.RepoRoot", test);
        var resolver = Read("backend/PlantaoPro.Tests/RepositoryPathResolver.cs");
        Assert.Contains("private static readonly Lazy<string> Root", resolver);
    }
}
