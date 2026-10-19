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

/// <summary>
/// Referência canônica dos dígitos verificadores oficiais de CPF/CNPJ usada pelo
/// login guiado (R63). O espelho client-side (wwwroot/js/auth-login.js) DEVE
/// concordar com esta implementação — o contrato R63 trava os dois lados com os
/// mesmos vetores. CPF/CNPJ com dígitos verificadores inválidos não existem no
/// Cadastro Nacional (regras oficiais da Receita Federal).
/// </summary>
public static class LoginIdentifierChecksums
{
    public static bool IsValidCpf(string? value)
    {
        var d = DigitsOnly(value);
        if (d.Length != 11 || AllSameDigits(d)) return false;
        int DvPrimeiro(string baseNove)
        {
            int soma = 0;
            for (var i = 0; i < baseNove.Length; i++) soma += (baseNove[i] - '0') * (10 - i);
            var resto = (soma * 10) % 11;
            return resto == 10 ? 0 : resto;
        }
        int DvSegundo(string baseDez)
        {
            int soma = 0;
            for (var i = 0; i < baseDez.Length; i++) soma += (baseDez[i] - '0') * (11 - i);
            var resto = (soma * 10) % 11;
            return resto == 10 ? 0 : resto;
        }
        return DvPrimeiro(d.AsSpan(0, 9).ToString()) == d[9] - '0'
            && DvSegundo(d.AsSpan(0, 10).ToString()) == d[10] - '0';
    }

    public static bool IsValidCnpj(string? value)
    {
        var d = DigitsOnly(value);
        if (d.Length != 14 || AllSameDigits(d)) return false;
        int Dv(string baseN, int[] pesos)
        {
            int soma = 0;
            for (var i = 0; i < baseN.Length; i++) soma += (baseN[i] - '0') * pesos[i];
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        var pesosUm = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var pesosDois = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        return Dv(d.AsSpan(0, 12).ToString(), pesosUm) == d[12] - '0'
            && Dv(d.AsSpan(0, 13).ToString(), pesosDois) == d[13] - '0';
    }

    private static string DigitsOnly(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    private static bool AllSameDigits(string d) => d.Distinct().Count() == 1;
}

public static class IdentityEligibilityPolicy
{
    public static bool IsActive(string? registrationStatus, string? userStatus) =>
        string.Equals(registrationStatus, "A", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(userStatus, "ATIVO", StringComparison.OrdinalIgnoreCase);

    public static bool HasExactlyOneCredentialMatch(int matches) => matches == 1;
}
