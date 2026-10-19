using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R57 — passada de tom (fila r53b1 §7 item 3, auditoria de tom R53 §6).
/// D-R57-1: tom declarativo — exclamação fim-de-frase é banida em texto visível ao usuário
/// (toasts, tooltips, mensagens inline); substituição por ponto, alinhado ao precedência já
/// existente na base ("Inventário aberto com sucesso."). Exceções travadas como permitidas:
/// o glifo decorativo de alerta &gt;!</span&gt; (aria-hidden, ícone não frase), CSS !important,
/// operador != do código, senhas de dados de teste e perguntas (que mantêm "?").
/// A contagem "31 exclamações em Views" da auditoria R53 incluía código (@(!Model.X), !==,
/// !important); o inventário real de copy visível desta rodada foi 1 tooltip + 16 toasts resx
/// + 1 mensagem de serviço — todos tratados aqui.
/// </summary>
public sealed class R57TomExclamacaoContratoTests
{
    private static string ViewsDir => Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");

    private static string ResxPath(string locale = "") => Path.Combine(
        RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Localization",
        string.IsNullOrEmpty(locale) ? "UiTexts.resx" : $"UiTexts.{locale}.resx");

    [Fact]
    public void Resx_NenhumValorVisivelContemExclamacao()
    {
        foreach (var locale in new[] { "", "en", "es", "fr" })
        {
            var resx = File.ReadAllText(ResxPath(locale), Encoding.UTF8);
            var valores = Regex.Matches(resx, "<value>([^<]*)</value>").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Assert.True(valores.Count > 500, $"resx {locale} com menos valores do que o esperado: {valores.Count}");
            var violacoes = valores
                .Where(v => v.Contains('!'))
                .Select(v => v.Length > 90 ? v.Substring(0, 90) : v)
                .ToList();
            Assert.True(violacoes.Count == 0,
                $"Valores do resx {(string.IsNullOrEmpty(locale) ? "pt-BR" : locale)} com exclamação (tom declarativo usa ponto):\n" + string.Join("\n", violacoes.Take(10)));
        }
    }

    [Fact]
    public void Views_SemExclamacaoEmTextoVisivelForaDoGlifoDeAlerta()
    {
        // Regra: "!" só pode aparecer colado a aspa/espaço/tag/vírgula/parêntese (posição de
        // fim ou meio de frase) — onde viveria um "!" de copy. O único uso legítimo é o glifo
        // decorativo de alerta ">!</span>" (aria-hidden=true), presente em 3 banners.
        // Não pegam: != , !== , CSS !important, classes *exclamation*.
        var violacoes = new List<string>();
        var totalExclamacoesPosicaoCopy = 0;
        var totalGlifosDeAlerta = 0;
        foreach (var arquivo in Directory.EnumerateFiles(ViewsDir, "*.cshtml", SearchOption.AllDirectories))
        {
            var texto = File.ReadAllText(arquivo, Encoding.UTF8);
            var relativo = arquivo.Replace(ViewsDir, "").Replace('\\', '/');
            var copy = Regex.Matches(texto, @"![\s""'<,)]").Count;
            var glifo = Regex.Matches(texto, ">!</span>").Count;
            totalExclamacoesPosicaoCopy += copy;
            totalGlifosDeAlerta += glifo;
            if (copy > glifo) violacoes.Add($"{relativo}: {copy - glifo} exclamação(ões) em posição de copy fora do glifo de alerta");
        }
        Assert.True(totalGlifosDeAlerta >= 3, $"glifos de alerta esperados (3 banners), encontrados {totalGlifosDeAlerta}");
        Assert.True(violacoes.Count == 0, "Exclamações em texto visível das views:\n" + string.Join("\n", violacoes.Take(10)));
    }

    [Fact]
    public void MensagensCorrigidas_NaoRegressamAExclamacao()
    {
        // As três ocorrências reais de copy encontradas no inventário R53/R57 travadas uma a uma:
        // tooltip do lote vencido (view), toast de aprovação já coberto pelo resx acima e a
        // mensagem de serviço do onboarding (API -> toast do tenant).
        var estoque = File.ReadAllText(Path.Combine(ViewsDir, "Administrativo360", "Estoque.cshtml"), Encoding.UTF8);
        Assert.DoesNotContain("Lote vencido!", estoque);
        Assert.Contains("title=\"Lote vencido\"", estoque);

        var onboarding = File.ReadAllText(
            Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Api", "OnboardingService.cs"), Encoding.UTF8);
        Assert.DoesNotContain("Cliente criado com sucesso!", onboarding);
        Assert.Contains("Cliente criado com sucesso.", onboarding);
    }
}
