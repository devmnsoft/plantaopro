using System.Globalization;
using System.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using PlantaoPro.CrossCutting.Localization;

namespace PlantaoPro.Web.Localization;

/// <summary>
/// R7-E1 — Acesso CANÔNICO aos textos da interface (recursos localizados UiTexts).
/// A cadeia de fallback do ResourceManager já garante: cultura ativa (en/es/fr) → recurso
/// neutro (arquivo-mestre com os textos pt-BR). Assim NENHUMA chave chega vazia ao usuário:
/// toda chave do catálogo existe em todos os 4 arquivos (garantido por teste de contrato) e
/// o último recurso só devolve a própria chave em cenário inalcançável (chave inexistente no
/// master — erro de desenvolvimento, não de operação). Conteúdo clínico, dados do usuário e
/// XMLs fiscais NÃO passam por aqui — apenas rótulos/ajudas da interface.
/// </summary>
public static class UiText
{
    private static readonly ResourceManager Resources = new("PlantaoPro.Web.Localization.UiTexts", typeof(UiText).Assembly);

    /// <summary>Texto da chave na UICulture ativa com fallback garantido para pt-BR.</summary>
    public static string L(string key) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    /// <summary>Código HTML (ex.: lang="...") do idioma ativo do request.</summary>
    public static string HtmlLang(HttpContext context) =>
        // No .NET 10 o middleware grava o feature com a CHAVE da interface IRequestCultureFeature
        // (não do tipo concreto RequestCultureFeature) — ler pelo concreto devolve null sempre.
        context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name ?? UiLanguageCatalog.Default;
}
