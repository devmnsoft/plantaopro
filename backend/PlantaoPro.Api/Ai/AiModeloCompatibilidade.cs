namespace PlantaoPro.Api.Ai;

/// <summary>
/// Governança de modelos por provedor (rodada 2 IA). A configuração aceita um
/// nome livre de modelo — mas ele precisa ser plausível para o provedor escolhido:
/// um modelo do catálogo do concorrente nunca chega à chamada HTTP.
///
/// <see cref="Validar"/> é BLOQUEANTE (erro impede salvar a configuração):
///   - groq: rejeita prefixos de outros provedores (gemini-, deepseek-);
///   - gemini: exige prefixo gemini-;
///   - deepseek: exige prefixo deepseek- e rejeita os aposentados
///     deepseek-chat / deepseek-reasoner (aposentados em 2026-07-24).
///
/// <see cref="Aviso"/> NÃO bloqueia: modelo com prefixo correto mas fora do
/// catálogo documentado (novidades de plano/enterprise entram aqui).
/// Referências consultadas em 2026-10-04: docs oficiais Groq, Google AI e DeepSeek.
/// </summary>
public static class AiModeloCompatibilidade
{
    /// <summary>Prefixos reconhecidos como catálogo self-service vigente da Groq.
    /// A partir de 2026-10 os ids expostos levam o prefixo do vendor (ex.: openai/gpt-oss-20b).</summary>
    private static readonly string[] GroqCatalogo = { "gpt-oss-", "llama-", "openai/gpt-oss-" };
    /// <summary>Famílias vigentes do Gemini (versões + aliases estáveis flash/pro).</summary>
    private static readonly string[] GeminiFamílias = { "gemini-1.5-", "gemini-2.0-", "gemini-2.5-", "gemini-3.", "gemini-flash-", "gemini-pro-" };
    /// <summary>Modelos vigentes do DeepSeek (deepseek-chat/-reasoner foram aposentados em 2026-07-24).</summary>
    private static readonly string[] DeepSeekCatalogo = { "deepseek-flash", "deepseek-v4-pro" };
    private static readonly string[] DeepSeekAposentados = { "deepseek-chat", "deepseek-reasoner" };

    public static string? Validar(string provedor, string modelo)
    {
        var m = modelo.Trim();
        return provedor switch
        {
            AiProviderCodes.Groq when m.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase) =>
                $"O modelo '{m}' pertence ao catálogo Gemini e não pode ser usado no provedor Groq.",

            AiProviderCodes.Groq when m.StartsWith("deepseek-", StringComparison.OrdinalIgnoreCase) =>
                $"O modelo '{m}' pertence ao catálogo DeepSeek e não pode ser usado no provedor Groq.",

            AiProviderCodes.Gemini when !m.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase) =>
                $"No provedor Gemini, use um modelo do catálogo Gemini (prefixo 'gemini-'); '{m}' não se aplica.",

            AiProviderCodes.DeepSeek when !m.StartsWith("deepseek-", StringComparison.OrdinalIgnoreCase) =>
                $"No provedor DeepSeek, use um modelo do catálogo DeepSeek (prefixo 'deepseek-'); '{m}' não se aplica.",

            AiProviderCodes.DeepSeek
                when Array.IndexOf(DeepSeekAposentados, m) >= 0 =>
                $"O modelo '{m}' foi aposentado pelo DeepSeek em 24/07/2026; use deepseek-flash ou deepseek-v4-pro.",

            _ => null
        };
    }

    public static string? Aviso(string provedor, string modelo)
    {
        var m = modelo.Trim();
        switch (provedor)
        {
            case AiProviderCodes.Groq:
                if (Validar(provedor, m) is not null) return null; // erro já tratado
                return GroqCatalogo.Any(p => m.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    ? null
                    : $"O modelo '{m}' não consta do catálogo self-service documentado da Groq "
                      + "(prefixos conhecidos: gpt-oss-, llama-). Se estiver disponível no seu plano, a chamada pode funcionar.";

            case AiProviderCodes.Gemini:
                if (Validar(provedor, m) is not null) return null;
                return GeminiFamílias.Any(p => m.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    ? null
                    : $"O modelo '{m}' não consta das famílias Gemini documentadas "
                      + "(gemini-1.5-*, gemini-2.0-*, gemini-2.5-*). Confirme a disponibilidade antes de habilitar.";

            case AiProviderCodes.DeepSeek:
                if (Validar(provedor, m) is not null) return null;
                return DeepSeekCatalogo.Contains(m, StringComparer.OrdinalIgnoreCase)
                    ? null
                    : $"O modelo '{m}' não consta do catálogo DeepSeek documentado "
                      + "(deepseek-flash, deepseek-v4-pro). Confirme a disponibilidade antes de habilitar.";

            default:
                return null;
        }
    }
}
