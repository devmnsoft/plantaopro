namespace PlantaoPro.CrossCutting.Localization;

/// <summary>
/// R7-E1 — Catálogo CANÔNICO de idiomas da interface PlantãoPro: pt-BR (padrão), en, es, fr.
/// Fonte única de verdade compartilhada pela API (validação da preferência persistida) e pela
/// Web (provação e resolução por request).
///
/// PRECEDÊNCIA de resolução: preferência do USUÁRIO &gt; padrão da ORGANIZAÇÃO &gt;
/// navegador (Accept-Language, com q-values) &gt; pt-BR.
///
/// INDEPENDÊNCIA de formatação: o idioma da interface NÃO altera moeda, fuso horário nem
/// documentos fiscais. A CurrentCulture (formatos de moeda/data usados nas views e no binding)
/// permanece fixada em pt-BR (<see cref="FormatCultureName"/>); apenas a UICulture (recursos
/// localizados) varia com o idioma escolhido. Conteúdo clínico, dados do usuário e XMLs fiscais
/// NUNCA são traduzidos automaticamente — apenas as rótulos/telas da própria interface.
/// </summary>
public static class UiLanguageCatalog
{
    public const string Default = "pt-BR";

    /// <summary>Cultura fixa de FORMATAÇÃO (moeda/data/fiscal). Nunca muda com o idioma da UI.</summary>
    public const string FormatCultureName = "pt-BR";

    public static readonly IReadOnlyList<string> Supported = new[] { "pt-BR", "en", "es", "fr" };

    /// <summary>Validação estrita: apenas os 4 códigos canônicos do catálogo.</summary>
    public static bool IsSupported(string? value) => Normalize(value) != null;

    /// <summary>
    /// Aceita variantes regionais/case (EN-US, FR-FR, PT, es-mx...) e devolve o nome canônico
    /// do catálogo, ou null quando o código não faz parte dos 4 idiomas suportados.
    /// </summary>
    public static string? Normalize(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length < 2) return null;
        var baseCode = v.Split('-', '/').First().ToLowerInvariant();
        return baseCode switch
        {
            "pt" => "pt-BR",
            "en" => "en",
            "es" => "es",
            "fr" => "fr",
            _ => null
        };
    }

    /// <summary>
    /// Resolve o idioma da interface na precedência canônica:
    /// usuário &gt; organização &gt; navegador (Accept-Language) &gt; pt-BR.
    /// Cada fonte é normalizada via <see cref="Normalize"/>: valores desconhecidos são
    /// ignorados (caem para a próxima fonte), nunca viram erro.
    /// </summary>
    public static string Resolve(string? userIdioma, string? orgIdioma, string? acceptLanguage)
    {
        var porUsuario = Normalize(userIdioma);
        if (porUsuario != null) return porUsuario;

        var porOrganizacao = Normalize(orgIdioma);
        if (porOrganizacao != null) return porOrganizacao;

        return ParseAcceptLanguage(acceptLanguage) ?? Default;
    }

    /// <summary>
    /// Lê um header Accept-Language seguindo a ordem preferencial declarada pelo navegador
    /// (itens separados por vírgula, q-qualidade opcional; q ausente = 1). Retorna o primeiro
    /// código suportado com q &gt; 0 na ordem dada, ou null quando nenhum item é suportado.
    /// </summary>
    public static string? ParseAcceptLanguage(string? header)
    {
        var raw = (header ?? string.Empty).Trim();
        if (raw.Length == 0) return null;

        var candidatos = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item =>
            {
                var partes = item.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var codigo = partes[0];
                var q = 1.0;
                foreach (var parte in partes.Skip(1))
                {
                    if (parte.StartsWith("q=", StringComparison.OrdinalIgnoreCase) && double.TryParse(parte[2..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var valor))
                        q = valor;
                }
                return (Codigo: Normalize(codigo), Q: q);
            })
            .Where(c => c.Codigo != null && c.Q > 0)
            // Estável: ordem do header preservada em caso de empate de q.
            .OrderByDescending(c => c.Q)
            .ToList();

        return candidatos.Count == 0 ? null : candidatos[0].Codigo!;
    }
}
