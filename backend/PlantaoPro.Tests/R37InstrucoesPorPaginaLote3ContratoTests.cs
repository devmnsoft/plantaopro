using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R37 (diretriz r24a1, epic E3 parte 3 — "instruções de uso por página"): as 8 páginas de
/// cadastros organizacionais + gestão não tinham bloco orientando o usuário. Foram adicionados os
/// cards "Guia da tela" no padrão da casa (mesmo formato dos lotes 1 e 2): 4 seções fixas, redação
/// simples (E1) e fiel às operações reais (inativar preserva histórico; só colaborador ativo pode ser
/// contratado; o monitor só observa, sem ação direta). Junto, o título "Cadastros Mestre" virou
/// "Cadastros Organizacionais" (linguagem simples + alinhado ao ViewData Title da própria página).
/// Com este lote o epic E3 está completo: as 29 páginas Adm360 do inventário do r35a0 têm instrução.
/// </summary>
public class R37InstrucoesPorPaginaLote3ContratoTests
{
    private static readonly string[] ViewsDoLote =
    {
        "Cadastros.cshtml", "Cargos.cshtml", "Colaboradores.cshtml", "Contratos.cshtml",
        "Departamentos.cshtml", "Index.cshtml", "Gestao\\Dashboard.cshtml", "Gestao\\Monitor.cshtml",
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
    public void Cadastros_NaoExibeMaisOTituloMestre()
    {
        var v = ReadView("Cadastros.cshtml");
        Assert.DoesNotContain("Cadastros Mestre", v, StringComparison.Ordinal);
        Assert.Contains("Cadastros Organizacionais", v, StringComparison.Ordinal);
    }
}
