using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class VendaRepository : Adm360Repository, IVendaRepository
{
    public VendaRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<VendaResumoDto>> ListarAsync(
        Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT v.id AS Id,
                   v.numero AS Numero,
                   v.valorizacao_id AS ValorizacaoId,
                   v.origem_tipo AS OrigemTipo,
                   COALESCE(c.nome, hosp.nome, 'Hospital') AS Hospital,
                   COALESCE(p.nome, c.nome, hosp.nome, 'Pagador') AS Pagador,
                   u.nome AS Vendedor,
                   v.competencia AS Competencia,
                   v.total_liquido AS TotalLiquido,
                   v.total_custo AS TotalCusto,
                   v.comissao_prevista AS ComissaoPrevista,
                   v.condicao_pagamento AS CondicaoPagamento,
                   v.situacao AS Situacao
            FROM plantaopro.adm360_vendas v
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = v.pagador_id AND p.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = v.vendedor_id AND u.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
              AND (@busca::text IS NULL OR v.numero ILIKE '%' || @busca || '%' OR c.nome ILIKE '%' || @busca || '%' OR p.nome ILIKE '%' || @busca || '%')
              AND (@situacao::varchar IS NULL OR v.situacao = @situacao)
              AND (@inicio::date IS NULL OR v.competencia >= @inicio::date)
              AND (@fim::date IS NULL OR v.competencia <= @fim::date)
            ORDER BY v.competencia DESC, v.numero DESC",
            new { tenantId, busca, situacao, inicio, fim }, cancellationToken: ct));

        return rows.Select(r => new VendaResumoDto(
            (Guid)r.id,
            (string)r.numero,
            (Guid?)r.valorizacaoid,
            (string)r.origemtipo,
            (string)r.hospital,
            (string)r.pagador,
            (string?)r.vendedor,
            ToDateOnly(r.competencia),
            (decimal)r.totalliquido,
            (decimal)r.totalcusto,
            (decimal)r.comissaoprevista,
            (string)r.condicaopagamento,
            (string)r.situacao
        )).ToList();
    }

    public async Task<VendaDetalhesDto?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        var v = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT v.id AS Id,
                   v.numero AS Numero,
                   v.valorizacao_id AS ValorizacaoId,
                   val.vale_id AS ValeId,
                   vale.numero AS ValeNumero,
                   v.cliente_id AS ClienteId,
                   COALESCE(c.nome, hosp.nome, 'Hospital') AS Cliente,
                   v.pagador_id AS PagadorId,
                   COALESCE(p.nome, c.nome, hosp.nome, 'Pagador') AS Pagador,
                   v.vendedor_id AS VendedorId,
                   u.nome AS Vendedor,
                   v.competencia AS Competencia,
                   v.total_bruto AS TotalBruto,
                   v.desconto AS Desconto,
                   v.total_liquido AS TotalLiquido,
                   v.total_custo AS TotalCusto,
                   v.comissao_percentual AS ComissaoPercentual,
                   v.comissao_prevista AS ComissaoPrevista,
                   v.condicao_pagamento AS CondicaoPagamento,
                   v.quantidade_parcelas AS QuantidadeParcelas,
                   v.situacao AS Situacao,
                   v.observacoes AS Observacoes,
                   v.created_at AS CriadoEm
            FROM plantaopro.adm360_vendas v
            LEFT JOIN plantaopro.adm360_valorizacoes val ON val.id = v.valorizacao_id AND val.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_vales vale ON vale.id = val.vale_id AND vale.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = v.pagador_id AND p.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = v.vendedor_id AND u.tenant_id = v.tenant_id
            WHERE v.id = @id AND v.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (v is null) return null;

        var itemRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT vi.id AS Id,
                   vi.produto_id AS ProdutoId,
                   p.sku AS Sku,
                   p.nome AS Produto,
                   vi.lote_id AS LoteId,
                   l.codigo AS Lote,
                   vi.quantidade AS Quantidade,
                   vi.preco_unitario AS PrecoUnitario,
                   vi.desconto AS Desconto,
                   vi.subtotal AS Subtotal,
                   vi.custo_unitario AS CustoUnitario,
                   vi.custo_total AS CustoTotal
            FROM plantaopro.adm360_venda_itens vi
            JOIN plantaopro.adm360_produtos p ON p.id = vi.produto_id AND p.tenant_id = vi.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = vi.lote_id AND l.tenant_id = vi.tenant_id
            WHERE vi.venda_id = @id AND vi.tenant_id = @tenantId
            ORDER BY p.nome, l.codigo",
            new { id, tenantId }, cancellationToken: ct));

        var itens = itemRows.Select(r => new VendaItemDto(
            (Guid)r.id,
            (Guid)r.produtoid,
            (string)r.sku,
            (string)r.produto,
            (Guid)r.loteid,
            (string)r.lote,
            (decimal)r.quantidade,
            (decimal)r.precounitario,
            (decimal)r.desconto,
            (decimal)r.subtotal,
            (decimal)r.custounitario,
            (decimal)r.custototal
        )).ToList();

        var tituloRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT t.id AS Id,
                   t.venda_id AS VendaId,
                   @vendaNumero AS VendaNumero,
                   t.numero AS Numero,
                   @pagadorNome AS Pagador,
                   t.parcela AS Parcela,
                   t.total_parcelas AS TotalParcelas,
                   t.data_emissao AS DataEmissao,
                   t.data_vencimento AS DataVencimento,
                   t.valor_principal AS ValorPrincipal,
                   t.valor_recebido AS ValorRecebido,
                   t.saldo_aberto AS SaldoAberto,
                   t.situacao AS Situacao
            FROM plantaopro.adm360_titulos_receber t
            WHERE t.venda_id = @id AND t.tenant_id = @tenantId
            ORDER BY t.parcela",
            new { id, tenantId, vendaNumero = (string)v.numero, pagadorNome = (string)v.pagador }, cancellationToken: ct));

        var titulos = tituloRows.Select(r => new TituloReceberResumoDto(
            (Guid)r.id,
            (Guid)r.vendaid,
            (string)r.vendanumero,
            (string)r.numero,
            (string)r.pagador,
            (int)r.parcela,
            (int)r.totalparcelas,
            ToDateOnly(r.dataemissao),
            ToDateOnly(r.datavencimento),
            (decimal)r.valorprincipal,
            (decimal)r.valorrecebido,
            (decimal)r.saldoaberto,
            (string)r.situacao,
            ToDateOnly(r.datavencimento) < DateOnly.FromDateTime(DateTime.UtcNow) && (decimal)r.saldoaberto > 0
        )).ToList();

        return new VendaDetalhesDto(
            (Guid)v.id,
            (string)v.numero,
            (Guid?)v.valorizacaoid,
            (Guid?)v.valeid,
            (string?)v.valenumero,
            (Guid)v.clienteid,
            (string)v.cliente,
            (Guid)v.pagadorid,
            (string)v.pagador,
            (Guid?)v.vendedorid,
            (string?)v.vendedor,
            ToDateOnly(v.competencia),
            (decimal)v.totalbruto,
            (decimal)v.desconto,
            (decimal)v.totalliquido,
            (decimal)v.totalcusto,
            (decimal)v.comissaopercentual,
            (decimal)v.comissaoprevista,
            (string)v.condicaopagamento,
            (int)v.quantidadeparcelas,
            (string)v.situacao,
            (string?)v.observacoes,
            (DateTime)v.criadoem,
            itens,
            titulos
        );
    }

    public async Task<Guid> ConfirmarVendaAsync(Guid tenantId, Guid usuarioId, ConfirmarVendaCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash(
            "CONFIRMAR_VENDA",
            tenantId,
            command.ValorizacaoId,
            command.CondicaoPagamento,
            command.QuantidadeParcelas,
            command.Observacoes ?? ""
        );
        Guid vendaId = Guid.Empty;

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
                    vendaId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_vendas
                        WHERE valorizacao_id = @ValorizacaoId AND tenant_id = @tenantId",
                        new { command.ValorizacaoId, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            // Verifica se a valorização já possui venda
            var vendaExistente = await cn.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition(@"
                SELECT id FROM plantaopro.adm360_vendas
                WHERE valorizacao_id = @ValorizacaoId AND tenant_id = @tenantId",
                new { command.ValorizacaoId, tenantId }, tx, cancellationToken: ct));

            if (vendaExistente.HasValue)
            {
                vendaId = vendaExistente.Value;
                return;
            }

            var val = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, hospital_id, pagador_id, vendedor_id, total_bruto, desconto,
                       total_liquido, total_custo, comissao_percentual, comissao_prevista
                FROM plantaopro.adm360_valorizacoes
                WHERE id = @ValorizacaoId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValorizacaoId, tenantId }, tx, cancellationToken: ct));

            if (val is null) throw new InvalidOperationException("Valorização não encontrada.");

            var valItens = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT produto_id, lote_id, quantidade_consumida, preco_unitario,
                       desconto, subtotal, custo_unitario, custo_total
                FROM plantaopro.adm360_valorizacao_itens
                WHERE valorizacao_id = @ValorizacaoId AND tenant_id = @tenantId",
                new { command.ValorizacaoId, tenantId }, tx, cancellationToken: ct))).ToList();

            if (valItens.Count == 0) throw new InvalidOperationException("Valorização sem itens comerciais.");

            var seq = await cn.ExecuteScalarAsync<long>("SELECT nextval('plantaopro.adm360_venda_numero')", transaction: tx);
            var numeroVenda = $"VEN-{seq:D6}";
            vendaId = Guid.NewGuid();
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_vendas(
                    id, tenant_id, numero, origem_tipo, origem_id, valorizacao_id,
                    cliente_id, pagador_id, vendedor_id, competencia, total_bruto,
                    desconto, total_liquido, total_custo, comissao_percentual,
                    comissao_prevista, condicao_pagamento, quantidade_parcelas,
                    situacao, observacoes, idempotency_key, created_by
                ) VALUES(
                    @vendaId, @tenantId, @numeroVenda, 'VALE_CONSIGNACAO', @ValorizacaoId, @ValorizacaoId,
                    @clienteId, @pagadorId, @vendedorId, @hoje, @totalBruto,
                    @desconto, @totalLiquido, @totalCusto, @comissaoPercentual,
                    @comissaoPrevista, @condicao, @parcelas, 'CONFIRMADA', @Observacoes, @key, @usuarioId
                )",
                new
                {
                    vendaId, tenantId, numeroVenda, command.ValorizacaoId,
                    clienteId = (Guid)val.hospital_id,
                    pagadorId = (Guid)val.pagador_id,
                    vendedorId = (Guid?)val.vendedor_id,
                    hoje,
                    totalBruto = (decimal)val.total_bruto,
                    desconto = (decimal)val.desconto,
                    totalLiquido = (decimal)val.total_liquido,
                    totalCusto = (decimal)val.total_custo,
                    comissaoPercentual = (decimal)val.comissao_percentual,
                    comissaoPrevista = (decimal)val.comissao_prevista,
                    condicao = command.CondicaoPagamento,
                    parcelas = command.QuantidadeParcelas,
                    command.Observacoes,
                    key = command.IdempotencyKey,
                    usuarioId
                }, tx, cancellationToken: ct));

            foreach (var vi in valItens)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_venda_itens(
                        id, tenant_id, venda_id, produto_id, lote_id, quantidade,
                        preco_unitario, desconto, subtotal, custo_unitario, custo_total
                    ) VALUES(
                        gen_random_uuid(), @tenantId, @vendaId, @produto_id, @lote_id, @quantidade_consumida,
                        @preco_unitario, @desconto, @subtotal, @custo_unitario, @custo_total
                    )",
                    new
                    {
                        tenantId, vendaId, vi.produto_id, vi.lote_id, vi.quantidade_consumida,
                        vi.preco_unitario, vi.desconto, vi.subtotal, vi.custo_unitario, vi.custo_total
                    }, tx, cancellationToken: ct));
            }

            // Geração das parcelas de Contas a Receber
            decimal totalLiquido = (decimal)val.total_liquido;
            int numParcelas = command.QuantidadeParcelas;
            var parcelas = VendaRegras.GerarParcelas(totalLiquido, numParcelas, hoje);

            foreach (var p in parcelas)
            {
                var tituloId = Guid.NewGuid();
                var numeroTitulo = $"{numeroVenda}/{p.Parcela:D2}";

                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_titulos_receber(
                        id, tenant_id, venda_id, numero, pagador_id, parcela, total_parcelas,
                        data_emissao, data_vencimento, valor_principal, valor_desconto,
                        valor_juros, valor_recebido, saldo_aberto, situacao, created_by
                    ) VALUES(
                        @tituloId, @tenantId, @vendaId, @numeroTitulo, @pagadorId, @Parcela, @numParcelas,
                        @hoje, @Vencimento, @Valor, 0, 0, 0, @Valor, 'ABERTO', @usuarioId
                    )",
                    new
                    {
                        tituloId, tenantId, vendaId, numeroTitulo,
                        pagadorId = (Guid)val.pagador_id,
                        p.Parcela, numParcelas, hoje,
                        p.Vencimento, p.Valor, usuarioId
                    }, tx, cancellationToken: ct));
            }

            // Registra operação de confirmação de venda
            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'CONFIRMAR_VENDA', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

        }, ct);

        return vendaId;
    }

    public async Task CancelarVendaAsync(Guid tenantId, Guid usuarioId, Guid vendaId, string motivo, CancellationToken ct)
    {
        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var venda = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, situacao FROM plantaopro.adm360_vendas
                WHERE id = @vendaId AND tenant_id = @tenantId FOR UPDATE",
                new { vendaId, tenantId }, tx, cancellationToken: ct));

            if (venda is null) throw new InvalidOperationException("Venda não encontrada.");
            if (venda.situacao == "CANCELADA") return;

            // Verifica se algum título já foi recebido/baixado
            var totalBaixas = await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"
                SELECT COUNT(*) FROM plantaopro.adm360_titulo_baixas b
                JOIN plantaopro.adm360_titulos_receber t ON t.id = b.titulo_id AND t.tenant_id = b.tenant_id
                WHERE t.venda_id = @vendaId AND t.tenant_id = @tenantId AND b.estornado = false",
                new { vendaId, tenantId }, tx, cancellationToken: ct));

            if (totalBaixas > 0)
                throw new InvalidOperationException("Não é possível cancelar uma venda com títulos já baixados/recebidos. Estorne os recebimentos antes de cancelar.");

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_titulos_receber
                SET situacao = 'CANCELADO', updated_at = now()
                WHERE venda_id = @vendaId AND tenant_id = @tenantId",
                new { vendaId, tenantId }, tx, cancellationToken: ct));

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_vendas
                SET situacao = 'CANCELADA',
                    observacoes = COALESCE(observacoes || E'\n', '') || 'Cancelamento: ' || @motivo,
                    versao = versao + 1
                WHERE id = @vendaId AND tenant_id = @tenantId",
                new { vendaId, tenantId, motivo }, tx, cancellationToken: ct));

        }, ct);
    }
}
