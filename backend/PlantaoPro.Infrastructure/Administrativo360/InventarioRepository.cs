using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class InventarioRepository : Adm360Repository, IInventarioRepository
{
    private sealed class InventarioRow
    {
        public Guid LocalId { get; set; }
        public string Situacao { get; set; } = string.Empty;
    }

    private sealed class InventarioItemRow
    {
        public Guid ProdutoId { get; set; }
        public Guid LoteId { get; set; }
        public string Condicao { get; set; } = "LIBERADO";
        public decimal Esperado { get; set; }
        public decimal? Contado { get; set; }
        public decimal Diferenca => (Contado ?? Esperado) - Esperado;
    }

    public InventarioRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<InventarioResumo>> ListarAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<InventarioResumo>(new CommandDefinition(@"
            SELECT i.id, l.nome AS Local, i.escopo, i.situacao, i.created_at AS CriadoEm
            FROM plantaopro.adm360_inventarios i
            JOIN plantaopro.adm360_locais l ON l.id = i.local_id AND l.tenant_id = i.tenant_id
            WHERE i.tenant_id = @tenantId
            ORDER BY i.created_at DESC",
            new { tenantId }, cancellationToken: ct))).AsList();
    }

    public async Task<Guid> AbrirAsync(Guid tenantId, Guid usuarioId, AbrirInventarioCommand c, CancellationToken ct)
    {
        InventarioRegras.ValidarAbertura(c.LocalId, c.Escopo);

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        // Lock determinístico de abertura de inventário no local para evitar corrida com movimentações simultâneas
        var lockKey = $"INVENTARIO_LOCAL:{tenantId}:{c.LocalId}";
        await BloquearChavesDeterministasAsync(cn, tx, new[] { lockKey }, ct);

        var id = Guid.NewGuid();
        var n = await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_inventarios(id, tenant_id, local_id, situacao, escopo, created_by)
            SELECT @id, @tenantId, @LocalId, 'CONTAGEM', trim(@Escopo), @usuarioId
            WHERE EXISTS(SELECT 1 FROM plantaopro.adm360_locais WHERE id = @LocalId AND tenant_id = @tenantId AND ativo)
              AND NOT EXISTS(SELECT 1 FROM plantaopro.adm360_inventarios WHERE tenant_id = @tenantId AND local_id = @LocalId AND situacao IN ('ABERTO', 'CONTAGEM', 'REVISAO'))",
            new { id, tenantId, c.LocalId, c.Escopo, usuarioId }, tx, cancellationToken: ct));

        if (n == 0)
            throw new InvalidOperationException("Local inválido ou já bloqueado por outro inventário ativo.");

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task ContarAsync(Guid tenantId, Guid usuarioId, Guid inventarioId, ContarInventarioCommand c, CancellationToken ct)
    {
        InventarioRegras.ValidarContagem(c.Quantidade);
        var condicao = string.IsNullOrWhiteSpace(c.Condicao) ? "LIBERADO" : c.Condicao.Trim().ToUpperInvariant();

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var localId = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition(@"
            SELECT local_id FROM plantaopro.adm360_inventarios
            WHERE id = @inventarioId AND tenant_id = @tenantId AND situacao IN ('CONTAGEM', 'REVISAO')
            FOR UPDATE",
            new { inventarioId, tenantId }, tx, cancellationToken: ct));

        if (!localId.HasValue)
            throw new InvalidOperationException("Inventário não está em contagem ou revisão.");

        // Consulta saldo físico esperado especificamente para aquela condição
        var expected = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
            SELECT COALESCE(SUM(fisico), 0)
            FROM plantaopro.adm360_saldos
            WHERE tenant_id = @tenantId AND local_id = @localId AND produto_id = @ProdutoId AND lote_id = @LoteId AND condicao = @condicao",
            new { tenantId, localId = localId.Value, c.ProdutoId, c.LoteId, condicao }, tx, cancellationToken: ct));

        // Grava ou atualiza a contagem por (produto, lote, condicao), diferenciando valor zero de item não contado
        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_inventario_itens(
                id, tenant_id, inventario_id, produto_id, lote_id, condicao, esperado, contado
            ) VALUES(
                gen_random_uuid(), @tenantId, @inventarioId, @ProdutoId, @LoteId, @condicao, @expected, @Quantidade
            )
            ON CONFLICT (tenant_id, inventario_id, produto_id, lote_id, condicao)
            DO UPDATE SET contado = EXCLUDED.contado",
            new { tenantId, inventarioId, c.ProdutoId, c.LoteId, condicao, expected, c.Quantidade }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task AprovarAsync(Guid tenantId, Guid usuarioId, Guid inventarioId, string justificativa, string key, CancellationToken ct)
    {
        InventarioRegras.ValidarAprovacao(justificativa);

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var inv = await cn.QuerySingleOrDefaultAsync<InventarioRow>(new CommandDefinition(@"
                SELECT local_id AS LocalId, situacao AS Situacao
                FROM plantaopro.adm360_inventarios
                WHERE id = @inventarioId AND tenant_id = @tenantId
                FOR UPDATE",
                new { inventarioId, tenantId }, tx, cancellationToken: ct));

            if (inv is null)
                throw new InvalidOperationException("Inventário não encontrado.");

            if (inv.Situacao == "APROVADO")
            {
                var jaAprovadoComMesmaChave = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                    SELECT EXISTS(
                        SELECT 1 FROM plantaopro.adm360_inventarios
                        WHERE id = @inventarioId AND tenant_id = @tenantId AND idempotency_aprovacao = @key
                    )",
                    new { inventarioId, tenantId, key }, tx, cancellationToken: ct));

                if (jaAprovadoComMesmaChave) return; // Idempotente
                throw new InvalidOperationException("Inventário já aprovado com outra chave de idempotência.");
            }

            if (inv.Situacao is not ("CONTAGEM" or "REVISAO"))
                throw new InvalidOperationException($"Inventário na situação '{inv.Situacao}' não pode ser aprovado.");

            // Trava determinística do local
            var lockKey = $"INVENTARIO_LOCAL:{tenantId}:{inv.LocalId}";
            await BloquearChavesDeterministasAsync(cn, tx, new[] { lockKey }, ct);

            // Busca todos os itens contados
            var itens = (await cn.QueryAsync<InventarioItemRow>(new CommandDefinition(@"
                SELECT produto_id AS ProdutoId, lote_id AS LoteId, condicao AS Condicao, esperado AS Esperado, contado AS Contado
                FROM plantaopro.adm360_inventario_itens
                WHERE tenant_id = @tenantId AND inventario_id = @inventarioId",
                new { tenantId, inventarioId }, tx, cancellationToken: ct))).ToList();

            // Não aprovar silenciosamente se há itens pendentes de contagem (contado IS NULL)
            if (itens.Any(i => !i.Contado.HasValue))
                throw new InvalidOperationException("O inventário possui itens sem contagem finalizada.");

            // Aplica os movimentos de ajuste preservando estritamente a condição de cada lote
            foreach (var item in itens.Where(i => i.Diferenca != 0))
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_movimentos(
                        id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                    ) VALUES(
                        gen_random_uuid(), @tenantId, @produto, @lote, @local, 'AJUSTE', @condicao, @qty, @justificativa, 'INVENTARIO', @inventarioId, @ikey, @usuarioId
                    )",
                    new
                    {
                        tenantId,
                        produto = item.ProdutoId,
                        lote = item.LoteId,
                        local = inv.LocalId,
                        condicao = item.Condicao, // PRESERVA A CONDIÇÃO: NÃO transforma Quarentena ou Bloqueado em Liberado!
                        qty = item.Diferenca,
                        justificativa,
                        inventarioId,
                        ikey = $"{key}:{item.ProdutoId}:{item.LoteId}:{item.Condicao}",
                        usuarioId
                    }, tx, cancellationToken: ct));

                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_inventario_itens
                    SET ajustado = @diferenca
                    WHERE tenant_id = @tenantId AND inventario_id = @inventarioId AND produto_id = @ProdutoId AND lote_id = @LoteId AND condicao = @condicao",
                    new { diferenca = item.Diferenca, tenantId, inventarioId, item.ProdutoId, item.LoteId, condicao = item.Condicao }, tx, cancellationToken: ct));
            }

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_inventarios
                SET situacao = 'APROVADO', motivo_ajuste = @justificativa, idempotency_aprovacao = @key, versao = versao + 1
                WHERE id = @inventarioId AND tenant_id = @tenantId",
                new { justificativa, key, inventarioId, tenantId }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid inventarioId, string motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Cancelamento de inventário exige motivo.");

        await using var cn = Connection();
        var n = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_inventarios
            SET situacao = 'CANCELADO', motivo_ajuste = @motivo, versao = versao + 1
            WHERE id = @inventarioId AND tenant_id = @tenantId AND situacao IN ('ABERTO', 'CONTAGEM', 'REVISAO')",
            new { tenantId, inventarioId, motivo }, cancellationToken: ct));

        if (n == 0)
            throw new InvalidOperationException("Inventário não encontrado ou já encerrado.");
    }
}
