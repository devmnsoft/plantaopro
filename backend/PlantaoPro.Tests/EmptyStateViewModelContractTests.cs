using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

public sealed class EmptyStateViewModelContractTests
{
    [Fact]
    public void Web_project_declares_single_canonical_empty_state_view_model()
    {
        var root = Path.Combine(RepositoryPathResolver.ResolveRoot(), "backend", "PlantaoPro.Web");
        var declarations = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), @"\b(?:class|record)\s+EmptyStateViewModel\b")
                .Select(_ => file))
            .ToArray();

        var declaration = Assert.Single(declarations);
        Assert.EndsWith(Path.Combine("Models", "EmptyStateViewModel.cs"), declaration);
    }
}
