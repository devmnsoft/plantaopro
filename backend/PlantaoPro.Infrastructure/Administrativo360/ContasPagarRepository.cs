using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class ContasPagarRepository : Adm360Repository, IContasPagarRepository
{
    public ContasPagarRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<TituloPagarResumoDto>> ListarAsync(
        Guid tenantId, string? busca, string? situacao, Guid? fornecedorId, string? centroCusto, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT t.id AS Id,
                   t.numero AS Numero,
                   t.fornecedor_id AS FornecedorId,
                   f.nome AS Fornecedor,
                   t.origem_tipo AS OrigemTipo,
                   t.origem_id AS OrigemId,
                   t.documento AS Documento,
                   t.competencia AS Competencia,
                   t.data_emissao AS DataEmissao,
                   t.data_vencimento AS DataVencimento,
                   t.parcela AS Parcela,
                   t.total_parcelas AS TotalParcelas,
                   t.valor_principal AS ValorPrincipal,
                   t.valor_pago AS ValorPago,
                   t.saldo_aberto AS SaldoAberto,
                   t.situacao AS Situacao,
                   t.centro_custo AS CentroCusto
            FROM plantaopro.adm360_titulos_pagar t
            JOIN plantaopro.adm360_parceiros f ON f.id = t.fornecedor_id AND f.tenant_id = t.tenant_id
            WHERE t.tenant_id = @tenantId
              AND (@busca::text IS NULL OR t.numero ILIKE '%' || @busca || '%' OR f.nome ILIKE '%' || @busca || '%' OR t.documento ILIKE '%' || @busca || '%')
              AND (@situacao::varchar IS NULL OR t.situacao = @situacao)
              AND (@fornecedorId::uuid IS NULL OR t.fornecedor_id = @fornecedorId::uuid)
              AND (@centroCusto::text IS NULL OR t.centro_custo = @centroCusto)
              AND (@inicio::date IS NULL OR t.data_vencimento >= @inicio::date)
              AND (@fim::date IS NULL OR t.data_vencimento <= @fim::date)
            ORDER BY t.data_vencimento ASC, t.numero ASC",
            new { tenantId, busca, situacao, fornecedorId, centroCusto, inicio, fim }, cancellationToken: ct));

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        return rows.Select(r => new TituloPagarResumoDto(
            (Guid)r.id,
            (string)r.numero,
            (Guid)r.fornecedorid,
            (string)r.fornecedor,
            (string)r.origemtipo,
            (Guid?)r.origemid,
            (string?)r.documento,
            ToDateOnly(r.competencia),
            ToDateOnly(r.dataemissao),
            ToDateOnly(r.datavencimento),
            (int)r.parcela,
            (int)r.totalparcelas,
            (decimal)r.valorprincipal,
            (decimal)r.valorpago,
            (decimal)r.saldoaberto,
            (string)r.situacao,
            (string?)r.centrocusto,
            ToDateOnly(r.datavencimento) < hoje && (decimal)r.saldoaberto > 0
        )).ToList();
    }

    public async Task<TituloPagarDetalhesDto?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        var t = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT t.id AS Id,
                   t.numero AS Numero,
                   t.fornecedor_id AS FornecedorId,
                   f.nome AS Fornecedor,
                   t.origem_tipo AS OrigemTipo,
                   t.origem_id AS OrigemId,
                   t.documento AS Documento,
                   t.competencia AS Competencia,
                   t.data_emissao AS DataEmissao,
                   t.data_vencimento AS DataVencimento,
                   t.parcela AS Parcela,
                   t.total_parcelas AS TotalParcelas,
                   t.valor_principal AS ValorPrincipal,
                   t.valor_desconto AS ValorDesconto,
                   t.valor_juros AS ValorJuros,
                   t.valor_pago AS ValorPago,
                   t.saldo_aberto AS SaldoAberto,
                   t.situacao AS Situacao,
                   t.centro_custo AS CentroCusto,
                   t.observacoes AS Observacoes,
                   t.aprovado_em AS AprovadoEm,
                   u_apr.nome AS AprovadoPor,
                   u_cr.nome AS CriadoPor,
                   t.created_at AS CriadoEm
            FROM plantaopro.adm360_titulos_pagar t
            JOIN plantaopro.adm360_parceiros f ON f.id = t.fornecedor_id AND f.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.usuarios u_apr ON u_apr.id = t.aprovado_por AND u_apr.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.usuarios u_cr ON u_cr.id = t.created_by AND u_cr.tenant_id = t.tenant_id
            WHERE t.id = @id AND t.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (t is null) return null;

        var pagamentosRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT p.id AS Id,
                   p.titulo_id AS TituloId,
                   p.conta_id AS ContaId,
                   cf.nome AS ContaNome,
                   p.data_pagamento AS DataPagamento,
                   p.valor_pago AS ValorPago,
                   p.meio_pagamento AS MeioPagamento,
                   p.referencia AS Referencia,
                   p.estornado AS Estornado,
                   u.nome AS PagoPor,
                   p.created_at AS CriadoEm
            FROM plantaopro.adm360_titulo_pagamentos p
            JOIN plantaopro.adm360_contas_financeiras cf ON cf.id = p.conta_id AND cf.tenant_id = p.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = p.pago_por AND u.tenant_id = p.tenant_id
            WHERE p.titulo_id = @id AND p.tenant_id = @tenantId
            ORDER BY p.created_at DESC",
            new { id, tenantId }, cancellationToken: ct));

        var pagamentos = pagamentosRows.Select(r => new TituloPagamentoDto(
            (Guid)r.id,
            (Guid)r.tituloid,
            (Guid)r.contaid,
            (string)r.contanome,
            ToDateOnly(r.datapagamento),
            (decimal)r.valorpago,
            (string)r.meiopagamento,
            (string?)r.referencia,
            (bool)r.estornado,
            (string?)r.pagopor,
            (DateTime)r.criadoem
        )).ToList();

        var estornosRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT e.id AS Id,
                   e.pagamento_id AS PagamentoId,
                   e.valor_estornado AS ValorEstornado,
                   e.motivo AS Motivo,
                   u.nome AS EstornadoPor,
                   e.created_at AS CriadoEm
            FROM plantaopro.adm360_pagamento_estornos e
            LEFT JOIN plantaopro.usuarios u ON u.id = e.estornado_por AND u.tenant_id = e.tenant_id
            WHERE e.titulo_id = @id AND e.tenant_id = @tenantId
            ORDER BY e.created_at DESC",
            new { id, tenantId }, cancellationToken: ct));

        var estornos = estornosRows.Select(r => new PagamentoEstornoDto(
            (Guid)r.id,
            (Guid)r.pagamentoid,
            (decimal)r.valorestornado,
            (string)r.motivo,
            (string?)r.estornadopor,
            (DateTime)r.criadoem
        )).ToList();

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        return new TituloPagarDetalhesDto(
            (Guid)t.id,
            (string)t.numero,
            (Guid)t.fornecedorid,
            (string)t.fornecedor,
            (string)t.origemtipo,
            (Guid?)t.origemid,
            (string?)t.documento,
            ToDateOnly(t.competencia),
            ToDateOnly(t.dataemissao),
            ToDateOnly(t.datavencimento),
            (int)t.parcela,
            (int)t.totalparcelas,
            (decimal)t.valorprincipal,
            (decimal)t.valordesconto,
            (decimal)t.valorjuros,
            (decimal)t.valorpago,
            (decimal)t.saldoaberto,
            (string)t.situacao,
            (string?)t.centrocusto,
            (string?)t.observacoes,
            t.aprovadoem is not null ? (DateTime?)t.aprovadoem : null,
            (string?)t.aprovadopor,
            (string?)t.criadopor,
            (DateTime)t.criadoem,
            ToDateOnly(t.datavencimento) < hoje && (decimal)t.saldoaberto > 0,
            pagamentos,
            estornos
        );
    }

    public async Task<Guid> CriarDespesaManualAsync(Guid tenantId, Guid usuarioId, CriarDespesaManualCommand command, CancellationToken ct)
    {
        if (command.ValorPrincipal <= 0)
            throw new ArgumentException("O valor principal da despesa deve ser positivo.");

        var payloadHash = IdempotenciaHelper.CalcularHash(
            "CRIAR_DESPESA_MANUAL",
            tenantId,
            command.FornecedorId,
            command.Documento ?? "",
            command.ValorPrincipal,
            command.Competencia,
            command.DataVencimento,
            command.CentroCusto ?? ""
        );

        Guid tituloId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, payload_hash, resultado FROM plantaopro.adm360_operacoes
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (opExistente.payload_hash == payloadHash)
                {
                    tituloId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_titulos_pagar
                        WHERE idempotency_key = @key AND tenant_id = @tenantId",
                        new { key = command.IdempotencyKey, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            var parceiroAtivo = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS(SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @id AND tenant_id = @tenantId AND ativo)",
                new { id = command.FornecedorId, tenantId }, tx, cancellationToken: ct));

            if (!parceiroAtivo)
                throw new InvalidOperationException("Fornecedor/Beneficiário inativo ou não encontrado.");

            var seq = await cn.ExecuteScalarAsync<long>("SELECT nextval('plantaopro.adm360_titulo_pagar_numero')", transaction: tx);
            var numeroTitulo = $"PAG-{seq:D6}";
            tituloId = Guid.NewGuid();
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_titulos_pagar(
                    id, tenant_id, numero, fornecedor_id, origem_tipo, documento,
                    competencia, data_emissao, data_vencimento, parcela, total_parcelas,
                    valor_principal, valor_pago, saldo_aberto, situacao, centro_custo,
                    observacoes, idempotency_key, created_by
                ) VALUES(
                    @tituloId, @tenantId, @numeroTitulo, @FornecedorId, 'DESPESA_MANUAL', @Documento,
                    @Competencia, @hoje, @DataVencimento, 1, 1,
                    @ValorPrincipal, 0, @ValorPrincipal, 'PENDENTE_APROVACAO', @CentroCusto,
                    @Observacoes, @key, @usuarioId
                )",
                new
                {
                    tituloId, tenantId, numeroTitulo, command.FornecedorId, command.Documento,
                    command.Competencia, hoje, command.DataVencimento, command.ValorPrincipal,
                    command.CentroCusto, command.Observacoes, key = command.IdempotencyKey, usuarioId
                }, tx, cancellationToken: ct));

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'CRIAR_DESPESA_MANUAL', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));
        }, ct);

        return tituloId;
    }

    public async Task AprovarAsync(Guid tenantId, Guid usuarioId, AprovarTituloPagarCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash("APROVAR_TITULO_PAGAR", tenantId, command.TituloId);

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, payload_hash, resultado FROM plantaopro.adm360_operacoes
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (opExistente.payload_hash == payloadHash) return;
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            var titulo = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, situacao FROM plantaopro.adm360_titulos_pagar
                WHERE id = @TituloId AND tenant_id = @tenantId FOR UPDATE",
                new { command.TituloId, tenantId }, tx, cancellationToken: ct));

            if (titulo is null) throw new KeyNotFoundException("Título a pagar não encontrado.");
            ContasPagarRegras.ValidarAprovacao((string)titulo.situacao);

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_titulos_pagar
                SET situacao = 'APROVADO',
                    aprovado_em = now(),
                    aprovado_por = @usuarioId,
                    versao = versao + 1,
                    updated_at = now()
                WHERE id = @TituloId AND tenant_id = @tenantId",
                new { command.TituloId, tenantId, usuarioId }, tx, cancellationToken: ct));

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'APROVAR_TITULO_PAGAR', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task<Guid> PagarAsync(Guid tenantId, Guid usuarioId, PagarTituloCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash(
            "PAGAR_TITULO",
            tenantId,
            command.TituloId,
            command.ContaId,
            command.Valor,
            command.DataPagamento,
            command.MeioPagamento,
            command.Referencia ?? ""
        );

        Guid pagamentoId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, payload_hash, resultado FROM plantaopro.adm360_operacoes
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (opExistente.payload_hash == payloadHash)
                {
                    pagamentoId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_titulo_pagamentos
                        WHERE idempotency_key = @key AND tenant_id = @tenantId",
                        new { key = command.IdempotencyKey, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            var titulo = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, numero, saldo_aberto, valor_pago, situacao, origem_tipo, origem_id
                FROM plantaopro.adm360_titulos_pagar
                WHERE id = @TituloId AND tenant_id = @tenantId FOR UPDATE",
                new { command.TituloId, tenantId }, tx, cancellationToken: ct));

            if (titulo is null) throw new KeyNotFoundException("Título a pagar não encontrado.");
            ContasPagarRegras.ValidarPagamento((string)titulo.situacao, (decimal)titulo.saldo_aberto, command.Valor);

            var conta = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, nome, ativo, saldo_inicial,
                       (saldo_inicial +
                        COALESCE((SELECT SUM(m.valor) FROM plantaopro.adm360_movimentos_financeiros m WHERE m.conta_id = c.id AND m.tenant_id = c.tenant_id AND m.tipo = 'ENTRADA'), 0) -
                        COALESCE((SELECT SUM(m.valor) FROM plantaopro.adm360_movimentos_financeiros m WHERE m.conta_id = c.id AND m.tenant_id = c.tenant_id AND m.tipo = 'SAIDA'), 0)
                       ) AS SaldoAtual
                FROM plantaopro.adm360_contas_financeiras c
                WHERE id = @ContaId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ContaId, tenantId }, tx, cancellationToken: ct));

            if (conta is null) throw new KeyNotFoundException("Conta financeira não encontrada.");
            if (!(bool)conta.ativo) throw new InvalidOperationException("Conta financeira inativa não pode realizar pagamentos.");

            // Política de bloqueio padrão para saldo insuficiente
            decimal saldoAtual = (decimal)conta.saldoatual;
            CaixaRegras.ValidarSaldoSuficiente(saldoAtual, command.Valor, permitirSaldoNegativo: false);

            // Bloqueio de período fechado
            var fechamentoBloqueador = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT data_fim FROM plantaopro.adm360_caixa_fechamentos
                WHERE tenant_id = @tenantId AND conta_id = @ContaId AND situacao = 'FECHADO' AND data_fim >= @dataPagamento
                ORDER BY data_fim DESC LIMIT 1",
                new { tenantId, command.ContaId, dataPagamento = command.DataPagamento }, tx, cancellationToken: ct));

            if (fechamentoBloqueador is not null)
            {
                DateOnly dataFim = ToDateOnly(fechamentoBloqueador.data_fim);
                throw new InvalidOperationException($"Não é permitido lançar pagamentos na data {command.DataPagamento:dd/MM/yyyy} devido a fechamento de caixa ativo até {dataFim:dd/MM/yyyy}.");
            }

            pagamentoId = Guid.NewGuid();
            var movId = Guid.NewGuid();

            // Movimento de saída realizado
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos_financeiros(
                    id, tenant_id, conta_id, tipo, valor, data_movimento,
                    descricao, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    @movId, @tenantId, @ContaId, 'SAIDA', @Valor, @DataPagamento,
                    'Pagamento manual título ' || @numero, 'PAGAMENTO_TITULO', @pagamentoId, @keyMov, @usuarioId
                )",
                new
                {
                    movId, tenantId, command.ContaId, command.Valor, command.DataPagamento,
                    numero = (string)titulo.numero, pagamentoId, keyMov = $"{command.IdempotencyKey}:MOV", usuarioId
                }, tx, cancellationToken: ct));

            // Registro do pagamento
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_titulo_pagamentos(
                    id, tenant_id, titulo_id, conta_id, data_pagamento,
                    valor_pago, meio_pagamento, referencia, movimento_financeiro_id,
                    estornado, idempotency_key, pago_por
                ) VALUES(
                    @pagamentoId, @tenantId, @TituloId, @ContaId, @DataPagamento,
                    @Valor, @MeioPagamento, @Referencia, @movId,
                    false, @key, @usuarioId
                )",
                new
                {
                    pagamentoId, tenantId, command.TituloId, command.ContaId, command.DataPagamento,
                    command.Valor, command.MeioPagamento, command.Referencia, movId,
                    key = command.IdempotencyKey, usuarioId
                }, tx, cancellationToken: ct));

            // Atualiza obrigação
            decimal novoSaldo = (decimal)titulo.saldo_aberto - command.Valor;
            string novaSituacao = ContasPagarRegras.DefinirNovaSituacaoAposPagamento(novoSaldo);

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_titulos_pagar
                SET valor_pago = valor_pago + @Valor,
                    saldo_aberto = @novoSaldo,
                    situacao = @novaSituacao,
                    versao = versao + 1,
                    updated_at = now()
                WHERE id = @TituloId AND tenant_id = @tenantId",
                new { command.Valor, novoSaldo, novaSituacao, command.TituloId, tenantId }, tx, cancellationToken: ct));

            // Se origem era comissão de vendedor e título foi totalmente quitado, atualiza comissões para PAGA
            if ((string)titulo.origem_tipo == "COMISSAO_VENDEDOR" && novaSituacao == "PAGO")
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_comissoes_apropriadas
                    SET situacao = 'PAGA'
                    WHERE titulo_pagar_id = @TituloId AND tenant_id = @tenantId AND situacao = 'APROPRIADA'",
                    new { command.TituloId, tenantId }, tx, cancellationToken: ct));
            }

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'PAGAR_TITULO', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));
        }, ct);

        return pagamentoId;
    }

    public async Task<Guid> EstornarPagamentoAsync(Guid tenantId, Guid usuarioId, EstornarPagamentoCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Motivo))
            throw new ArgumentException("Motivo do estorno é obrigatório.");

        var payloadHash = IdempotenciaHelper.CalcularHash("ESTORNO_PAGAMENTO", tenantId, command.PagamentoId, command.Motivo);
        Guid estornoId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, payload_hash, resultado FROM plantaopro.adm360_operacoes
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (opExistente.payload_hash == payloadHash)
                {
                    estornoId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_pagamento_estornos
                        WHERE pagamento_id = @PagamentoId AND tenant_id = @tenantId",
                        new { command.PagamentoId, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            var pag = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT p.id, p.titulo_id, p.conta_id, p.valor_pago, p.estornado, t.origem_tipo
                FROM plantaopro.adm360_titulo_pagamentos p
                JOIN plantaopro.adm360_titulos_pagar t ON t.id = p.titulo_id AND t.tenant_id = p.tenant_id
                WHERE p.id = @PagamentoId AND p.tenant_id = @tenantId FOR UPDATE",
                new { command.PagamentoId, tenantId }, tx, cancellationToken: ct));

            if (pag is null) throw new KeyNotFoundException("Pagamento não encontrado.");
            ContasPagarRegras.ValidarEstorno((bool)pag.estornado, (decimal)pag.valor_pago, (decimal)pag.valor_pago);

            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            // Bloqueio de período fechado na conta original
            var fechamentoBloqueador = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT data_fim FROM plantaopro.adm360_caixa_fechamentos
                WHERE tenant_id = @tenantId AND conta_id = @contaId AND situacao = 'FECHADO' AND data_fim >= @hoje
                ORDER BY data_fim DESC LIMIT 1",
                new { tenantId, contaId = (Guid)pag.conta_id, hoje }, tx, cancellationToken: ct));

            if (fechamentoBloqueador is not null)
            {
                DateOnly dataFim = ToDateOnly(fechamentoBloqueador.data_fim);
                throw new InvalidOperationException($"Não é permitido estornar pagamentos na data {hoje:dd/MM/yyyy} devido a fechamento de caixa ativo até {dataFim:dd/MM/yyyy}.");
            }

            estornoId = Guid.NewGuid();
            var movEstornoId = Guid.NewGuid();
            decimal valorEstorno = (decimal)pag.valor_pago;

            // Movimento financeiro de entrada compensatória na conta original
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos_financeiros(
                    id, tenant_id, conta_id, tipo, valor, data_movimento,
                    descricao, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    @movEstornoId, @tenantId, @contaId, 'ENTRADA', @valorEstorno, @hoje,
                    'Estorno de pagamento: ' || @motivo, 'ESTORNO_PAGAMENTO', @estornoId, @keyMov, @usuarioId
                )",
                new
                {
                    movEstornoId, tenantId, contaId = (Guid)pag.conta_id, valorEstorno,
                    hoje, motivo = command.Motivo, estornoId, keyMov = $"{command.IdempotencyKey}:MOV", usuarioId
                }, tx, cancellationToken: ct));

            // Registro do estorno
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_pagamento_estornos(
                    id, tenant_id, pagamento_id, titulo_id, valor_estornado,
                    motivo, movimento_financeiro_id, idempotency_key, estornado_por
                ) VALUES(
                    @estornoId, @tenantId, @PagamentoId, @tituloId, @valorEstorno,
                    @Motivo, @movEstornoId, @key, @usuarioId
                )",
                new
                {
                    estornoId, tenantId, command.PagamentoId, tituloId = (Guid)pag.titulo_id,
                    valorEstorno, command.Motivo, movEstornoId, key = command.IdempotencyKey, usuarioId
                }, tx, cancellationToken: ct));

            // Marca pagamento como estornado
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_titulo_pagamentos
                SET estornado = true
                WHERE id = @PagamentoId AND tenant_id = @tenantId",
                new { command.PagamentoId, tenantId }, tx, cancellationToken: ct));

            // Reverte obrigação
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_titulos_pagar
                SET valor_pago = valor_pago - @valorEstorno,
                    saldo_aberto = saldo_aberto + @valorEstorno,
                    situacao = (CASE WHEN valor_pago - @valorEstorno <= 0 THEN 'APROVADO' ELSE 'PARCIAL' END),
                    versao = versao + 1,
                    updated_at = now()
                WHERE id = @tituloId AND tenant_id = @tenantId",
                new { valorEstorno, tituloId = (Guid)pag.titulo_id, tenantId }, tx, cancellationToken: ct));

            // Se origem era comissão de vendedor, recompõe situação de PAGA de volta para APROPRIADA
            if ((string)pag.origem_tipo == "COMISSAO_VENDEDOR")
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_comissoes_apropriadas
                    SET situacao = 'APROPRIADA'
                    WHERE titulo_pagar_id = @tituloId AND tenant_id = @tenantId AND situacao = 'PAGA'",
                    new { tituloId = (Guid)pag.titulo_id, tenantId }, tx, cancellationToken: ct));
            }

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'ESTORNO_PAGAMENTO', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));
        }, ct);

        return estornoId;
    }

    public async Task<Guid> GerarDeRecebimentoAsync(Guid tenantId, Guid usuarioId, GerarTituloPagarDeRecebimentoCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash("GERAR_AP_RECEBIMENTO", tenantId, command.RecebimentoId);
        Guid tituloId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, payload_hash, resultado FROM plantaopro.adm360_operacoes
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (opExistente.payload_hash == payloadHash)
                {
                    tituloId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_titulos_pagar
                        WHERE origem_tipo = 'RECEBIMENTO_COMPRA' AND origem_id = @RecebimentoId AND tenant_id = @tenantId",
                        new { command.RecebimentoId, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            var existId = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition(@"
                SELECT id FROM plantaopro.adm360_titulos_pagar
                WHERE origem_tipo = 'RECEBIMENTO_COMPRA' AND origem_id = @RecebimentoId AND tenant_id = @tenantId",
                new { command.RecebimentoId, tenantId }, tx, cancellationToken: ct));

            if (existId.HasValue)
            {
                tituloId = existId.Value;
                return;
            }

            var rec = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT r.id, r.documento, r.pedido_id, p.numero AS PedidoNumero, p.fornecedor_id, p.previsao
                FROM plantaopro.adm360_recebimentos r
                JOIN plantaopro.adm360_pedidos p ON p.id = r.pedido_id AND p.tenant_id = r.tenant_id
                WHERE r.id = @RecebimentoId AND r.tenant_id = @tenantId",
                new { command.RecebimentoId, tenantId }, tx, cancellationToken: ct));

            if (rec is null) throw new InvalidOperationException("Recebimento de compra não encontrado.");

            var itensRec = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT ri.quantidade, pi.preco_unitario, pi.desconto, pi.quantidade AS QtdPedida
                FROM plantaopro.adm360_recebimento_itens ri
                JOIN plantaopro.adm360_pedido_itens pi ON pi.id = ri.pedido_item_id AND pi.tenant_id = ri.tenant_id
                WHERE ri.recebimento_id = @RecebimentoId AND ri.tenant_id = @tenantId",
                new { command.RecebimentoId, tenantId }, tx, cancellationToken: ct))).ToList();

            if (itensRec.Count == 0)
                throw new InvalidOperationException("Recebimento não possui itens válidos para gerar obrigação.");

            decimal totalRecebimento = 0m;
            foreach (var ir in itensRec)
            {
                decimal qtd = (decimal)ir.quantidade;
                decimal pu = (decimal)ir.preco_unitario;
                decimal desc = (decimal)ir.desconto;
                decimal qtdPedida = (decimal)ir.qtdpedida;
                decimal descUnit = qtdPedida > 0 ? (desc / qtdPedida) : 0m;
                totalRecebimento += qtd * Math.Max(0m, pu - descUnit);
            }

            totalRecebimento = Math.Round(totalRecebimento, 2);

            var seq = await cn.ExecuteScalarAsync<long>("SELECT nextval('plantaopro.adm360_titulo_pagar_numero')", transaction: tx);
            var numeroTitulo = $"PAG-{seq:D6}";
            tituloId = Guid.NewGuid();
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly vencimento = rec.previsao is not null ? ToDateOnly(rec.previsao) : hoje.AddDays(30);

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_titulos_pagar(
                    id, tenant_id, numero, fornecedor_id, origem_tipo, origem_id,
                    documento, competencia, data_emissao, data_vencimento, parcela, total_parcelas,
                    valor_principal, valor_pago, saldo_aberto, situacao, centro_custo,
                    idempotency_key, created_by
                ) VALUES(
                    @tituloId, @tenantId, @numeroTitulo, @FornecedorId, 'RECEBIMENTO_COMPRA', @RecebimentoId,
                    @Documento, @hoje, @hoje, @vencimento, 1, 1,
                    @totalRecebimento, 0, @totalRecebimento, 'APROVADO', 'SUPRIMENTOS',
                    @key, @usuarioId
                )",
                new
                {
                    tituloId, tenantId, numeroTitulo, FornecedorId = (Guid)rec.fornecedor_id,
                    command.RecebimentoId, Documento = (string?)rec.documento ?? (string)rec.pedidonumero,
                    hoje, vencimento, totalRecebimento, key = command.IdempotencyKey, usuarioId
                }, tx, cancellationToken: ct));

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'GERAR_AP_RECEBIMENTO', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));
        }, ct);

        return tituloId;
    }

    public async Task<Guid> GerarDeComissaoAsync(Guid tenantId, Guid usuarioId, GerarTituloPagarDeComissaoCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash("GERAR_AP_COMISSAO", tenantId, command.VendedorId, command.DataVencimento);
        Guid tituloId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, payload_hash, resultado FROM plantaopro.adm360_operacoes
                WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (opExistente.payload_hash == payloadHash)
                {
                    tituloId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                        SELECT id FROM plantaopro.adm360_titulos_pagar
                        WHERE idempotency_key = @key AND tenant_id = @tenantId",
                        new { key = command.IdempotencyKey, tenantId }, tx, cancellationToken: ct));
                    return;
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com dados diferentes.");
            }

            // Seleciona comissões apropriadas do vendedor ainda não vinculadas a título
            var comissoes = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT id, valor_comissao FROM plantaopro.adm360_comissoes_apropriadas
                WHERE vendedor_id = @VendedorId AND tenant_id = @tenantId
                  AND situacao = 'APROPRIADA' AND titulo_pagar_id IS NULL
                FOR UPDATE",
                new { command.VendedorId, tenantId }, tx, cancellationToken: ct))).ToList();

            if (comissoes.Count == 0)
                throw new InvalidOperationException("Nenhuma comissão apropriada pendente para este vendedor.");

            decimal totalComissao = comissoes.Sum(c => (decimal)c.valor_comissao);

            // Obtém parceiro correspondente ao vendedor ou cria/busca
            var parceiroId = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition(@"
                SELECT id FROM plantaopro.adm360_parceiros
                WHERE tenant_id = @tenantId AND ativo AND (
                    nome ILIKE (SELECT nome FROM plantaopro.usuarios WHERE id = @VendedorId AND tenant_id = @tenantId)
                ) LIMIT 1",
                new { command.VendedorId, tenantId }, tx, cancellationToken: ct));

            if (!parceiroId.HasValue)
            {
                var usuario = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                    SELECT nome, email FROM plantaopro.usuarios WHERE id = @VendedorId AND tenant_id = @tenantId",
                    new { command.VendedorId, tenantId }, tx, cancellationToken: ct));

                if (usuario is null) throw new InvalidOperationException("Vendedor não encontrado.");

                parceiroId = Guid.NewGuid();
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, email, fornecedor, ativo, created_by)
                    VALUES(@parceiroId, @tenantId, @Nome, @Email, true, true, @usuarioId)",
                    new { parceiroId = parceiroId.Value, tenantId, Nome = (string)usuario.nome, Email = (string?)usuario.email, usuarioId },
                    tx, cancellationToken: ct));
            }

            var seq = await cn.ExecuteScalarAsync<long>("SELECT nextval('plantaopro.adm360_titulo_pagar_numero')", transaction: tx);
            var numeroTitulo = $"PAG-{seq:D6}";
            tituloId = Guid.NewGuid();
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_titulos_pagar(
                    id, tenant_id, numero, fornecedor_id, origem_tipo, origem_id,
                    documento, competencia, data_emissao, data_vencimento, parcela, total_parcelas,
                    valor_principal, valor_pago, saldo_aberto, situacao, centro_custo,
                    idempotency_key, created_by
                ) VALUES(
                    @tituloId, @tenantId, @numeroTitulo, @forcecedorId, 'COMISSAO_VENDEDOR', @VendedorId,
                    'COMISSAO-' || @VendedorId, @hoje, @hoje, @DataVencimento, 1, 1,
                    @totalComissao, 0, @totalComissao, 'APROVADO', 'COMERCIAL',
                    @key, @usuarioId
                )",
                new
                {
                    tituloId, tenantId, numeroTitulo, forcecedorId = parceiroId.Value, command.VendedorId,
                    hoje, command.DataVencimento, totalComissao, key = command.IdempotencyKey, usuarioId
                }, tx, cancellationToken: ct));

            // Vincula comissões ao título a pagar gerado
            var comissaoIds = comissoes.Select(c => (Guid)c.id).ToArray();
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_comissoes_apropriadas
                SET titulo_pagar_id = @tituloId
                WHERE id = ANY(@comissaoIds) AND tenant_id = @tenantId",
                new { tituloId, comissaoIds, tenantId }, tx, cancellationToken: ct));

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'GERAR_AP_COMISSAO', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));
        }, ct);

        return tituloId;
    }

    public async Task<IReadOnlyList<ComissaoPendenteDto>> ListarComissoesPendentesAsync(Guid tenantId, Guid? vendedorId, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT c.id AS Id,
                   c.venda_id AS VendaId,
                   v.numero AS VendaNumero,
                   c.baixa_id AS BaixaId,
                   c.vendedor_id AS VendedorId,
                   u.nome AS VendedorNome,
                   c.base_calculo AS BaseCalculo,
                   c.percentual AS Percentual,
                   c.valor_comissao AS ValorComissao,
                   c.situacao AS Situacao,
                   c.created_at AS CriadoEm
            FROM plantaopro.adm360_comissoes_apropriadas c
            JOIN plantaopro.usuarios u ON u.id = c.vendedor_id AND u.tenant_id = c.tenant_id
            JOIN plantaopro.adm360_vendas v ON v.id = c.venda_id AND v.tenant_id = c.tenant_id
            WHERE c.tenant_id = @tenantId
              AND c.situacao = 'APROPRIADA'
              AND c.titulo_pagar_id IS NULL
              AND (@vendedorId::uuid IS NULL OR c.vendedor_id = @vendedorId::uuid)
              AND (@inicio::date IS NULL OR c.created_at::date >= @inicio::date)
              AND (@fim::date IS NULL OR c.created_at::date <= @fim::date)
            ORDER BY c.created_at DESC",
            new { tenantId, vendedorId, inicio, fim }, cancellationToken: ct));

        return rows.Select(r => new ComissaoPendenteDto(
            (Guid)r.id,
            (Guid)r.vendaid,
            (string)r.vendanumero,
            (Guid)r.baixaid,
            (Guid)r.vendedorid,
            (string)r.vendedornome,
            (decimal)r.basecalculo,
            (decimal)r.percentual,
            (decimal)r.valorcomissao,
            (string)r.situacao,
            (DateTime)r.criadoem
        )).ToList();
    }
}
