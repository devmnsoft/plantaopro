using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R34 (diretriz r24a1, epic E1 varredura final): o último jargão técnico renderizado vivia nos
/// VALORES dos UiTexts.*.resx (zonas Clinical/Saúde 360, Onboarding e parte do Adm360, em 4 culturas)
/// e em dois alertas da zona Observabilidade ("Use o endpoint /api/observabilidade/..."). A reescrita
/// manteve o sentido e tirou o vocabulário técnico (API, tenant, outbox, UUID, GUID, payload,
/// server-side, POST/antiforgery, logs). KEEP documentado no r34a0: as 6 chaves Adm360.RelFin.Ind*
/// (definições de indicadores fiéis ao SQL, família R8-B27) e o "tenant" do AdminSaas (cockpit global
/// MNSOFT, D-R24-3; revisitado no E2). Este fato é anti-regressão POR TOKEN: nenhuma das 27 chaves
/// pode voltar a conter um termo da lista em qualquer cultura (não trava redação exata, só o domínio
/// vocabular). Chaves não mudaram (R7DF6 continua com 653 Adm360).
/// </summary>
public class R34LinguagemSimplesResxContratoTests
{
    private static readonly string[] Chaves =
    {
        "Clinical.PhWhenToUse", "Clinical.PhTipTenant", "Clinical.NextBody", "Clinical.PatientBlockBody",
        "Clinical.EmptyBody", "Clinical.KpiUnavailableHint", "Clinical.KpiProtectedHint", "Clinical.LgpdBody",
        "Clinical.FormHelpDescription", "Clinical.FormHelpStep2", "Clinical.FormHelpTip2", "Clinical.FormSubline",
        "Clinical.TriageReferDisabledTitle",
        "Onboarding.NoClientBody", "Onboarding.CompanySectionBody", "Onboarding.HelpCnpj",
        "Onboarding.HelpInitialStatus", "Onboarding.AdminSectionBody",
        "Adm360.Cotacoes.GuideStep1Body", "Adm360.Cotacoes.GuideStep4Title", "Adm360.Dash.CapabilitiesTitle",
        "Adm360.Dash.CapabilitiesIsolationBadge", "Adm360.Dash.CapQuotePortalsDesc", "Adm360.Dash.KpiFailuresLast24hDesc",
        "Adm360.Org.Subheading", "Adm360.CotDet.LblIdentificadorManual", "Adm360.CotDet.OutboxTitulo",
    };

    private static readonly Regex Jargao = new(
        @"(?i)\b(API|BFF|endpoint|endpoints|UUID|GUID|tenant|tenants|outbox|payload|JSON|HTTP|POST|antiforgery|server|logs|log|logs|inquilino)\b|points d'acc?s");

    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    private static string ExtrairValor(string resx, string chave)
    {
        var m = Regex.Match(resx, "<data name=\"" + chave + "\"[^>]*>\\s*<value>(.*?)</value>", RegexOptions.Singleline);
        Assert.True(m.Success, "chave " + chave + " nao encontrada no resx");
        return m.Groups[1].Value;
    }

    [Fact]
    public void ValoresResxSemJargaoTecnicas_NasQuatroCulturas()
    {
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = ReadRelative("backend", "PlantaoPro.Web", "Localization", arquivo);
            foreach (var chave in Chaves)
            {
                var valor = ExtrairValor(resx, chave);
                Assert.False(string.IsNullOrWhiteSpace(valor), arquivo + "/" + chave + " vazio");
                var m = Jargao.Match(valor);
                Assert.False(m.Success, arquivo + "/" + chave + " voltou a usar jargao tecnico '" + m.Value + "': " + valor);
            }
        }
    }

    [Fact]
    public void Observabilidade_SemEndpointCruNoAlerta()
    {
        var erros = ReadRelative("backend", "PlantaoPro.Web", "Views", "Observabilidade", "Erros.cshtml");
        var perf = ReadRelative("backend", "PlantaoPro.Web", "Views", "Observabilidade", "Performance.cshtml");

        Assert.DoesNotContain("/api/observabilidade/", erros, StringComparison.Ordinal);
        Assert.DoesNotContain("Use o endpoint", erros, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/observabilidade/", perf, StringComparison.Ordinal);
        Assert.DoesNotContain("Use o endpoint", perf, StringComparison.Ordinal);
        Assert.DoesNotContain("Endpoints com maior tempo", perf, StringComparison.Ordinal);
    }
}
