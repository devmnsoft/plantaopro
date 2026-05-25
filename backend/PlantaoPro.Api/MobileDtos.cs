namespace PlantaoPro.Api.Models;

public class MobileLoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class MobileLoginResponseDto
{
    public MobileLoginResponseDto(string token, string? refreshToken, DateTime expiresAtUtc, string[] roles)
    {
        Token = token;
        RefreshToken = refreshToken;
        ExpiresAtUtc = expiresAtUtc;
        Roles = roles ?? Array.Empty<string>();
    }

    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}
