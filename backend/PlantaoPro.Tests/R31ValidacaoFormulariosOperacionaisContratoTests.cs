using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R31 (diretriz r24a1, epic E5 parte 2 — validação dos formulários operacionais, decisão D-R30-1):
/// tetos = tamanho exato da coluna (fonte: information_schema do banco dev) nos campos de operação de
/// estoque e financeiro Adm360, piso min para valores monetários/quantitativos de entrada, e o residual
/// E1 "ID do Produto" em placeholder. Colunas text (motivo/justificativa/descricao/observacoes/referência)
/// e campos controlados por select ficam whitelist — ver r31a0. Este fato trava o CONTRATO DE ATRIBUTO
/// por arquivo: perder teto/piso ou voltar o placeholder quebra a suíte.
/// </summary>
public class R31ValidacaoFormulariosOperacionaisContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void EstoqueOperacional_TetosDaColunaEmRecebimentoColetaEPedido()
    {
        var recebimentos = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Recebimentos.cshtml");
        Assert.Matches(@"name=""documento""[^>]*maxlength=""80""", recebimentos);
        Assert.Matches(@"name=""lote""[^>]*maxlength=""80""", recebimentos);

        var coleta = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "Coleta.cshtml");
        Assert.Matches(@"name=""codigo""[^>]*maxlength=""100""", coleta);
        Assert.Matches(@"name=""lote""[^>]*maxlength=""80""", coleta);

        var pedidos = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "PedidosCompra.cshtml");
        Assert.Contains("step=\"0.01\" min=\"0\" name=\"frete\"", pedidos, StringComparison.Ordinal);
    }

    [Fact]
    public void FinanceiroEOrçamento_TetosEPisosNosCamposOperacionais()
    {
        var fluxo = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "FluxoCaixa.cshtml");
        Assert.Matches(@"id=""nomeConta""[^>]*maxlength=""120""", fluxo);
        Assert.Matches(@"id=""bancoConta""[^>]*maxlength=""40""", fluxo);
        Assert.Matches(@"id=""agenciaConta""[^>]*maxlength=""20""", fluxo);
        Assert.Matches(@"id=""numeroConta""[^>]*maxlength=""30""", fluxo);

        var contas = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "ContasFinanceiras.cshtml");
        Assert.Contains("value=\"@c.Nome\" maxlength=\"120\" required />", contas, StringComparison.Ordinal);
        Assert.Contains("value=\"@c.Banco\" maxlength=\"40\" />", contas, StringComparison.Ordinal);
        Assert.Contains("value=\"@c.Agencia\" maxlength=\"20\" />", contas, StringComparison.Ordinal);
        Assert.Contains("value=\"@c.Conta\" maxlength=\"30\" />", contas, StringComparison.Ordinal);
        Assert.Contains("Ex: Santander - Conta Operacional\" maxlength=\"120\"", contas, StringComparison.Ordinal);
        Assert.Contains("Ex: 033 - Santander\" maxlength=\"40\"", contas, StringComparison.Ordinal);
        Assert.Contains("Ex: 1234\" maxlength=\"20\"", contas, StringComparison.Ordinal);
        Assert.Contains("Ex: 56789-0\" maxlength=\"30\"", contas, StringComparison.Ordinal);

        var titulo = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "TituloDetalhes.cshtml");
        Assert.Contains("step=\"0.01\" min=\"0.01\" id=\"valorRecebido\"", titulo, StringComparison.Ordinal);

        var tituloPagar = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "TituloPagarDetalhes.cshtml");
        Assert.Contains("placeholder=\"Ex: Aut 982341\" maxlength=\"80\"", tituloPagar, StringComparison.Ordinal);

        var despesa = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "NovaDespesa.cshtml");
        Assert.Contains("placeholder=\"Ex: NF-e 4589\" maxlength=\"80\"", despesa, StringComparison.Ordinal);
        Assert.Contains("LOGÍSTICA, TI\" maxlength=\"80\"", despesa, StringComparison.Ordinal);

        var orcNovo = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "OrcamentoNovo.cshtml");
        Assert.Matches(@"name=""Procedimento""[^>]*maxlength=""180""", orcNovo);
        Assert.Matches(@"name=""VendedorId""[^>]*maxlength=""36""", orcNovo);

        var orcEd = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "OrcamentoEditar.cshtml");
        Assert.Matches(@"name=""Procedimento""[^>]*required[^>]*maxlength=""180""", orcEd);
        Assert.Matches(@"name=""VendedorId""[^>]*maxlength=""36""", orcEd);
        Assert.DoesNotContain("ID do Produto", orcEd, StringComparison.Ordinal);
        // Nota R32: o placeholder "Código do produto" deixou de existir quando o campo virou select de
        // lookup (R32LookupsCampoDeIdContratoTests cobre a estrutura nova).
    }
}
