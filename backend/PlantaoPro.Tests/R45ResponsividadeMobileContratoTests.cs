using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R45 (diretriz r24a1, epic E0 design — responsividade mobile/campo): varredura de todas as
/// views confirmou que 114 das tabelas de dados já estavam dentro de .table-responsive; restavam
/// três telas institucionais sem o wrapper (ConvitesEquipe, MinhaAssinatura/Faturas,
/// Usuario/Admin) — largura fixa que vira rolagem/estouro no celular. Correção + contrato
/// permanente: NENHUMA view (exceto as duas telas de impressão, que são para A4 paisagem e ficam
/// declaradas como keeps abaixo) pode ter tabela fora do .table-responsive. O &lt;table&gt; citado
/// em comentários HTML (ex.: a nota de parsing dos modais em DocumentosXml/Triagens) não conta,
/// por isso a contagem ignora linhas de comentário. Plano:
/// docs/evidencias/2026-10-18-rodada-45/r45a0-e0-responsividade-plano.md.
/// </summary>
public class R45ResponsividadeMobileContratoTests
{
    // Telas de impressão: layout A4 paisagem propositalmente sem rolagem horizontal.
    private static readonly string[] ViewsImpressao =
    {
        Path.Combine("Administrativo360", "OrcamentoImprimir.cshtml"),
        Path.Combine("Administrativo360", "ValeImprimir.cshtml"),
    };

    private static int ContarTabelasReais(string caminho)
    {
        var total = 0;
        foreach (var linha in File.ReadLines(caminho, Encoding.UTF8))
        {
            var corte = linha.TrimStart();
            // Comentários HTML e Razor não são renderização — não contam como tabela.
            if (corte.StartsWith("<!--", StringComparison.Ordinal) || corte.StartsWith("@*", StringComparison.Ordinal)) continue;
            total += System.Text.RegularExpressions.Regex.Matches(linha, @"<table[\s>]").Count;
        }
        return total;
    }

    [Fact]
    public void LayoutRaiz_DefineViewportParaDispositivos()
    {
        var p = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views", "Shared", "_Layout.cshtml");
        Assert.Contains("name=\"viewport\"", File.ReadAllText(p, Encoding.UTF8), StringComparison.Ordinal);
    }

    [Fact]
    public void NenhumaViewTemTabelaForaDoTableResponsive()
    {
        var raizViews = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");
        var ofensores = new List<string>();
        foreach (var arquivo in Directory.GetFiles(raizViews, "*.cshtml", SearchOption.AllDirectories))
        {
            var relativo = Path.GetRelativePath(raizViews, arquivo).Replace('\\', '/');
            if (ViewsImpressao.Contains(relativo.Replace('/', Path.DirectorySeparatorChar))) continue;
            var texto = File.ReadAllText(arquivo, Encoding.UTF8);
            var tabelas = ContarTabelasReais(arquivo);
            var wrappers = System.Text.RegularExpressions.Regex.Matches(texto, "table-responsive").Count;
            if (tabelas > wrappers)
                ofensores.Add($"{relativo} (tabelas={tabelas} wrappers={wrappers})");
        }
        Assert.True(ofensores.Count == 0, string.Join("; ", ofensores));
    }
}
