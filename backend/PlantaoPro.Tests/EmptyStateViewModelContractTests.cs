using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

public sealed class EmptyStateViewModelContractTests
{
    [Fact]
    public void EmptyStateViewModel_DeveTerDeclaracaoUnica()
    {
        var repo = RepositoryPathResolver.Resolve();
        var matches = Directory.EnumerateFiles(repo, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                && !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"\b(class|record)\s+EmptyStateViewModel\b")
                .Select(_ => Path.GetRelativePath(repo, path)))
            .ToArray();

        Assert.Single(matches);
        Assert.Equal(Path.Combine("backend", "PlantaoPro.Web", "Models", "EmptyStateViewModel.cs"), matches[0]);
    }
}
