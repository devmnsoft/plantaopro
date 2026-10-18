using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R32 (diretriz r24a1, epic E6 parte 1 — "listas buscam por nome / salvam id"): a tela de edição de
/// orçamento usava inputs text para o usuário digitar UUID de hospital/pagador/médico/produto porque o
/// GET não chamava PreencherLookupsOrcamentoAsync (os arrays do VM chegam vazios). Correção: o GET
/// espelha o NOVO e a view usa selects com o mesmo markup. Este fato trava o CONTRATO DE ESTRUTURA:
/// o helper no corpo do GET + selects na view (e a ausência dos inputs text dos mesmos nomes). Os 7
/// campos restantes sem lookup por ação ficam na decisão D-E6-2 (fila R33) — ver
/// docs/evidencias/2026-10-18-rodada-32/r32a0-e6-lookups-plano.md.
/// </summary>
public class R32LookupsCampoDeIdContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void OrcamentoEditar_GetCarregaLookupsComoONovo()
    {
        var controller = ReadRelative("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Orcamentos.cs");

        // Isolando o corpo do GET OrcamentoEditar (até o próximo atributo HttpPost/HttpGet).
        int inicio = controller.IndexOf("OrcamentoEditar(Guid id)", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "GET OrcamentoEditar não encontrado no controller");
        int fim = controller.IndexOf("[HttpPost", inicio, StringComparison.Ordinal);
        Assert.True(fim > inicio, "não localizei o fim do GET OrcamentoEditar");
        var corpoGet = controller.Substring(inicio, fim - inicio);

        Assert.Contains("await PreencherLookupsOrcamentoAsync(client, form);", corpoGet, StringComparison.Ordinal);
    }

    [Fact]
    public void OrcamentoEditar_ViewUsaSelectsENaoMaisInputDeIdLivre()
    {
        var v = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "OrcamentoEditar.cshtml");

        // Selects presentes (campos que antes eram texto com UUID colado na mão).
        Assert.Matches(@"<select name=""HospitalId""[^>]*required", v);
        Assert.Matches(@"<select name=""ResponsavelFinanceiroId""[^>]*required", v);
        Assert.Matches(@"<select name=""MedicoId""", v);
        Assert.Contains("<select name=\"Itens[@i].ProdutoId\" class=\"form-select", v, StringComparison.Ordinal);
        Assert.Contains("produtoOptionsTemplate", v, StringComparison.Ordinal);

        // Inputs text dos mesmos nomes não podem voltar (regressão ao digitar UUID).
        Assert.DoesNotMatch(@"<input type=""text"" name=""HospitalId""", v);
        Assert.DoesNotMatch(@"<input type=""text"" name=""ResponsavelFinanceiroId""", v);
        Assert.DoesNotMatch(@"<input type=""text"" name=""MedicoId""", v);
        Assert.DoesNotMatch(@"<input type=""text"" name=""Itens\[\$\{index\}\]\.ProdutoId""", v);
        Assert.DoesNotMatch(@"<input type=""text"" name=""Itens\[@i\]\.ProdutoId""", v);
    }
}
