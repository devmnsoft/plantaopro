using System.Globalization;
using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class EstoqueRepository : Adm360Repository, IEstoqueRepository
{
    private sealed class OperacaoRow
    {
        public Guid Id { get; set; }
        public string PayloadHash { get; set; } = string.Empty;
        public string? Resultado { get; set; }
    }

    private sealed class ReservaExistingRow
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public Guid LoteId { get; set; }
        public Guid LocalId { get; set; }
        public decimal Quantidade { get; set; }
    }

    public EstoqueRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<SaldoEstoque>> ConsultarAsync(Guid tenantId, string? busca, string? condicao, Guid? localId, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<SaldoEstoque>(new CommandDefinition(@"
            SELECT produto_id AS ProdutoId, produto, lote_id AS LoteId, lote, validade,
                   local_id AS LocalId, local, condicao, fisico, reservado, disponivel
            FROM plantaopro.adm360_saldos
            WHERE tenant_id = @tenantId
              AND (@busca IS NULL OR produto ILIKE '%' || @busca || '%' OR lote ILIKE '%' || @busca || '%')
              AND (@condicao IS NULL OR condicao = @condicao)
              AND (@localId IS NULL OR local_id = @localId)
            ORDER BY produto, validade NULLS LAST",
            new { tenantId, busca, condicao, localId }, cancellationToken: ct))).AsList();
    }

    public async Task TransferirAsync(Guid tenantId, Guid usuarioId, TransferirCommand c, CancellationToken ct)
    {
        Estoque.ValidarTransferencia(c.OrigemId, c.DestinoId, c.Quantidade, c.Motivo);

        var payloadHash = IdempotenciaHelper.CalcularHash(
            "TRANSFERENCIA",
            tenantId,
            c.ProdutoId,
            c.LoteId,
            c.OrigemId,
            c.DestinoId,
            c.Quantidade.ToString("0.0000", CultureInfo.InvariantCulture));

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            // Verificação de idempotência na tabela mestre de operações
            var opExistente = await cn.QuerySingleOrDefaultAsync<OperacaoRow>(new CommandDefinition(
                "SELECT id, payload_hash AS PayloadHash, resultado FROM plantaopro.adm360_operacoes WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = c.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (string.Equals(opExistente.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase))
                {
                    return; // Reenvio idempotente com mesmo conteúdo: sucesso garantido sem reaplicar efeitos
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com conteúdo divergente.");
            }

            var locais = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT count(*) FROM plantaopro.adm360_locais WHERE tenant_id = @tenantId AND ativo AND id = ANY(@ids)",
                new { tenantId, ids = new[] { c.OrigemId, c.DestinoId } }, tx, cancellationToken: ct));
            if (locais != 2) throw new ArgumentException("Origem e destino devem ser locais ativos da organização.");

            // Bloqueio de inventário na origem e no destino
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, c.OrigemId, ct);
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, c.DestinoId, ct);

            // Bloqueio determinístico ordenado para evitar deadlocks
            var lockOrigem = $"{tenantId}:{c.ProdutoId}:{c.LoteId}:{c.OrigemId}";
            var lockDestino = $"{tenantId}:{c.ProdutoId}:{c.LoteId}:{c.DestinoId}";
            await BloquearChavesDeterministasAsync(cn, tx, new[] { lockOrigem, lockDestino }, ct);

            var available = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                SELECT COALESCE(disponivel, 0)
                FROM plantaopro.adm360_saldos
                WHERE tenant_id = @tenantId AND produto_id = @ProdutoId AND lote_id = @LoteId AND local_id = @OrigemId AND condicao = 'LIBERADO'",
                new { tenantId, c.ProdutoId, c.LoteId, c.OrigemId }, tx, cancellationToken: ct));

            if (available < c.Quantidade)
                throw new InvalidOperationException("Saldo disponível insuficiente para transferência.");

            var opId = Guid.NewGuid();

            // Grava operação principal
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'TRANSFERENCIA', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = c.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

            // Ambas as pernas referenciam a mesma operação principal
            var legs = new[]
            {
                (LocalId: c.OrigemId, Qty: -c.Quantidade, Tipo: "TRANSFERENCIA_SAIDA"),
                (LocalId: c.DestinoId, Qty: c.Quantidade, Tipo: "TRANSFERENCIA_ENTRADA")
            };

            foreach (var leg in legs)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_movimentos(
                        id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                    ) VALUES(
                        gen_random_uuid(), @tenantId, @ProdutoId, @LoteId, @local, @tipo, 'LIBERADO', @qty, @Motivo, 'TRANSFERENCIA', @opId, @key, @usuarioId
                    )",
                    new
                    {
                        tenantId,
                        c.ProdutoId,
                        c.LoteId,
                        local = leg.LocalId,
                        qty = leg.Qty,
                        tipo = leg.Tipo,
                        c.Motivo,
                        opId,
                        key = $"{c.IdempotencyKey}:{leg.Tipo}",
                        usuarioId
                    }, tx, cancellationToken: ct));
            }
        }, ct);
    }

    public async Task ReservarAsync(Guid tenantId, Guid usuarioId, ReservarCommand c, CancellationToken ct)
    {
        Estoque.ValidarQuantidade(c.Quantidade);

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var existing = await cn.QuerySingleOrDefaultAsync<ReservaExistingRow>(new CommandDefinition(@"
                SELECT id, produto_id AS ProdutoId, lote_id AS LoteId, local_id AS LocalId, quantidade AS Quantidade
                FROM plantaopro.adm360_reservas
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = c.IdempotencyKey }, tx, cancellationToken: ct));

            if (existing is not null)
            {
                if (existing.ProdutoId == c.ProdutoId && existing.LoteId == c.LoteId && existing.LocalId == c.LocalId && existing.Quantidade == c.Quantidade)
                {
                    return; // Reenvio idempotente
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave de reserva foi utilizada com parâmetros diferentes.");
            }

            // Bloqueio determinístico do local/produto/lote
            var lockKey = $"{tenantId}:{c.ProdutoId}:{c.LoteId}:{c.LocalId}";
            await BloquearChavesDeterministasAsync(cn, tx, new[] { lockKey }, ct);

            // Bloqueio de inventário no local
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, c.LocalId, ct);

            // Validar se o lote está vencido ou se vencerá antes do procedimento
            var loteInfo = await cn.QuerySingleOrDefaultAsync<DateOnly?>(new CommandDefinition(
                "SELECT validade FROM plantaopro.adm360_lotes WHERE id = @LoteId AND tenant_id = @tenantId",
                new { c.LoteId, tenantId }, tx, cancellationToken: ct));

            if (loteInfo.HasValue)
            {
                if (loteInfo.Value < DateOnly.FromDateTime(DateTime.UtcNow))
                    throw new InvalidOperationException("Lote vencido não está disponível para reserva.");

                if (c.DataPrevistaUso.HasValue && loteInfo.Value < c.DataPrevistaUso.Value)
                    throw new InvalidOperationException($"Lote vence em {loteInfo.Value:yyyy-MM-dd}, antes da data prevista de utilização ({c.DataPrevistaUso.Value:yyyy-MM-dd}).");
            }

            var available = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                SELECT COALESCE(disponivel, 0)
                FROM plantaopro.adm360_saldos
                WHERE tenant_id = @tenantId AND produto_id = @ProdutoId AND lote_id = @LoteId AND local_id = @LocalId AND condicao = 'LIBERADO'",
                new { tenantId, c.ProdutoId, c.LoteId, c.LocalId }, tx, cancellationToken: ct));

            if (available < c.Quantidade)
                throw new InvalidOperationException("Quantidade solicitada excede o estoque disponível.");

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_reservas(
                    id, tenant_id, produto_id, lote_id, local_id, quantidade, situacao, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    gen_random_uuid(), @tenantId, @ProdutoId, @LoteId, @LocalId, @Quantidade, 'ATIVA', @Origem, @OrigemId, @key, @usuarioId
                )",
                new { tenantId, c.ProdutoId, c.LoteId, c.LocalId, c.Quantidade, c.Origem, c.OrigemId, key = c.IdempotencyKey, usuarioId }, tx, cancellationToken: ct));
        }, ct);
    }
}
