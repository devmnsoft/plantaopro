using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

/// <summary>
/// Resolve o trilho de navegacao (breadcrumb) de uma pagina a partir da rota e do catalogo,
/// sem acoplar a view ao controller/acao. A view apenas aporta dados opcionais
/// (rotulo curto e/ou folha dinamica); a estrutura (raiz + area) vem daqui.
/// </summary>
public interface IBreadcrumbService
{
    IReadOnlyList<BreadcrumbViewModel> ResolveTrail(
        string controller,
        string action,
        string? pageTitle = null,
        string? breadcrumbLabel = null,
        string? dynamicLeaf = null);
}

public sealed class BreadcrumbService : IBreadcrumbService
{
    private const string HomeUrl = "/Home/Dashboard";

    // Prefixo de area por controller para rotas fora dos 10 recursos canonicos.
    // Mantido propositalmente enxuto e legivel; nao expoe o nome bruto do controller.
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AreaPrefixes =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Administrativo360"] = new[] { "Administrativo 360" },
            ["CentralEscala"] = new[] { "Central de Cobertura" },
            ["V112Web"] = new[] { "Homologação" },
        };

    private readonly IFeatureCatalogService catalog;

    public BreadcrumbService(IFeatureCatalogService catalog) => this.catalog = catalog;

    public IReadOnlyList<BreadcrumbViewModel> ResolveTrail(
        string controller,
        string action,
        string? pageTitle = null,
        string? breadcrumbLabel = null,
        string? dynamicLeaf = null)
    {
        var featurePage = catalog.FindPage(controller, action);
        if (featurePage is not null)
        {
            // Trilha rica do catalogo (ex.: Inicio / Atendimento / Pacientes).
            // Apenas a raiz recebe link; o ultimo item e a pagina atual (marcado no renderer).
            return featurePage.Breadcrumb
                .Select((label, index) => new BreadcrumbViewModel(label, index == 0 ? HomeUrl : null))
                .ToList();
        }

        var trail = new List<BreadcrumbViewModel> { new("Início", HomeUrl) };
        var prefix = AreaPrefixes.TryGetValue(controller, out var areaPrefix) ? areaPrefix : Array.Empty<string>();

        foreach (var segment in prefix)
        {
            trail.Add(new BreadcrumbViewModel(segment, null));
        }

        var current = FirstNonBlank(dynamicLeaf, breadcrumbLabel, pageTitle) ?? Humanize(action);
        if (!string.Equals(prefix.LastOrDefault(), current, StringComparison.OrdinalIgnoreCase))
        {
            trail.Add(new BreadcrumbViewModel(current, null));
        }

        return trail;
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string Humanize(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return "Página";
        }

        var chars = new List<char>(action.Length + 8);
        for (var i = 0; i < action.Length; i++)
        {
            var c = action[i];
            if (i > 0 && char.IsUpper(c) && char.IsLower(action[i - 1]))
            {
                chars.Add(' ');
            }

            chars.Add(c);
        }

        return new string(chars.ToArray());
    }
}
