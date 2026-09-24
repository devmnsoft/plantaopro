using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class QualidadeRepository : Adm360Repository, IQualidadeRepository
{
    private sealed class InspectionRow
    {
        public Guid ProdutoId { get; set; }
        public Guid LoteId { get; set; }
        public Guid LocalId { get; set; }
        public decimal Pendente { get; set; }
        public string Condicao { get; set; } = string.Empty;
    }

    private sealed class InspecaoExistenteRow
    {
        public decimal Aprovada { get; set; }
        public decimal Reprovada { get; set; }
    }

    public QualidadeRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<InspecaoPendente>> PendentesAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<InspecaoPendente>(new CommandDefinition(@"
            SELECT r.id AS RecebimentoItemId, p.nome AS Produto, l.codigo AS Lote,
                   r.quantidade AS Recebida, r.quantidade - r.quantidade_decidida AS Pendente, o.nome AS Local
            FROM plantaopro.adm360_recebimento_itens r
            JOIN plantaopro.adm360_produtos p ON p.id = r.produto_id AND p.tenant_id = r.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = r.lote_id AND l.tenant_id = r.tenant_id
            JOIN plantaopro.adm360_locais o ON o.id = r.local_id AND o.tenant_id = r.tenant_id
            WHERE r.tenant_id = @tenantId
              AND r.condicao IN ('QUARENTENA', 'VENCIDO')
              AND r.quantidade_decidida < r.quantidade
            UNION ALL
            SELECT ve.id AS RecebimentoItemId, p.nome AS Produto, l.codigo AS Lote,
                   ve.quantidade AS Recebida, ve.quantidade - COALESCE(ve.quantidade_decidida, 0) AS Pendente, o.nome AS Local
            FROM plantaopro.adm360_vale_eventos ve
            JOIN plantaopro.adm360_vale_itens vi ON vi.id = ve.vale_item_id AND vi.tenant_id = ve.tenant_id
            JOIN plantaopro.adm360_vales v ON v.id = ve.vale_id AND v.tenant_id = ve.tenant_id
            JOIN plantaopro.adm360_produtos p ON p.id = vi.produto_id AND p.tenant_id = ve.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = vi.lote_id AND l.tenant_id = ve.tenant_id
            JOIN plantaopro.adm360_locais o ON o.id = v.local_origem_id AND o.tenant_id = ve.tenant_id
            WHERE ve.tenant_id = @tenantId
              AND ve.tipo = 'RETORNO'
              AND COALESCE(ve.quantidade_decidida, 0) < ve.quantidade
            ORDER BY Produto",
            new { tenantId }, cancellationToken: ct))).AsList();
    }

    public async Task DecidirAsync(Guid tenantId, Guid usuarioId, DecidirInspecaoCommand c, CancellationToken ct)
    {
        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var existente = await cn.QuerySingleOrDefaultAsync<InspecaoExistenteRow>(new CommandDefinition(@"
                SELECT aprovada AS Aprovada, reprovada AS Reprovada
                FROM plantaopro.adm360_inspecoes
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = c.IdempotencyKey }, tx, cancellationToken: ct));

            if (existente is not null)
            {
                if (existente.Aprovada == c.Aprovada && existente.Reprovada == c.Reprovada)
                {
                    return; // Reenvio idempotente
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave de inspeção foi utilizada com decisão diferente.");
            }

            var r = await cn.QuerySingleOrDefaultAsync<InspectionRow>(new CommandDefinition(@"
                SELECT produto_id AS ProdutoId, lote_id AS LoteId, local_id AS LocalId,
                       quantidade - quantidade_decidida AS Pendente, condicao AS Condicao
                FROM plantaopro.adm360_recebimento_itens
                WHERE id = @id AND tenant_id = @tenantId
                FOR UPDATE",
                new { id = c.RecebimentoItemId, tenantId }, tx, cancellationToken: ct));

            bool isRetornoConsignacao = false;

            if (r is null || r.ProdutoId == Guid.Empty)
            {
                // Verifica se é um retorno de consignação em adm360_vale_eventos
                var retRow = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                    SELECT vi.produto_id, vi.lote_id, v.local_origem_id AS local_id,
                           ve.quantidade - COALESCE(ve.quantidade_decidida, 0) AS pendente
                    FROM plantaopro.adm360_vale_eventos ve
                    JOIN plantaopro.adm360_vale_itens vi ON vi.id = ve.vale_item_id AND vi.tenant_id = ve.tenant_id
                    JOIN plantaopro.adm360_vales v ON v.id = ve.vale_id AND v.tenant_id = ve.tenant_id
                    WHERE ve.id = @id AND ve.tenant_id = @tenantId AND ve.tipo = 'RETORNO'
                    FOR UPDATE",
                    new { id = c.RecebimentoItemId, tenantId }, tx, cancellationToken: ct));

                if (retRow is null)
                    throw new InvalidOperationException("Item de recebimento ou retorno de consignação não encontrado.");

                isRetornoConsignacao = true;
                r = new InspectionRow
                {
                    ProdutoId = (Guid)retRow.produto_id,
                    LoteId = (Guid)retRow.lote_id,
                    LocalId = (Guid)retRow.local_id,
                    Pendente = (decimal)retRow.pendente,
                    Condicao = "QUARENTENA"
                };
            }

            Inspecao.ValidarDecisao(r.Pendente, c.Aprovada, c.Reprovada, c.Justificativa);

            if (r.Condicao == "VENCIDO" && c.Aprovada > 0)
                throw new InvalidOperationException("Material vencido não pode ser liberado.");

            // Bloqueio de inventário no local do item
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, r.LocalId, ct);

            // Lock determinístico
            var lockKey = $"{tenantId}:{r.ProdutoId}:{r.LoteId}:{r.LocalId}";
            await BloquearChavesDeterministasAsync(cn, tx, new[] { lockKey }, ct);

            var iid = Guid.NewGuid();
            var total = c.Aprovada + c.Reprovada;

            if (isRetornoConsignacao)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_inspecoes(
                        id, tenant_id, retorno_evento_id, origem_tipo, aprovada, reprovada, justificativa, destino, idempotency_key, decidido_por, decidido_em
                    ) VALUES(
                        @iid, @tenantId, @RecebimentoItemId, 'RETORNO_CONSIGNACAO', @Aprovada, @Reprovada, @Justificativa, @Destino, @key, @usuarioId, now()
                    );
                    UPDATE plantaopro.adm360_vale_eventos
                    SET quantidade_decidida = COALESCE(quantidade_decidida, 0) + @total
                    WHERE id = @RecebimentoItemId",
                    new { iid, tenantId, c.RecebimentoItemId, c.Aprovada, c.Reprovada, c.Justificativa, c.Destino, key = c.IdempotencyKey, usuarioId, total }, tx, cancellationToken: ct));
            }
            else
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_inspecoes(
                        id, tenant_id, recebimento_item_id, origem_tipo, aprovada, reprovada, justificativa, destino, idempotency_key, decidido_por, decidido_em
                    ) VALUES(
                        @iid, @tenantId, @RecebimentoItemId, 'RECEBIMENTO', @Aprovada, @Reprovada, @Justificativa, @Destino, @key, @usuarioId, now()
                    );
                    UPDATE plantaopro.adm360_recebimento_itens
                    SET quantidade_decidida = quantidade_decidida + @total
                    WHERE id = @RecebimentoItemId",
                    new { iid, tenantId, c.RecebimentoItemId, c.Aprovada, c.Reprovada, c.Justificativa, c.Destino, key = c.IdempotencyKey, usuarioId, total }, tx, cancellationToken: ct));
            }

            // Definição de pernas com identificação estável e distinta por perna para eliminar qualquer risco de colisão
            var pernas = new List<(string Condicao, decimal Qty, string PernaIdentificador)>();

            if (c.Aprovada > 0)
            {
                pernas.Add(("QUARENTENA", -c.Aprovada, "SAIDA_QUARENTENA_APROVACAO"));
                pernas.Add(("LIBERADO", c.Aprovada, "ENTRADA_LIBERADO"));
            }

            if (c.Reprovada > 0)
            {
                pernas.Add(("QUARENTENA", -c.Reprovada, "SAIDA_QUARENTENA_REPROVACAO"));
                pernas.Add(("REPROVADO", c.Reprovada, "ENTRADA_REPROVADO"));
            }

            foreach (var leg in pernas)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_movimentos(
                        id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                    ) VALUES(
                        gen_random_uuid(), @tenantId, @produto, @lote, @local, 'INSPECAO', @condicao, @qty, @Justificativa, 'INSPECAO', @iid, @key, @usuarioId
                    )",
                    new
                    {
                        tenantId,
                        produto = r.ProdutoId,
                        lote = r.LoteId,
                        local = r.LocalId,
                        condicao = leg.Condicao,
                        qty = leg.Qty,
                        c.Justificativa,
                        iid,
                        key = $"{c.IdempotencyKey}:{leg.PernaIdentificador}",
                        usuarioId
                    }, tx, cancellationToken: ct));
            }
        }, ct);
    }
}
