using Xunit;

namespace PlantaoPro.Tests;

public sealed class RepositoryPathResolverBehaviorTests
{
    [Fact]
    public void CanonicalRoots_DoNotDuplicateBackendSegment()
    {
        Assert.True(File.Exists(Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.sln")));
        Assert.DoesNotContain(
            $"backend{Path.DirectorySeparatorChar}backend",
            RepositoryPathResolver.BackendRoot,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData("node_modules")]
    [InlineData(".git")]
    [InlineData("artifacts")]
    public void GeneratedDirectories_AreIgnored(string directory)
    {
        var path = Path.Combine(RepositoryPathResolver.RepoRoot, directory, "arquivo.cs");
        Assert.True(RepositoryPathResolver.IsGeneratedOrIgnoredPath(path));
    }

    [Fact]
    public void SourceEnumeration_ReturnsSourcesButNotBuildOutputs()
    {
        var files = RepositoryPathResolver.EnumerateSourceFiles(RepositoryPathResolver.ApiRoot).ToArray();
        Assert.Contains(files, path => path.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, path => RepositoryPathResolver.IsGeneratedOrIgnoredPath(path));
    }
}
