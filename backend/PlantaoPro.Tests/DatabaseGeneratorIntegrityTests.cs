using System.Security.Cryptography;
using System.Text.Json;

namespace PlantaoPro.Tests;

public sealed class DatabaseGeneratorIntegrityTests
{
    [Fact]
    public void ScriptCompleto_DeveReferenciarHashAtualDeCadaFonteCanonica()
    {
        var root = RepositoryPathResolver.FindRepositoryRoot();
        var checksumsPath = Path.Combine(root, "database", "source-checksums.json");
        var script = File.ReadAllText(Path.Combine(root, "database", "scrpt_completo.sql"));
        var checksums = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(checksumsPath))!;

        Assert.NotEmpty(checksums);
        foreach (var (source, expectedHash) in checksums)
        {
            var bytes = File.ReadAllBytes(Path.Combine(root, source));
            var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            Assert.Equal(expectedHash, actualHash);
            Assert.Contains($"-- SOURCE: {source}\n-- SOURCE-SHA256: {actualHash}", script);
        }
    }
}
