using Dapper;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class ComprasRepository : Adm360Repository, IComprasRepository
{
    private sealed class ReceivingRow
    {
        public Guid ProdutoId { get; set; }
        public bool ControlaLote { get; set; }
        public bool Inspecao { get; set; }
        public decimal Saldo { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Desconto { get; set; }
        public decimal QtdTotal { get; set; }
    }

    public ComprasRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<PedidoResumo>> ListarAsync(Guid tenantId, string? fornecedor, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<PedidoResumo>(new CommandDefinition(@"
            SELECT p.id, p.numero, f.nome AS fornecedor, p.situacao,
                   (SELECT COALESCE(SUM(i.quantidade * i.preco_unitario - i.desconto), 0) FROM plantaopro.adm360_pedido_itens i WHERE i.pedido_id = p.id) + p.frete AS total,
                   p.created_at AS criadoem
            FROM plantaopro.adm360_pedidos p
            JOIN plantaopro.adm360_parceiros f ON f.id = p.fornecedor_id AND f.tenant_id = p.tenant_id
            WHERE p.tenant_id = @tenantId
              AND (@fornecedor IS NULL OR f.nome ILIKE '%' || @fornecedor || '%')
              AND (@situacao IS NULL OR p.situacao = @situacao)
              AND (@inicio IS NULL OR p.created_at::date >= @inicio)
              AND (@fim IS NULL OR p.created_at::date <= @fim)
            ORDER BY p.created_at DESC", new { tenantId, fornecedor, situacao, inicio, fim }, cancellationToken: ct))).AsList();
    }

    public async Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarPedidoCommand c, CancellationToken ct)
    {
        if (c.Frete < 0 || c.Itens.Count == 0 || c.Itens.Any(x => x.Quantidade <= 0 || x.PrecoUnitario < 0 || x.Desconto < 0))
            throw new ArgumentException("Informe itens positivos e valores não negativos.");

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var fornecedor = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @id AND tenant_id = @tenantId AND ativo AND fornecedor)",
            new { id = c.FornecedorId, tenantId }, tx, cancellationToken: ct));
        if (!fornecedor) throw new ArgumentException("Fornecedor inativo ou não acessível.");

        var produtos = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT count(*) FROM plantaopro.adm360_produtos WHERE tenant_id = @tenantId AND ativo AND id = ANY(@ids)",
            new { tenantId, ids = c.Itens.Select(x => x.ProdutoId).Distinct().ToArray() }, tx, cancellationToken: ct));
        if (produtos != c.Itens.Select(x => x.ProdutoId).Distinct().Count())
            throw new ArgumentException("Há produto inativo ou não acessível.");

        var id = Guid.NewGuid();
        await cn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO plantaopro.adm360_pedidos(id, tenant_id, numero, fornecedor_id, previsao, frete, created_by) VALUES(@id, @tenantId, 'PC-' || nextval('plantaopro.adm360_pedido_numero'), @FornecedorId, @Previsao, @Frete, @usuarioId)",
            new { id, tenantId, c.FornecedorId, c.Previsao, c.Frete, usuarioId }, tx, cancellationToken: ct));

        foreach (var item in c.Itens)
        {
            await cn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO plantaopro.adm360_pedido_itens(id, tenant_id, pedido_id, produto_id, quantidade, preco_unitario, desconto) VALUES(gen_random_uuid(), @tenantId, @id, @ProdutoId, @Quantidade, @PrecoUnitario, @Desconto)",
                new { tenantId, id, item.ProdutoId, item.Quantidade, item.PrecoUnitario, item.Desconto }, tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task AprovarAsync(Guid tenantId, Guid usuarioId, Guid pedidoId, string key, CancellationToken ct)
    {
        await using var cn = Connection();
        var changed = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_pedidos
            SET situacao = 'APROVADO', aprovado_em = now(), aprovado_por = @usuarioId, versao = versao + 1, idempotency_key = @key
            WHERE id = @pedidoId AND tenant_id = @tenantId AND situacao = 'RASCUNHO'
              AND NOT EXISTS(SELECT 1 FROM plantaopro.adm360_pedidos x WHERE x.tenant_id = @tenantId AND x.idempotency_key = @key AND x.id <> @pedidoId)",
            new { tenantId, usuarioId, pedidoId, key }, cancellationToken: ct));

        if (changed == 0 && !await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM plantaopro.adm360_pedidos WHERE id = @pedidoId AND tenant_id = @tenantId AND idempotency_key = @key AND situacao <> 'RASCUNHO')",
            new { tenantId, pedidoId, key }, cancellationToken: ct)))
        {
            throw new InvalidOperationException("Pedido não está disponível para aprovação.");
        }
    }

    public async Task<Guid> ReceberAsync(Guid tenantId, Guid usuarioId, ConfirmarRecebimentoCommand c, CancellationToken ct)
    {
        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var existing = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            "SELECT id FROM plantaopro.adm360_recebimentos WHERE tenant_id = @tenantId AND idempotency_key = @key",
            new { tenantId, key = c.IdempotencyKey }, tx, cancellationToken: ct));
        if (existing.HasValue)
        {
            await tx.CommitAsync(ct);
            return existing.Value;
        }

        var pedido = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(
            "SELECT numero, fornecedor_id, previsao, situacao FROM plantaopro.adm360_pedidos WHERE id = @id AND tenant_id = @tenantId FOR UPDATE",
            new { id = c.PedidoId, tenantId }, tx, cancellationToken: ct));

        if (pedido is null) throw new InvalidOperationException("Pedido de compra não encontrado.");
        string status = (string)pedido.situacao;
        if (status is not ("APROVADO" or "PARCIAL"))
            throw new InvalidOperationException("Somente pedido aprovado ou parcial pode ser recebido.");

        var rid = Guid.NewGuid();
        await cn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO plantaopro.adm360_recebimentos(id, tenant_id, pedido_id, documento, idempotency_key, confirmado_em, created_by) VALUES(@rid, @tenantId, @PedidoId, @Documento, @key, now(), @usuarioId)",
            new { rid, tenantId, c.PedidoId, c.Documento, key = c.IdempotencyKey, usuarioId }, tx, cancellationToken: ct));

        decimal totalRecebimento = 0m;

        foreach (var item in c.Itens)
        {
            if (item.Quantidade <= 0) throw new ArgumentException("Quantidade recebida deve ser positiva.");

            var row = await cn.QuerySingleOrDefaultAsync<ReceivingRow>(new CommandDefinition(@"
                SELECT i.produto_id AS ProdutoId, p.controla_lote AS ControlaLote, p.exige_inspecao AS Inspecao,
                       i.quantidade - i.quantidade_recebida AS Saldo,
                       i.preco_unitario AS PrecoUnitario, i.desconto AS Desconto, i.quantidade AS QtdTotal
                FROM plantaopro.adm360_pedido_itens i
                JOIN plantaopro.adm360_produtos p ON p.id = i.produto_id AND p.tenant_id = i.tenant_id
                WHERE i.id = @id AND i.pedido_id = @PedidoId AND i.tenant_id = @tenantId
                FOR UPDATE",
                new { id = item.PedidoItemId, c.PedidoId, tenantId }, tx, cancellationToken: ct));

            if (row is null || row.ProdutoId == Guid.Empty || item.Quantidade > row.Saldo)
                throw new InvalidOperationException("Quantidade excede o saldo autorizado do pedido.");
            if (row.ControlaLote && string.IsNullOrWhiteSpace(item.Lote))
                throw new ArgumentException("Lote é obrigatório para produto controlado.");

            var localOk = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM plantaopro.adm360_locais WHERE id = @id AND tenant_id = @tenantId AND ativo)",
                new { id = item.LocalId, tenantId }, tx, cancellationToken: ct));
            if (!localOk) throw new ArgumentException("Local não pertence à organização.");

            // Bloqueio de inventário no local de recebimento
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, item.LocalId, ct);

            decimal descUnit = row.QtdTotal > 0 ? (row.Desconto / row.QtdTotal) : 0m;
            decimal custoUnit = Math.Max(0m, row.PrecoUnitario - descUnit);
            totalRecebimento += item.Quantidade * custoUnit;

            var condicao = item.Validade.HasValue && item.Validade.Value < DateOnly.FromDateTime(DateTime.UtcNow)
                ? "VENCIDO"
                : row.Inspecao ? "QUARENTENA" : "LIBERADO";

            var loteId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade, custo_unitario)
                VALUES(gen_random_uuid(), @tenantId, @produto, @lote, @validade, @custoUnit)
                ON CONFLICT(tenant_id, produto_id, codigo) DO UPDATE
                SET validade = COALESCE(plantaopro.adm360_lotes.validade, excluded.validade),
                    custo_unitario = COALESCE(plantaopro.adm360_lotes.custo_unitario, excluded.custo_unitario)
                RETURNING id",
                new { tenantId, produto = row.ProdutoId, lote = item.Lote ?? "SEM-LOTE", validade = item.Validade, custoUnit }, tx, cancellationToken: ct));

            var ri = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_recebimento_itens(id, tenant_id, recebimento_id, pedido_item_id, produto_id, lote_id, local_id, quantidade, condicao)
                VALUES(@ri, @tenantId, @rid, @PedidoItemId, @produto, @loteId, @LocalId, @Quantidade, @condicao);
                UPDATE plantaopro.adm360_pedido_itens SET quantidade_recebida = quantidade_recebida + @Quantidade WHERE id = @PedidoItemId",
                new { ri, tenantId, rid, item.PedidoItemId, produto = row.ProdutoId, loteId, item.LocalId, item.Quantidade, condicao }, tx, cancellationToken: ct));

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos(id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, origem_tipo, origem_id, idempotency_key, created_by)
                VALUES(gen_random_uuid(), @tenantId, @produto, @loteId, @LocalId, 'ENTRADA', @condicao, @Quantidade, 'RECEBIMENTO', @ri, @key, @usuarioId)",
                new { tenantId, produto = row.ProdutoId, loteId, item.LocalId, item.Quantidade, condicao, ri, key = $"{c.IdempotencyKey}:{item.PedidoItemId}", usuarioId }, tx, cancellationToken: ct));
        }

        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_pedidos p
            SET situacao = CASE WHEN EXISTS(SELECT 1 FROM plantaopro.adm360_pedido_itens i WHERE i.pedido_id = p.id AND i.quantidade_recebida < i.quantidade) THEN 'PARCIAL' ELSE 'RECEBIDO' END,
                versao = versao + 1
            WHERE id = @PedidoId",
            new { c.PedidoId }, tx, cancellationToken: ct));

        // Gera obrigação em Contas a Pagar para este recebimento
        totalRecebimento = Math.Round(totalRecebimento, 2);
        if (totalRecebimento > 0)
        {
            var seq = await cn.ExecuteScalarAsync<long>("SELECT nextval('plantaopro.adm360_titulo_pagar_numero')", transaction: tx);
            var numeroTitulo = $"PAG-{seq:D6}";
            var tituloId = Guid.NewGuid();
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly vencimento = pedido.previsao != null ? ToDateOnly(pedido.previsao) : hoje.AddDays(30);

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_titulos_pagar(
                    id, tenant_id, numero, fornecedor_id, origem_tipo, origem_id,
                    documento, competencia, data_emissao, data_vencimento, parcela, total_parcelas,
                    valor_principal, valor_pago, saldo_aberto, situacao, centro_custo,
                    idempotency_key, created_by
                ) VALUES(
                    @tituloId, @tenantId, @numeroTitulo, @fornecedorId, 'RECEBIMENTO_COMPRA', @rid,
                    @documento, @hoje, @hoje, @vencimento, 1, 1,
                    @totalRecebimento, 0, @totalRecebimento, 'APROVADO', 'SUPRIMENTOS',
                    @keyTitulo, @usuarioId
                )",
                new
                {
                    tituloId, tenantId, numeroTitulo, fornecedorId = (Guid)pedido.fornecedor_id,
                    rid, documento = c.Documento ?? (string)pedido.numero, hoje, vencimento,
                    totalRecebimento, keyTitulo = $"{c.IdempotencyKey}:AP", usuarioId
                }, tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
        return rid;
    }
}
