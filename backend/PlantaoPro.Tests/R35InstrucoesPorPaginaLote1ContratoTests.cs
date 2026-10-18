using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R35 (diretriz r24a1, epic E3 parte 1 — "instruções de uso por página"): as 7 páginas do fluxo
/// cirúrgico central + compras/estoque não tinham nenhum bloco orientando o usuário. Foram adicionados
/// os cards "Guia da tela" no padrão da casa (Cirurgias.cshtml): 4 seções fixas (para que serve / como
/// utilizar / efeito da ação / próxima ação) com redação simples (E1) e fiel ao que a operação faz de
/// verdade — verificado contra os controllers (aprovação de pedido congela preços; edição de orçamento
/// aprovado gera revisão e volta ao rascunho; expedição registra saída da custódia). Inventário e
/// divisão em lotes em docs/evidencias/2026-10-18-rodada-35/r35a0-e3-instrucoes-inventario-plano.md.
/// Este fato trava a ESTRUTURA do guia nas 7 views (comentário-âncora + seções 1, 2 e 4 invariantes;
/// a seção 3 varia de título por página e é verificada pelo scanner R29 de jargão, não aqui).
/// </summary>
public class R35InstrucoesPorPaginaLote1ContratoTests
{
    private static readonly string[] ViewsDoLote =
    {
        "OrcamentoNovo.cshtml", "OrcamentoEditar.cshtml", "OrcamentoDetalhes.cshtml",
        "PedidosCompra.cshtml", "Estoque.cshtml", "Coleta.cshtml", "ValeDetalhes.cshtml",
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
}
