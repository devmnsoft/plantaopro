namespace PlantaoPro.Api.Security;

public static class JwtConfigurationValidator
{
    public const int MinimumKeyLength = 32;

    public static void Validate(string? key, string? issuer, string? audience)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length < MinimumKeyLength)
        {
            throw new InvalidOperationException("Configuração Jwt:Key não encontrada ou inválida. Configure Jwt__Key com pelo menos 32 caracteres via variável de ambiente, user-secrets ou appsettings.Development.json local. Não versionar segredo real.");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Configuração Jwt:Issuer não encontrada ou inválida. Configure Jwt__Issuer via variável de ambiente, user-secrets ou appsettings.Development.json local.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Configuração Jwt:Audience não encontrada ou inválida. Configure Jwt__Audience via variável de ambiente, user-secrets ou appsettings.Development.json local.");
        }
    }
}
