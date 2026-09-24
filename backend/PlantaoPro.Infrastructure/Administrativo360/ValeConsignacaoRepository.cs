using System.Globalization;
using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class ValeConsignacaoRepository : Adm360Repository, IValeConsignacaoRepository
{
    public ValeConsignacaoRepository(string connectionString) : base(connectionString) { }

    private sealed class ValeHeaderRow
    {
        public Guid Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public Guid? CirurgiaId { get; set; }
        public string? CirurgiaNumero { get; set; }
        public Guid? OrcamentoId { get; set; }
        public string? OrcamentoNumero { get; set; }
        public int? OrcamentoRevisao { get; set; }
        public Guid HospitalId { get; set; }
        public string Hospital { get; set; } = string.Empty;
        public Guid? CustodianteId { get; set; }
        public string? Custodiante { get; set; }
        public Guid LocalOrigemId { get; set; }
        public string LocalOrigem { get; set; } = string.Empty;
        public Guid LocalDestinoId { get; set; }
        public string LocalDestino { get; set; } = string.Empty;
        public DateOnly DataSaidaPrevista { get; set; }
        public DateTimeOffset? DataSaidaEfetiva { get; set; }
        public DateOnly? DataRetornoPrevista { get; set; }
        public DateTimeOffset? DataReconciliacao { get; set; }
        public string Situacao { get; set; } = string.Empty;
        public string SituacaoFinanceira { get; set; } = string.Empty;
        public string? Observacoes { get; set; }
        public DateTimeOffset CriadoEm { get; set; }
        public decimal TotalItens { get; set; }
    }

    private sealed class ValeItemRow
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Produto { get; set; } = string.Empty;
        public string Unidade { get; set; } = string.Empty;
        public Guid LoteId { get; set; }
        public string Lote { get; set; } = string.Empty;
        public DateOnly? Validade { get; set; }
        public Guid? ReservaId { get; set; }
        public decimal QuantidadeSolicitada { get; set; }
        public decimal QuantidadeSeparada { get; set; }
        public decimal QuantidadeExpedida { get; set; }
        public decimal QuantidadeConsumida { get; set; }
        public decimal QuantidadeDevolvida { get; set; }
        public decimal QuantidadePerda { get; set; }
        public decimal PrecoUnitario { get; set; }
    }

    private sealed class ValeEventoRow
    {
        public Guid Id { get; set; }
        public Guid ValeItemId { get; set; }
        public string Produto { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public DateTimeOffset DataEvento { get; set; }
        public string? Motivo { get; set; }
        public string? RegistradoPor { get; set; }
    }

    private sealed class OperacaoExistenteRow
    {
        public Guid Id { get; set; }
        public string PayloadHash { get; set; } = string.Empty;
        public string? Resultado { get; set; }
    }

    public async Task<IReadOnlyList<ValeResumo>> ListarAsync(
        Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<ValeHeaderRow>(new CommandDefinition(@"
            SELECT v.id AS Id, v.numero AS Numero,
                   c.numero AS CirurgiaNumero,
                   o.numero AS OrcamentoNumero,
                   COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital,
                   orig.nome AS LocalOrigem,
                   dest.nome AS LocalDestino,
                   v.data_saida_prevista AS DataSaidaPrevista,
                   v.data_saida_efetiva AS DataSaidaEfetiva,
                   v.situacao AS Situacao,
                   v.situacao_financeira AS SituacaoFinanceira,
                   COALESCE((SELECT SUM(vi.quantidade_solicitada) FROM plantaopro.adm360_vale_itens vi WHERE vi.vale_id = v.id), 0) AS TotalItens,
                   v.created_at AS CriadoEm
            FROM plantaopro.adm360_vales v
            LEFT JOIN plantaopro.adm360_cirurgias c ON c.id = v.cirurgia_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_orcamentos o ON o.id = v.orcamento_id AND o.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = v.hospital_id AND h.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.hospital_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_locais orig ON orig.id = v.local_origem_id AND orig.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_locais dest ON dest.id = v.local_destino_id AND dest.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
              AND (@busca IS NULL OR v.numero ILIKE '%' || @busca || '%' OR c.numero ILIKE '%' || @busca || '%' OR o.numero ILIKE '%' || @busca || '%' OR h.nome ILIKE '%' || @busca || '%' OR hosp.nome ILIKE '%' || @busca || '%')
              AND (@situacao IS NULL OR v.situacao = @situacao)
              AND (@inicio IS NULL OR v.data_saida_prevista >= @inicio)
              AND (@fim IS NULL OR v.data_saida_prevista <= @fim)
            ORDER BY v.data_saida_prevista DESC, v.created_at DESC",
            new { tenantId, busca, situacao, inicio, fim }, cancellationToken: ct));

        return rows.Select(r => new ValeResumo(
            r.Id, r.Numero, r.CirurgiaNumero, r.OrcamentoNumero, r.Hospital,
            r.LocalOrigem, r.LocalDestino, r.DataSaidaPrevista, r.DataSaidaEfetiva,
            r.Situacao, r.SituacaoFinanceira, r.TotalItens, r.CriadoEm)).ToList();
    }

    public async Task<ValeDetalhes?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        var v = await cn.QuerySingleOrDefaultAsync<ValeHeaderRow>(new CommandDefinition(@"
            SELECT v.id AS Id, v.numero AS Numero,
                   v.cirurgia_id AS CirurgiaId, c.numero AS CirurgiaNumero,
                   v.orcamento_id AS OrcamentoId, o.numero AS OrcamentoNumero, v.orcamento_revisao AS OrcamentoRevisao,
                   v.hospital_id AS HospitalId, COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital,
                   v.custodiante_id AS CustodianteId, COALESCE(cust.nome, 'Custodiante') AS Custodiante,
                   v.local_origem_id AS LocalOrigemId, orig.nome AS LocalOrigem,
                   v.local_destino_id AS LocalDestinoId, dest.nome AS LocalDestino,
                   v.data_saida_prevista AS DataSaidaPrevista,
                   v.data_saida_efetiva AS DataSaidaEfetiva,
                   v.data_retorno_prevista AS DataRetornoPrevista,
                   v.data_reconciliacao AS DataReconciliacao,
                   v.situacao AS Situacao,
                   v.situacao_financeira AS SituacaoFinanceira,
                   v.observacoes AS Observacoes,
                   v.created_at AS CriadoEm
            FROM plantaopro.adm360_vales v
            LEFT JOIN plantaopro.adm360_cirurgias c ON c.id = v.cirurgia_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_orcamentos o ON o.id = v.orcamento_id AND o.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = v.hospital_id AND h.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.hospital_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros cust ON cust.id = v.custodiante_id AND cust.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_locais orig ON orig.id = v.local_origem_id AND orig.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_locais dest ON dest.id = v.local_destino_id AND dest.tenant_id = v.tenant_id
            WHERE v.id = @id AND v.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (v is null) return null;

        var items = (await cn.QueryAsync<ValeItemRow>(new CommandDefinition(@"
            SELECT vi.id AS Id, vi.produto_id AS ProdutoId, p.sku AS Sku, p.nome AS Produto, p.unidade AS Unidade,
                   vi.lote_id AS LoteId, l.codigo AS Lote, l.validade AS Validade, vi.reserva_id AS ReservaId,
                   vi.quantidade_solicitada AS QuantidadeSolicitada,
                   vi.quantidade_separada AS QuantidadeSeparada,
                   vi.quantidade_expedida AS QuantidadeExpedida,
                   vi.quantidade_consumida AS QuantidadeConsumida,
                   vi.quantidade_devolvida AS QuantidadeDevolvida,
                   vi.quantidade_perda AS QuantidadePerda,
                   vi.preco_unitario AS PrecoUnitario
            FROM plantaopro.adm360_vale_itens vi
            JOIN plantaopro.adm360_produtos p ON p.id = vi.produto_id AND p.tenant_id = vi.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = vi.lote_id AND l.tenant_id = vi.tenant_id
            WHERE vi.vale_id = @id AND vi.tenant_id = @tenantId
            ORDER BY p.nome, l.codigo",
            new { id, tenantId }, cancellationToken: ct))).ToList();

        var eventos = (await cn.QueryAsync<ValeEventoRow>(new CommandDefinition(@"
            SELECT ve.id AS Id, ve.vale_item_id AS ValeItemId, p.nome AS Produto, l.codigo AS Lote,
                   ve.tipo AS Tipo, ve.quantidade AS Quantidade, ve.data_evento AS DataEvento,
                   ve.motivo AS Motivo, COALESCE(u.nome, 'Sistema') AS RegistradoPor
            FROM plantaopro.adm360_vale_eventos ve
            JOIN plantaopro.adm360_vale_itens vi ON vi.id = ve.vale_item_id AND vi.tenant_id = ve.tenant_id
            JOIN plantaopro.adm360_produtos p ON p.id = vi.produto_id AND p.tenant_id = vi.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = vi.lote_id AND l.tenant_id = vi.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = ve.registrado_por AND u.tenant_id = ve.tenant_id
            WHERE ve.vale_id = @id AND ve.tenant_id = @tenantId
            ORDER BY ve.data_evento DESC",
            new { id, tenantId }, cancellationToken: ct))).ToList();

        var itensDetalhes = items.Select(it =>
        {
            var pendente = ValeConsignacaoRegras.CalcularPendenteCustodia(
                it.QuantidadeExpedida, it.QuantidadeConsumida, it.QuantidadeDevolvida, it.QuantidadePerda);

            return new ValeItemDetalhes(
                it.Id, it.ProdutoId, it.Sku, it.Produto, it.Unidade, it.LoteId, it.Lote, it.Validade,
                it.ReservaId, it.QuantidadeSolicitada, it.QuantidadeSeparada, it.QuantidadeExpedida,
                it.QuantidadeConsumida, it.QuantidadeDevolvida, it.QuantidadePerda, pendente, it.PrecoUnitario);
        }).ToList();

        var eventosDetalhes = eventos.Select(e => new ValeEventoDetalhes(
            e.Id, e.ValeItemId, e.Produto, e.Lote, e.Tipo, e.Quantidade, e.DataEvento, e.Motivo, e.RegistradoPor)).ToList();

        return new ValeDetalhes(
            v.Id, v.Numero, v.CirurgiaId, v.CirurgiaNumero, v.OrcamentoId, v.OrcamentoNumero,
            v.OrcamentoRevisao, v.HospitalId, v.Hospital, v.CustodianteId, v.Custodiante,
            v.LocalOrigemId, v.LocalOrigem, v.LocalDestinoId, v.LocalDestino,
            v.DataSaidaPrevista, v.DataSaidaEfetiva, v.DataRetornoPrevista, v.DataReconciliacao,
            v.Situacao, v.SituacaoFinanceira, v.Observacoes, v.CriadoEm, itensDetalhes, eventosDetalhes);
    }

    public async Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarValeCommand command, CancellationToken ct)
    {
        if (command.Itens.Count == 0)
            throw new ArgumentException("O vale de consignação deve conter ao menos um item.");

        foreach (var item in command.Itens)
        {
            Estoque.ValidarQuantidade(item.QuantidadeSolicitada);
        }

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        // Valida hospital
        var hospValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @id AND tenant_id = @tenantId AND ativo
                UNION ALL
                SELECT 1 FROM plantaopro.hospitais WHERE id = @id AND tenant_id = @tenantId AND (status IS NULL OR status IN ('A', 'ATIVO'))
                UNION ALL
                SELECT 1 FROM plantaopro.adm360_locais WHERE id = @id AND tenant_id = @tenantId AND tipo = 'EXTERNO' AND ativo
            )", new { id = command.HospitalId, tenantId }, tx, cancellationToken: ct));
        if (!hospValido) throw new ArgumentException("Hospital informado é inválido ou inativo.");

        // Valida locais
        var origValida = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM plantaopro.adm360_locais WHERE id = @id AND tenant_id = @tenantId AND ativo)",
            new { id = command.LocalOrigemId, tenantId }, tx, cancellationToken: ct));
        if (!origValida) throw new ArgumentException("Local de origem informado é inválido ou inativo.");

        var destValida = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS(SELECT 1 FROM plantaopro.adm360_locais WHERE id = @id AND tenant_id = @tenantId AND tipo = 'EXTERNO' AND ativo)",
            new { id = command.LocalDestinoId, tenantId }, tx, cancellationToken: ct));
        if (!destValida) throw new ArgumentException("Local de destino deve ser um local de custódia EXTERNO ativo.");

        // Valida que nenhuma das reservas já está vinculada a outro vale ativo
        foreach (var item in command.Itens.Where(i => i.ReservaId.HasValue))
        {
            var jaAlocada = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS(
                    SELECT 1 FROM plantaopro.adm360_vale_itens vi
                    JOIN plantaopro.adm360_vales v ON v.id = vi.vale_id
                    WHERE vi.tenant_id = @tenantId AND vi.reserva_id = @reservaId
                      AND v.situacao NOT IN ('CANCELADO', 'RECONCILIADO')
                )", new { tenantId, reservaId = item.ReservaId!.Value }, tx, cancellationToken: ct));

            if (jaAlocada)
                throw new InvalidOperationException("Uma ou mais reservas selecionadas já estão vinculadas a outro vale em andamento.");
        }

        var id = Guid.NewGuid();
        var seq = await cn.ExecuteScalarAsync<long>("SELECT nextval('plantaopro.adm360_vale_numero')", transaction: tx);
        var numero = $"VAL-{seq:D6}";

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_vales(
                id, tenant_id, numero, cirurgia_id, orcamento_id, orcamento_revisao,
                hospital_id, custodiante_id, local_origem_id, local_destino_id,
                data_saida_prevista, data_retorno_prevista, situacao, situacao_financeira,
                observacoes, idempotency_key, created_by
            ) VALUES(
                @id, @tenantId, @numero, @CirurgiaId, @OrcamentoId, @OrcamentoRevisao,
                @HospitalId, @CustodianteId, @LocalOrigemId, @LocalDestinoId,
                @DataSaidaPrevista, @DataRetornoPrevista, 'RASCUNHO', 'PENDENTE_VALORIZACAO',
                @Observacoes, @key, @usuarioId
            )",
            new
            {
                id, tenantId, numero, command.CirurgiaId, command.OrcamentoId, command.OrcamentoRevisao,
                command.HospitalId, command.CustodianteId, command.LocalOrigemId, command.LocalDestinoId,
                command.DataSaidaPrevista, command.DataRetornoPrevista, command.Observacoes,
                key = command.IdempotencyKey, usuarioId
            }, tx, cancellationToken: ct));

        foreach (var item in command.Itens)
        {
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_vale_itens(
                    id, tenant_id, vale_id, produto_id, lote_id, reserva_id,
                    quantidade_solicitada, quantidade_separada, preco_unitario
                ) VALUES(
                    gen_random_uuid(), @tenantId, @id, @ProdutoId, @LoteId, @ReservaId,
                    @QuantidadeSolicitada, 0, @PrecoUnitario
                )",
                new
                {
                    tenantId, id, item.ProdutoId, item.LoteId, item.ReservaId,
                    item.QuantidadeSolicitada, item.PrecoUnitario
                }, tx, cancellationToken: ct));
        }

        // Gera tarefa móvel de separação automaticamente
        var tarefaId = Guid.NewGuid();
        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_tarefas_coleta(
                id, tenant_id, tipo, descricao, situacao,
                atribuida_a, origem_id
            ) VALUES(
                @tarefaId, @tenantId, 'SEPARACAO',
                'Separação de materiais para o Vale ' || @numero, 'ABERTA',
                @usuarioId, @id
            )", new { tarefaId, tenantId, id, numero, usuarioId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task SepararItemAsync(Guid tenantId, Guid usuarioId, Guid valeId, SepararItemValeCommand command, CancellationToken ct)
    {
        Estoque.ValidarQuantidade(command.QuantidadeSeparada);

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT situacao, local_origem_id FROM plantaopro.adm360_vales
            WHERE id = @valeId AND tenant_id = @tenantId FOR UPDATE",
            new { valeId, tenantId }, tx, cancellationToken: ct));

        if (vale is null) throw new InvalidOperationException("Vale de consignação não encontrado.");
        if (vale.situacao is not ("RASCUNHO" or "EM_SEPARACAO"))
            throw new InvalidOperationException($"Não é permitido separar itens para vale na situação '{vale.situacao}'.");

        var item = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT produto_id, lote_id, quantidade_solicitada FROM plantaopro.adm360_vale_itens
            WHERE id = @ValeItemId AND vale_id = @valeId AND tenant_id = @tenantId FOR UPDATE",
            new { command.ValeItemId, valeId, tenantId }, tx, cancellationToken: ct));

        if (item is null) throw new InvalidOperationException("Item do vale não encontrado.");

        if (command.QuantidadeSeparada > (decimal)item.quantidade_solicitada)
            throw new InvalidOperationException("Quantidade separada não pode ser superior à quantidade solicitada.");

        // Valida que o lote não está bloqueado, vencido ou em quarentena
        Guid loteId = item.lote_id;
        Guid prodId = item.produto_id;
        Guid origId = vale.local_origem_id;

        var saldoLiberado = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
            SELECT COALESCE(fisico, 0)
            FROM plantaopro.adm360_saldos
            WHERE tenant_id = @tenantId AND produto_id = @prodId AND lote_id = @loteId AND local_id = @origId AND condicao = 'LIBERADO'",
            new { tenantId, prodId, loteId, origId }, tx, cancellationToken: ct));

        if (saldoLiberado < command.QuantidadeSeparada)
            throw new InvalidOperationException("Lote sem saldo físico liberado suficiente no local de origem.");

        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_vale_itens
            SET quantidade_separada = @QuantidadeSeparada
            WHERE id = @ValeItemId AND tenant_id = @tenantId",
            new { command.QuantidadeSeparada, command.ValeItemId, tenantId }, tx, cancellationToken: ct));

        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_vales
            SET situacao = 'EM_SEPARACAO', versao = versao + 1, updated_at = now()
            WHERE id = @valeId AND tenant_id = @tenantId AND situacao = 'RASCUNHO'",
            new { valeId, tenantId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task ConcluirSeparacaoAsync(Guid tenantId, Guid usuarioId, Guid valeId, CancellationToken ct)
    {
        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT situacao FROM plantaopro.adm360_vales
            WHERE id = @valeId AND tenant_id = @tenantId FOR UPDATE",
            new { valeId, tenantId }, tx, cancellationToken: ct));

        if (vale is null) throw new InvalidOperationException("Vale de consignação não encontrado.");
        if (vale.situacao is not ("RASCUNHO" or "EM_SEPARACAO"))
            throw new InvalidOperationException($"Separação não pode ser concluída para vale em situação '{vale.situacao}'.");

        var pendentes = await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"
            SELECT COUNT(*) FROM plantaopro.adm360_vale_itens
            WHERE vale_id = @valeId AND tenant_id = @tenantId AND quantidade_separada < quantidade_solicitada",
            new { valeId, tenantId }, tx, cancellationToken: ct));

        if (pendentes > 0)
            throw new InvalidOperationException("Existem itens com separação pendente ou incompleta. Todos os itens devem ser conferidos integralmente.");

        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_vales
            SET situacao = 'PRONTO_PARA_EXPEDICAO',
                separado_por = @usuarioId,
                separado_em = now(),
                versao = versao + 1,
                updated_at = now()
            WHERE id = @valeId AND tenant_id = @tenantId",
            new { valeId, tenantId, usuarioId }, tx, cancellationToken: ct));

        // Conclui tarefa móvel de separação
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_tarefas_coleta
            SET situacao = 'CONCLUIDA'
            WHERE tenant_id = @tenantId AND origem_id = @valeId AND tipo = 'SEPARACAO' AND situacao <> 'CONCLUIDA'",
            new { tenantId, valeId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task ExpedirAsync(Guid tenantId, Guid usuarioId, ExpedirValeCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash("EXPEDICAO_VALE", tenantId, command.ValeId);

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            // Verificação de idempotência antecipada
            var opExistente = await cn.QuerySingleOrDefaultAsync<OperacaoExistenteRow>(new CommandDefinition(
                "SELECT id, payload_hash AS PayloadHash, resultado FROM plantaopro.adm360_operacoes WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (string.Equals(opExistente.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase))
                {
                    return; // Reenvio idempotente com sucesso
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com conteúdo divergente.");
            }

            var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, numero, situacao, local_origem_id, local_destino_id
                FROM plantaopro.adm360_vales
                WHERE id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (vale is null) throw new InvalidOperationException("Vale de consignação não encontrado.");

            if (!ValeConsignacaoRegras.PodeExpedir(vale.situacao))
                throw new InvalidOperationException($"Vale na situação '{vale.situacao}' não está pronto para expedição.");

            // Validar bloqueio de inventário nos locais de origem e destino
            Guid origId = vale.local_origem_id;
            Guid destId = vale.local_destino_id;
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, origId, ct);
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, destId, ct);

            // Carrega itens do vale
            var itens = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT id, produto_id, lote_id, reserva_id, quantidade_solicitada, quantidade_separada
                FROM plantaopro.adm360_vale_itens
                WHERE vale_id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct))).ToList();

            if (itens.Count == 0)
                throw new InvalidOperationException("Vale sem itens cadastrados.");

            // Bloqueio determinístico ordenado para evitar deadlocks
            var lockKeys = itens.Select(i => $"{tenantId}:{i.produto_id}:{i.lote_id}:{origId}")
                .Concat(itens.Select(i => $"{tenantId}:{i.produto_id}:{i.lote_id}:{destId}"))
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            await BloquearChavesDeterministasAsync(cn, tx, lockKeys, ct);

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'EXPEDICAO_VALE', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

            foreach (var it in itens)
            {
                decimal qtd = it.quantidade_separada > 0 ? it.quantidade_separada : it.quantidade_solicitada;
                Guid prodId = it.produto_id;
                Guid loteId = it.lote_id;
                Guid? reservaId = it.reserva_id;

                // Revalida saldo físico no local de origem
                var saldoFisico = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                    SELECT COALESCE(fisico, 0)
                    FROM plantaopro.adm360_saldos
                    WHERE tenant_id = @tenantId AND produto_id = @prodId AND lote_id = @loteId AND local_id = @origId AND condicao = 'LIBERADO'",
                    new { tenantId, prodId, loteId, origId }, tx, cancellationToken: ct));

                if (saldoFisico < qtd)
                    throw new InvalidOperationException("Saldo físico liberado insuficiente no local de origem para expedição.");

                // Registra movimentação de saída do estoque interno
                var movSaidaId = Guid.NewGuid();
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_movimentos(
                        id, tenant_id, produto_id, lote_id, local_id,
                        tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                    ) VALUES(
                        @movSaidaId, @tenantId, @prodId, @loteId, @origId,
                        'SAIDA', 'LIBERADO', @negQtd, 'Expedição vale consignação', 'VALE_CONSIGNACAO', @ValeId, @keySaida, @usuarioId
                    )",
                    new { movSaidaId, tenantId, prodId, loteId, origId, negQtd = -qtd, command.ValeId, keySaida = $"{payloadHash}:{it.id}:SAIDA", usuarioId }, tx, cancellationToken: ct));

                // Registra movimentação de entrada no estoque externo (custódia no hospital)
                var movEntradaId = Guid.NewGuid();
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_movimentos(
                        id, tenant_id, produto_id, lote_id, local_id,
                        tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                    ) VALUES(
                        @movEntradaId, @tenantId, @prodId, @loteId, @destId,
                        'ENTRADA', 'LIBERADO', @qtd, 'Expedição custódia hospitalar', 'VALE_CONSIGNACAO', @ValeId, @keyEntrada, @usuarioId
                    )",
                    new { movEntradaId, tenantId, prodId, loteId, destId, qtd, command.ValeId, keyEntrada = $"{payloadHash}:{it.id}:ENTRADA", usuarioId }, tx, cancellationToken: ct));

                // Se houver reserva ativa associada, atualiza para CONSUMIDA
                if (reservaId.HasValue)
                {
                    await cn.ExecuteAsync(new CommandDefinition(@"
                        UPDATE plantaopro.adm360_reservas
                        SET situacao = 'CONSUMIDA'
                        WHERE id = @reservaId AND tenant_id = @tenantId AND situacao = 'ATIVA'",
                        new { reservaId = reservaId.Value, tenantId }, tx, cancellationToken: ct));
                }

                // Atualiza item do vale
                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_vale_itens
                    SET quantidade_expedida = @qtd, quantidade_separada = @qtd
                    WHERE id = @itemId AND tenant_id = @tenantId",
                    new { qtd, itemId = (Guid)it.id, tenantId }, tx, cancellationToken: ct));
            }

            // Atualiza status do vale
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_vales
                SET situacao = 'EXPEDIDO',
                    data_saida_efetiva = now(),
                    expedido_por = @usuarioId,
                    expedido_em = now(),
                    versao = versao + 1,
                    updated_at = now()
                WHERE id = @ValeId AND tenant_id = @tenantId",
                new { command.ValeId, tenantId, usuarioId }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task RegistrarConsumoAsync(Guid tenantId, Guid usuarioId, RegistrarConsumoValeCommand command, CancellationToken ct)
    {
        Estoque.ValidarQuantidade(command.Quantidade);
        var payloadHash = IdempotenciaHelper.CalcularHash(
            "CONSUMO_VALE", tenantId, command.ValeId, command.ValeItemId,
            command.Quantidade.ToString("0.0000", CultureInfo.InvariantCulture));

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<OperacaoExistenteRow>(new CommandDefinition(
                "SELECT id, payload_hash AS PayloadHash, resultado FROM plantaopro.adm360_operacoes WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (string.Equals(opExistente.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase)) return;
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com conteúdo divergente.");
            }

            var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, situacao, local_destino_id FROM plantaopro.adm360_vales
                WHERE id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (vale is null) throw new InvalidOperationException("Vale não encontrado.");
            if (vale.situacao is not ("EXPEDIDO" or "RETORNO_PARCIAL"))
                throw new InvalidOperationException($"Consumo não permitido para vale em situação '{vale.situacao}'.");

            var item = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, produto_id, lote_id, quantidade_expedida, quantidade_consumida, quantidade_devolvida, quantidade_perda
                FROM plantaopro.adm360_vale_itens
                WHERE id = @ValeItemId AND vale_id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeItemId, command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (item is null) throw new InvalidOperationException("Item do vale não encontrado.");

            decimal pendente = ValeConsignacaoRegras.CalcularPendenteCustodia(
                (decimal)item.quantidade_expedida, (decimal)item.quantidade_consumida,
                (decimal)item.quantidade_devolvida, (decimal)item.quantidade_perda);

            if (command.Quantidade > pendente)
                throw new InvalidOperationException($"Quantidade informada ({command.Quantidade}) excede o saldo pendente em custódia ({pendente}).");

            Guid destId = vale.local_destino_id;
            Guid prodId = item.produto_id;
            Guid loteId = item.lote_id;

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'CONSUMO_VALE', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

            // Baixa no estoque externo (consumo da custódia do hospital)
            var movId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id,
                    tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    @movId, @tenantId, @prodId, @loteId, @destId,
                    'SAIDA', 'LIBERADO', @negQtd, 'Consumo em cirurgia', 'VALE_CONSUMO', @ValeId, @keyMov, @usuarioId
                )",
                new { movId, tenantId, prodId, loteId, destId, negQtd = -command.Quantidade, command.ValeId, keyMov = $"{command.IdempotencyKey}:SAIDA", usuarioId }, tx, cancellationToken: ct));

            // Atualiza item do vale
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_vale_itens
                SET quantidade_consumida = quantidade_consumida + @Quantidade
                WHERE id = @ValeItemId AND tenant_id = @tenantId",
                new { command.Quantidade, command.ValeItemId, tenantId }, tx, cancellationToken: ct));

            // Registra evento de consumo
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_vale_eventos(
                    id, tenant_id, vale_id, vale_item_id, tipo, quantidade,
                    data_evento, motivo, movimento_id, idempotency_key, registrado_por
                ) VALUES(
                    gen_random_uuid(), @tenantId, @ValeId, @ValeItemId, 'CONSUMO', @Quantidade,
                    now(), @Motivo, @movId, @key, @usuarioId
                )",
                new { tenantId, command.ValeId, command.ValeItemId, command.Quantidade, command.Motivo, movId, key = command.IdempotencyKey, usuarioId }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task RegistrarRetornoAsync(Guid tenantId, Guid usuarioId, RegistrarRetornoValeCommand command, CancellationToken ct)
    {
        Estoque.ValidarQuantidade(command.Quantidade);
        var payloadHash = IdempotenciaHelper.CalcularHash(
            "RETORNO_VALE", tenantId, command.ValeId, command.ValeItemId,
            command.Quantidade.ToString("0.0000", CultureInfo.InvariantCulture));

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<OperacaoExistenteRow>(new CommandDefinition(
                "SELECT id, payload_hash AS PayloadHash, resultado FROM plantaopro.adm360_operacoes WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (string.Equals(opExistente.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase)) return;
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com conteúdo divergente.");
            }

            var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, situacao, local_origem_id, local_destino_id FROM plantaopro.adm360_vales
                WHERE id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (vale is null) throw new InvalidOperationException("Vale não encontrado.");
            if (vale.situacao is not ("EXPEDIDO" or "RETORNO_PARCIAL"))
                throw new InvalidOperationException($"Retorno não permitido para vale em situação '{vale.situacao}'.");

            var item = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, produto_id, lote_id, quantidade_expedida, quantidade_consumida, quantidade_devolvida, quantidade_perda
                FROM plantaopro.adm360_vale_itens
                WHERE id = @ValeItemId AND vale_id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeItemId, command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (item is null) throw new InvalidOperationException("Item do vale não encontrado.");

            decimal pendente = ValeConsignacaoRegras.CalcularPendenteCustodia(
                (decimal)item.quantidade_expedida, (decimal)item.quantidade_consumida,
                (decimal)item.quantidade_devolvida, (decimal)item.quantidade_perda);

            if (command.Quantidade > pendente)
                throw new InvalidOperationException($"Quantidade de devolução ({command.Quantidade}) excede o saldo pendente em custódia ({pendente}).");

            Guid destId = vale.local_destino_id;
            Guid origId = vale.local_origem_id;
            Guid prodId = item.produto_id;
            Guid loteId = item.lote_id;

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'RETORNO_VALE', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

            // Saída do estoque externo (hospital)
            var movSaidaId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id,
                    tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    @movSaidaId, @tenantId, @prodId, @loteId, @destId,
                    'SAIDA', 'LIBERADO', @negQtd, 'Retorno de consignação', 'VALE_RETORNO', @ValeId, @keySaida, @usuarioId
                )",
                new { movSaidaId, tenantId, prodId, loteId, destId, negQtd = -command.Quantidade, command.ValeId, keySaida = $"{command.IdempotencyKey}:SAIDA", usuarioId }, tx, cancellationToken: ct));

            // Entrada no estoque interno em QUARENTENA para inspeção de qualidade
            var movEntradaId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id,
                    tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    @movEntradaId, @tenantId, @prodId, @loteId, @origId,
                    'ENTRADA', 'QUARENTENA', @qtd, 'Retorno consignação - inspeção', 'VALE_RETORNO', @ValeId, @keyEntrada, @usuarioId
                )",
                new { movEntradaId, tenantId, prodId, loteId, origId, qtd = command.Quantidade, command.ValeId, keyEntrada = $"{command.IdempotencyKey}:ENTRADA", usuarioId }, tx, cancellationToken: ct));

            // Atualiza item do vale
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_vale_itens
                SET quantidade_devolvida = quantidade_devolvida + @Quantidade
                WHERE id = @ValeItemId AND tenant_id = @tenantId",
                new { command.Quantidade, command.ValeItemId, tenantId }, tx, cancellationToken: ct));

            // Registra evento de retorno
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_vale_eventos(
                    id, tenant_id, vale_id, vale_item_id, tipo, quantidade,
                    data_evento, motivo, movimento_id, idempotency_key, registrado_por
                ) VALUES(
                    gen_random_uuid(), @tenantId, @ValeId, @ValeItemId, 'RETORNO', @Quantidade,
                    now(), @Motivo, @movEntradaId, @key, @usuarioId
                )",
                new { tenantId, command.ValeId, command.ValeItemId, command.Quantidade, command.Motivo, movEntradaId, key = command.IdempotencyKey, usuarioId }, tx, cancellationToken: ct));

            // Atualiza situação do vale para RETORNO_PARCIAL se ainda não reconciliado
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_vales
                SET situacao = 'RETORNO_PARCIAL', versao = versao + 1, updated_at = now()
                WHERE id = @ValeId AND tenant_id = @tenantId AND situacao = 'EXPEDIDO'",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task RegistrarPerdaAsync(Guid tenantId, Guid usuarioId, RegistrarPerdaValeCommand command, CancellationToken ct)
    {
        Estoque.ValidarQuantidade(command.Quantidade);
        if (string.IsNullOrWhiteSpace(command.Motivo))
            throw new ArgumentException("Motivo da perda/avaria é obrigatório para decisão autorizada.");

        var payloadHash = IdempotenciaHelper.CalcularHash(
            "PERDA_VALE", tenantId, command.ValeId, command.ValeItemId,
            command.Quantidade.ToString("0.0000", CultureInfo.InvariantCulture));

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<OperacaoExistenteRow>(new CommandDefinition(
                "SELECT id, payload_hash AS PayloadHash, resultado FROM plantaopro.adm360_operacoes WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (string.Equals(opExistente.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase)) return;
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com conteúdo divergente.");
            }

            var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, situacao, local_destino_id FROM plantaopro.adm360_vales
                WHERE id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (vale is null) throw new InvalidOperationException("Vale não encontrado.");
            if (vale.situacao is not ("EXPEDIDO" or "RETORNO_PARCIAL"))
                throw new InvalidOperationException($"Registro de perda não permitido para vale em situação '{vale.situacao}'.");

            var item = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, produto_id, lote_id, quantidade_expedida, quantidade_consumida, quantidade_devolvida, quantidade_perda
                FROM plantaopro.adm360_vale_itens
                WHERE id = @ValeItemId AND vale_id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeItemId, command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (item is null) throw new InvalidOperationException("Item do vale não encontrado.");

            decimal pendente = ValeConsignacaoRegras.CalcularPendenteCustodia(
                (decimal)item.quantidade_expedida, (decimal)item.quantidade_consumida,
                (decimal)item.quantidade_devolvida, (decimal)item.quantidade_perda);

            if (command.Quantidade > pendente)
                throw new InvalidOperationException($"Quantidade de perda ({command.Quantidade}) excede o saldo pendente em custódia ({pendente}).");

            Guid destId = vale.local_destino_id;
            Guid prodId = item.produto_id;
            Guid loteId = item.lote_id;

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'PERDA_VALE', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

            // Baixa física no estoque externo por descarte/perda
            var movId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id,
                    tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    @movId, @tenantId, @prodId, @loteId, @destId,
                    'SAIDA', 'LIBERADO', @negQtd, 'Perda/avaria em consignação: ' || @motivo, 'VALE_PERDA', @ValeId, @keyMov, @usuarioId
                )",
                new { movId, tenantId, prodId, loteId, destId, negQtd = -command.Quantidade, motivo = command.Motivo, command.ValeId, keyMov = $"{command.IdempotencyKey}:SAIDA", usuarioId }, tx, cancellationToken: ct));

            // Atualiza item do vale
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_vale_itens
                SET quantidade_perda = quantidade_perda + @Quantidade
                WHERE id = @ValeItemId AND tenant_id = @tenantId",
                new { command.Quantidade, command.ValeItemId, tenantId }, tx, cancellationToken: ct));

            // Registra evento de perda/avaria
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_vale_eventos(
                    id, tenant_id, vale_id, vale_item_id, tipo, quantidade,
                    data_evento, motivo, movimento_id, idempotency_key, registrado_por
                ) VALUES(
                    gen_random_uuid(), @tenantId, @ValeId, @ValeItemId, 'PERDA', @Quantidade,
                    now(), @Motivo, @movId, @key, @usuarioId
                )",
                new { tenantId, command.ValeId, command.ValeItemId, command.Quantidade, command.Motivo, movId, key = command.IdempotencyKey, usuarioId }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task ReconciliarAsync(Guid tenantId, Guid usuarioId, ReconciliarValeCommand command, CancellationToken ct)
    {
        var payloadHash = IdempotenciaHelper.CalcularHash("RECONCILIACAO_VALE", tenantId, command.ValeId);

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var opExistente = await cn.QuerySingleOrDefaultAsync<OperacaoExistenteRow>(new CommandDefinition(
                "SELECT id, payload_hash AS PayloadHash, resultado FROM plantaopro.adm360_operacoes WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (string.Equals(opExistente.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase)) return;
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com conteúdo divergente.");
            }

            var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, situacao FROM plantaopro.adm360_vales
                WHERE id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct));

            if (vale is null) throw new InvalidOperationException("Vale não encontrado.");
            if (vale.situacao == "RECONCILIADO") return; // Idempotente
            if (vale.situacao is not ("EXPEDIDO" or "RETORNO_PARCIAL"))
                throw new InvalidOperationException($"Vale na situação '{vale.situacao}' não pode ser reconciliado.");

            // Verifica se todos os itens estão 100% conciliados (pendente == 0)
            var itens = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT id, quantidade_expedida, quantidade_consumida, quantidade_devolvida, quantidade_perda
                FROM plantaopro.adm360_vale_itens
                WHERE vale_id = @ValeId AND tenant_id = @tenantId FOR UPDATE",
                new { command.ValeId, tenantId }, tx, cancellationToken: ct))).ToList();

            foreach (var it in itens)
            {
                decimal expedido = it.quantidade_expedida;
                decimal consumido = it.quantidade_consumida;
                decimal devolvido = it.quantidade_devolvida;
                decimal perda = it.quantidade_perda;

                var pendente = ValeConsignacaoRegras.CalcularPendenteCustodia(expedido, consumido, devolvido, perda);
                if (pendente > 0)
                {
                    throw new InvalidOperationException(
                        $"Não é possível reconciliar o vale com pendências em custódia ({pendente} un). Todo material expedido deve estar consumido, devolvido ou com perda autorizada.");
                }
            }

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'RECONCILIACAO_VALE', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_vales
                SET situacao = 'RECONCILIADO',
                    data_reconciliacao = now(),
                    reconciliado_por = @usuarioId,
                    observacoes = COALESCE(observacoes || E'\n', '') || COALESCE(@Observacoes, ''),
                    versao = versao + 1,
                    updated_at = now()
                WHERE id = @ValeId AND tenant_id = @tenantId",
                new { command.ValeId, tenantId, usuarioId, command.Observacoes }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid valeId, string motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Motivo do cancelamento do vale é obrigatório.");

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var vale = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT situacao FROM plantaopro.adm360_vales
            WHERE id = @valeId AND tenant_id = @tenantId FOR UPDATE",
            new { valeId, tenantId }, tx, cancellationToken: ct));

        if (vale is null) throw new InvalidOperationException("Vale de consignação não encontrado.");
        if (vale.situacao == "CANCELADO") return;

        if (vale.situacao is "EXPEDIDO" or "RETORNO_PARCIAL" or "RECONCILIADO")
        {
            throw new InvalidOperationException("Vale com materiais já expedidos não pode ser cancelado diretamente. Utilize consumo, devolução e reconciliação.");
        }

        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_vales
            SET situacao = 'CANCELADO',
                observacoes = COALESCE(observacoes || E'\n', '') || 'Cancelamento: ' || @motivo,
                versao = versao + 1,
                updated_at = now()
            WHERE id = @valeId AND tenant_id = @tenantId",
            new { valeId, tenantId, motivo }, tx, cancellationToken: ct));

        // Cancela tarefas móveis pendentes do vale
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_tarefas_coleta
            SET situacao = 'CANCELADA'
            WHERE tenant_id = @tenantId AND origem_id = @valeId AND situacao <> 'CONCLUIDA'",
            new { tenantId, valeId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }
}
