using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace PlantaoPro.Api;

public static class ApiRouteStartupValidator
{
    public static void Validate(WebApplication app)
    {
        if (!(app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing") || app.Environment.IsEnvironment("CI") || app.Configuration.GetValue<bool>("ApiRoutes:FailOnDuplicatesInProduction"))) return;
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        var duplicates = provider.ApiDescriptionGroups.Items.SelectMany(g => g.Items)
            .Where(x => !string.IsNullOrWhiteSpace(x.HttpMethod))
            .GroupBy(x => x.HttpMethod!.ToUpperInvariant() + " " + NormalizePath(x.RelativePath))
            .Where(g => g.Count() > 1).ToArray();
        if (duplicates.Length == 0) return;
        var message = "Duplicate API route detected:" + Environment.NewLine + string.Join(Environment.NewLine, duplicates.Select(g => g.Key + Environment.NewLine + string.Join(Environment.NewLine, g.Select(d => "- " + d.ActionDescriptor.DisplayName))));
        throw new InvalidOperationException(message);
    }

    public static string NormalizePath(string? path)
    {
        var value = (path ?? string.Empty).Split('?', 2)[0].Trim().Trim('/').ToLowerInvariant();
        value = Regex.Replace(value, @"\{([^}:]+):[^}]+\}", "{$1}");
        value = Regex.Replace(value, @"/+", "/");
        return "/" + value;
    }
}
