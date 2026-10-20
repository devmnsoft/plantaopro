using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R74 - Guardas de design: (1) trava o mapa completo de tokens canonicos - todas as custom
/// properties declaradas em :root nas camadas tokens.css, v2150-bloco-c-canonical.css e
/// v2151-wp-c-ajuste-canonical.css (ordem da cascata documentada em WcagContrasteAaContratoTests);
/// qualquer mudanca de nome ou valor exige atualizar este contrato e registrar a evidencia da
/// rodada. (2)-(4) varreduras permanentes de anti-padroes: pares literais cor+fundo abaixo de AA
/// em CSS fora dos tokens, pares conhecidos em estilos inline de views e cor de funcao sem
/// texto/simbolo em views. Scanner de referencia (mesmos criterios) e saida da execucao:
/// docs/evidencias/&lt;data&gt;-rodada-74/r74_scan.ps1 / r74_gap_scan.txt. Fora de escopo
/// declarado pelos fatos de contraste: fundo translucido ou em gradiente (a composicao efetiva
/// depende da superficie por baixo) e valores indiretos via var() fora das camadas canonicas.
/// </summary>
public sealed class R74GuardasDesignContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRepositoryRoot();
    private static string Read(string relativo) => File.ReadAllText(Path.Combine(Root, relativo.Replace('/', Path.DirectorySeparatorChar)));

    private const string CssDir = "backend/PlantaoPro.Web/wwwroot/css";
    private const string ViewsDir = "backend/PlantaoPro.Web/Views";

    // ------------------------------------------------------- 1) mapa de tokens travado

    private static readonly (string Arquivo, int Qtd, (string Token, string Valor)[] Pares)[] MapaTravado =
        new (string Arquivo, int Qtd, (string Token, string Valor)[] Pares)[]
    {
        (
            "design-system/tokens.css",
            170,
            new (string Token, string Valor)[]
        {
            ("--pp-amber-500", "#f59e0b"),
            ("--pp-bg", "#f4f7fb"),
            ("--pp-bg-app", "var(--pp-page-background)"),
            ("--pp-bg-body", "#f3f7fb"),
            ("--pp-bg-elevated", "var(--pp-surface-elevated)"),
            ("--pp-bg-subtle", "var(--pp-surface-subtle)"),
            ("--pp-bg-surface", "var(--pp-surface-default)"),
            ("--pp-blue-600", "#2563eb"),
            ("--pp-border", "var(--pp-border-default)"),
            ("--pp-border-default", "#ccd7e5"),
            ("--pp-border-soft", "#dce6f1"),
            ("--pp-border-soft-v154", "#e5edf5"),
            ("--pp-border-strong", "#91a5bf"),
            ("--pp-border-subtle", "#e5ebf3"),
            ("--pp-border-width", "1px"),
            ("--pp-brand-dark", "#0b2a52"),
            ("--pp-brand-dark-hover", "#071f3e"),
            ("--pp-brand-ink", "#0e1d33"),
            ("--pp-brand-light", "#eaf4ff"),
            ("--pp-brand-muted", "#51698d"),
            ("--pp-brand-primary", "#1f73f1"),
            ("--pp-brand-primary-active", "#124b9f"),
            ("--pp-brand-primary-hover", "#185fc8"),
            ("--pp-brand-primary-soft", "#eaf4ff"),
            ("--pp-clinical-amber", "#d97706"),
            ("--pp-clinical-bg", "#f4f8fb"),
            ("--pp-clinical-blue", "#2563eb"),
            ("--pp-clinical-border", "#dbe5ef"),
            ("--pp-clinical-cyan", "#0891b2"),
            ("--pp-clinical-green", "#16a34a"),
            ("--pp-clinical-muted", "#475569"),
            ("--pp-clinical-navy", "#071a2f"),
            ("--pp-clinical-radius", "16px"),
            ("--pp-clinical-red", "#dc2626"),
            ("--pp-clinical-shadow", "0 12px 32px rgb(15 23 42 / .08)"),
            ("--pp-clinical-surface", "#ffffff"),
            ("--pp-clinical-surface-muted", "#f8fbfd"),
            ("--pp-clinical-teal", "#0d9488"),
            ("--pp-clinical-text", "#0f172a"),
            ("--pp-color-accent", "#0891a6"),
            ("--pp-color-amber-500", "#c87800"),
            ("--pp-color-blue-600", "#1768d7"),
            ("--pp-color-cyan-500", "#14a9bd"),
            ("--pp-color-danger", "#b42336"),
            ("--pp-color-green-600", "#087a4d"),
            ("--pp-color-navy-900", "#0b2747"),
            ("--pp-color-navy-950", "#06172c"),
            ("--pp-color-primary", "#165dba"),
            ("--pp-color-primary-dark", "#0a315f"),
            ("--pp-color-red-600", "#b42336"),
            ("--pp-color-success", "#087a4d"),
            ("--pp-color-teal-500", "#0d9488"),
            ("--pp-color-warning", "#a15c00"),
            ("--pp-content-max", "100rem"),
            ("--pp-cyan-500", "#06b6d4"),
            ("--pp-danger", "#b42336"),
            ("--pp-danger-soft", "#ffeaed"),
            ("--pp-density", "1"),
            ("--pp-disabled-opacity", ".58"),
            ("--pp-focus", "var(--pp-focus-ring)"),
            ("--pp-focus-ring", "var(--pp-brand-primary)"),
            ("--pp-focus-shadow", "0 0 0 .2rem color-mix(in srgb, var(--pp-brand-primary) 30%, transparent)"),
            ("--pp-font-body", "1rem"),
            ("--pp-font-body-small", ".875rem"),
            ("--pp-font-caption", ".75rem"),
            ("--pp-font-card-title", "1.0625rem"),
            ("--pp-font-display", "clamp(2rem, 4vw, 3.5rem)"),
            ("--pp-font-helper", ".8125rem"),
            ("--pp-font-label", ".875rem"),
            ("--pp-font-page-title", "clamp(1.65rem, 2.5vw, 2.25rem)"),
            ("--pp-font-sans", "Inter, ui-sans-serif, system-ui, -apple-system, \"Segoe UI\", sans-serif"),
            ("--pp-font-section-title", "clamp(1.25rem, 1.5vw, 1.5rem)"),
            ("--pp-font-size-base", "1rem"),
            ("--pp-font-size-sm", ".875rem"),
            ("--pp-green-600", "#16a34a"),
            ("--pp-hover-bg", "#edf4fc"),
            ("--pp-info", "#245ea8"),
            ("--pp-info-soft", "#e8f2ff"),
            ("--pp-line-body", "1.55"),
            ("--pp-line-height", "1.55"),
            ("--pp-line-tight", "1.2"),
            ("--pp-muted", "var(--pp-text-muted)"),
            ("--pp-navy-900", "#0b243f"),
            ("--pp-navy-950", "#06172b"),
            ("--pp-page-background", "#f5f8fc"),
            ("--pp-primary", "var(--pp-brand-primary)"),
            ("--pp-primary-hover", "var(--pp-brand-primary-hover)"),
            ("--pp-primary-soft", "var(--pp-brand-primary-soft)"),
            ("--pp-radius", "var(--pp-radius-md)"),
            ("--pp-radius-card", "1.125rem"),
            ("--pp-radius-lg", "1rem"),
            ("--pp-radius-md", ".75rem"),
            ("--pp-radius-panel", "1.5rem"),
            ("--pp-radius-pill", "999px"),
            ("--pp-radius-sm", ".5rem"),
            ("--pp-radius-xl", "1.375rem"),
            ("--pp-radius-xs", ".375rem"),
            ("--pp-rail-width", "4.75rem"),
            ("--pp-reading-width", "68ch"),
            ("--pp-red-600", "#dc2626"),
            ("--pp-shadow", "var(--pp-shadow-sm)"),
            ("--pp-shadow-card", "0 8px 28px rgb(6 23 44 / .08)"),
            ("--pp-shadow-floating", "0 20px 54px rgb(6 23 44 / .16)"),
            ("--pp-shadow-lg", "0 24px 64px rgb(8 32 61 / .16)"),
            ("--pp-shadow-md", "0 16px 40px rgb(14 29 51 / .12)"),
            ("--pp-shadow-panel", "0 18px 50px rgb(15 23 42 / .12)"),
            ("--pp-shadow-shell", "0 14px 42px rgb(6 23 44 / .12)"),
            ("--pp-shadow-sm", "0 6px 20px rgb(14 29 51 / .08)"),
            ("--pp-shadow-xs", "0 1px 2px rgb(14 29 51 / .06)"),
            ("--pp-sidebar-active", "var(--pp-brand-primary)"),
            ("--pp-sidebar-bg", "var(--pp-brand-dark)"),
            ("--pp-sidebar-bg-secondary", "var(--pp-brand-dark-hover)"),
            ("--pp-sidebar-collapsed-width", "var(--pp-rail-width)"),
            ("--pp-sidebar-max", "17rem"),
            ("--pp-sidebar-min", "14rem"),
            ("--pp-sidebar-muted", "#c6d7eb"),
            ("--pp-sidebar-text", "var(--pp-text-inverse)"),
            ("--pp-sidebar-width", "var(--pp-sidebar-max)"),
            ("--pp-sidebar-width-v154", "280px"),
            ("--pp-space-1", ".25rem"),
            ("--pp-space-10", "2.5rem"),
            ("--pp-space-12", "3rem"),
            ("--pp-space-2", ".5rem"),
            ("--pp-space-3", ".75rem"),
            ("--pp-space-4", "1rem"),
            ("--pp-space-5", "1.25rem"),
            ("--pp-space-6", "1.5rem"),
            ("--pp-space-8", "2rem"),
            ("--pp-success", "#087a4d"),
            ("--pp-success-soft", "#e8f7f0"),
            ("--pp-surface", "#ffffff"),
            ("--pp-surface-canvas", "#f3f7fb"),
            ("--pp-surface-card", "#ffffff"),
            ("--pp-surface-dark", "#071f3a"),
            ("--pp-surface-default", "#ffffff"),
            ("--pp-surface-elevated", "#ffffff"),
            ("--pp-surface-glass", "rgb(255 255 255 / .86)"),
            ("--pp-surface-inverse", "#0b2a52"),
            ("--pp-surface-muted", "#f8fafc"),
            ("--pp-surface-page", "var(--pp-page-background)"),
            ("--pp-surface-primary", "var(--pp-surface-default)"),
            ("--pp-surface-raised", "#ffffff"),
            ("--pp-surface-secondary", "var(--pp-surface-subtle)"),
            ("--pp-surface-subtle", "#f8fafd"),
            ("--pp-teal-500", "#14b8a6"),
            ("--pp-text", "#0f172a"),
            ("--pp-text-inverse", "#ffffff"),
            ("--pp-text-main", "#10233d"),
            ("--pp-text-muted", "#51698d"),
            ("--pp-text-primary", "#0e1d33"),
            ("--pp-text-secondary", "#405a80"),
            ("--pp-text-soft", "#5c6c81"),
            ("--pp-topbar-height", "4.5rem"),
            ("--pp-topbar-height-v154", "84px"),
            ("--pp-touch-target", "2.75rem"),
            ("--pp-transition", "220ms cubic-bezier(.2,.8,.2,1)"),
            ("--pp-transition-fast", "140ms ease"),
            ("--pp-warning", "#805000"),
            ("--pp-warning-soft", "#fff4d8"),
            ("--pp-z-base", "0"),
            ("--pp-z-drawer", "1070"),
            ("--pp-z-dropdown", "1030"),
            ("--pp-z-modal", "1080"),
            ("--pp-z-overlay", "1060"),
            ("--pp-z-sidebar", "1040"),
            ("--pp-z-skip", "1100"),
            ("--pp-z-sticky", "1010"),
            ("--pp-z-toast", "1090"),
            ("--pp-z-topbar", "1020"),
            ("--pp-z-tour", "1110"),
            }),
        (
            "design-system/v2150-bloco-c-canonical.css",
            21,
            new (string Token, string Valor)[]
        {
            ("--bs-link-color", "var(--pp-brand-primary)"),
            ("--bs-link-color-rgb", "31, 115, 241"),
            ("--bs-link-hover-color", "var(--pp-brand-primary-hover)"),
            ("--bs-link-hover-color-rgb", "26, 99, 216"),
            ("--bs-primary", "var(--pp-brand-primary)"),
            ("--bs-primary-rgb", "31, 115, 241"),
            ("--pp-bg", "var(--pp-canvas-base)"),
            ("--pp-bg-app", "var(--pp-canvas-base)"),
            ("--pp-brand-primary", "#1f73f1"),
            ("--pp-brand-primary-active", "#1549ab"),
            ("--pp-brand-primary-hover", "#1a63d8"),
            ("--pp-canvas", "var(--pp-canvas-base)"),
            ("--pp-canvas-base", "#f4f8fb"),
            ("--pp-focus-ring", "var(--pp-brand-primary)"),
            ("--pp-page-background", "var(--pp-canvas-base)"),
            ("--pp-primary", "var(--pp-brand-primary)"),
            ("--pp-v1970-canvas", "var(--pp-canvas-base)"),
            ("--pp-v2116-canvas", "var(--pp-canvas-base)"),
            ("--pp-v2120-canvas", "var(--pp-canvas-base)"),
            ("--pp-v2132-canvas", "var(--pp-canvas-base)"),
            ("--pp-v2137-surface", "var(--pp-canvas-base)"),
            }),
        (
            "design-system/v2151-wp-c-ajuste-canonical.css",
            30,
            new (string Token, string Valor)[]
        {
            ("--bs-danger", "#b42336"),
            ("--bs-danger-rgb", "180, 35, 54"),
            ("--bs-info", "#0e7490"),
            ("--bs-info-rgb", "14, 116, 144"),
            ("--bs-link-color", "var(--pp-brand-primary-text)"),
            ("--bs-link-color-rgb", "26, 95, 208"),
            ("--bs-link-hover-color", "var(--pp-brand-primary-active)"),
            ("--bs-link-hover-color-rgb", "21, 73, 171"),
            ("--bs-primary", "var(--pp-brand-primary-text)"),
            ("--bs-primary-rgb", "26, 95, 208"),
            ("--bs-secondary", "#66727c"),
            ("--bs-secondary-rgb", "102, 114, 124"),
            ("--bs-success", "#087a4d"),
            ("--bs-success-rgb", "8, 122, 77"),
            ("--bs-warning", "#b45309"),
            ("--bs-warning-rgb", "180, 83, 9"),
            ("--pp-brand-primary", "#1d6fe9"),
            ("--pp-brand-primary-active", "#1549ab"),
            ("--pp-brand-primary-hover", "#1a63d8"),
            ("--pp-brand-primary-text", "#1a5fd0"),
            ("--pp-tone-danger-ink", "#b42336"),
            ("--pp-tone-danger-soft", "#ffeaed"),
            ("--pp-tone-info-ink", "#0e7490"),
            ("--pp-tone-info-soft", "#e8f2ff"),
            ("--pp-tone-secondary-ink", "#66727c"),
            ("--pp-tone-secondary-soft", "#e8eef5"),
            ("--pp-tone-success-ink", "#087a4d"),
            ("--pp-tone-success-soft", "#e8f7f0"),
            ("--pp-tone-warning-ink", "#b45309"),
            ("--pp-tone-warning-soft", "#fff4d8"),
            }),
    };

    [Fact]
    public void Mapa_de_tokens_canonico_travado()
    {
        var falhas = new List<string>();
        foreach (var (arquivo, qtd, pares) in MapaTravado)
        {
            var real = ExtrairMapaTokens(Read(CssDir + "/" + arquivo));
            if (real.Count != qtd)
            {
                falhas.Add($"{arquivo}: {qtd} tokens travados x {real.Count} no arquivo");
                continue;
            }
            foreach (var (token, valor) in pares)
            {
                if (!real.TryGetValue(token, out var v))
                {
                    falhas.Add($"{arquivo}: token ausente {token}");
                }
                else if (!string.Equals(v, valor, StringComparison.Ordinal))
                {
                    falhas.Add($"{arquivo}: {token} travado={valor} atual={v}");
                }
            }
        }
        Assert.True(falhas.Count == 0,
            "mapa de tokens mudou - atualize o contrato e registre a mudanca na evidencia da rodada:\n" +
            string.Join("\n", falhas.Take(30)));
    }

    private static Dictionary<string, string> ExtrairMapaTokens(string css)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
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
            var corpo = css.Substring(inicio + 1, Math.Max(0, i - inicio - 2));
            foreach (Match d in Regex.Matches(corpo, "(--[a-zA-Z0-9-]+)\\s*:\\s*([^;{}]+)"))
                map[d.Groups[1].Value] = d.Groups[2].Value.Trim();
        }
        return map;
    }

    // ------------------------------- 2) pares literais cor+fundo abaixo de AA em CSS

    [Fact]
    public void Css_fora_dos_tokens_sem_par_literal_cor_fundo_abaixo_da_Aa()
    {
        var falhas = new List<string>();
        foreach (var f in CaminhosCss())
        {
            var txt = File.ReadAllText(f);
            var nome = RelativoCss(f);
            var i = 0;
            while (i < txt.Length)
            {
                var ob = txt.IndexOf('{', i);
                if (ob < 0) break;
                var fe = txt.IndexOf('}', ob);
                if (fe < 0) break;
                var sel = txt.Substring(i, ob - i).Trim();
                var dec = txt.Substring(ob + 1, fe - ob - 1);
                i = fe + 1;
                if (sel.StartsWith("@", StringComparison.Ordinal)) continue;

                var cor = ExtrairDecl(dec, "^\\s*color\\s*:\\s*(.+)$");
                var fundo = ExtrairDecl(dec, "^\\s*background(?:-color)?\\s*:\\s*(.+)$");
                if (cor is null || fundo is null) continue;
                var img = fundo.Contains("url(", StringComparison.Ordinal)
                    || Regex.IsMatch(dec, "^\\s*background-image\\s*:", RegexOptions.Multiline);
                AnalisarTinta(cor, out var pulsoFg, out var rgbFg);
                AnalisarTinta(fundo, out var pulsoBg, out var rgbBg);
                if (rgbFg is null || rgbBg is null) continue;
                var pulso = pulsoFg || pulsoBg
                    || cor.Contains("gradient", StringComparison.OrdinalIgnoreCase)
                    || fundo.Contains("gradient", StringComparison.OrdinalIgnoreCase);
                var razao = RazaoContraste(rgbFg.Value, rgbBg.Value);
                if (pulso || img) continue; // composicao nao resolvida estaticamente (escopo declarado)

                var limite = 4.5;
                // indicador nao-texto (icone): piso 3:1; UI desabilitada: isenta (WCAG 1.4.3)
                if (sel.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0
                    || Regex.IsMatch(sel, ">\\s*i(?=[:\\s,{}]|$)")) limite = 3.0;
                if (sel.Contains(":disabled", StringComparison.Ordinal) || sel.Contains(".disabled", StringComparison.Ordinal)) continue;
                if (razao + 1e-9 < limite)
                    falhas.Add($"{nome}:L{LinNum(txt, ob)} {cor} sobre {fundo} = {razao.ToString("0.00", CultureInfo.InvariantCulture)}:1 [{sel}]");
            }
        }
        Assert.True(falhas.Count == 0, "par cor+fundo abaixo de AA em CSS:\n" + string.Join("\n", falhas.Take(25)));
    }

    private static string? ExtrairDecl(string dec, string propRe)
    {
        string? v = null;
        foreach (var parte in dec.Split(';'))
        {
            var m = Regex.Match(parte, propRe);
            if (m.Success) v = m.Groups[1].Value.Trim();
        }
        return v;
    }

    // ------------------------------- 3) estilos inline de views com cor+fundo conhecidos

    [Fact]
    public void Views_estilos_inline_cor_fundo_conhecidos_AtenAa()
    {
        var falhas = new List<string>();
        foreach (var f in CaminhosViews())
        {
            var nome = RelativoViews(f);
            var linhas = File.ReadAllLines(f);
            for (var i = 0; i < linhas.Length; i++)
            {
                foreach (Match st in Regex.Matches(linhas[i], "style\\s*=\\s*\"([^\"]+)\""))
                {
                    var s = st.Groups[1].Value;
                    var mc = Regex.Match(s, "(?:^|;)\\s*color\\s*:\\s*([^;]+)");
                    var mb = Regex.Match(s, "(?:^|;)\\s*background(?:-color)?\\s*:\\s*([^;]+)");
                    if (!mc.Success || !mb.Success) continue;
                    AnalisarTinta(mc.Groups[1].Value, out var pulsoFg, out var rgbFg);
                    AnalisarTinta(mb.Groups[1].Value, out var pulsoBg, out var rgbBg);
                    if (rgbFg is null || rgbBg is null) continue;
                    var pulso = pulsoFg || pulsoBg
                        || s.Contains("gradient", StringComparison.OrdinalIgnoreCase);
                    if (pulso) continue;
                    var razao = RazaoContraste(rgbFg.Value, rgbBg.Value);
                    if (razao + 1e-9 < 4.5)
                    {
                        var snip = s.Length > 120 ? s[..120] : s;
                        falhas.Add($"{nome}:L{i + 1} {snip}");
                    }
                }
            }
        }
        Assert.True(falhas.Count == 0, "estilo inline abaixo de AA em view:\n" + string.Join("\n", falhas.Take(25)));
    }

    // ------------------------------- 4) cor de funcao sem texto/simbolo em views

    [Fact]
    public void Views_sem_cor_de_funcao_sem_texto_ou_simbolo()
    {
        var falhas = new List<string>();
        var utilRe = new Regex(@"(?:^|\s)(?:bg|text|border)-(?:success|danger|warning|info)\b|text-bg-(?:success|danger|warning|info)\b");
        var tagRe = new Regex("<([a-z][a-z0-9]*)\\b([^<>]*)>");
        var classRe = new Regex("class\\s*=\\s*\"([^\"]*)\"");
        foreach (var f in CaminhosViews())
        {
            var txt = File.ReadAllText(f);
            var nome = RelativoViews(f);
            foreach (Match m in tagRe.Matches(txt))
            {
                var attrs = m.Groups[2].Value;
                if (attrs.Contains("aria-label=") || attrs.Contains("title=") || attrs.Contains('<')) continue;
                // campos de validacao ASP: a mensagem de erro chega junto com a cor
                if (Regex.IsMatch(attrs, "asp-validation-(for|summary)")) continue;
                var cm = classRe.Match(attrs);
                if (!cm.Success) continue;
                var cl = cm.Groups[1].Value;
                if (!utilRe.IsMatch(cl)) continue;
                var start = m.Index + m.Length;
                var nextLt = txt.IndexOf('<', start);
                var inner = nextLt < 0 ? txt[start..] : txt.Substring(start, nextLt - start);
                if (Regex.IsMatch(inner, "\\S")) continue; // simbolo/texto direto no elemento (ex.: "*" de obrigatorio)

                var ok = false;
                if (nextLt >= 0)
                {
                    if (nextLt + 1 < txt.Length && txt[nextLt + 1] == 'i')
                    {
                        // icone como primeiro filho: aceita com texto apos o </i>
                        var endI = txt.IndexOf("</i>", nextLt);
                        if (endI >= 0)
                        {
                            var afterI = endI + 4;
                            var next2 = txt.IndexOf('<', afterI);
                            var seg = next2 < 0 ? txt[afterI..] : txt.Substring(afterI, Math.Min(300, next2 - afterI));
                            if (Regex.IsMatch(seg, "[A-Za-z0-9]")) ok = true;
                            // banner rotulado (role="alert"/alert-*): texto dentro do primeiro container filho
                            if (!ok && (attrs.Contains("role=", StringComparison.Ordinal) || Regex.IsMatch(cl, "\\balert")))
                            {
                                var regiao = txt.Substring(afterI, Math.Min(300, txt.Length - afterI));
                                if (Regex.IsMatch(Regex.Replace(regiao, "<[^>]*>", ""), "[A-Za-z0-9]")) ok = true;
                            }
                        }
                    }
                    else
                    {
                        ok = true; // container: semantica resolvida mais fundo
                    }
                }
                if (!ok && cl.Contains("bg-opacity-25", StringComparison.Ordinal)) ok = true; // chip decorativo de icone (precedente R71)
                if (!ok && Regex.IsMatch(cl, "fs-\\d")) ok = true; // glifo decorativo dimensionado (rotulo vive fora do elemento)
                if (!ok)
                {
                    var snip = m.Value.Length > 120 ? m.Value[..120] : m.Value;
                    falhas.Add($"{nome}:L{LinNum(txt, m.Index)}: {snip}");
                }
            }
        }
        Assert.True(falhas.Count == 0, "cor de funcao sem texto/simbolo em:\n" + string.Join("\n", falhas.Take(25)));
    }

    // ------------------------------------------------------------- infraestrutura comum

    private static IEnumerable<string> CaminhosCss() =>
        Directory
            .EnumerateFiles(Path.Combine(Root, CssDir.Replace('/', Path.DirectorySeparatorChar)), "*.css", SearchOption.AllDirectories)
            .Where(p => p.Replace('\\', '/').IndexOf("/vendor/", StringComparison.OrdinalIgnoreCase) < 0)
            .OrderBy(p => p, StringComparer.Ordinal);

    private static IEnumerable<string> CaminhosViews() =>
        Directory
            .EnumerateFiles(Path.Combine(Root, ViewsDir.Replace('/', Path.DirectorySeparatorChar)), "*.cshtml", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal);

    private static string RelativoCss(string caminho)
    {
        var baseDir = Path.Combine(Root, CssDir.Replace('/', Path.DirectorySeparatorChar));
        return caminho[(baseDir.Length + 1)..].Replace('\\', '/');
    }

    private static string RelativoViews(string caminho)
    {
        var baseDir = Path.Combine(Root, ViewsDir.Replace('/', Path.DirectorySeparatorChar));
        return caminho[(baseDir.Length + 1)..].Replace('\\', '/');
    }

    private static int LinNum(string txt, int pos) =>
        txt.AsSpan(0, Math.Min(pos, txt.Length)).Count('\n') + 1;

    /// <summary>Retorna (pulso, rgb): pulso = alpha &lt; 1 em rgba/hex8 (composicao nao resolvida estaticamente).</summary>
    private static void AnalisarTinta(string spec, out bool pulso, out (int R, int G, int B)? rgb)
    {
        pulso = false;
        rgb = null;
        var v = (spec ?? "").Trim();
        if (v.EndsWith("!important", StringComparison.Ordinal)) v = v[..^10].Trim();
        if (v.StartsWith('#'))
        {
            var h = v[1..];
            if (h.Length == 3) h = $"{h[0]}{h[0]}{h[1]}{h[1]}{h[2]}{h[2]}";
            if (h.Length is 6 or 8)
            {
                if (h.Length == 8 && Convert.ToInt32(h.Substring(6, 2), 16) < 255) pulso = true;
                if (int.TryParse(h.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
                    && int.TryParse(h.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
                    && int.TryParse(h.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                {
                    rgb = (r, g, b);
                }
            }
            return;
        }
        if (!Regex.IsMatch(v, "rgba?\\(")) return;
        var dentro = Regex.Match(v, "rgba?\\(([^)]*)\\)").Groups[1].Value;
        if (dentro.Contains('/'))
        {
            var mo = Regex.Match(dentro, "/\\s*(\\d+(?:\\.\\d+)?)%?");
            if (mo.Success)
            {
                var opac = double.Parse(mo.Groups[1].Value, CultureInfo.InvariantCulture);
                if (dentro.TrimEnd().EndsWith("%")) opac /= 100.0;
                if (opac < 1.0) pulso = true;
            }
        }
        else
        {
            var canais = dentro.Split(',');
            if (canais.Length == 4)
            {
                var alvo = canais[3].Trim();
                pulso = double.TryParse(alvo, NumberStyles.Float, CultureInfo.InvariantCulture, out var a) && a < 1.0;
            }
        }
        var mrgb = Regex.Match(v, "rgba?\\(\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)");
        if (mrgb.Success)
        {
            rgb = (int.Parse(mrgb.Groups[1].Value, CultureInfo.InvariantCulture),
                   int.Parse(mrgb.Groups[2].Value, CultureInfo.InvariantCulture),
                   int.Parse(mrgb.Groups[3].Value, CultureInfo.InvariantCulture));
        }
    }

    private static double LuzRelativa((int R, int G, int B) c)
    {
        return 0.2126 * Canal(c.R) + 0.7152 * Canal(c.G) + 0.0722 * Canal(c.B);
        double Canal(int valor)
        {
            var s = valor / 255.0;
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
