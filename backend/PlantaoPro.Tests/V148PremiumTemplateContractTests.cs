using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

public sealed class V148PremiumTemplateContractTests
{
    [Fact]
    public void Sidebar_links_only_to_existing_controllers()
    {
        var sidebar = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Shared", "_AppSidebar.cshtml"));
        var controllerNames = Regex.Matches(sidebar, "asp-controller=\\\"(?<name>[A-Za-z0-9]+)\\\"")
            .Select(match => match.Groups["name"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var controllerSources = string.Join("\n", Directory.GetFiles(
            Path.Combine(RepositoryPathResolver.WebRoot, "Controllers"), "*.cs").Select(File.ReadAllText));

        Assert.All(controllerNames, name =>
            Assert.Contains($"class {name}Controller", controllerSources, StringComparison.Ordinal));
    }

    [Fact]
    public void Global_shell_uses_canonical_premium_design_system()
    {
        var layout = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Shared", "_Layout.cshtml"));
        var tokens = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "wwwroot", "css", "design-system", "tokens.css"));
        var experience = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "wwwroot", "css", "design-system", "premium-experience.css"));

        Assert.Contains("~/css/plantaopro.css", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("_NavigationRail", layout, StringComparison.Ordinal);
        Assert.Contains("--pp-color-primary", tokens, StringComparison.Ordinal);
        Assert.Contains("v1.48", experience, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prefers-reduced-motion", experience, StringComparison.Ordinal);
    }
}
