namespace PlantaoPro.Tests;

public static class RepositoryPathResolver
{
    private static readonly Lazy<string> Root = new(FindRepoRoot);

    public static string RepoRoot => Root.Value;
    public static string BackendRoot => Path.Combine(RepoRoot, "backend");
    public static string ApiRoot => Path.Combine(BackendRoot, "PlantaoPro.Api");
    public static string WebRoot => Path.Combine(BackendRoot, "PlantaoPro.Web");
    public static string DatabaseRoot => Path.Combine(RepoRoot, "database");
    public static string DocsRoot => Path.Combine(RepoRoot, "docs");
    public static string ScriptsRoot => Path.Combine(RepoRoot, "scripts");
    public static string ArtifactsRoot => Path.Combine(RepoRoot, "artifacts");

    public static string ResolveRoot() => RepoRoot;
    public static string BackendProject(string projectName) => Path.Combine(BackendRoot, projectName);
    public static string DatabaseFile(params string[] parts) => Path.Combine(new[] { DatabaseRoot }.Concat(parts).ToArray());
    public static string ArtifactFile(params string[] parts) => Path.Combine(new[] { ArtifactsRoot }.Concat(parts).ToArray());

    public static string ReadRepositoryFile(params string[] parts) =>
        File.ReadAllText(Path.Combine(new[] { RepoRoot }.Concat(parts).ToArray()));

    public static string ReadSourceContaining(string searchRoot, string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        var declaration = new System.Text.RegularExpressions.Regex(
            $@"\bclass\s+{System.Text.RegularExpressions.Regex.Escape(typeName)}\b",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        var matches = Directory.EnumerateFiles(searchRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path))
            .Where(path => declaration.IsMatch(File.ReadAllText(path)))
            .ToArray();

        return matches.Length switch
        {
            1 => File.ReadAllText(matches[0]),
            0 => throw new FileNotFoundException($"Tipo {typeName} não encontrado em {searchRoot}."),
            _ => throw new InvalidOperationException($"Tipo {typeName} possui {matches.Length} declarações em {searchRoot}.")
        };
    }

    private static bool IsGeneratedPath(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                         part.Equals("obj", StringComparison.OrdinalIgnoreCase));

    private static string FindRepoRoot()
    {
        var candidates = new[]
        {
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var candidate in candidates)
        {
            var dir = new DirectoryInfo(candidate);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "backend", "PlantaoPro.sln")))
                {
                    return dir.FullName;
                }
                if (dir.Name.Equals("backend", StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(dir.FullName, "PlantaoPro.sln")))
                {
                    return dir.Parent?.FullName ?? dir.FullName;
                }
                dir = dir.Parent;
            }
        }

        throw new DirectoryNotFoundException("Raiz do repositório PlantãoPro não encontrada procurando backend/PlantaoPro.sln.");
    }
}
