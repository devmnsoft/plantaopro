using System.Text.RegularExpressions;
using PlantaoPro.Web.Localization;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R71 — Semantica de cores: todo estado de negocio e comunicado com icone + texto
/// (nunca cor sozinha), e o pareamento estado -&gt; tom (cor) -&gt; icone vive em um
/// unico lugar: o mapa C# (PlantaoPro.Web.Localization.EstadoVisual) e a secao 9 do
/// v2151 (tokens --pp-tone-&lt;tom&gt;-ink | --pp-tone-&lt;tom&gt;-soft e as regras
/// .pp-status--&lt;tom&gt;). Os clients JS (agenda-operacional.js e
/// notification-center.js) espelham esse mapa como subconjunto com os mesmos pares
/// (estado, tom, icone). Os fatos abaixo espelham os criterios da auditoria
/// deterministica docs/evidencias/2026-10-20-rodada-71/r71_audit_mapa.ps1 (antes da
/// rodada: 28 falhas; depois: 0) e travam os dois artefatos unicos da semantica.
/// </summary>
public sealed class R71SemanticaDeCoresContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRepositoryRoot();
    private static string Read(string caminho) => File.ReadAllText(Path.Combine(Root, caminho));
    private static readonly string[] Tons = { "success", "warning", "info", "danger", "secondary" };

    private static List<(string Estado, string Tom, string Icone)> MapaDe(string fonte) =>
        Regex.Matches(fonte, @"M\(""([a-z0-9][a-z0-9-]*)""\s*,\s*""([a-z]+)""\s*,\s*""(bi-[a-z0-9-]+)""\)")
            .Cast<Match>()
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value))
            .ToList();

    // ------------------------------------------- camada canonica v2151 (secao 9)

    [Fact]
    public void Camada_canonica_v2151_define_tons_e_regras_de_estado()
    {
        const string caminho = "backend/PlantaoPro.Web/wwwroot/css/design-system/v2151-wp-c-ajuste-canonical.css";
        var css = Read(caminho);
        Assert.Contains("R71-MAPA-ESTADOS", css);
        foreach (var tom in Tons)
        {
            Assert.Contains($"--pp-tone-{tom}-ink: #", css);
            Assert.Contains($"--pp-tone-{tom}-soft: #", css);
            var regra = $@"\.pp-status--{tom}\s*\{{\s*color:\s*var\(--pp-tone-{tom}-ink\);\s*background:\s*var\(--pp-tone-{tom}-soft\);\s*\}}";
            Assert.Matches(regra, css);
        }
    }

    // -------------------------------------------------------- mapa canonico C#

    [Fact]
    public void Mapa_canonico_e_integro_e_cobre_os_guard_do_banco()
    {
        var src = Read("backend/PlantaoPro.Web/Localization/EstadoVisual.cs");
        var mapa = MapaDe(src);
        Assert.True(mapa.Count >= 70, $"o mapa deve ter >= 70 estados; achei {mapa.Count}");

        var slugs = mapa.Select(e => e.Estado).ToList();
        Assert.Equal(slugs.Count, slugs.Distinct(StringComparer.Ordinal).Count());
        foreach (var entrada in mapa)
            Assert.Contains(entrada.Tom, Tons);

        // Fallback neutro para estado fora do mapa: tom secondary + icone pergunta.
        Assert.Contains("M(\"indefinido\", \"secondary\", \"bi-question-circle\");", src);
        var foraDoMapa = EstadoVisual.Interpretar(null);
        Assert.Equal("secondary", foraDoMapa.Tom);
        Assert.Equal("bi-question-circle", foraDoMapa.Icone);
        var pago = EstadoVisual.Interpretar("PAGO");
        Assert.Equal(("pago", "success", "bi-check-circle-fill"), (pago.Nome, pago.Tom, pago.Icone));

        // Cobertura: todo valor admitido por um guard CHECK (status in (...)) do
        // banco resolve no mapa; nenhum estado real cai no fallback por esquecimento.
        var sql = Read("database/scrpt_completo.sql");
        var valores = Regex.Matches(sql, @"check\s*\(\s*status\s+in\s*\(([^)]*)\)", RegexOptions.IgnoreCase)
            .Cast<Match>()
            .SelectMany(m => Regex.Matches(m.Groups[1].Value, @"'([A-Z][A-Z0-9_]*)'").Cast<Match>().Select(v => v.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        Assert.True(valores.Count >= 30, $"esperava >= 30 valores de guard no banco; achei {valores.Count}");
        var slugsSet = slugs.ToHashSet(StringComparer.Ordinal);
        var fora = valores.Where(v => !slugsSet.Contains(EstadoVisual.Normalizar(v))).ToList();
        Assert.True(fora.Count == 0, $"estados do banco fora do mapa: {string.Join(", ", fora)}");
    }

    // --------------------------------------- nenhum CSS fora dos tons canonicos

    [Fact]
    public void Nenhum_css_fora_da_camada_canonica_define_cor_de_estado()
    {
        var falhas = EscaneiaCss().ToList();
        Assert.True(falhas.Count == 0, "cor de estado fora dos tons canonicos em:\n" + string.Join("\n", falhas));
    }

    private static readonly Regex SelStatusRe = new("[.-]status-[a-z][a-z-]*");
    private static readonly Regex PropRe = new(@"^\s*(color|background[a-z-]*)\s*:\s*(.+)$");
    private static readonly Regex ToneVarRe = new("^var\\(\\s*--pp-tone-[a-z]+-(?:ink|soft)");
    private static readonly Regex TextVarRe = new("^var\\(\\s*--pp-text");

    // Mesmo criterio do PASS A de r71_audit_mapa.ps1 (bloco a bloco, sem vendor).
    private static IEnumerable<(string File, int Line, string Sel)> EscaneiaCss()
    {
        var dir = Path.Combine(Root, "backend", "PlantaoPro.Web", "wwwroot", "css");
        foreach (var f in Directory.EnumerateFiles(dir, "*.css", SearchOption.AllDirectories)
                     .Where(p => p.Replace('\\', '/').IndexOf("/vendor/", StringComparison.OrdinalIgnoreCase) < 0)
                     .OrderBy(p => p, StringComparer.Ordinal))
        {
            var txt = File.ReadAllText(f);
            var nome = f.Substring(dir.Length + 1).Replace('\\', '/');
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
                if (!SelStatusRe.IsMatch(sel)) continue;
                foreach (var p in dec.Split(';'))
                {
                    var m = PropRe.Match(p);
                    if (!m.Success) continue;
                    var val = m.Groups[2].Value.Trim();
                    if (val is "currentColor" or "inherit" or "transparent") continue;
                    if (ToneVarRe.IsMatch(val) || TextVarRe.IsMatch(val)) continue;
                    var line = txt.Substring(0, ob).Count(c => c == '\n') + 1;
                    var selCorta = sel.Length > 80 ? sel[..80] : sel;
                    yield return (nome, line, selCorta);
                    break;
                }
            }
        }
    }

    // -------------------------------------------- nunca estado comunicado por cor

    [Fact]
    public void Nenhuma_view_comunica_estado_somente_por_cor()
    {
        var falhas = EscaneiaViews().ToList();
        Assert.True(falhas.Count == 0, "estado comunicado so por cor em:\n" + string.Join("\n", falhas));
    }

    // Dots decorativos dentro de containers rotulados (role="status" com texto
    // vizinho): o texto comunica o estado. Whitelist exata do r71_audit_mapa.ps1.
    private static readonly string[] WhitelistCorSola =
    {
        "pp-timeline-dot", "pp-login-connection__dot", "pp-session-health__dot", "bg-opacity-25", "save-indicator",
    };
    private static readonly Regex CandTokRe = new(
        @"(?:status-badge|status-[a-z][a-z-]*|pp-status(?:-badge|-with-icon|--[a-z]+)?|text-bg-(?:success|danger|warning|info|secondary)|(?<![a-z-])(?:badge|chip|chips|pill|pills|dot|dots|indicator|pulsar)\b)");
    private static readonly Regex TagRe = new(@"<([a-z][a-z0-9]*)\b((?:""[^""]*""|'[^']*'|[^>""'])*)>");
    private static readonly Regex ClassRe = new("class\\s*=\\s*\"([^\"]*)\"");
    private static readonly Regex AlfanumRe = new("[A-Za-z0-9]");

    // Mesmo criterio do PASS B de r71_audit_mapa.ps1 (tag a tag em Views/**).
    private static IEnumerable<string> EscaneiaViews()
    {
        var dir = Path.Combine(Root, "backend", "PlantaoPro.Web", "Views");
        foreach (var f in Directory.EnumerateFiles(dir, "*.cshtml", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
        {
            var txt = File.ReadAllText(f);
            var rel = f.Substring(dir.Length + 1).Replace('\\', '/');
            foreach (Match m in TagRe.Matches(txt))
            {
                var attrs = m.Groups[2].Value;
                if (attrs.Contains("aria-label=", StringComparison.Ordinal)
                    || attrs.Contains("title=", StringComparison.Ordinal)
                    || attrs.Contains("data-notif", StringComparison.Ordinal)) continue;
                var cm = ClassRe.Match(attrs);
                if (!cm.Success) continue;
                var classVal = cm.Groups[1].Value;
                if (!CandTokRe.IsMatch(classVal)) continue;
                var tokens = classVal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Intersect(WhitelistCorSola, StringComparer.Ordinal).Any()) continue;
                var start = m.Index + m.Length;
                var nextLt = txt.IndexOf('<', start);
                var inner = nextLt < 0 ? txt.Substring(start) : txt.Substring(start, nextLt - start);
                if (AlfanumRe.IsMatch(inner)) continue;
                if (nextLt >= 0 && nextLt + 2 <= txt.Length && txt[nextLt] == '<' && txt[nextLt + 1] == 'i')
                {
                    // icone como primeiro filho: aceita se ha texto apos o </i>
                    var endI = txt.IndexOf("</i>", nextLt);
                    var seg = string.Empty;
                    if (endI >= 0)
                    {
                        var afterI = endI + 4;
                        var next2 = txt.IndexOf('<', afterI);
                        seg = next2 < 0 ? txt.Substring(afterI) : txt.Substring(afterI, next2 - afterI);
                    }
                    if (AlfanumRe.IsMatch(seg)) continue;
                }
                else if (nextLt >= 0)
                {
                    continue; // container: semantica resolvida mais fundo
                }
                var line = txt.Substring(0, m.Index).Count(c => c == '\n') + 1;
                var snip = m.Value.Length > 120 ? m.Value[..120] : m.Value;
                yield return $"{rel}:L{line}: {snip}";
            }
        }
    }

    // --------------------------------------------------- componentes canonicos

    [Fact]
    public void Componentes_canonicos_sempre_emitem_icone_texto_e_rotulo()
    {
        var caminhos = new[]
        {
            "backend/PlantaoPro.Web/Views/Shared/_StatusBadge.cshtml",
            "backend/PlantaoPro.Web/Views/Shared/_StatusBadgeComRotulo.cshtml",
        };
        foreach (var caminho in caminhos)
        {
            var s = Read(caminho);
            Assert.Contains("role=\"status\"", s);
            Assert.Contains("data-status-label", s);
            Assert.Contains("EstadoVisual.Interpretar", s);
            Assert.Contains("status-badge pp-status pp-status--@estado.Tom status-@estado.Nome", s);
            Assert.Contains("\"bi @estado.Icone\"", s);
        }
    }

    // ------------------------------------------- clients JS espelham o mapa

    [Fact]
    public void Clients_js_espelham_o_mapa_canonico_sem_divergir()
    {
        var src = Read("backend/PlantaoPro.Web/Localization/EstadoVisual.cs");
        var mapa = new Dictionary<string, (string Tom, string Icone)>(StringComparer.Ordinal);
        foreach (var e in MapaDe(src)) mapa[e.Estado] = (e.Tom, e.Icone);

        var parRe = new Regex(@"['""]?([a-z0-9][a-z0-9-]*)['""]?\s*:\s*\['(success|warning|info|danger|secondary)','(bi-[a-z0-9-]+)'\]");
        var fallbackRe = new Regex(@"\[\s*'secondary'\s*,\s*'bi-question-circle'\s*\]");

        (string Caminho, int Minimo)[] alvos =
        {
            ("backend/PlantaoPro.Web/wwwroot/js/agenda-operacional.js", 20),
            ("backend/PlantaoPro.Web/wwwroot/js/pages/notification-center.js", 8),
        };
        foreach (var alvo in alvos)
        {
            var fonte = Read(alvo.Caminho);
            Assert.Matches(fallbackRe, fonte);
            var pares = parRe.Matches(fonte).Cast<Match>()
                .Select(m => (m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value))
                .ToList();
            Assert.True(pares.Count >= alvo.Minimo, $"{alvo.Caminho}: esperava >= {alvo.Minimo} pares; achei {pares.Count}");
            var divergentes = pares
                .Where(p => !mapa.TryGetValue(p.Item1, out var can) || can.Tom != p.Item2 || can.Icone != p.Item3)
                .Select(p => $"{p.Item1}->{p.Item2}/{p.Item3}")
                .ToList();
            Assert.True(divergentes.Count == 0, $"{alvo.Caminho}: pares divergentes do mapa canonico: {string.Join(", ", divergentes)}");
        }
    }
}
