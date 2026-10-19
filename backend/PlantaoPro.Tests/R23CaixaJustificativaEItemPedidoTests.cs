using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using PlantaoPro.Web.Models;
using System.Text.RegularExpressions;
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

    // Tenant fixo compartilhado do módulo ADM360 (mesmo padrão R8-B4 / A3).
    private static readonly Guid Tenant = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid Usuario = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    // F-R15-C/A — espelhamento ADR-0003 (reflexivo, sem depender de endpoint): props Web ⊆ props
    // Api nos dois records novos (PedidoResumo/PedidoItemResumo x *ViewModel), incluindo o nível
    // aninhado de itens. O contrato R20Bff já cobre o mesmo par pelo dicionário de endpoints
    // ("api/administrativo360/compras"); este fato trava a grafia idêntica independentemente dele.
    [Fact]
    public void EspelhoAjuste_PedidoEItens_GrafiaIdenticaWebApi()
    {
        var propsWeb = typeof(PedidoCompraResumoViewModel).GetProperties().Select(p => p.Name.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        var propsApi = typeof(PedidoResumo).GetProperties().Select(p => p.Name.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        Assert.True(propsWeb.IsSubsetOf(propsApi),
            $"Props Web fora do espelho Api em PedidoCompraResumoViewModel: {string.Join(", ", propsWeb.Except(propsApi))}");
        Assert.Contains("itens", propsWeb);

        var elWeb = typeof(PedidoCompraResumoViewModel).GetProperty(nameof(PedidoCompraResumoViewModel.Itens))!
            .PropertyType.GetGenericArguments()[0];
        var elApi = typeof(PedidoResumo).GetProperty(nameof(PedidoResumo.Itens))!
            .PropertyType.GetGenericArguments()[0];
        var nestedWeb = elWeb.GetProperties().Select(p => p.Name.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        var nestedApi = elApi.GetProperties().Select(p => p.Name.ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        Assert.True(nestedWeb.IsSubsetOf(nestedApi),
            $"Props Web fora do espelho Api em PedidoItemResumoViewModel: {string.Join(", ", nestedWeb.Except(nestedApi))}");
        Assert.Contains("quantidaderecebida", nestedWeb);
    }

    // F-R15-C/B — fonte: a view expõe o seletor real de item (optgroups por pedido) e o hidden de
    // GUID vazio sumiu; o BFF agora rejeita pedidoItemId vazio antes de bater na API.
    [Fact]
    public void Fonte_Recebimentos_ItemDoPedidoSelecionavelEGuardaBff()
    {
        var raiz = RepositoryPathResolver.RepoRoot;

        var view = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Views", "Administrativo360", "Recebimentos.cshtml"));
        Assert.True(Regex.IsMatch(view, @"<select[^>]*name=""pedidoItemId"""),
            "form de recebimento sem <select name=\"pedidoItemId\"> — o campo volta a depender de preenchimento manual");
        Assert.DoesNotContain("<input type=\"hidden\" name=\"pedidoItemId\"", view); // legado com GUID vazio
        Assert.Contains("data-pedido=", view);                                     // optgroups por pedido p/ o filtro
        Assert.Contains("atualizarItensRecebimento", view);                         // filtro no change e nos botões da tabela

        var ctrl = File.ReadAllText(Path.Combine(raiz, "backend", "PlantaoPro.Web", "Controllers", "Administrativo360Controller.ComprasRecebimentos.cs"));
        var acao = ctrl.IndexOf("public async Task<IActionResult> ConfirmarRecebimento(", StringComparison.Ordinal);
        Assert.True(acao >= 0, "ação ConfirmarRecebimento não encontrada");
        Assert.Contains("pedidoItemId == Guid.Empty", ctrl.Substring(acao, 700));  // guarda no BFF
    }

    // F-R15-C/C — banco: ListarAsync passa a expor os itens por pedido (com saldo) e o saldo
    // atualiza após recebimento parcial — exatamente a leitura que alimenta o novo seletor.
    [Fact]
    public async Task Integracao_ListarCompras_ExpoaItensPorPedido_E_QuantidadeRecebidaAtualiza()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await using var probe = new NpgsqlConnection(cs);
            await probe.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para a suíte de integração não está acessível: {ex.Message}");
        }

        var suf = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var parceiro = Guid.NewGuid();
        var produto = Guid.NewGuid();
        var local = Guid.NewGuid();
        var repo = new ComprasRepository(cs);
        var pedido = Guid.Empty;
        try
        {
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
                    VALUES(@id, @t, 'Fornecedor R23 LTDA', @doc, true, true)",
                    new { id = parceiro, t = Tenant, doc = "R23F-" + suf });
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_produtos(id, tenant_id, sku, nome, unidade, controla_lote, controla_serie, exige_inspecao, preco_custo, ativo)
                    VALUES(@id, @t, @sku, 'Produto R23', 'UN', false, false, false, 10, true)",
                    new { id = produto, t = Tenant, sku = "R23P-" + suf });
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_locais(id, tenant_id, codigo, nome, tipo, ativo)
                    VALUES(@id, @t, @codigo, 'Local R23', 'INTERNO', true)",
                    new { id = local, t = Tenant, codigo = "R23L-" + suf });
            }

            pedido = await repo.CriarAsync(Tenant, Usuario,
                new CriarPedidoCommand(parceiro, null, 0m, new[] { new PedidoItemCommand(produto, 5m, 10m, 0m) }), CancellationToken.None);
            await repo.AprovarAsync(Tenant, Usuario, pedido, "APR-R23-" + suf, CancellationToken.None);

            var lista = await repo.ListarAsync(Tenant, null, "APROVADO", null, null, CancellationToken.None);
            var ped = lista.First(p => p.Id == pedido);
            Assert.Single(ped.Itens);
            var item = ped.Itens[0];
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.Equal(5m, item.Quantidade);
            Assert.Equal(0m, item.QuantidadeRecebida);
            Assert.Equal("R23P-" + suf, item.Sku);
            Assert.Equal("Produto R23", item.Produto);

            // recebe 2 de 5 pelo id do item (o que a UI agora envia do seletor) -> saldo reflete
            await repo.ReceberAsync(Tenant, Usuario,
                new ConfirmarRecebimentoCommand(pedido, "NF-R23-" + suf, "REC-R23-" + suf,
                    new[] { new ReceberItemCommand(item.Id, 2m, null, null, local) }, null), CancellationToken.None);

            var depois = (await repo.ListarAsync(Tenant, null, null, null, null, CancellationToken.None)).First(p => p.Id == pedido);
            Assert.Equal(2m, depois.Itens[0].QuantidadeRecebida);
        }
        finally
        {
            if (pedido != Guid.Empty || parceiro != Guid.Empty)
                await PurgarAsync(cs, parceiro, produto, local, pedido);
        }
    }

    // Purge escopado (ordem de dependência idêntica ao padrão R8-B4).
    private static async Task PurgarAsync(string cs, Guid parceiro, Guid produto, Guid local, Guid pedido)
    {
        await TestInfraRetry.ExecutarComRetryImpasseAsync(async () =>
        {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync("delete from plantaopro.adm360_documento_eventos where tenant_id=@t and documento_id in (select id from plantaopro.adm360_documentos_recebidos where tenant_id=@t and recebimento_id in (select id from plantaopro.adm360_recebimentos where pedido_id=@p))", new { t = Tenant, p = pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_inspecoes where tenant_id=@t and recebimento_item_id in (select id from plantaopro.adm360_recebimento_itens where recebimento_id in (select id from plantaopro.adm360_recebimentos where pedido_id=@p))", new { t = Tenant, p = pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_recebimento_itens where recebimento_id in (select id from plantaopro.adm360_recebimentos where pedido_id=@p)", new { p = pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_titulos_pagar where tenant_id=@t and origem_tipo='RECEBIMENTO_COMPRA' and origem_id in (select id from plantaopro.adm360_recebimentos where pedido_id=@p)", new { t = Tenant, p = pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_recebimentos where pedido_id=@p and tenant_id=@t", new { t = Tenant, p = pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_reservas where tenant_id=@t and produto_id=@prod", new { t = Tenant, prod = produto });
        await cn.ExecuteAsync("delete from plantaopro.adm360_movimentos where tenant_id=@t and local_id=@loc", new { t = Tenant, loc = local });
        await cn.ExecuteAsync("delete from plantaopro.adm360_lotes where tenant_id=@t and produto_id=@prod", new { t = Tenant, prod = produto });
        await cn.ExecuteAsync("delete from plantaopro.adm360_pedido_itens where pedido_id=@p and tenant_id=@t", new { t = Tenant, p = pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_pedidos where id=@p and tenant_id=@t", new { t = Tenant, p = pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_produtos where id=@prod and tenant_id=@t", new { t = Tenant, prod = produto });
        await cn.ExecuteAsync("delete from plantaopro.adm360_locais where id=@loc and tenant_id=@t", new { t = Tenant, loc = local });
        await cn.ExecuteAsync("delete from plantaopro.adm360_parceiros where id=@par and tenant_id=@t", new { t = Tenant, par = parceiro });
        });
    }
}
