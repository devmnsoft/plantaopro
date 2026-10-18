using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R30 (diretriz r24a1, epic E5 parte 1 — validação dos formulários de cadastro, decisão D-R30-1 em
/// docs/evidencias/2026-10-18-rodada-30/r30a0-validacao-cadastros-plano.md): os modais rápidos de
/// Cadastros.cshtml (parceiro/produto/local) ganharam tetos = tamanho exato da coluna (fonte:
/// information_schema do banco dev), pattern de formato fechado (CNPJ/CPF, EAN/GTIN) e piso min="0"
/// para preço; Produtos.cshtml teve dois SOBRETETOS corrigidos (sku 60→40, nome 200→180 — o usuário
/// passava a digitar o que o banco rejeita). Este fato trava o CONTRATO DE ATRIBUTO (nome do campo →
/// atributo esperado), não strings: renomear texto de label não quebra; perder o teto/pattern/required
/// ou voltar o sobreteto quebra. Os cadastros detalhados (Parceiros, Locais, Cargos, Colaboradores,
/// Departamentos) já eram válidos — auditados no r30a0 e sem alteração.
/// </summary>
public class R30ValidacaoFormulariosCadastrosContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void Cadastros_ModaisRapidos_TemosTetoEPatternDoFormato()
    {
        var v = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Cadastros.cshtml");

        // Modal parceiro: nome = coluna 160; documento CNPJ/CPF = coluna 20 com formato fechado.
        Assert.Matches(@"name=""nome""[^>]*maxlength=""160""[^>]*required", v);
        Assert.Matches(@"name=""documento""[^>]*maxlength=""20""[^>]*pattern=""\[0-9./ -\]\*""", v);

        // Modal produto: sku 40, nome 180, unidade 12, precoCusto com piso 0, codigoBarras EAN/GTIN 80.
        Assert.Matches(@"name=""sku""[^>]*maxlength=""40""[^>]*required", v);
        Assert.Matches(@"name=""nome""[^>]*maxlength=""180""[^>]*required", v);
        Assert.Matches(@"name=""unidade""[^>]*maxlength=""12""[^>]*required", v);
        Assert.Matches(@"step=""0\.01"" min=""0"" name=""precoCusto""", v);
        Assert.Matches(@"name=""codigoBarras""[^>]*maxlength=""80""[^>]*pattern=""\[0-9 -\]\*""", v);

        // Modal local: codigo 30, nome 120.
        Assert.Matches(@"name=""codigo""[^>]*maxlength=""30""[^>]*required", v);
        Assert.Matches(@"name=""nome""[^>]*maxlength=""120""[^>]*required", v);
    }

    [Fact]
    public void Cadastros_ProdutosDetalhe_SobretetosCorrigidosParaOTamanhoDaColuna()
    {
        var v = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Produtos.cshtml");

        Assert.Matches(@"id=""produtoSku""[^>]*required[^>]*maxlength=""40""", v);
        Assert.Matches(@"id=""produtoNome""[^>]*required[^>]*maxlength=""180""", v);

        // Sobretetos antigos (60 > coluna 40; 200 > coluna 180) não podem voltar nesses campos.
        Assert.DoesNotMatch(@"id=""produtoSku""[^>]*maxlength=""60""", v);
        Assert.DoesNotMatch(@"id=""produtoNome""[^>]*maxlength=""200""", v);
    }
}
