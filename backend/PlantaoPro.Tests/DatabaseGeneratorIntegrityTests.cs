using System.Security.Cryptography;
using System.Text.Json;

namespace PlantaoPro.Tests;

public sealed class DatabaseGeneratorIntegrityTests
{
    [Fact]
    public void ScriptCompleto_DeveReferenciarHashAtualDeCadaFonteCanonica()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        var checksumsPath = Path.Combine(root, "database", "source-checksums.json");
        var script = File.ReadAllText(Path.Combine(root, "database", "scrpt_completo.sql"));
        var checksums = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(checksumsPath))!;

        Assert.NotEmpty(checksums);
        foreach (var (source, expectedHash) in checksums)
        {
            var rawBytes = File.ReadAllBytes(Path.Combine(root, source));
            var actualHash = Convert.ToHexString(SHA256.HashData(rawBytes)).ToLowerInvariant();
            if (actualHash != expectedHash)
            {
                var normalizedBytes = System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(root, source)).Replace("\r\n", "\n"));
                var normalizedHash = Convert.ToHexString(SHA256.HashData(normalizedBytes)).ToLowerInvariant();
                if (normalizedHash == expectedHash) actualHash = normalizedHash;
            }
            Assert.Equal(expectedHash, actualHash);
            Assert.Contains($"-- SOURCE: {source}\n-- SOURCE-SHA256: {expectedHash}", script.Replace("\r\n", "\n"));
        }
    }

    [Fact]
    public void ScriptCompleto_NaoContemCriacaoDeIndiceSemIdempotencia()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        var sql = File.ReadAllText(Path.Combine(root, "database", "scrpt_completo.sql")).Replace("\r\n", "\n");
        var idempotente = new System.Text.RegularExpressions.Regex(
            @"^\s*CREATE\s+(?:UNIQUE\s+)?INDEX\s+(?:IF\s+NOT\s+EXISTS\b|CONCURRENTLY\b)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        foreach (var line in sql.Split('\n'))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(line, @"^\s*CREATE\s+(?:UNIQUE\s+)?INDEX\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                continue;
            if (!idempotente.IsMatch(line))
                Assert.Fail($"Criação de índice não idempotente impede reexecução do script: {line.Trim()}");
        }
    }
}
