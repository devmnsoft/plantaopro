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

    [Fact]
    public void Razor_views_do_not_use_case_sensitive_named_arguments_for_empty_state_view_model()
    {
        var root = Path.Combine(RepositoryPathResolver.ResolveRoot(), "backend", "PlantaoPro.Web", "Views");
        var offenders = Directory.EnumerateFiles(root, "*.cshtml", SearchOption.AllDirectories)
            .Where(file => Regex.IsMatch(
                File.ReadAllText(file),
                @"new\s+(?:PlantaoPro\.Web\.Models\.)?EmptyStateViewModel\s*\([^)]*\b(?:Icon|Title|Description|ButtonText|ButtonAction|ButtonController|ButtonDisabled)\s*:",
                RegexOptions.Singleline))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Empty_state_partial_supports_primary_and_secondary_actions()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        var model = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Models", "EmptyStateViewModel.cs"));
        var partial = File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Shared", "_EmptyState.cshtml"));

        Assert.Contains("PrimaryActionText", model);
        Assert.Contains("PrimaryController", model);
        Assert.Contains("PrimaryAction", model);
        Assert.Contains("SecondaryActionText", model);
        Assert.Contains("SecondaryController", model);
        Assert.Contains("SecondaryAction", model);
        Assert.Contains("PrimaryActionText", partial);
        Assert.Contains("SecondaryActionText", partial);
    }

    [Fact]
    public void Razor_compile_is_enabled_for_web_project()
    {
        var project = File.ReadAllText(Path.Combine(
            RepositoryPathResolver.ResolveRoot(),
            "backend",
            "PlantaoPro.Web",
            "PlantaoPro.Web.csproj"));

        Assert.Contains("RazorCompileOnBuild", project);
        Assert.Contains("true", project);
    }
}
