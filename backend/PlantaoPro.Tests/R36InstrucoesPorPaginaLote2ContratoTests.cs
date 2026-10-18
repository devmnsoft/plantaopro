using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R36 (diretriz r24a1, epic E3 parte 2 — "instruções de uso por página"): as 12 páginas de
/// qualidade/transferências/contas a pagar/fiscal/XML/cotações não tinham bloco orientando o usuário.
/// Foram adicionados os cards "Guia da tela" no padrão da casa (mesmo formato do lote 1 da R35):
/// 4 seções fixas (para que serve / como utilizar / efeito / próxima ação), redação simples (E1) e
/// fiel às operações reais (aprovação de título congela a obrigação para pagamento; envio de nota vai
/// para processamento e retorna autorizada/rejeitada; importação manual alimenta a lista de recebidos).
/// Junto, 5 pontos de código interno saíram do texto renderizado das telas fiscais: "(A33)", "(A29)"
/// (×2) e "(MVP A29)", "(P1)" e "(H19)". Inventário e divisão em lotes:
/// docs/evidencias/2026-10-18-rodada-35/r35a0-e3-instrucoes-inventario-plano.md.
/// Fato 1 trava a ESTRUTURA do guia nas 12 views (âncora + seções 1, 2 e 4 invariantes); o fato 2
/// trava a ausência definitiva dos códigos internos nos quatro textos fiscais.
/// </summary>
public class R36InstrucoesPorPaginaLote2ContratoTests
{
    private static readonly string[] ViewsDoLote =
    {
        "Inspecoes.cshtml", "Movimentacoes.cshtml", "TituloPagarDetalhes.cshtml",
        "Fiscal\\Configurar.cshtml", "Fiscal\\Detalhes.cshtml", "Fiscal\\Notas.cshtml", "Fiscal\\Nova.cshtml",
        "DocumentosXml\\Importar.cshtml", "DocumentosXml\\Sincronizacao.cshtml",
        "Cotacoes\\ContasPortal.cshtml", "Cotacoes\\Estabelecimentos.cshtml", "Cotacoes\\Relacionamento.cshtml",
    };

    private static string ReadView(string view) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(
            new[] { "backend", "PlantaoPro.Web", "Views", "Administrativo360", view }).ToArray()), Encoding.UTF8);

    [Fact]
    public void CadaPaginaDoLoteTemGuiaDaTelaCompleto()
    {
        foreach (var view in ViewsDoLote)
        {
            var v = ReadView(view);
            Assert.Contains("<!-- Guia da tela -->", v, StringComparison.Ordinal);
            Assert.Contains("1. Para que serve", v, StringComparison.Ordinal);
            Assert.Contains("2. Como utilizar", v, StringComparison.Ordinal);
            Assert.Contains("4. Próxima ação", v, StringComparison.Ordinal);
            Assert.Contains("bi-info-circle me-2", v, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TelasFiscais_NaoExibemCodigoInternoEmTextoRenderizado()
    {
        var codigos = new[] { "(A29)", "(A33)", "(MVP", "(H19)", "(L33", "(P1)" };
        foreach (var view in new[] { "Fiscal\\Configurar.cshtml", "Fiscal\\Detalhes.cshtml", "Fiscal\\Notas.cshtml", "Fiscal\\Nova.cshtml" })
        {
            var v = ReadView(view);
            foreach (var c in codigos)
            {
                Assert.DoesNotContain(c, v, StringComparison.Ordinal);
            }
        }
    }
}
