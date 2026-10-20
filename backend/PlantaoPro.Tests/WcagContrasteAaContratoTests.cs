using System.Globalization;
using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

/// <summary>
/// Contrato de contraste WCAG AA (R69): trava o piso de 4,5:1 para texto sobre superficies
/// claras do design system e de 3:1 para indicadores nao-texto, medindo os pares efetivos —
/// tokens canonicos (tokens.css) e paleta funcional do Bootstrap 5.3 ajustada na camada
/// v2151-wp-c-ajuste-canonical.css (carregada por ultimo em _Layout.cshtml). A auditoria
/// empirica completa (badges, alertas, tabelas, overlays e estilos inline de views) vive em
/// docs/evidencias/&lt;data&gt;-rodada-69/r69_audit_contraste.ps1.
/// </summary>
public class WcagContrasteAaContratoTests
{
    // Ordem efetiva da cascata para as custom properties consumidas por este contrato:
    // vendor Bootstrap -> compat -> tokens (via @import de plantaopro.css) -> v2150 -> v2151.
    private static readonly string[][] FontesCascatas =
    {
        new[] { "backend", "PlantaoPro.Web", "wwwroot", "css", "vendor", "bootstrap.min.css" },
        new[] { "backend", "PlantaoPro.Web", "wwwroot", "css", "vendor", "bootstrap-compat.css" },
        new[] { "backend", "PlantaoPro.Web", "wwwroot", "css", "design-system", "tokens.css" },
        new[] { "backend", "PlantaoPro.Web", "wwwroot", "css", "design-system", "v2150-bloco-c-canonical.css" },
        new[] { "backend", "PlantaoPro.Web", "wwwroot", "css", "design-system", "v2151-wp-c-ajuste-canonical.css" },
    };

    private static readonly Lazy<Dictionary<string, string>> TokenMap = new(BuildTokenMap);

    [Fact]
    public void TextosDoSistema_x_SuperficiesClaras_AtenAa()
    {
        var falhas = new List<string>();
        foreach (var tinta in new[] { "--pp-text-primary", "--pp-text-secondary", "--pp-text-muted", "--pp-text-soft" })
        {
            Verificar(falhas, "tinta " + tinta, tinta, "#ffffff", 4.5);
            Verificar(falhas, "tinta " + tinta + " sobre pagina", tinta, "--pp-page-background", 4.5);
        }
        Verificar(falhas, "texto inverso sobre superficie inversa", "--pp-text-inverse", "--pp-surface-inverse", 4.5);
        Assert.True(falhas.Count == 0, "Contraste AA violado:\n" + string.Join("\n", falhas.Take(25)));
    }

    [Fact]
    public void SemanticaFuncional_TokensSuaveESolidos_AtenAa()
    {
        var falhas = new List<string>();
        foreach (var estado in new[] { "success", "warning", "danger", "info" })
        {
            Verificar(falhas, "texto " + estado + " sobre suave", $"--pp-{estado}", $"--pp-{estado}-soft", 4.5);
            Verificar(falhas, "texto " + estado + " sobre branco", $"--pp-{estado}", "#ffffff", 4.5);
            Verificar(falhas, "branco sobre solido " + estado, "#ffffff", $"--pp-{estado}", 4.5);
        }
        Verificar(falhas, "branco sobre azul institucional", "#ffffff", "--pp-brand-primary", 4.5);
        Assert.True(falhas.Count == 0, "Contraste AA violado:\n" + string.Join("\n", falhas.Take(25)));
    }

    [Fact]
    public void Bootstrap_BadgesETextBg_BrancoSobreSolidosFuncionais_AtenAa()
    {
        var falhas = new List<string>();
        foreach (var nome in new[] { "primary", "success", "warning", "info", "danger", "secondary" })
        {
            Verificar(falhas, ".badge/.text-bg " + nome, "#ffffff", $"--bs-{nome}-rgb", 4.5);
        }
        Assert.True(falhas.Count == 0, "Contraste AA violado:\n" + string.Join("\n", falhas.Take(25)));
    }

    [Fact]
    public void Bootstrap_UtilitariosDeTexto_e_Links_AtenAa()
    {
        var falhas = new List<string>();
        foreach (var nome in new[] { "primary", "success", "warning", "info", "danger", "secondary" })
        {
            Verificar(falhas, ".text-" + nome + " sobre branco", $"--bs-{nome}-rgb", "#ffffff", 4.5);
            Verificar(falhas, ".text-" + nome + " sobre pagina", $"--bs-{nome}-rgb", "--pp-page-background", 4.5);
        }
        Verificar(falhas, "link primario sobre branco", "--bs-link-color", "#ffffff", 4.5);
        Assert.True(falhas.Count == 0, "Contraste AA violado:\n" + string.Join("\n", falhas.Take(25)));
    }

    [Fact]
    public void Bootstrap_BotoesContorno_TintaEfetiva_AtenAa()
    {
        var falhas = new List<string>();
        var textoVendor = RepositoryPathResolver.ReadRepositoryFile(FontesCascatas[0]);
        var textoV2151 = RepositoryPathResolver.ReadRepositoryFile(FontesCascatas[4]);
        foreach (var nome in new[] { "primary", "success", "warning", "info", "danger", "secondary" })
        {
            var tinta = null
                ?? ExtrairTintaContorno(textoV2151, nome)
                ?? ExtrairTintaContorno(textoVendor, nome);
            if (tinta is null) { falhas.Add("botao contorno " + nome + ": tinta nao localizada"); continue; }
            Verificar(falhas, "botao contorno " + nome, tinta, "#ffffff", 4.5);
        }
        Assert.True(falhas.Count == 0, "Contraste AA violado:\n" + string.Join("\n", falhas.Take(25)));
    }

