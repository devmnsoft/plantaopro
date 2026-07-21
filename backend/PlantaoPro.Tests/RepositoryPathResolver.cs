namespace PlantaoPro.Tests;

public static class RepositoryPathResolver
{
    public static string ResolveRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "backend", "PlantaoPro.sln")) && Directory.Exists(Path.Combine(dir.FullName, "database")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Raiz do repositório PlantãoPro não encontrada.");
    }

    public static string BackendProject(string projectName) => Path.Combine(ResolveRoot(), "backend", projectName);
    public static string DatabaseFile(params string[] parts) => Path.Combine(new[] { ResolveRoot(), "database" }.Concat(parts).ToArray());
}
