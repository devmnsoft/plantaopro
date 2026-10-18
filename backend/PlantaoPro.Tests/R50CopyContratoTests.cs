using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R50 (roadmap r24a1): passada final de copy — tom de voz único e glossário canônico (D-R50-1).
/// Contrato permanente que trava o resultado da passada: jargão técnico não aparece em texto visível,
/// o resx pt-BR não traz jargão de sessão e as listas não exibem fragmentos de id cru.
/// Plano: docs/evidencias/2026-10-18-rodada-50/r50a0-passada-final-copy-plano.md.
/// </summary>
public sealed class R50CopyContratoTests
{
    private static string ViewsDir => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");

    [Fact]
    public void JargaoTecnico_NaoApareceEmTextoVisivelDasViews()
    {
        // Frases (não palavras soltas) para não bater em nomes de código: name="FallbackProvedor",
        // id="fallback_...", Endpoint = "/api/..." permanecem como identificadores, não como copy.
        var frases = new[]
        {
            "fallback de modelo", "modelo do fallback", "provedor de fallback",
            "como fallback", "usa o fallback", "\"fallback\"", "neste endpoint",
        };
        var violacoes = new List<string>();
        foreach (var arquivo in System.IO.Directory.EnumerateFiles(ViewsDir, "*.cshtml", System.IO.SearchOption.AllDirectories))
        {
            var texto = System.IO.File.ReadAllText(arquivo, System.Text.Encoding.UTF8).ToLowerInvariant();
            var relativo = arquivo.Replace(ViewsDir, "").Replace('\\', '/');
            foreach (var frase in frases)
            {
                if (texto.Contains(frase.ToLowerInvariant(), StringComparison.Ordinal))
                    violacoes.Add($"{relativo}: jargão \"{frase}\" em texto visível");
            }
        }
        Assert.True(violacoes.Count == 0, "Jargão técnico em texto visível:\n" + string.Join("\n", violacoes.Take(10)));
    }

    [Fact]
    public void ResxPtBr_SemJargaoDeSessaoNosValores()
    {
        var caminho = System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Localization", "UiTexts.resx");
        var resx = System.IO.File.ReadAllText(caminho, System.Text.Encoding.UTF8);
        var valores = Regex.Matches(resx, "<value>([^<]*)</value>").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        Assert.True(valores.Count > 500, "resx pt-BR com menos valores do que o esperado: " + valores.Count);
        var violacoes = valores
            .Where(v => v.IndexOf("fallback", StringComparison.OrdinalIgnoreCase) >= 0 || Regex.IsMatch(v, @"\btoken\b", RegexOptions.IgnoreCase))
            .Select(v => v.Length > 90 ? v.Substring(0, 90) : v)
            .ToList();
        Assert.True(violacoes.Count == 0, "Valores do resx pt-BR com jargão (use \"padrão/reserva\" e \"link/sessão\"):\n" + string.Join("\n", violacoes.Take(10)));
    }

    [Fact]
    public void Convites_NaoExibeFragmentoDeIdCru()
    {
        var caminho = System.IO.Path.Combine(ViewsDir, "Convites", "Index.cshtml");
        var t = System.IO.File.ReadAllText(caminho, System.Text.Encoding.UTF8);
        Assert.DoesNotContain("PlantaoId.ToString()[..8]", t, StringComparison.Ordinal);
    }
}
