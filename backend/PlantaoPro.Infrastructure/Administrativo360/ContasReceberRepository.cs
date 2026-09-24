using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class ContasReceberRepository : Adm360Repository, IContasReceberRepository
{
    public ContasReceberRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<TituloReceberResumoDto>> ListarAsync(
        Guid tenantId, string? busca, string? situacao, Guid? pagadorId, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT t.id AS Id,
                   t.venda_id AS VendaId,
                   v.numero AS VendaNumero,
                   t.numero AS Numero,
                   COALESCE(p.nome, c.nome, hosp.nome, 'Pagador') AS Pagador,
                   t.parcela AS Parcela,
                   t.total_parcelas AS TotalParcelas,
                   t.data_emissao AS DataEmissao,
                   t.data_vencimento AS DataVencimento,
                   t.valor_principal AS ValorPrincipal,
                   t.valor_recebido AS ValorRecebido,
                   t.saldo_aberto AS SaldoAberto,
                   t.situacao AS Situacao
            FROM plantaopro.adm360_titulos_receber t
            JOIN plantaopro.adm360_vendas v ON v.id = t.venda_id AND v.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = t.pagador_id AND p.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = t.tenant_id
            WHERE t.tenant_id = @tenantId
              AND (@busca::text IS NULL OR t.numero ILIKE '%' || @busca || '%' OR v.numero ILIKE '%' || @busca || '%' OR p.nome ILIKE '%' || @busca || '%')
              AND (@situacao::varchar IS NULL OR t.situacao = @situacao)
              AND (@pagadorId::uuid IS NULL OR t.pagador_id = @pagadorId::uuid)
              AND (@inicio::date IS NULL OR t.data_vencimento >= @inicio::date)
              AND (@fim::date IS NULL OR t.data_vencimento <= @fim::date)
            ORDER BY t.data_vencimento, t.numero",
            new { tenantId, busca, situacao, pagadorId, inicio, fim }, cancellationToken: ct));

        return rows.Select(r => new TituloReceberResumoDto(
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
    }

    public async Task<TituloReceberDetalhesDto?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        var t = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT t.id AS Id,
                   t.venda_id AS VendaId,
                   v.numero AS VendaNumero,
                   t.numero AS Numero,
                   t.pagador_id AS PagadorId,
                   COALESCE(p.nome, c.nome, hosp.nome, 'Pagador') AS Pagador,
                   t.parcela AS Parcela,
                   t.total_parcelas AS TotalParcelas,
                   t.data_emissao AS DataEmissao,
                   t.data_vencimento AS DataVencimento,
                   t.valor_principal AS ValorPrincipal,
                   t.valor_desconto AS ValorDesconto,
                   t.valor_juros AS ValorJuros,
                   t.valor_recebido AS ValorRecebido,
                   t.saldo_aberto AS SaldoAberto,
                   t.situacao AS Situacao
            FROM plantaopro.adm360_titulos_receber t
            JOIN plantaopro.adm360_vendas v ON v.id = t.venda_id AND v.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = t.pagador_id AND p.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = t.tenant_id
            WHERE t.id = @id AND t.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (t is null) return null;

        var baixaRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT b.id AS Id,
                   b.titulo_id AS TituloId,
                   b.conta_id AS ContaId,
                   cf.nome AS ContaNome,
                   b.data_recebimento AS DataRecebimento,
                   b.valor_recebido AS ValorRecebido,
                   b.meio_pagamento AS MeioPagamento,
                   b.referencia AS Referencia,
                   b.estornado AS Estornado,
                   u.nome AS RecebidoPor,
                   b.created_at AS CriadoEm
            FROM plantaopro.adm360_titulo_baixas b
            JOIN plantaopro.adm360_contas_financeiras cf ON cf.id = b.conta_id AND cf.tenant_id = b.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = b.recebido_por AND u.tenant_id = b.tenant_id
            WHERE b.titulo_id = @id AND b.tenant_id = @tenantId
            ORDER BY b.created_at DESC",
            new { id, tenantId }, cancellationToken: ct));

        var baixas = baixaRows.Select(r => new TituloBaixaDto(
            (Guid)r.id,
            (Guid)r.tituloid,
            (Guid)r.contaid,
            (string)r.contanome,
            ToDateOnly(r.datarecebimento),
            (decimal)r.valorrecebido,
            (string)r.meiopagamento,
            (string?)r.referencia,
            (bool)r.estornado,
            (string?)r.recebidopor,
            (DateTime)r.criadoem
        )).ToList();

        var estornoRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT e.id AS Id,
                   e.baixa_id AS BaixaId,
                   e.valor_estornado AS ValorEstornado,
                   e.motivo AS Motivo,
                   u.nome AS EstornadoPor,
                   e.created_at AS CriadoEm
            FROM plantaopro.adm360_titulo_estornos e
            LEFT JOIN plantaopro.usuarios u ON u.id = e.estornado_por AND u.tenant_id = e.tenant_id
            WHERE e.titulo_id = @id AND e.tenant_id = @tenantId
            ORDER BY e.created_at DESC",
            new { id, tenantId }, cancellationToken: ct));

        var estornos = estornoRows.Select(r => new TituloEstornoDto(
            (Guid)r.id,
            (Guid)r.baixaid,
            (decimal)r.valorestornado,
            (string)r.motivo,
            (string?)r.estornadopor,
            (DateTime)r.criadoem
        )).ToList();

        return new TituloReceberDetalhesDto(
            (Guid)t.id,
            (Guid)t.vendaid,
            (string)t.vendanumero,
            (string)t.numero,
            (Guid)t.pagadorid,
            (string)t.pagador,
            (int)t.parcela,
            (int)t.totalparcelas,
            ToDateOnly(t.dataemissao),
            ToDateOnly(t.datavencimento),
            (decimal)t.valorprincipal,
            (decimal)t.valordesconto,
            (decimal)t.valorjuros,
            (decimal)t.valorrecebido,
            (decimal)t.saldoaberto,
            (string)t.situacao,
            ToDateOnly(t.datavencimento) < DateOnly.FromDateTime(DateTime.UtcNow) && (decimal)t.saldoaberto > 0,
            baixas,
            estornos
        );
    }

    public async Task<Guid> ReceberAsync(Guid tenantId, Guid usuarioId, ReceberTituloCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash("RECEBER_TITULO", tenantId, command.TituloId, command.Valor, command.ContaId);
        Guid baixaId = Guid.Empty;

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
                    baixaId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_titulo_baixas
                        WHERE idempotency_key = @key AND tenant_id = @tenantId",
                        new { key = command.IdempotencyKey, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            var titulo = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, venda_id, numero, saldo_aberto, valor_recebido, situacao
                FROM plantaopro.adm360_titulos_receber
                WHERE id = @TituloId AND tenant_id = @tenantId FOR UPDATE",
                new { command.TituloId, tenantId }, tx, cancellationToken: ct));

            if (titulo is null) throw new InvalidOperationException("Título a receber não encontrado.");
            if (titulo.situacao == "CANCELADO") throw new InvalidOperationException("Título cancelado não pode receber pagamentos.");

            TituloRegras.ValidarBaixa((decimal)titulo.saldo_aberto, command.Valor);

            var conta = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, nome, ativo FROM plantaopro.adm360_contas_financeiras
                WHERE id = @ContaId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ContaId, tenantId }, tx, cancellationToken: ct));

            if (conta is null) throw new InvalidOperationException("Conta financeira de destino não encontrada.");
            if (!(bool)conta.ativo) throw new InvalidOperationException("Conta financeira inativa não pode receber lançamentos.");

            baixaId = Guid.NewGuid();
            var movId = Guid.NewGuid();

            // Registra movimento financeiro de entrada no caixa/banco
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos_financeiros(
                    id, tenant_id, conta_id, tipo, valor, data_movimento,
                    descricao, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    @movId, @tenantId, @ContaId, 'ENTRADA', @Valor, @DataRecebimento,
                    'Recebimento título ' || @numero, 'RECEBIMENTO_TITULO', @baixaId, @keyMov, @usuarioId
                )",
                new
                {
                    movId, tenantId, command.ContaId, command.Valor, command.DataRecebimento,
                    numero = (string)titulo.numero, baixaId, keyMov = $"{command.IdempotencyKey}:MOV", usuarioId
                }, tx, cancellationToken: ct));

            // Registra baixa do título
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_titulo_baixas(
                    id, tenant_id, titulo_id, conta_id, data_recebimento,
                    valor_recebido, meio_pagamento, referencia, movimento_financeiro_id,
                    estornado, idempotency_key, recebido_por
                ) VALUES(
                    @baixaId, @tenantId, @TituloId, @ContaId, @DataRecebimento,
                    @Valor, @MeioPagamento, @Referencia, @movId,
                    false, @key, @usuarioId
                )",
                new
                {
                    baixaId, tenantId, command.TituloId, command.ContaId, command.DataRecebimento,
                    command.Valor, command.MeioPagamento, command.Referencia, movId,
                    key = command.IdempotencyKey, usuarioId
                }, tx, cancellationToken: ct));

            // Atualiza saldo e situação do título
            decimal novoSaldo = (decimal)titulo.saldo_aberto - command.Valor;
            string novaSituacao = TituloRegras.DefinirNovaSituacao(novoSaldo);

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_titulos_receber
                SET valor_recebido = valor_recebido + @Valor,
                    saldo_aberto = @novoSaldo,
                    situacao = @novaSituacao,
                    versao = versao + 1,
                    updated_at = now()
                WHERE id = @TituloId AND tenant_id = @tenantId",
                new { command.Valor, novoSaldo, novaSituacao, command.TituloId, tenantId }, tx, cancellationToken: ct));

            // Apropriação proporcional de comissão
            var venda = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT vendedor_id, comissao_percentual FROM plantaopro.adm360_vendas
                WHERE id = @vendaId AND tenant_id = @tenantId",
                new { vendaId = (Guid)titulo.venda_id, tenantId }, tx, cancellationToken: ct));

            if (venda is not null && venda.vendedor_id is not null && (decimal)venda.comissao_percentual > 0)
            {
                Guid vendedorId = (Guid)venda.vendedor_id;
                decimal pct = (decimal)venda.comissao_percentual;

                var comissoesAnteriores = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                    SELECT COALESCE(SUM(valor_comissao), 0)
                    FROM plantaopro.adm360_comissoes_apropriadas
                    WHERE venda_id = @vendaId AND tenant_id = @tenantId AND situacao = 'APROPRIADA'",
                    new { vendaId = (Guid)titulo.venda_id, tenantId }, tx, cancellationToken: ct));

                var baixasAnteriores = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                    SELECT COALESCE(SUM(b.valor_recebido), 0)
                    FROM plantaopro.adm360_titulo_baixas b
                    JOIN plantaopro.adm360_titulos_receber t ON t.id = b.titulo_id AND t.tenant_id = b.tenant_id
                    WHERE t.venda_id = @vendaId AND t.tenant_id = @tenantId AND b.estornado = false AND b.id <> @baixaId",
                    new { vendaId = (Guid)titulo.venda_id, tenantId, baixaId }, tx, cancellationToken: ct));

                decimal valorComissao = ComissaoRegras.CalcularComissaoApropriada(baixasAnteriores, command.Valor, pct, comissoesAnteriores);

                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_comissoes_apropriadas(
                        id, tenant_id, venda_id, baixa_id, vendedor_id,
                        base_calculo, percentual, valor_comissao, situacao
                    ) VALUES(
                        gen_random_uuid(), @tenantId, @vendaId, @baixaId, @vendedorId,
                        @Valor, @pct, @valorComissao, 'APROPRIADA'
                    )",
                    new
                    {
                        tenantId, vendaId = (Guid)titulo.venda_id, baixaId, vendedorId,
                        command.Valor, pct, valorComissao
                    }, tx, cancellationToken: ct));
            }

            // Registra operação de recebimento
            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'RECEBER_TITULO', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

        }, ct);

        return baixaId;
    }

    public async Task<Guid> EstornarAsync(Guid tenantId, Guid usuarioId, EstornarBaixaCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash("ESTORNO_BAIXA", tenantId, command.BaixaId);
        Guid estornoId = Guid.Empty;

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
                    estornoId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_titulo_estornos
                        WHERE baixa_id = @BaixaId AND tenant_id = @tenantId",
                        new { command.BaixaId, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            var baixa = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT b.id, b.titulo_id, b.conta_id, b.valor_recebido, b.estornado, t.venda_id
                FROM plantaopro.adm360_titulo_baixas b
                JOIN plantaopro.adm360_titulos_receber t ON t.id = b.titulo_id AND t.tenant_id = b.tenant_id
                WHERE b.id = @BaixaId AND b.tenant_id = @tenantId FOR UPDATE",
                new { command.BaixaId, tenantId }, tx, cancellationToken: ct));

            if (baixa is null) throw new InvalidOperationException("Baixa não encontrada.");

            TituloRegras.ValidarEstorno((bool)baixa.estornado, (decimal)baixa.valor_recebido, (decimal)baixa.valor_recebido);

            estornoId = Guid.NewGuid();
            var movEstornoId = Guid.NewGuid();
            decimal valorEstorno = (decimal)baixa.valor_recebido;

            // Movimento financeiro de saída compensatória
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos_financeiros(
                    id, tenant_id, conta_id, tipo, valor, data_movimento,
                    descricao, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    @movEstornoId, @tenantId, @contaId, 'SAIDA', @valorEstorno, current_date,
                    'Estorno de recebimento: ' || @motivo, 'ESTORNO_RECEBIMENTO', @estornoId, @keyMov, @usuarioId
                )",
                new
                {
                    movEstornoId, tenantId, contaId = (Guid)baixa.conta_id, valorEstorno,
                    motivo = command.Motivo, estornoId, keyMov = $"{command.IdempotencyKey}:MOV", usuarioId
                }, tx, cancellationToken: ct));

            // Registra evento de estorno
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_titulo_estornos(
                    id, tenant_id, baixa_id, titulo_id, valor_estornado,
                    motivo, movimento_financeiro_id, idempotency_key, estornado_por
                ) VALUES(
                    @estornoId, @tenantId, @BaixaId, @tituloId, @valorEstorno,
                    @Motivo, @movEstornoId, @key, @usuarioId
                )",
                new
                {
                    estornoId, tenantId, command.BaixaId, tituloId = (Guid)baixa.titulo_id,
                    valorEstorno, command.Motivo, movEstornoId, key = command.IdempotencyKey, usuarioId
                }, tx, cancellationToken: ct));

            // Marca baixa como estornada
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_titulo_baixas
                SET estornado = true
                WHERE id = @BaixaId AND tenant_id = @tenantId",
                new { command.BaixaId, tenantId }, tx, cancellationToken: ct));

            // Reverte saldo do título
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_titulos_receber
                SET valor_recebido = valor_recebido - @valorEstorno,
                    saldo_aberto = saldo_aberto + @valorEstorno,
                    situacao = (CASE WHEN valor_recebido - @valorEstorno <= 0 THEN 'ABERTO' ELSE 'PARCIAL' END),
                    versao = versao + 1,
                    updated_at = now()
                WHERE id = @tituloId AND tenant_id = @tenantId",
                new { valorEstorno, tituloId = (Guid)baixa.titulo_id, tenantId }, tx, cancellationToken: ct));

            // Reverte comissão apropriada desta baixa
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_comissoes_apropriadas
                SET situacao = 'ESTORNADA'
                WHERE baixa_id = @BaixaId AND tenant_id = @tenantId",
                new { command.BaixaId, tenantId }, tx, cancellationToken: ct));

            // Registra operação de estorno
            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'ESTORNO_BAIXA', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

        }, ct);

        return estornoId;
    }
}
