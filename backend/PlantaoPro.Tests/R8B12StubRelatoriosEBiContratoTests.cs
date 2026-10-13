using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B12 — §29 item 4 (mapa C7.2/C7.3): placeholder de relatório duplicado em 5 views + BiController ações→views (views-fantasma).
///
/// Achado (fonte canônica): os 5 relatórios-stub (Cobertura, Convites, ProdutividadeMedica, Sla, FaturamentoSaas)
/// reproduziam BYTE-A-BYTE o mesmo markup (placeholder "Sem dados no período selecionado" + botão de exportação
/// bloqueado), diferindo só pelo título. E o Web/Controllers/BiController tem N ações apontando para poucas views,
/// várias reusando View("Index") e outras servindo stubs "em evolução".
///
/// Nesta fatia (rito §29: comparar → canônico → migrar → regressões):
///   (A) consolidou o markup repetido no partial canônico <c>Views/Shared/_RelatorioStub.cshtml</c> e migrou as 5
///       views para ele — a fonte ÚNICA do placeholder agora vive no partial (o marker some das 5 views).
///   (B) contratou o inventário do BiController: toda ação deve resolver para uma view .cshtml EXISTENTE
///       (integridade de rota — view renomeada/errada vira rota fantasma) e o conjunto de views distintas é fixado.
///       A remoção física dos ghosts/aliases segue pendente (roadmap de sub-páginas BI + wiring de menu), documentada.
/// </summary>
public sealed class R8B12StubRelatoriosEBiContratoTests
{
    private static readonly string[] StubsRelatorio = { "Cobertura", "Convites", "ProdutividadeMedica", "FaturamentoSaas", "Sla" };

    private const string MarkerVazio = "Sem dados no período selecionado";

    private static string WebFile(params string[] partes) =>
        Path.Combine(new[] { RepositoryPathResolver.WebRoot }.Concat(partes).ToArray());

    [Fact]
    public void StubRelatorio_MarkupDuplicado_FoiConsolidadoNoPartialCanonico()
    {
        // Fonte única: o marker de vazio vive NO partial canônico.
        var partialSrc = File.ReadAllText(WebFile("Views", "Shared", "_RelatorioStub.cshtml"));
        Assert.Contains(MarkerVazio, partialSrc);

        // Cada um dos 5 relatórios-stub delega ao partial e NÃO carrega mais o markup duplicado inline.
        foreach (var nome in StubsRelatorio)
        {
            var src = File.ReadAllText(WebFile("Views", "Relatorios", nome + ".cshtml"));
            Assert.Contains("_RelatorioStub", src);
            Assert.DoesNotContain(MarkerVazio, src);
        }
    }

    [Fact]
    public void BiController_TodaAcaoResolveParaViewExistente()
    {
        var (mapeadas, _) = ExtrairAcoesBi();
        // Sanidade do parsing: o BiController tem múltiplas ações (views próprias + aliases).
        Assert.True(mapeadas.Count >= 5, $"Esperado >= 5 ações mapeadas no BiController; obtido {mapeadas.Count}.");

        // Cada ação deve resolver para uma view .cshtml EXISTENTE (integridade de rota).
        foreach (var (acao, target) in mapeadas)
        {
            var viewPath = WebFile("Views", "Bi", target + ".cshtml");
            Assert.True(File.Exists(viewPath), $"Ação Bi.{acao} aponta para a view '{target}', mas {viewPath} não existe (rota fantasma).");
        }
    }

    [Fact]
    public void BiController_InventarioDeViewsDistintas_FixoEEspelhosDeStub()
    {
        var (mapeadas, aliasParaIndex) = ExtrairAcoesBi();

        // Inventário atual das VIEWS distintas do Bi (1 dashboard real + 4 stubs "em evolução").
        // Ao adicionar/remover uma sub-página BI, atualize este conjunto (e a documentação da fatia).
        var distinct = mapeadas.Select(x => x.Target).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var esperado = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Index", "Financeiro", "Medicos", "Clientes", "Sla" };
        Assert.True(distinct.SetEquals(esperado),
            $"Views distintas do Bi mudaram: [{string.Join(", ", distinct.OrderBy(x => x, StringComparer.Ordinal))}].");

        // As 6 ações não-Index apontam para Index (aliases); o dashboard real é Bi.Index.
        Assert.True(aliasParaIndex == 6, $"Esperado 6 ações aliasando para Index; obtido {aliasParaIndex}.");
        Assert.True(File.Exists(WebFile("Views", "Bi", "Index.cshtml")));

        // Os 4 stubs seguem sendo placeholders "em evolução" (classificação, não acidental).
        foreach (var stub in new[] { "Financeiro", "Medicos", "Clientes", "Sla" })
            Assert.Contains("em evolução", File.ReadAllText(WebFile("Views", "Bi", stub + ".cshtml")));
    }

    /// <summary>
    /// Lê <c>Web/Controllers/BiController.cs</c> e extrai cada ação
    /// <c>public IActionResult Acao() =&gt; View("<Target>?")</c> (target = "&lt;Target&gt;" ou Acao quando View()).
    /// </summary>
    private static (List<(string Acao, string Target)> Acoes, int AliasParaIndex) ExtrairAcoesBi()
    {
        var src = File.ReadAllText(WebFile("Controllers", "BiController.cs"));
        var rx = new Regex(
            @"public\s+IActionResult\s+(\w+)\s*\(\s*\)\s*=>\s*View\(\s*(?:""(\w+)""?)?\s*\)",
            RegexOptions.CultureInvariant);
        var acoes = new List<(string, string)>();
        var alias = 0;
        foreach (Match m in rx.Matches(src))
        {
            var acao = m.Groups[1].Value;
            var alvoArg = m.Groups[2].Value;
            var target = string.IsNullOrEmpty(alvoArg) ? acao : alvoArg;
            acoes.Add((acao, target));
            if (target.Equals("Index", StringComparison.OrdinalIgnoreCase) && !acao.Equals("Index", StringComparison.OrdinalIgnoreCase))
                alias++;
        }
        return (acoes, alias);
    }
}
