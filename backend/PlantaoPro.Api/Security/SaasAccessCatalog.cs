namespace PlantaoPro.Api.Security;

public static class SaasPermissions
{
    public static readonly IReadOnlyCollection<string> All = new[]
    {
        "tenants.read", "tenants.manage", "users.read", "users.manage", "roles.read", "roles.manage",
        "units.read", "units.manage", "professionals.read", "professionals.manage", "schedules.read",
        "schedules.manage", "shifts.read", "shifts.manage", "reports.read", "reports.export",
        "finance.read", "finance.manage", "audit.read", "settings.manage", "white_label.read",
        "white_label.manage", "plans.read", "plans.manage", "modules.manage"
    };

    public static bool IsKnown(string permission) => All.Contains(permission, StringComparer.OrdinalIgnoreCase);
}

public static class SaasModules
{
    public static readonly IReadOnlyCollection<string> All = new[]
    {
        "SCHEDULES", "SHIFTS", "PROFESSIONALS", "UNITS", "REPORTS", "FINANCE", "AUDIT",
        "WHITE_LABEL", "API_INTEGRATIONS", "MOBILE"
    };
}

public static class SaasProfilePermissions
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> Matrix =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [RolesConstants.PlatformAdmin] = SaasPermissions.All,
            [RolesConstants.TenantAdmin] = P("users.read", "users.manage", "roles.read", "roles.manage", "units.read", "units.manage", "professionals.read", "professionals.manage", "schedules.read", "schedules.manage", "shifts.read", "shifts.manage", "reports.read", "reports.export", "finance.read", "audit.read", "settings.manage", "white_label.read", "white_label.manage", "plans.read"),
            [RolesConstants.UnitManager] = P("units.read", "professionals.read", "schedules.read", "shifts.read", "reports.read"),
            [RolesConstants.ScheduleManager] = P("units.read", "professionals.read", "schedules.read", "schedules.manage", "shifts.read", "shifts.manage", "reports.read", "reports.export"),
            [RolesConstants.Professional] = P("schedules.read", "shifts.read"),
            [RolesConstants.FinanceManager] = P("reports.read", "reports.export", "finance.read", "finance.manage"),
            [RolesConstants.AuditorRole] = P("reports.read", "reports.export", "audit.read"),
            [RolesConstants.Support] = P("tenants.read", "users.read", "audit.read")
        };

    public static IReadOnlyCollection<string> For(string role) => Matrix.TryGetValue(role, out var value) ? value : Array.Empty<string>();
    public static bool Allows(string role, string permission) => IsReadOnlySupport(role, permission) && For(role).Contains(permission, StringComparer.OrdinalIgnoreCase);
    private static bool IsReadOnlySupport(string role, string permission) => !string.Equals(role, RolesConstants.Support, StringComparison.OrdinalIgnoreCase) || permission.EndsWith(".read", StringComparison.OrdinalIgnoreCase);
    private static IReadOnlyCollection<string> P(params string[] values) => values;
}

public static class WhiteLabelSecurityValidator
{
    private const double MinimumContrast = 4.5;
    private static readonly string[] AllowedAssetTypes = { "image/png", "image/jpeg", "image/webp", "image/svg+xml", "image/x-icon" };

    public static string? Validate(Models.WhiteLabelConfiguracaoDto value)
    {
        if (!Hex(value.CorPrimaria) || !Hex(value.CorSecundaria) || !Hex(value.CorFundo) || !Hex(value.CorMenu)) return "Cores devem estar no formato hexadecimal #RRGGBB.";
        if (Contrast(value.CorPrimaria, value.CorFundo) < MinimumContrast) return "A cor primária não possui contraste mínimo WCAG AA (4.5:1) com o fundo.";
        if (ContainsMarkup(value.NomePlataforma) || ContainsMarkup(value.ClienteNome) || ContainsMarkup(value.Slogan) || ContainsMarkup(value.TextoBoasVindas) || ContainsMarkup(value.TextoRodape)) return "HTML e scripts não são permitidos no white label.";
        return null;
    }

    public static string? ValidateAsset(string? contentType, long size, string? url)
    {
        if (size <= 0 || size > 2 * 1024 * 1024) return "Arquivo deve ter até 2MB.";
        if (!AllowedAssetTypes.Contains(contentType ?? string.Empty, StringComparer.OrdinalIgnoreCase)) return "Tipo de imagem não permitido.";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) return "URL HTTP(S) válida é obrigatória.";
        return null;
    }

    public static double Contrast(string foreground, string background)
    {
        var a = Luminance(foreground); var b = Luminance(background);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }

    private static bool Hex(string? value) => value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static bool ContainsMarkup(string? value) => value?.IndexOfAny(new[] { '<', '>' }) >= 0;
    private static double Luminance(string hex)
    {
        var channels = new[] { Convert.ToInt32(hex.Substring(1, 2), 16), Convert.ToInt32(hex.Substring(3, 2), 16), Convert.ToInt32(hex.Substring(5, 2), 16) };
        return channels.Select(channel => { var c = channel / 255d; return c <= .03928 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }).Zip(new[] { .2126, .7152, .0722 }, (channel, weight) => channel * weight).Sum();
    }
}