    [Fact]
    public void IndicadoresNaoTexto_e_Foco_Aten31()
    {
        var falhas = new List<string>();
        foreach (var acento in new[] { "--pp-clinical-teal", "--pp-clinical-cyan", "--pp-clinical-green", "--pp-clinical-amber", "--pp-color-accent" })
        {
            Verificar(falhas, "acento " + acento + " (nao-texto)", "#ffffff", acento, 3.0);
        }
        Verificar(falhas, "anel de foco sobre pagina (nao-texto)", "--pp-focus-ring", "--pp-page-background", 3.0);
        Assert.True(falhas.Count == 0, "Contraste nao-texto violado:\n" + string.Join("\n", falhas.Take(25)));
    }

    private static void Verificar(List<string> falhas, string rotulo, string fg, string bg, double minimo)
    {
        var c1 = Resolve(fg);
        var c2 = Resolve(bg);
        if (c1 is null || c2 is null)
        {
            falhas.Add($"{rotulo}: valor nao resolvido ({fg} x {bg})");
            return;
        }
        var razao = RazaoContraste(c1.Value, c2.Value);
        if (razao + 1e-9 < minimo)
            falhas.Add($"{rotulo}: {fg} sobre {bg} = {razao.ToString("0.00", CultureInfo.InvariantCulture)}:1 (minimo {minimo.ToString("0.#", CultureInfo.InvariantCulture)}:1)");
    }

    private static Dictionary<string, string> BuildTokenMap()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var partes in FontesCascatas)
        {
            var texto = RepositoryPathResolver.ReadRepositoryFile(partes);
            foreach (var bloco in ExtrairBlocosRoot(texto))
            {
                foreach (Match m in Regex.Matches(bloco, @"(--[a-z0-9-]+)\s*:\s*([^;{}]+)"))
                    map[m.Groups[1].Value] = m.Groups[2].Value.Trim();
            }
        }
        return map;
    }

    private static IEnumerable<string> ExtrairBlocosRoot(string css)
    {
        foreach (Match m in Regex.Matches(css, ":root\\s*\\{"))
        {
            var inicio = m.Index + m.Length - 1;
            var profundidade = 1;
            var i = inicio + 1;
            while (i < css.Length && profundidade > 0)
            {
                if (css[i] == '{') profundidade++;
                else if (css[i] == '}') profundidade--;
                i++;
            }
            yield return css.Substring(inicio + 1, i - inicio - 2);
        }
    }

    private static string? ExtrairTintaContorno(string css, string nome)
    {
        var m = Regex.Match(css, $@"\.btn-outline-{nome}\s*\{{[^{{}}]*?--bs-btn-color:\s*(#[0-9a-fA-F]{{3,8}}|var\(--[a-z0-9-]+\))");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static (int R, int G, int B)? Resolve(string spec)
    {
        var v = (spec ?? "").Trim();
        if (v.EndsWith("!important", StringComparison.Ordinal))
            v = v[..^"!important".Length].Trim();
        if (v.StartsWith("--"))
            return TokenMap.Value.TryGetValue(v, out var direta) ? Resolve(direta) : null;
        if (v.StartsWith("var(", StringComparison.Ordinal) && v.EndsWith(")"))
        {
            var crase = v.IndexOf(',', 4);
            var interno = (crase < 0 ? v.Substring(4, v.Length - 5) : v.Substring(4, crase - 4)).Trim();
            return TokenMap.Value.TryGetValue(interno, out var def) ? Resolve(def) : null;
        }
        if (v.StartsWith('#'))
        {
            var h = v[1..];
            if (h.Length == 3) h = $"{h[0]}{h[0]}{h[1]}{h[1]}{h[2]}{h[2]}";
            if (h.Length < 6) return null;
            if (int.TryParse(h.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
                && int.TryParse(h.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
                && int.TryParse(h.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                return (r, g, b);
            return null;
        }
        var canais = v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (canais.Length == 3
            && canais.All(c => int.TryParse(c, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
        {
            return (int.Parse(canais[0], CultureInfo.InvariantCulture),
                    int.Parse(canais[1], CultureInfo.InvariantCulture),
                    int.Parse(canais[2], CultureInfo.InvariantCulture));
        }
        return null;
    }

    private static double LuzRelativa((int R, int G, int B) c)
    {
        return 0.2126 * Canal(c.R) + 0.7152 * Canal(c.G) + 0.0722 * Canal(c.B);
        double Canal(int v)
        {
            var s = v / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
    }

    private static double RazaoContraste((int R, int G, int B) a, (int R, int G, int B) b)
    {
        var la = LuzRelativa(a);
        var lb = LuzRelativa(b);
        var hi = Math.Max(la, lb);
        var lo = Math.Min(la, lb);
        return (hi + 0.05) / (lo + 0.05);
    }
}
