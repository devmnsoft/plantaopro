using System.Security.Cryptography;
using System.Text;

namespace PlantaoPro.Domain.Identity;

public enum LoginIdentifierKind
{
    Invalid,
    Email,
    Cpf,
    Cnpj
}

/// <summary>Canonical, infrastructure-free interpretation of the login identifiers already supported.</summary>
public static class LoginIdentifierNormalizer
{
    public static LoginIdentifierKind Classify(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Contains('@') && normalized.Length <= 254) return LoginIdentifierKind.Email;
        return Digits(normalized).Length switch
        {
            11 => LoginIdentifierKind.Cpf,
            14 => LoginIdentifierKind.Cnpj,
            _ => LoginIdentifierKind.Invalid
        };
    }

    public static string Normalize(string? value, LoginIdentifierKind kind) =>
        kind == LoginIdentifierKind.Email ? (value ?? string.Empty).Trim().ToLowerInvariant() : Digits(value);

    public static string AuditValue(string? value, LoginIdentifierKind kind)
    {
        var normalized = Normalize(value, kind);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).Substring(0, 12);
        return $"{kind.ToString().ToUpperInvariant()}:{fingerprint}";
    }

    private static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}

public static class IdentityEligibilityPolicy
{
    public static bool IsActive(string? registrationStatus, string? userStatus) =>
        string.Equals(registrationStatus, "A", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(userStatus, "ATIVO", StringComparison.OrdinalIgnoreCase);

    public static bool HasExactlyOneCredentialMatch(int matches) => matches == 1;
}
