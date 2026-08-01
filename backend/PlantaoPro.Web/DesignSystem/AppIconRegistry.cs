using System.Collections.ObjectModel;

namespace PlantaoPro.Web.DesignSystem;

public static class AppIconRegistry
{
    private static readonly IReadOnlyDictionary<AppIconKey, AppIcon> Icons =
        new ReadOnlyDictionary<AppIconKey, AppIcon>(Enum.GetValues<AppIconKey>()
            .ToDictionary(key => key, key => new AppIcon(key, ToKebabCase(key.ToString()))));

    public static AppIcon Resolve(AppIconKey key) =>
        Icons.TryGetValue(key, out var icon) ? icon : Icons[AppIconKey.Unknown];

    public static bool IsRegistered(AppIconKey key) => Icons.ContainsKey(key);

    private static string ToKebabCase(string value) =>
        string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? "-" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));
}
