using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R33 (diretriz r24a1, epic E6 parte 2, decisão D-E6-2): os últimos campos de ID colado na mão
/// viraram escolha por nome usando endpoints que JÁ existiam na API — vale escolhe a reserva ativa do
/// planejamento do orçamento aprovado (GET reserva-planejamento) e o modal de vínculo de XML escolhe
/// pedido de compra + local de estoque em selects. Este fato trava o CONTRATO DE ESTRUTURA nos 3 camadas
/// (view select + controller chamando o endpoint certo + VM expondo a lista) contra regressão a input de
/// UUID. KEEP documentados no r33a0: Cotacoes ProdutoId manual (s8), Relacionamento EntidadeInternaId
/// (entidade genérica), VendedorId (sem endpoint de vendedores). Inventário completo em
/// docs/evidencias/2026-10-18-rodada-33/r33a0-e6-lookups-por-acao-plano.md.
/// </summary>
public class R33E6LookupsReservaEPedidoContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void ValeNovo_ReservaAtivaEscolhidaPorNome()
    {
        var v = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "ValeNovo.cshtml");

        Assert.Matches(@"<select name=""Itens\[0\]\.ReservaId""[^>]*select-reserva", v);
        Assert.Contains("tmplReservaOptions", v, StringComparison.Ordinal);
        Assert.Contains("-- Sem reserva --", v, StringComparison.Ordinal);
        // O input de UUID (placeholder) não pode voltar nas duas linhas (server + template JS).
        Assert.DoesNotContain("Código da reserva ativa", v, StringComparison.Ordinal);

        var controller = ReadRelative("backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.cs");
        Assert.Contains("reserva-planejamento", controller, StringComparison.Ordinal);

        var models = ReadRelative("backend", "PlantaoPro.Web", "Models", "Administrativo360Models.cs");
        Assert.Contains("public IReadOnlyList<ReservaAtivaPlanejamentoViewModel> ReservasAtivas { get; set; }", models, StringComparison.Ordinal);
        // O registro consumido pela view e o mesmo espelho ja versionado no contrato BFF (R20/ADR-0003)
        // do endpoint reserva-planejamento — sem tipo duplicado.
        Assert.Contains("public sealed record ReservaAtivaPlanejamentoViewModel(", models, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentoXml_VinculoEscolhePedidoELocalPorNome()
    {
        var v = ReadRelative("backend", "PlantaoPro.Web", "Views", "Administrativo360", "DocumentosXml", "Detalhes.cshtml");

        Assert.Matches(@"<select name=""PedidoId""[^>]*required", v);
        Assert.Matches(@"<select name=""LocalId""[^>]*required", v);
        // O placeholder de UUID zero dos dois campos não pode voltar.
        Assert.DoesNotContain("00000000-0000-0000-0000-000000000000", v, StringComparison.Ordinal);

        var vm = ReadRelative("backend", "PlantaoPro.Web", "Models", "Administrativo360CotacoesXmlModels.cs");
        Assert.Contains("public IReadOnlyList<PedidoCompraResumoViewModel> Pedidos { get; init; }", vm, StringComparison.Ordinal);
        Assert.Contains("public IReadOnlyList<Local360ViewModel> Locais { get; init; }", vm, StringComparison.Ordinal);

        var controller = ReadRelative("backend", "PlantaoPro.Web", "Controllers", "Adm360DocumentosXmlWebController.cs");
        Assert.Contains("\"api/administrativo360/compras\"", controller, StringComparison.Ordinal);
        Assert.Contains("\"api/administrativo360/cadastros/lookups\"", controller, StringComparison.Ordinal);
    }
}
