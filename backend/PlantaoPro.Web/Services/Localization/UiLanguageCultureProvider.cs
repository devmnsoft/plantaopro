using Microsoft.AspNetCore.Localization;
using PlantaoPro.CrossCutting.Localization;

namespace PlantaoPro.Web.Services.Localization;

/// <summary>
/// R7-E1 — Provedor de cultura por request da interface Web.
/// Resolve o idioma na precedência canônica USUÁRIO &gt; ORGANIZAÇÃO &gt; navegador &gt; pt-BR,
/// lendo das claims da sessão (emitidas no login/refresh pelo SessionClaimsBuilder — as únicas
/// fontes persistentes visíveis ao Web, que não tem acesso direto ao banco) e do header
/// Accept-Language quando nenhuma fonte persistente definiu algo.
///
/// INDEPENDÊNCIA DE FORMATAÇÃO: a cultura de formatação (CurrentCulture → moeda/data usados nas
/// views e no binding) permanece SEMPRE em pt-BR (<see cref="UiLanguageCatalog.FormatCultureName"/>);
/// apenas a UICulture varia. Assim trocar o idioma nunca muda valores monetários, datas exibidas
/// nem qualquer dado clínico/fiscal — só os rótulos da própria interface (recursos localizados).
/// </summary>
public sealed class UiLanguageCultureProvider : IRequestCultureProvider
{
    public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var ui = UiLanguageCatalog.Resolve(
            httpContext.User?.FindFirst("idioma")?.Value,
            httpContext.User?.FindFirst("org_idioma")?.Value,
            httpContext.Request.Headers.AcceptLanguage.ToString());
        // No .NET 10 o resultado é (culture, uiCulture) direto; a RequestCulture equivalente
        // seria (FormatCultureName, ui) — formatação sempre pt-BR, só a UI varia.
        return Task.FromResult<ProviderCultureResult?>(new(UiLanguageCatalog.FormatCultureName, ui));
    }
}
