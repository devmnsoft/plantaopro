using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

public sealed class V1200BuildAndDatabaseContractTests
{
    [Fact]
    public void LookupsController_NormalizeTerm_accepts_termo_and_term_with_priority_and_limit()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryPathResolver.ResolveRoot(), "backend", "PlantaoPro.Api", "Controllers", "Saude360SupportControllers.cs"));
        Assert.Contains("private static string? NormalizeTerm(string? termo, string? term)", source);
        Assert.Contains("? termo", source);
        Assert.Contains("value.Trim()", source);
        Assert.Contains("value.Length <= 120", source);
        Assert.Contains("value.Substring(0, 120)", source);
        Assert.Matches(new Regex(@"Pacientes\(\[FromQuery\] string\? termo, \[FromQuery\] string\? term\).*NormalizeTerm\(termo, term\)", RegexOptions.Singleline), source);
    }

    [Fact]
    public void WebAccountController_uses_sub_claim_without_jwt_dependency()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryPathResolver.ResolveRoot(), "backend", "PlantaoPro.Web", "Controllers", "AccountController.cs"));
        Assert.DoesNotContain("System.IdentityModel.Tokens.Jwt", source);
        Assert.DoesNotContain("JwtRegisteredClaimNames.Sub", source);
        Assert.Contains("new Claim(\"sub\", login.UsuarioId.ToString())", source);
    }

    [Fact]
    public void Database_unaccent_bootstrap_is_legacy_schema_aware()
    {
        var schema = File.ReadAllText(RepositoryPathResolver.DatabaseFile("schema", "000_extensions_schema.sql"));
        Assert.Contains("WHERE e.extname = 'unaccent'", schema);
        Assert.Contains("ALTER EXTENSION unaccent SET SCHEMA public", schema);
        var generator = File.ReadAllText(Path.Combine(RepositoryPathResolver.ResolveRoot(), "scripts", "generate-scrpt-completo.py"));
        Assert.DoesNotContain("'public.unaccent('", generator);
    }
}
