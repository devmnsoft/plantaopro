using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class ValorizacaoRepository : Adm360Repository, IValorizacaoRepository
{
    public ValorizacaoRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<ValeResumo>> ListarValesPendentesAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT v.id AS Id,
                   v.numero AS Numero,
                   c.numero AS CirurgiaNumero,
                   o.numero AS OrcamentoNumero,
                   COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital,
                   COALESCE(orig.nome, 'CD') AS LocalOrigem,
                   COALESCE(dest.nome, 'Hospital') AS LocalDestino,
                   v.data_saida_prevista AS DataSaidaPrevista,
                   v.data_saida_efetiva AS DataSaidaEfetiva,
                   v.situacao AS Situacao,
                   COALESCE(v.situacao_financeira, 'PENDENTE_VALORIZACAO') AS SituacaoFinanceira,
                   (SELECT COUNT(*)::numeric FROM plantaopro.adm360_vale_itens vi WHERE vi.vale_id = v.id) AS TotalItens,
                   v.created_at AS CriadoEm
            FROM plantaopro.adm360_vales v
            LEFT JOIN plantaopro.adm360_cirurgias c ON c.id = v.cirurgia_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_orcamentos o ON o.id = v.orcamento_id AND o.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = v.hospital_id AND h.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.hospital_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_locais orig ON orig.id = v.local_origem_id AND orig.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_locais dest ON dest.id = v.local_destino_id AND dest.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
              AND v.situacao = 'RECONCILIADO'
              AND (v.situacao_financeira = 'PENDENTE_VALORIZACAO' OR v.situacao_financeira IS NULL)
            ORDER BY v.created_at DESC",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new ValeResumo(
            (Guid)r.id,
            (string)r.numero,
            (string?)r.cirurgianumero,
            (string?)r.orcamentonumero,
            (string)r.hospital,
            (string)r.localorigem,
            (string)r.localdestino,
            ToDateOnly(r.datasaidaprevista),
            r.datasaidaefetiva is not null ? (DateTimeOffset?)r.datasaidaefetiva : null,
            (string)r.situacao,
            (string)r.situacaofinanceira,
            (decimal)r.totalitens,
            (DateTimeOffset)r.criadoem
        )).ToList();
    }

    public async Task<PreviaValorizacaoDto?> ObterPreviaAsync(Guid tenantId, Guid valeId, CancellationToken ct)
    {
        await using var cn = Connection();
        var v = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT v.id AS Id,
                   v.numero AS Numero,
                   v.cirurgia_id AS CirurgiaId,
                   c.numero AS CirurgiaNumero,
                   v.orcamento_id AS OrcamentoId,
                   v.orcamento_revisao AS OrcamentoRevisao,
                   v.hospital_id AS HospitalId,
                   COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital,
                   COALESCE(o.responsavel_financeiro_id, v.hospital_id) AS PagadorId,
                   COALESCE(pag.nome, h.nome, hosp.nome, 'Pagador') AS Pagador,
                   c.responsavel_id AS VendedorId,
                   u.nome AS Vendedor,
                   v.situacao AS Situacao,
                   v.situacao_financeira AS SituacaoFinanceira
            FROM plantaopro.adm360_vales v
            LEFT JOIN plantaopro.adm360_cirurgias c ON c.id = v.cirurgia_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_orcamentos o ON o.id = v.orcamento_id AND o.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = v.hospital_id AND h.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.hospital_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros pag ON pag.id = o.responsavel_financeiro_id AND pag.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = c.responsavel_id AND u.tenant_id = v.tenant_id
            WHERE v.id = @valeId AND v.tenant_id = @tenantId",
            new { valeId, tenantId }, cancellationToken: ct));

        if (v is null) return null;

        var pendencias = new List<string>();
        if (!string.Equals((string)v.situacao, "RECONCILIADO", StringComparison.OrdinalIgnoreCase))
            pendencias.Add($"O vale está na situação '{v.situacao}'. Apenas vales RECONCILIADOS podem ser valorizados.");

        if (string.Equals((string?)v.situacaofinanceira, "VALORIZADO", StringComparison.OrdinalIgnoreCase))
            pendencias.Add("Este vale já foi valorizado anteriormente.");

        var itemRows = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT vi.id AS ValeItemId,
                   vi.produto_id AS ProdutoId,
                   p.sku AS Sku,
                   p.nome AS Produto,
                   vi.lote_id AS LoteId,
                   l.codigo AS Lote,
                   vi.quantidade_consumida AS QuantidadeConsumida,
                   vi.preco_unitario AS PrecoUnitario,
                   l.custo_unitario AS CustoUnitarioDoc,
                   p.preco_custo AS PrecoCustoCadastro
            FROM plantaopro.adm360_vale_itens vi
            JOIN plantaopro.adm360_produtos p ON p.id = vi.produto_id AND p.tenant_id = vi.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = vi.lote_id AND l.tenant_id = vi.tenant_id
            WHERE vi.vale_id = @valeId AND vi.tenant_id = @tenantId
              AND vi.quantidade_consumida > 0
            ORDER BY p.nome, l.codigo",
            new { valeId, tenantId }, cancellationToken: ct))).ToList();

        if (itemRows.Count == 0)
            pendencias.Add("Nenhum item com consumo registrado foi encontrado neste vale.");

        var itens = new List<PreviaValorizacaoItemDto>();
        decimal totalBruto = 0m;
        decimal totalCusto = 0m;

        foreach (var r in itemRows)
        {
            decimal qtdConsumida = (decimal)r.quantidadeconsumida;
            decimal precoUnitario = (decimal)r.precounitario;
            decimal? custoDoc = r.custounitariodoc is not null ? (decimal)r.custounitariodoc : null;
            bool custoAusente = custoDoc == null;
            decimal custoUnitario = custoDoc ?? 0m;

            if (custoAusente)
            {
                pendencias.Add($"O produto {r.Produto} (Lote: {r.Lote}) não possui custo documental rastreado de entrada. A margem de contribuição ficará pendente de conferência.");
            }

            if (precoUnitario <= 0m)
                pendencias.Add($"O produto {r.Produto} (Lote: {r.Lote}) está com preço unitário zerado ou ausente.");

            decimal subtotal = qtdConsumida * precoUnitario;
            decimal custoTotal = qtdConsumida * custoUnitario;

            totalBruto += subtotal;
            totalCusto += custoTotal;

            itens.Add(new PreviaValorizacaoItemDto(
                (Guid)r.valeitemid,
                (Guid)r.produtoid,
                (string)r.sku,
                (string)r.produto,
                (Guid)r.loteid,
                (string)r.lote,
                qtdConsumida,
                precoUnitario,
                0m,
                subtotal,
                custoUnitario,
                custoTotal,
                custoAusente
            ));
        }

        decimal comissaoPct = 5.0m; // Default do MVP
        decimal comissaoPrevista = Math.Round(totalBruto * (comissaoPct / 100m), 2);

        return new PreviaValorizacaoDto(
            (Guid)v.id,
            (string)v.numero,
            (Guid?)v.cirurgiaid,
            (string?)v.cirurgianumero,
            (Guid?)v.orcamentoid,
            (int?)r_int(v.orcamentorevisao),
            (Guid)v.hospitalid,
            (string)v.hospital,
            (Guid)v.pagadorid,
            (string)v.pagador,
            (Guid?)v.vendedorid,
            (string?)v.vendedor,
            totalBruto,
            0m,
            totalBruto,
            totalCusto,
            comissaoPct,
            comissaoPrevista,
            itens,
            pendencias
        );
    }

    private static int? r_int(object? val) =>
        val is null ? null : Convert.ToInt32(val);

    public async Task<Guid> ValorizarAsync(Guid tenantId, Guid usuarioId, ValorizarValeCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash(
            "VALORIZACAO_VALE",
            tenantId,
            command.ValeId,
            command.PagadorId,
            command.VendedorId ?? Guid.Empty,
            command.DescontoGeral,
            command.ComissaoPercentual
        );
        Guid valorizacaoId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            // Idempotência antecipada
            var opExistente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, payload_hash, resultado FROM plantaopro.adm360_operacoes
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (opExistente.payload_hash == payloadHash)
                {
                    valorizacaoId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_valorizacoes
                        WHERE vale_id = @ValeId AND tenant_id = @tenantId",
                        new { command.ValeId, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT v.id, v.numero, v.cirurgia_id, v.orcamento_id, v.orcamento_revisao,
                       v.hospital_id, v.situacao, v.situacao_financeira
                FROM plantaopro.adm360_vales v
                WHERE v.id = @ValeId AND v.tenant_id = @tenantId FOR UPDATE",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (vale is null) throw new InvalidOperationException("Vale de consignação não encontrado.");

            ValorizacaoRegras.ValidarElegibilidadeVale(vale.situacao, (string?)vale.situacao_financeira ?? "PENDENTE_VALORIZACAO");

            var itens = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT vi.id AS vale_item_id, vi.produto_id, vi.lote_id, vi.quantidade_consumida,
                       vi.preco_unitario, l.custo_unitario AS custo_unitario_doc
                FROM plantaopro.adm360_vale_itens vi
                JOIN plantaopro.adm360_produtos p ON p.id = vi.produto_id AND p.tenant_id = vi.tenant_id
                JOIN plantaopro.adm360_lotes l ON l.id = vi.lote_id AND l.tenant_id = vi.tenant_id
                WHERE vi.vale_id = @ValeId AND vi.tenant_id = @tenantId
                  AND vi.quantidade_consumida > 0",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct))).ToList();

            if (itens.Count == 0)
                throw new InvalidOperationException("Não existem itens consumidos para valorizar este vale.");

            decimal totalBruto = 0m;
            decimal totalCusto = 0m;
            var itensValorizados = new List<(Guid ValeItemId, Guid ProdutoId, Guid LoteId, decimal Qtd, decimal Preco, decimal Desconto, decimal Subtotal, decimal CustoUnit, decimal CustoTot)>();

            foreach (var it in itens)
            {
                decimal qtd = (decimal)it.quantidade_consumida;
                decimal preco = (decimal)it.preco_unitario;
                decimal? custoDoc = it.custo_unitario_doc is not null ? (decimal)it.custo_unitario_doc : null;
                decimal custo = custoDoc ?? 0m;
                var (subtotal, custoTot) = ValorizacaoRegras.CalcularTotaisItem(qtd, preco, 0m, custo);
                totalBruto += subtotal;
                totalCusto += custoTot;
                itensValorizados.Add(((Guid)it.vale_item_id, (Guid)it.produto_id, (Guid)it.lote_id, qtd, preco, 0m, subtotal, custo, custoTot));
            }

            ValorizacaoRegras.ValidarDesconto(totalBruto, command.DescontoGeral);
            decimal descontoGeral = command.DescontoGeral;
            decimal totalLiquido = totalBruto - descontoGeral;
            decimal comissaoPrevista = Math.Round(totalLiquido * (command.ComissaoPercentual / 100m), 2);

            valorizacaoId = Guid.NewGuid();

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_valorizacoes(
                    id, tenant_id, vale_id, cirurgia_id, orcamento_id, orcamento_revisao,
                    hospital_id, pagador_id, vendedor_id, total_bruto, desconto,
                    total_liquido, total_custo, comissao_percentual, comissao_prevista,
                    situacao, created_by
                ) VALUES(
                    @valorizacaoId, @tenantId, @ValeId, @cirurgiaId, @orcamentoId, @orcamentoRevisao,
                    @hospitalId, @PagadorId, @VendedorId, @totalBruto, @descontoGeral,
                    @totalLiquido, @totalCusto, @ComissaoPercentual, @comissaoPrevista,
                    'CONFIRMADA', @usuarioId
                )",
                new
                {
                    valorizacaoId, tenantId, command.ValeId,
                    cirurgiaId = (Guid?)vale.cirurgia_id,
                    orcamentoId = (Guid?)vale.orcamento_id,
                    orcamentoRevisao = (int?)r_int(vale.orcamento_revisao),
                    hospitalId = (Guid)vale.hospital_id,
                    command.PagadorId, command.VendedorId,
                    totalBruto, descontoGeral, totalLiquido, totalCusto,
                    command.ComissaoPercentual, comissaoPrevista, usuarioId
                }, tx, cancellationToken: ct));

            foreach (var iv in itensValorizados)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_valorizacao_itens(
                        id, tenant_id, valorizacao_id, vale_item_id, produto_id, lote_id,
                        quantidade_consumida, preco_unitario, desconto, subtotal, custo_unitario, custo_total
                    ) VALUES(
                        gen_random_uuid(), @tenantId, @valorizacaoId, @ValeItemId, @ProdutoId, @LoteId,
                        @Qtd, @Preco, @Desconto, @Subtotal, @CustoUnit, @CustoTot
                    )",
                    new
                    {
                        tenantId, valorizacaoId, iv.ValeItemId, iv.ProdutoId, iv.LoteId,
                        iv.Qtd, iv.Preco, iv.Desconto, iv.Subtotal, iv.CustoUnit, iv.CustoTot
                    }, tx, cancellationToken: ct));
            }

            // Atualiza situação financeira do vale para VALORIZADO
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_vales
                SET situacao_financeira = 'VALORIZADO', updated_at = now()
                WHERE id = @ValeId AND tenant_id = @tenantId",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct));

            // Registra operação idempotente
            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'VALORIZACAO_VALE', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

        }, ct);

        return valorizacaoId;
    }
}
