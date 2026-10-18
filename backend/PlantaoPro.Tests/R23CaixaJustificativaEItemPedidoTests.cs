using System.Text.RegularExpressions;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R23 — fechamento dos achados herdados r15/r16 (docs/evidencias/2026-10-18-rodada-23/r23a0-*):
/// F14 (falso positivo): a cadeia FecharCaixa sempre teve justificativa de ponta a ponta (campo no
/// form já presente em 8669951; BFF binda e repassa; domain exige só com diferença ≠ 0) — o 400 da
/// smoke R16 era o comportamento correto da regra com campo vazio e a observação estava defasada da
/// superfície. Fatos permanentes abaixo travam a cadeia para evitar "ajuste" futuro sem alarme.
/// F-R15-C (r23b2): seletor de item do pedido na tela de Recebimentos (extensão de superfície
/// PedidoResumo.Itens — antes o hidden input ia Guid.Empty e a API devolvia 400 de saldo).
/// </summary>
public sealed class R23CaixaJustificativaEItemPedidoTests
{
    // F14-A — regra de domain: fato unitário puro (sem banco). Trava a semântica que a UI espelha:
    // justificativa é obrigatória APENAS quando |conferido − calculado| > 0.
    [Fact]
    public void Domain_ValidarFechamento_ExigeJustificativaApenasQuandoHaDiferenca()
    {
        var ex = Assert.Throws<ArgumentException>(() => CaixaRegras.ValidarFechamento(100m, 97m, null));
        Assert.Contains("A justificativa é obrigatória.", ex.Message);
        Assert.Throws<ArgumentException>(() => CaixaRegras.ValidarFechamento(100m, 97m, ""));
        Assert.Throws<ArgumentException>(() => CaixaRegras.ValidarFechamento(100m, 97m, "   "));
        Assert.Throws<ArgumentException>(() => CaixaRegras.ValidarFechamento(100m, 103m, null)); // simétrico: conferido > calculado

        CaixaRegras.ValidarFechamento(100m, 97m, "Divergência explicada na conferência física");
        CaixaRegras.ValidarFechamento(100m, 100m, null); // diferença zero: dispensada
        CaixaRegras.ValidarFechamento(100m, 100m, "qualquer texto");
    }

    // F14-B — fonte: trava a cadeia completa (campo DENTRO do form → assinatura binda → payload
    // encaminhado). Se qualquer elo sumir, o 400-surpresa da R16 volta como regressão silenciosa de UX.
    [Fact]
    public void Fonte_FecharCaixa_ChamadaJustificativa_DePontaAPonta()
    {
        var raiz = RepositoryPathResolver.RepoRoot;

        var view = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "Administrativo360", "FechamentoCaixa.cshtml"));
        var inicio = view.IndexOf("asp-action=\"FecharCaixa\"", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "form FecharCaixa não encontrado em FechamentoCaixa.cshtml");
        var fim = view.IndexOf("</form>", inicio, StringComparison.Ordinal);
        Assert.True(fim > inicio, "</form> do modal FecharCaixa não encontrado");
        var slice = view.Substring(inicio, fim - inicio);
        Assert.Contains("name=\"justificativa\"", slice); // o campo está dentro do form

        var ctrl = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.Financeiro.cs"));
        var acao = ctrl.IndexOf("public async Task<IActionResult> FecharCaixa(", StringComparison.Ordinal);
        Assert.True(acao >= 0, "ação FecharCaixa não encontrada no parcial Financeiro");
        var chamada = ctrl.IndexOf("caixa/fechar", acao, StringComparison.Ordinal);
        Assert.True(chamada > acao, "chamada à api caixa/fechar não encontrada após a assinatura da ação");
        var trecho = ctrl.Substring(acao, chamada - acao);
        Assert.True(Regex.IsMatch(trecho, @"string\s*\?\s*justificativa"), "assinatura da ação sem bind de 'string? justificativa'");
        Assert.Contains("Justificativa = justificativa", trecho); // encaminhado ao payload da api
    }
}
