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
}
