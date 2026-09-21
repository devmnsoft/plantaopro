using System.Text.Json;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class DevelopmentConfigurationSecurityTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Development_settings_do_not_enable_unsafe_database_or_demo_defaults()
    {
        using var document = LoadDevelopmentSettings();
        var root = document.RootElement;

        Assert.False(root.GetProperty("Database").GetProperty("AllowLegacyPostgresDatabase").GetBoolean());
        Assert.False(root.GetProperty("Database").GetProperty("AllowDevelopmentAutoCreate").GetBoolean());
        Assert.True(root.GetProperty("Authentication").GetProperty("LoginLockoutEnabled").GetBoolean());
        Assert.False(root.GetProperty("DemoSeed").GetProperty("Enabled").GetBoolean());
        Assert.False(root.GetProperty("DemoSeed").GetProperty("AutoProvisionIfEmpty").GetBoolean());
        Assert.False(root.GetProperty("DemoSeed").GetProperty("ResetPasswordsOnStartup").GetBoolean());
    }

    [Fact]
    public void Development_settings_do_not_contain_jwt_or_demo_passwords()
    {
        using var document = LoadDevelopmentSettings();
        var root = document.RootElement;

        Assert.Equal(string.Empty, root.GetProperty("Jwt").GetProperty("Key").GetString());
        var demoSeed = root.GetProperty("DemoSeed");
        Assert.DoesNotContain(demoSeed.EnumerateObject(), property =>
            property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Senha", StringComparison.OrdinalIgnoreCase));
    }

    private static JsonDocument LoadDevelopmentSettings()
    {
        var path = Path.Combine(RepositoryRoot, "backend", "PlantaoPro.Api", "appsettings.Development.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "backend", "PlantaoPro.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Não foi possível localizar a raiz do repositório.");
    }
}
