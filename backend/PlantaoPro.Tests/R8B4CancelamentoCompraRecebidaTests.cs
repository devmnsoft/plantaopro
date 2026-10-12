using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B4 — Cancelamento de pedido de compra com compensação de efeitos (achado C3 da varredura
/// r8a0-varredura-aprovacoes-precos-cancelamento §21 / matriz linha 148): o módulo ADM360
/// só expunha Listar/Criar/Aprovar/Receber — pedido RECEBIDO não tinha como ser cancelado
/// sem ficar com estoque e título pendurados.
/// Correção (mesma política de cancelamento do módulo — vale consignação/inventário/venda):
/// - RASCUNHO/APROVADO: cancela direto (ainda sem efeitos).
/// - PARCIAL/RECEBIDO: cancela APENAS quando seguro — exige saldo físico integral por grupo
///   (produto, lote, local, condicao) da linha recebida e reserva ativa coberta após a saída
///   (linhas LIBERADO); título com pagamento exige estorno prévio (409 fundamentado, jamais
///   estorno fictício). Compensação = SAIDA espelhando a condição de origem + título
///   CANCELADO/saldo zerado + evento no documento fiscal vinculado (quando houver).
/// - Idempotente por estado (CANCELADO -> no-op), igual ao precedente de vale.
/// Migration v2347 adiciona a trilha de auditoria (motivo_cancelamento/cancelado_em/cancelado_por).
/// </summary>
public sealed class R8B4CancelamentoCompraRecebidaTests
{
    // Mesmo tenant fixo compartilhado do módulo ADM360 (padrão B1/A3); cada fato usa
    // parceiro/produto/local com sufixo único e purge escopado em try/finally.
    private static readonly Guid Tenant = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid Usuario = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    private static string ObterConnectionString() => TestDatabase.ConnectionString;

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static async Task GarantirConexaoBancoAsync(string cs)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para a suíte de integração não está acessível: {ex.Message}");
        }
    }

    // Padrão R8-B2: aplica a migration v2347 uma única vez (probe por coluna) sob lock de
    // conselheiro para serializar DDL entre classes paralelas.
    private static async Task GarantirSchemaAsync(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r8b4_cancelamento_compras'));");
        try
        {
            var temColuna = await cn.ExecuteScalarAsync<bool>(
                "select exists(select 1 from information_schema.columns where table_schema='plantaopro' and table_name='adm360_pedidos' and column_name='motivo_cancelamento')");
            if (!temColuna)
            {
                var migrationPath = Path.Combine(RepositoryPathResolver.RepoRoot,
                    "database/migrations/2026_10_v2347_r8b4_cancelamento_compras.sql");
                Assert.True(File.Exists(migrationPath), "Migration v2347 (cancelamento de compras) não encontrada.");
                await cn.ExecuteAsync(await File.ReadAllTextAsync(migrationPath));
            }
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r8b4_cancelamento_compras'));");
        }
    }

    private sealed class Fixture
    {
        public Guid Parceiro = Guid.Empty;
        public Guid Produto = Guid.Empty;
        public Guid Local = Guid.Empty;
        public Guid Pedido = Guid.Empty;
    }

    private static async Task SeedBaseAsync(string cs, Fixture f, decimal preco)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES(@id, @t, 'Fornecedor R8B4 LTDA', @doc, true, true)",
            new { id = f.Parceiro, t = Tenant, doc = "R8B4F-" + Sufixo() });
        // controla_lote=false (lote SEM-LOTE sintetizado no recebimento) e exige_inspecao=false
        // (linha criada LIBERADA direto — sem rota de inspeção neste corte).
        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_produtos(id, tenant_id, sku, nome, unidade, controla_lote, controla_serie, exige_inspecao, preco_custo, ativo)
            VALUES(@id, @t, @sku, 'Produto R8B4', 'UN', false, false, false, @preco, true)",
            new { id = f.Produto, t = Tenant, sku = "R8B4P-" + Sufixo(), preco });
        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_locais(id, tenant_id, codigo, nome, tipo, ativo)
            VALUES(@id, @t, @codigo, 'Local R8B4', 'INTERNO', true)",
            new { id = f.Local, t = Tenant, codigo = "R8B4L-" + Sufixo() });
    }

    private static async Task<Guid> ReceberAsync(string cs, ComprasRepository repo, Fixture f, decimal quantidade, CancellationToken ct)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        var itemId = await cn.ExecuteScalarAsync<Guid>(
            "SELECT id FROM plantaopro.adm360_pedido_itens WHERE pedido_id=@p AND tenant_id=@t",
            new { p = f.Pedido, t = Tenant });
        return await repo.ReceberAsync(Tenant, Usuario,
            new ConfirmarRecebimentoCommand(f.Pedido, "NF-R8B4-" + Sufixo(), "REC-R8B4-" + Sufixo(),
                new[] { new ReceberItemCommand(itemId, quantidade, null, null, f.Local) }, null), ct);
    }

    private static async Task<decimal> SaldoDoGrupoAsync(string cs, Fixture f)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<decimal>(@"
            SELECT COALESCE(sum(quantidade), 0) FROM plantaopro.adm360_movimentos
            WHERE tenant_id = @t AND produto_id = @p AND local_id = @loc",
            new { t = Tenant, p = f.Produto, loc = f.Local });
    }

    private static async Task PurgarAsync(string cs, Fixture f)
    {
        if (f.Parceiro == Guid.Empty) return;
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync("delete from plantaopro.adm360_documento_eventos where tenant_id=@t and documento_id in (select id from plantaopro.adm360_documentos_recebidos where tenant_id=@t and recebimento_id in (select id from plantaopro.adm360_recebimentos where pedido_id=@p))", new { t = Tenant, p = f.Pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_inspecoes where tenant_id=@t and recebimento_item_id in (select id from plantaopro.adm360_recebimento_itens where recebimento_id in (select id from plantaopro.adm360_recebimentos where pedido_id=@p))", new { t = Tenant, p = f.Pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_recebimento_itens where recebimento_id in (select id from plantaopro.adm360_recebimentos where pedido_id=@p)", new { p = f.Pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_titulos_pagar where tenant_id=@t and origem_tipo='RECEBIMENTO_COMPRA' and origem_id in (select id from plantaopro.adm360_recebimentos where pedido_id=@p)", new { t = Tenant, p = f.Pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_recebimentos where pedido_id=@p and tenant_id=@t", new { t = Tenant, p = f.Pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_reservas where tenant_id=@t and produto_id=@prod", new { t = Tenant, prod = f.Produto });
        await cn.ExecuteAsync("delete from plantaopro.adm360_movimentos where tenant_id=@t and local_id=@loc", new { t = Tenant, loc = f.Local });
        await cn.ExecuteAsync("delete from plantaopro.adm360_lotes where tenant_id=@t and produto_id=@prod", new { t = Tenant, prod = f.Produto });
        await cn.ExecuteAsync("delete from plantaopro.adm360_pedido_itens where pedido_id=@p and tenant_id=@t", new { t = Tenant, p = f.Pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_pedidos where id=@p and tenant_id=@t", new { t = Tenant, p = f.Pedido });
        await cn.ExecuteAsync("delete from plantaopro.adm360_produtos where id=@prod and tenant_id=@t", new { t = Tenant, prod = f.Produto });
        await cn.ExecuteAsync("delete from plantaopro.adm360_locais where id=@loc and tenant_id=@t", new { t = Tenant, loc = f.Local });
        await cn.ExecuteAsync("delete from plantaopro.adm360_parceiros where id=@par and tenant_id=@t", new { t = Tenant, par = f.Parceiro });
    }

    [Fact(DisplayName = "R8-B4: pedido recebido sem consumo cancela — reverte estoque, zera título e é idempotente")]
    public async Task recebido_sem_consumo_cancela_compensa_estoque_e_titulo_e_e_idempotente()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await GarantirSchemaAsync(cs);

        var repo = new ComprasRepository(cs);
        var ct = CancellationToken.None;
        var f = new Fixture { Parceiro = Guid.NewGuid(), Produto = Guid.NewGuid(), Local = Guid.NewGuid() };
        try
        {
            await SeedBaseAsync(cs, f, 10m);
            f.Pedido = await repo.CriarAsync(Tenant, Usuario,
                new CriarPedidoCommand(f.Parceiro, null, 0m, new[] { new PedidoItemCommand(f.Produto, 5m, 10m, 0m) }), ct);
            await repo.AprovarAsync(Tenant, Usuario, f.Pedido, "APR-R8B4-" + Sufixo(), ct);
            await ReceberAsync(cs, repo, f, 5m, ct);

            var saldoAntes = await SaldoDoGrupoAsync(cs, f);
            Assert.Equal(5m, saldoAntes);;
            const string motivo = "Duplicidade de fornecimento com pedido anterior";
            await repo.CancelarAsync(Tenant, Usuario, f.Pedido, motivo, ct);

            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var ped = await cn.QuerySingleAsync<dynamic>(
                    "SELECT situacao, motivo_cancelamento, cancelado_por, cancelado_em FROM plantaopro.adm360_pedidos WHERE id=@p AND tenant_id=@t",
                    new { p = f.Pedido, t = Tenant });
                Assert.Equal("CANCELADO", (string)ped.situacao);
                Assert.Equal(motivo, (string)ped.motivo_cancelamento);
                Assert.Equal(Usuario, (Guid)ped.cancelado_por);
                Assert.NotNull(ped.cancelado_em);

                var mov = await cn.QuerySingleOrDefaultAsync<dynamic>(@"
                    SELECT quantidade, condicao, origem_id FROM plantaopro.adm360_movimentos
                    WHERE tenant_id=@t AND produto_id=@prod AND origem_tipo='CANCELAMENTO_COMPRA'",
                    new { t = Tenant, prod = f.Produto });
                Assert.NotNull(mov);
                Assert.Equal(-5m, (decimal)mov.quantidade);
                Assert.Equal("LIBERADO", (string)mov.condicao);
                Assert.Equal(f.Pedido, (Guid)mov.origem_id);

                var titulo = await cn.QuerySingleAsync<dynamic>(@"
                    SELECT situacao, saldo_aberto, valor_pago, observacoes FROM plantaopro.adm360_titulos_pagar
                    WHERE tenant_id=@t AND origem_tipo='RECEBIMENTO_COMPRA'
                      AND origem_id IN (SELECT id FROM plantaopro.adm360_recebimentos WHERE pedido_id=@p)",
                    new { t = Tenant, p = f.Pedido });
                Assert.Equal("CANCELADO", (string)titulo.situacao);
                Assert.Equal(0m, (decimal)titulo.saldo_aberto);
                Assert.Equal(0m, (decimal)titulo.valor_pago);
                Assert.Contains("Título cancelado", (string)titulo.observacoes, StringComparison.OrdinalIgnoreCase);
            }
            Assert.Equal(0m, await SaldoDoGrupoAsync(cs, f));

            // Reenvio idempotente: mesmo pedido já CANCELADO -> no-op, sem movimento duplicado.
            await repo.CancelarAsync(Tenant, Usuario, f.Pedido, motivo, ct);
            await using (var cn2 = new NpgsqlConnection(cs))
            {
                await cn2.OpenAsync();
                var qtd = await cn2.ExecuteScalarAsync<int>(
                    "SELECT count(1) FROM plantaopro.adm360_movimentos WHERE tenant_id=@t AND produto_id=@prod AND origem_tipo='CANCELAMENTO_COMPRA'",
                    new { t = Tenant, prod = f.Produto });
                Assert.Equal(1, qtd);
            }
        }
        finally
        {
            await PurgarAsync(cs, f);
        }
    }

    [Fact(DisplayName = "R8-B4: estoque parcialmente consumido bloqueia o cancelamento (409) e mantém registro intacto")]
    public async Task estoque_consumido_bloqueia_com_erro_fundamentado_e_registro_intacto()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await GarantirSchemaAsync(cs);

        var repo = new ComprasRepository(cs);
        var ct = CancellationToken.None;
        var f = new Fixture { Parceiro = Guid.NewGuid(), Produto = Guid.NewGuid(), Local = Guid.NewGuid() };
        try
        {
            await SeedBaseAsync(cs, f, 10m);
            f.Pedido = await repo.CriarAsync(Tenant, Usuario,
                new CriarPedidoCommand(f.Parceiro, null, 0m, new[] { new PedidoItemCommand(f.Produto, 5m, 10m, 0m) }), ct);
            await repo.AprovarAsync(Tenant, Usuario, f.Pedido, "APR-R8B4-" + Sufixo(), ct);
            await ReceberAsync(cs, repo, f, 5m, ct);

            // Consumo simulado de 2 unidades sobre o lote do recebimento.
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var lote = await cn.ExecuteScalarAsync<Guid>(
                    "SELECT lote_id FROM plantaopro.adm360_recebimento_itens ri JOIN plantaopro.adm360_recebimentos r ON r.id=ri.recebimento_id WHERE r.pedido_id=@p",
                    new { p = f.Pedido });
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_movimentos(
                        id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, motivo,
                        origem_tipo, origem_id, idempotency_key, created_by
                    ) VALUES (gen_random_uuid(), @t, @prod, @lote, @loc, 'SAIDA', 'LIBERADO', -2,
                        'Consumo simulado', 'VALE_CONSUMO', gen_random_uuid(), @key, @user)",
                    new { t = Tenant, prod = f.Produto, lote, loc = f.Local, key = "R8B4C-" + Sufixo(), user = Usuario });
            }

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.CancelarAsync(Tenant, Usuario, f.Pedido, "Estoque ja movimentado", ct));
            Assert.Contains("estoque", ex.Message, StringComparison.OrdinalIgnoreCase);

            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var situacao = await cn.ExecuteScalarAsync<string>(
                    "SELECT situacao FROM plantaopro.adm360_pedidos WHERE id=@p AND tenant_id=@t", new { p = f.Pedido, t = Tenant });
                Assert.Equal("RECEBIDO", situacao);
                var compensatorios = await cn.ExecuteScalarAsync<int>(
                    "SELECT count(1) FROM plantaopro.adm360_movimentos WHERE tenant_id=@t AND produto_id=@prod AND origem_tipo='CANCELAMENTO_COMPRA'",
                    new { t = Tenant, prod = f.Produto });
                Assert.Equal(0, compensatorios);
            }
            Assert.Equal(3m, await SaldoDoGrupoAsync(cs, f));
        }
        finally
        {
            await PurgarAsync(cs, f);
        }
    }

    [Fact(DisplayName = "R8-B4: título pago exige estorno prévio (409) — nada é compensado")]
    public async Task titulo_pago_exige_estorno_previo_e_bloqueia()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await GarantirSchemaAsync(cs);

        var repo = new ComprasRepository(cs);
        var ct = CancellationToken.None;
        var f = new Fixture { Parceiro = Guid.NewGuid(), Produto = Guid.NewGuid(), Local = Guid.NewGuid() };
        try
        {
            await SeedBaseAsync(cs, f, 10m);
            f.Pedido = await repo.CriarAsync(Tenant, Usuario,
                new CriarPedidoCommand(f.Parceiro, null, 0m, new[] { new PedidoItemCommand(f.Produto, 5m, 10m, 0m) }), ct);
            await repo.AprovarAsync(Tenant, Usuario, f.Pedido, "APR-R8B4-" + Sufixo(), ct);
            await ReceberAsync(cs, repo, f, 5m, ct);

            // Pagamento parcial simulado no título (estado que o PagarAsync produz).
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await cn.ExecuteAsync(@"
                    UPDATE plantaopro.adm360_titulos_pagar
                    SET valor_pago = 25, saldo_aberto = 25, situacao = 'PARCIAL', versao = versao + 1
                    WHERE tenant_id=@t AND origem_tipo='RECEBIMENTO_COMPRA'
                      AND origem_id IN (SELECT id FROM plantaopro.adm360_recebimentos WHERE pedido_id=@p)",
                    new { t = Tenant, p = f.Pedido });
            }

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.CancelarAsync(Tenant, Usuario, f.Pedido, "Queria cancelar, mas ha pagamento", ct));
            Assert.Contains("estorne", ex.Message, StringComparison.OrdinalIgnoreCase);

            string situacaoFinal;
            await using (var cn2 = new NpgsqlConnection(cs))
            {
                await cn2.OpenAsync();
                situacaoFinal = await cn2.ExecuteScalarAsync<string>(
                    "SELECT situacao FROM plantaopro.adm360_pedidos WHERE id=@p AND tenant_id=@t", new { p = f.Pedido, t = Tenant });
            }
            Assert.Equal("RECEBIDO", situacaoFinal);
            Assert.Equal(5m, await SaldoDoGrupoAsync(cs, f));
        }
        finally
        {
            await PurgarAsync(cs, f);
        }
    }

    [Fact(DisplayName = "R8-B4: pedido PARCIAL cancela compensando apenas o que foi recebido")]
    public async Task pedido_parcial_compensa_apenas_o_que_foi_recebido()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await GarantirSchemaAsync(cs);

        var repo = new ComprasRepository(cs);
        var ct = CancellationToken.None;
        var f = new Fixture { Parceiro = Guid.NewGuid(), Produto = Guid.NewGuid(), Local = Guid.NewGuid() };
        try
        {
            await SeedBaseAsync(cs, f, 10m);
            f.Pedido = await repo.CriarAsync(Tenant, Usuario,
                new CriarPedidoCommand(f.Parceiro, null, 0m, new[] { new PedidoItemCommand(f.Produto, 10m, 10m, 0m) }), ct);
            await repo.AprovarAsync(Tenant, Usuario, f.Pedido, "APR-R8B4-" + Sufixo(), ct);
            await ReceberAsync(cs, repo, f, 4m, ct); // 4 de 10 -> PARCIAL

            await using (var cnCheck = new NpgsqlConnection(cs))
            {
                await cnCheck.OpenAsync();
                Assert.Equal("PARCIAL", await cnCheck.ExecuteScalarAsync<string>(
                    "SELECT situacao FROM plantaopro.adm360_pedidos WHERE id=@p AND tenant_id=@t", new { p = f.Pedido, t = Tenant }));
            }

            await repo.CancelarAsync(Tenant, Usuario, f.Pedido, "Fornecedor remanescente nao vai entregar", ct);

            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                Assert.Equal("CANCELADO", await cn.ExecuteScalarAsync<string>(
                    "SELECT situacao FROM plantaopro.adm360_pedidos WHERE id=@p AND tenant_id=@t", new { p = f.Pedido, t = Tenant }));
                var mov = await cn.QuerySingleOrDefaultAsync<dynamic>(@"
                    SELECT quantidade FROM plantaopro.adm360_movimentos
                    WHERE tenant_id=@t AND produto_id=@prod AND origem_tipo='CANCELAMENTO_COMPRA'",
                    new { t = Tenant, prod = f.Produto });
                Assert.NotNull(mov);
                Assert.Equal(-4m, (decimal)mov.quantidade); // compensação espelha o recebido (4), não o pedido (10)
                var titulo = await cn.ExecuteScalarAsync<decimal>(@"
                    SELECT saldo_aberto FROM plantaopro.adm360_titulos_pagar
                    WHERE tenant_id=@t AND origem_tipo='RECEBIMENTO_COMPRA'
                      AND origem_id IN (SELECT id FROM plantaopro.adm360_recebimentos WHERE pedido_id=@p)",
                    new { t = Tenant, p = f.Pedido });
                Assert.Equal(0m, titulo);
            }
            Assert.Equal(0m, await SaldoDoGrupoAsync(cs, f));
        }
        finally
        {
            await PurgarAsync(cs, f);
        }
    }

    [Fact(DisplayName = "R8-B4: rascunho cancela sem efeitos e trava a aprovação posterior")]
    public async Task rascunho_cancela_sem_efitos_e_trava_aprovar_posterior()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await GarantirSchemaAsync(cs);

        var repo = new ComprasRepository(cs);
        var ct = CancellationToken.None;
        var f = new Fixture { Parceiro = Guid.NewGuid(), Produto = Guid.NewGuid(), Local = Guid.NewGuid() };
        try
        {
            await SeedBaseAsync(cs, f, 10m);
            f.Pedido = await repo.CriarAsync(Tenant, Usuario,
                new CriarPedidoCommand(f.Parceiro, null, 0m, new[] { new PedidoItemCommand(f.Produto, 5m, 10m, 0m) }), ct);

            await repo.CancelarAsync(Tenant, Usuario, f.Pedido, "Pedido criado por engano", ct);

            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                Assert.Equal("CANCELADO", await cn.ExecuteScalarAsync<string>(
                    "SELECT situacao FROM plantaopro.adm360_pedidos WHERE id=@p AND tenant_id=@t", new { p = f.Pedido, t = Tenant }));
                Assert.Equal(0, await cn.ExecuteScalarAsync<int>(
                    "SELECT count(1) FROM plantaopro.adm360_movimentos WHERE tenant_id=@t AND produto_id=@prod", new { t = Tenant, prod = f.Produto }));
                Assert.Equal(0, await cn.ExecuteScalarAsync<int>(@"
                    SELECT count(1) FROM plantaopro.adm360_titulos_pagar
                    WHERE tenant_id=@t AND origem_id IN (SELECT id FROM plantaopro.adm360_recebimentos WHERE pedido_id=@p)",
                    new { t = Tenant, p = f.Pedido }));
            }

            // Estado terminal respeitado a jusante: aprovação exige RASCUNHO.
            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.AprovarAsync(Tenant, Usuario, f.Pedido, "APR-R8B4-" + Sufixo(), ct));
        }
        finally
        {
            await PurgarAsync(cs, f);
        }
    }

    [Fact(DisplayName = "R8-B4: reserva ativa sobre o lote do recebimento bloqueia o cancelamento (invariante de disponível)")]
    public async Task reserva_ativa_bloqueia_o_cancelamento()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await GarantirSchemaAsync(cs);

        var repo = new ComprasRepository(cs);
        var ct = CancellationToken.None;
        var f = new Fixture { Parceiro = Guid.NewGuid(), Produto = Guid.NewGuid(), Local = Guid.NewGuid() };
        try
        {
            await SeedBaseAsync(cs, f, 10m);
            f.Pedido = await repo.CriarAsync(Tenant, Usuario,
                new CriarPedidoCommand(f.Parceiro, null, 0m, new[] { new PedidoItemCommand(f.Produto, 5m, 10m, 0m) }), ct);
            await repo.AprovarAsync(Tenant, Usuario, f.Pedido, "APR-R8B4-" + Sufixo(), ct);
            await ReceberAsync(cs, repo, f, 5m, ct);

            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var lote = await cn.ExecuteScalarAsync<Guid>(@"
                    SELECT ri.lote_id FROM plantaopro.adm360_recebimento_itens ri
                    JOIN plantaopro.adm360_recebimentos r ON r.id = ri.recebimento_id WHERE r.pedido_id=@p",
                    new { p = f.Pedido });
                // Único produto do fixture: a linha recebida é sempre dele.
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_reservas(id, tenant_id, produto_id, lote_id, local_id, quantidade, situacao, origem_tipo, origem_id, idempotency_key)
                    VALUES(gen_random_uuid(), @t, @prod, @lote, @loc, 3, 'ATIVA', 'R8B4_TESTE', gen_random_uuid(), @key)",
                    new { t = Tenant, prod = f.Produto, lote, loc = f.Local, key = "R8B4R-" + Sufixo() });
            }

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.CancelarAsync(Tenant, Usuario, f.Pedido, "Havia reserva ativa", ct));
            Assert.Contains("reserva", ex.Message, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(5m, await SaldoDoGrupoAsync(cs, f));
        }
        finally
        {
            await PurgarAsync(cs, f);
        }
    }

    [Fact(DisplayName = "R8-B4: motivo obrigatório — vazio gera ArgumentException antes de tocar no banco")]
    public async Task sem_motivo_exige_argumentexception()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await GarantirSchemaAsync(cs);

        var repo = new ComprasRepository(cs);
        var f = new Fixture { Parceiro = Guid.NewGuid(), Produto = Guid.NewGuid(), Local = Guid.NewGuid() };
        try
        {
            await SeedBaseAsync(cs, f, 10m);
            f.Pedido = await repo.CriarAsync(Tenant, Usuario,
                new CriarPedidoCommand(f.Parceiro, null, 0m, new[] { new PedidoItemCommand(f.Produto, 5m, 10m, 0m) }), CancellationToken.None);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                repo.CancelarAsync(Tenant, Usuario, f.Pedido, "   ", CancellationToken.None));
        }
        finally
        {
            await PurgarAsync(cs, f);
        }
    }

    [Fact(DisplayName = "R8-B4 contrato: endpoint, interface, action web, view e migration estão presentes na fonte")]
    public void contrato_endpoint_interface_web_e_migration_presentes_na_fonte()
    {
        var root = RepositoryPathResolver.RepoRoot;

        var ctrl = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Api/Controllers/Adm360SuprimentosControllers.cs"));
        Assert.Contains("[HttpPost(\"{id:guid}/cancelar\")]", ctrl);
        Assert.Contains("CancelarAsync(x.Tenant,x.User,id,motivo,ct)", ctrl);

        var contrato = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Application/Administrativo360/Contracts.cs"));
        Assert.Contains("Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid pedidoId, string motivo, CancellationToken ct);", contrato);

        var web = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Web/Controllers/Administrativo360Controller.ComprasRecebimentos.cs"));
        Assert.Contains("CancelarPedidoCompra", web);
        Assert.Contains("api/administrativo360/compras/{id}/cancelar", web);

        var view = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Web/Views/Administrativo360/PedidosCompra.cshtml"));
        Assert.Contains("asp-action=\"CancelarPedidoCompra\"", view);
        Assert.Contains("modalCancelar-", view);
        Assert.Contains("name=\"motivo\"", view);

        var migration = File.ReadAllText(Path.Combine(root, "database/migrations/2026_10_v2347_r8b4_cancelamento_compras.sql"));
        Assert.Contains("motivo_cancelamento", migration);

        // Paridade i18n das 3 novas chaves nos 4 idiomas.
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, "backend/PlantaoPro.Web/Localization", arquivo));
            Assert.Contains("Adm360.Compras.ErrCancelMotivo", resx);
            Assert.Contains("Adm360.Compras.OkOrderCancelled", resx);
            Assert.Contains("Adm360.Compras.FallbackCancelOrder", resx);
        }
    }
}
