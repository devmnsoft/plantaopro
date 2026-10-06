namespace PlantaoPro.CrossCutting.Security;

/// <summary>
/// B6 (rodada 4): fonte única dos perfis de administração global MNSOFT.
///
/// Historicamente os quatro códigos abaixo estavam duplicados em seis consultas
/// SQL da API (validação de sessão, teste de permissão efetiva, perfis
/// atribuíveis, proteção do último super administrador). A partir daqui toda
/// leitura/escrita usa esta lista — um novo alias só precisa ser acrescentado
/// em um lugar.
///
/// NÃO confundir com RoleCatalog.IsGlobal: esse classifica o ESCOPO do catálogo
/// de papéis (GLOBAL/HYBRID, inclui SUPORTE/COMERCIAL/CUSTOMER_SUCCESS) e orienta
/// navegação/contexto. Este classe define quem administra o SaaS como um todo.
/// </summary>
public static class GlobalAdminProfiles
{
    public static readonly IReadOnlyList<string> ProfileCodes = new[]
    {
        "ADMIN_GLOBAL",
        "ADMINISTRADOR_GLOBAL",
        "SUPER_ADMIN",
        "SUPER_ADMINISTRADOR"
    };

    /// <summary>Lista para embutir em SQL (valores compilados, sem entrada externa).</summary>
    public const string SqlInList = "('ADMIN_GLOBAL','ADMINISTRADOR_GLOBAL','SUPER_ADMIN','SUPER_ADMINISTRADOR')";

    public static bool IsGlobalProfile(string? perfil) =>
        !string.IsNullOrWhiteSpace(perfil) && ProfileCodes.Contains(perfil.Trim().ToUpperInvariant());
}
