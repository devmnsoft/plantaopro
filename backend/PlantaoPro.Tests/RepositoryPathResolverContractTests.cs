namespace PlantaoPro.Tests;

public sealed class RepositoryPathResolverContractTests
{
    [Fact]
    public void Resolver_DeveExporTodosOsCaminhosCanonicosDoRepositorio()
    {
        Assert.True(File.Exists(Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.sln")));
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
