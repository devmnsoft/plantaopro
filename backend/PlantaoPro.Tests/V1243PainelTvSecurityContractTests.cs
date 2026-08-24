using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

public sealed class V1243PainelTvSecurityContractTests
{
    private readonly string root = RepositoryPathResolver.ResolveRepositoryRoot();

    [Fact]
    public void Tv_publica_valida_identidade_token_validade_revogacao_e_escopo()
    {
        var service = File.ReadAllText(Path.Combine(root, "backend", "PlantaoPro.Api", "PainelTvService.cs"));

        Assert.Contains("SHA256.HashData", service, StringComparison.Ordinal);
        Assert.Contains("p.id=@painelId", service, StringComparison.Ordinal);
        Assert.Contains("t.token_hash=@tokenHash", service, StringComparison.Ordinal);
        Assert.Contains("t.revogado_em is null", service, StringComparison.Ordinal);
        Assert.Contains("t.expira_em>now()", service, StringComparison.Ordinal);
        Assert.Contains("f.cliente_id=@TenantId", service, StringComparison.Ordinal);
        Assert.Contains("a.unidade_id=@UnidadeId", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Tv_publica_projeta_somente_campos_minimos_e_mascara_nome()
    {
        var service = File.ReadAllText(Path.Combine(root, "backend", "PlantaoPro.Api", "PainelTvService.cs"));
        var dto = Regex.Match(service, @"record PainelTvCallDto\((?<fields>[^;]+)\);", RegexOptions.Singleline);

        Assert.True(dto.Success);
        Assert.Equal(new[] { "Senha", "NomeAbreviado", "Destino", "Horario", "Status" },
            Regex.Matches(dto.Groups["fields"].Value, @"\b[A-Z][A-Za-z]+(?=,|$)").Select(match => match.Value));
        Assert.Contains("left(split_part", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Token_publico_persiste_apenas_hash_sha256_com_ciclo_de_vida()
    {
        var migration = File.ReadAllText(Path.Combine(root, "database", "migrations", "2026_v1243_painel_publico_seguro.sql"));

        Assert.Contains("token_hash char(64) not null", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expira_em timestamptz not null", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("revogado_em timestamptz", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(new Regex(@"\btoken\s+(?:text|varchar)", RegexOptions.IgnoreCase), migration);
    }
}
