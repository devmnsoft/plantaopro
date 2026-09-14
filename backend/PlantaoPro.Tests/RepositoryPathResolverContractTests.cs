namespace PlantaoPro.Tests;

public sealed class RepositoryPathResolverContractTests
{
    [Fact]
    public void RepositoryRoot_RemainsEncapsulatedBehindRepoRoot()
    {
        var helper = File.ReadAllText(Path.Combine(
            RepositoryPathResolver.BackendProject("PlantaoPro.Tests"),
            "RepositoryPathResolver.cs"));
        var invalidAccess = "RepositoryPathResolver." + "Root";
        var externalAccesses = Directory
            .EnumerateFiles(RepositoryPathResolver.BackendProject("PlantaoPro.Tests"), "*.cs")
            .Where(path => !path.EndsWith("RepositoryPathResolverContractTests.cs", StringComparison.OrdinalIgnoreCase))
            .SelectMany(File.ReadLines)
            .Where(line => line.Contains(invalidAccess, StringComparison.Ordinal))
            .ToArray();

        Assert.Contains("private static readonly Lazy<string> Root", helper);
        Assert.Contains("public static string RepoRoot => Root.Value", helper);
        Assert.Empty(externalAccesses);
    }

    [Fact]
    public void Resolver_DeveExporTodosOsCaminhosCanonicosDoRepositorio()
    {
        Assert.True(File.Exists(Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.sln")));
        Assert.EndsWith("backend", RepositoryPathResolver.BackendRoot.Replace('\\', '/'));
        Assert.EndsWith("backend/PlantaoPro.Api", RepositoryPathResolver.ApiRoot.Replace('\\', '/'));
        Assert.EndsWith("backend/PlantaoPro.Web", RepositoryPathResolver.WebRoot.Replace('\\', '/'));
        Assert.EndsWith("database", RepositoryPathResolver.DatabaseRoot.Replace('\\', '/'));
        Assert.EndsWith("scripts", RepositoryPathResolver.ScriptsRoot.Replace('\\', '/'));
        Assert.EndsWith("docs", RepositoryPathResolver.DocsRoot.Replace('\\', '/'));
        Assert.EndsWith("artifacts", RepositoryPathResolver.ArtifactsRoot.Replace('\\', '/'));
    }

    [Fact]
    public void Resolver_NaoDeveGerarCaminhosBackendDuplicados()
    {
        var caminhos = new[]
        {
            RepositoryPathResolver.ApiRoot,
            RepositoryPathResolver.WebRoot,
            RepositoryPathResolver.BackendProject("PlantaoPro.Api"),
            RepositoryPathResolver.BackendProject("PlantaoPro.Web")
        };

        foreach (var caminho in caminhos.Select(c => c.Replace('\\', '/')))
        {
            Assert.DoesNotContain("backend" + "/backend/", caminho, StringComparison.OrdinalIgnoreCase);
            Assert.True(Directory.Exists(caminho), $"Caminho canônico ausente: {caminho}");
        }
    }

    [Fact]
    public void Suite_NaoDeveConterCaminhoBackendDuplicado()
    {
        const string segmentoInvalido = "backend" + "/backend";
        var ocorrencias = Directory
            .EnumerateFiles(RepositoryPathResolver.BackendProject("PlantaoPro.Tests"), "*.cs", SearchOption.AllDirectories)
            .Where(arquivo => !arquivo.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(arquivo => !arquivo.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(arquivo => File.ReadLines(arquivo).Select((linha, indice) => new { arquivo, linha, indice }))
            .Where(item => item.linha.Contains(segmentoInvalido, StringComparison.OrdinalIgnoreCase))
            .Select(item => $"{Path.GetRelativePath(RepositoryPathResolver.RepoRoot, item.arquivo)}:{item.indice + 1}")
            .ToArray();

        Assert.True(ocorrencias.Length == 0,
            $"A suíte contém caminho duplicado de backend: {string.Join(", ", ocorrencias)}");
    }
}
