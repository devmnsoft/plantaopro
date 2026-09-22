namespace PlantaoPro.Api.Security;

/// <summary>
/// Single password-hashing boundary for authentication and account provisioning.
/// BCrypt embeds the random salt and work factor in the encoded hash, so no
/// separate salt column is required.
/// </summary>
public static class PasswordHashService
{
    public const int WorkFactor = 11;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public static bool Verify(string? password, string? encodedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(encodedHash)) return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, encodedHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
