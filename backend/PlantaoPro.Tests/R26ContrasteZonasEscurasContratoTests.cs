using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R26 (diretriz r24a1, epic E4 — "uma área escura não pode ter letra escura"): o scanner de
/// adjacência DOM (mesmo algoritmo de %TEMP%\opencode\r26_scan_contraste.ps1 usado no recon r26a0)
/// fica permanente aqui: NENHUM elemento text-muted pode existir dentro de um ancestor escuro
/// (bg-black/bg-dark/table-black/pp-hero__bg/operational-card) nas VIEWS da casa. Os 19 pontos
/// reais encontrados (8 views Adm360) foram corrigidos na r26b1 trocando text-muted por
/// text-white-50 (mesma família de clareza, desenhada para fundo escuro) — ou removendo a classe
/// quando o próprio elemento já tinha cor correta (text-light) ou o fundo era claro
/// (alert-secondary).
/// Aproximação documentada: o parser de tags ignora ângulos dentro de expressões Razor; tags
/// falsas de generics entram sem classes e nunca ativam o gatilho escuro (Clas vazio). Se um
/// futuro caso legítimo falhar neste contrato, ajustar o scanner aqui mesmo e documentar.
/// </summary>
public class R26ContrasteZonasEscurasContratoTests
{
    private static readonly Regex TagRe = new(@"</?([a-zA-Z][a-zA-Z0-9]*)([^>]*)>", RegexOptions.Compiled);
    private static readonly Regex EscurosRe = new(@"bg-black|bg-dark|table-black|pp-hero__bg|operational-card", RegexOptions.Compiled);
    // Superfícies claras próprias do elemento: o texto muted sobre ELAS está correto mesmo que um
    // card escuro envolva o bloco (ex.: alert cinza-claro dentro de painel escuro).
    private static readonly Regex ClarasRe = new(@"bg-white|bg-light(\b|$)|bg-lighter|alert-secondary|alert-light|alert-success|alert-warning|alert-info|bg-body|card-bg-claro", RegexOptions.Compiled);
    private static readonly Regex ClasseRe = new("class\\s*=\\s*\"([^\"]*)\"", RegexOptions.Compiled);

    private static IReadOnlyList<int> MutedSobreZonaEscura(string conteudo)
    {
        var achados = new List<int>();
        var pilha = new Stack<(string Tag, string Cls)>();
        foreach (Match m in TagRe.Matches(conteudo))
        {
            var fechando = m.Value.StartsWith("</", StringComparison.Ordinal);
            var tag = m.Groups[1].Value.ToLowerInvariant();
            var attrs = m.Groups[2].Value;
            if (fechando)
            {
                while (pilha.Count > 0)
                {
                    var top = pilha.Pop();
                    if (top.Tag == tag)
                    {
                        break;
                    }
                }

                continue;
            }

            if (attrs.TrimEnd().EndsWith("/", StringComparison.Ordinal))
            {
                continue;
            }

            var cls = string.Empty;
            var cm = ClasseRe.Match(attrs);
            if (cm.Success)
            {
                cls = cm.Groups[1].Value;
            }

            if (cls.Contains("text-muted", StringComparison.Ordinal) &&
                !ClarasRe.IsMatch(cls) &&
                (EscurosRe.IsMatch(cls) || pilha.Any(a => EscurosRe.IsMatch(a.Cls))))
            {
                achados.Add(conteudo.Substring(0, m.Index).Count(c => c == '\n') + 1);
            }

            pilha.Push((tag, cls));
        }

        return achados;
    }

    [Fact]
    public void Scanner_DeContraste_TemControlesPositivoENegativo()
    {
        // O scanner precisa ser sensível exatamente ao padrão que a diretriz veta.
        var positivo = "<div class=\"card bg-black p-3\"><span class=\"text-muted\">letra escura em zona escura</span></div>";
        Assert.Single(MutedSobreZonaEscura(positivo));

        var positivoMesmoElemento = "<li class=\"list-group-item bg-dark text-muted py-3\">vazio</li>";
        Assert.Single(MutedSobreZonaEscura(positivoMesmoElemento));

        var negativoZonaClara = "<div class=\"card card-body p-3\"><span class=\"text-muted\">ajuda em zona clara</span></div>";
        Assert.Empty(MutedSobreZonaEscura(negativoZonaClara));

        var substitutoCorreto = "<div class=\"card bg-black p-3\"><span class=\"text-white-50\">texto claro em zona escura</span></div>";
        Assert.Empty(MutedSobreZonaEscura(substitutoCorreto));

        // Superfície clara própria dentro de card escuro: muted está correto sobre o próprio fundo.
        var alertaClaroEmCardEscuro = "<div class=\"card bg-dark\"><div class=\"alert alert-secondary small text-muted\">aviso</div></div>";
        Assert.Empty(MutedSobreZonaEscura(alertaClaroEmCardEscuro));
    }

    [Fact]
    public void NenhumaView_ExibeTextoMutedSobreZonaEscura()
    {
        var raizViews = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");
        var violacoes = new List<string>();
        foreach (var arquivo in Directory.EnumerateFiles(raizViews, "*.cshtml", SearchOption.AllDirectories))
        {
            var rel = arquivo[raizViews.Length..].Replace('\\', '/');
            var linhas = MutedSobreZonaEscura(File.ReadAllText(arquivo));
            foreach (var ln in linhas)
            {
                violacoes.Add($"{rel}:{ln}");
            }
        }

        Assert.True(violacoes.Count == 0,
            "text-muted sobre zona escura (E4 — letra escura em área escura não pode existir):\n" + string.Join("\n", violacoes));
    }
}
