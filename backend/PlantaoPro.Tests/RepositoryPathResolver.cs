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

    public static IEnumerable<string> EnumerateRepositoryFiles(
        IEnumerable<string> roots,
        string searchPattern)
    {
        ArgumentNullException.ThrowIfNull(roots);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchPattern);

        return roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories))
            .Where(path => !IsGeneratedOrIgnoredPath(path));
    }

    public static IEnumerable<string> EnumerateSourceFiles(string root) =>
        EnumerateRepositoryFiles(new[] { root }, "*.*")
            .Where(path => SourceExtensions.Contains(Path.GetExtension(path)));

    public static bool IsGeneratedOrIgnoredPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => IgnoredDirectories.Contains(part)) ||
               IgnoredExtensions.Contains(Path.GetExtension(path));
    }

    private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".cshtml", ".js", ".ts", ".tsx", ".json", ".sql", ".yml", ".yaml", ".md", ".css", ".html"
    };

    private static readonly HashSet<string> IgnoredExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll", ".exe", ".pdb", ".so", ".dylib", ".zip", ".gz", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".woff", ".woff2"
    };

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", ".git", "artifacts", ".vs", ".idea", "TestResults", "tmp", "temp"
    };

    private static bool IsGeneratedPath(string path) => IsGeneratedOrIgnoredPath(path);

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
