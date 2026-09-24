using System.Globalization;
using System.Text.Json;
using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class OrcamentoCirurgicoRepository : Adm360Repository, IOrcamentoCirurgicoRepository
{
    private sealed class OrcamentoHeaderRow
    {
        public Guid Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int Revisao { get; set; }
        public Guid HospitalId { get; set; }
        public string Hospital { get; set; } = string.Empty;
        public Guid? MedicoId { get; set; }
        public string? Medico { get; set; }
        public string Procedimento { get; set; } = string.Empty;
        public Guid ResponsavelFinanceiroId { get; set; }
        public string ResponsavelFinanceiro { get; set; } = string.Empty;
        public Guid? VendedorId { get; set; }
        public DateOnly DataPrevista { get; set; }
        public DateOnly Validade { get; set; }
        public string Situacao { get; set; } = string.Empty;
        public decimal TotalProdutos { get; set; }
        public decimal DescontoGeral { get; set; }
        public decimal TotalGeral { get; set; }
        public string? Observacoes { get; set; }
        public DateTimeOffset? AprovadoEm { get; set; }
        public int Versao { get; set; }
    }

    private sealed class OrcamentoItemQueryRow
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Produto { get; set; } = string.Empty;
        public string Unidade { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal Desconto { get; set; }
        public decimal Total { get; set; }
    }

    private sealed class LoteQueryRow
    {
        public Guid LoteId { get; set; }
        public string Lote { get; set; } = string.Empty;
        public DateOnly? Validade { get; set; }
        public Guid LocalId { get; set; }
        public string Local { get; set; } = string.Empty;
        public decimal Fisico { get; set; }
        public decimal Reservado { get; set; }
        public decimal Disponivel { get; set; }
    }

    private sealed class OrcamentoItemRow
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public decimal Quantidade { get; set; }
    }

    private sealed class OrcamentoItemPlanejamentoRow
    {
        public Guid ItemId { get; set; }
        public Guid ProdutoId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Produto { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
    }

    private sealed class OperacaoExistenteRow
    {
        public Guid Id { get; set; }
        public string PayloadHash { get; set; } = string.Empty;
        public string? Resultado { get; set; }
    }

    private sealed class LoteValidacaoRow
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public DateOnly? Validade { get; set; }
    }

    private sealed class OrcamentoResumoRow
    {
        public Guid Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public int Revisao { get; set; }
        public string Hospital { get; set; } = string.Empty;
        public string? Medico { get; set; }
        public string Procedimento { get; set; } = string.Empty;
        public string ResponsavelFinanceiro { get; set; } = string.Empty;
        public DateOnly DataPrevista { get; set; }
        public DateOnly Validade { get; set; }
        public string Situacao { get; set; } = string.Empty;
        public decimal TotalGeral { get; set; }
        public DateTime CriadoEm { get; set; }
    }

    public OrcamentoCirurgicoRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<OrcamentoResumo>> ListarAsync(Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<OrcamentoResumoRow>(new CommandDefinition(@"
            SELECT o.id AS Id, o.numero AS Numero, o.revisao AS Revisao,
                   COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital,
                   COALESCE(m.nome, med.nome, '') AS Medico,
                   o.procedimento AS Procedimento,
                   COALESCE(f.nome, hospf.nome, 'Responsável') AS ResponsavelFinanceiro,
                   o.data_prevista AS DataPrevista, o.validade AS Validade, o.situacao AS Situacao,
                   o.total_geral AS TotalGeral, o.created_at AS CriadoEm
            FROM plantaopro.adm360_orcamentos o
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = o.hospital_id AND h.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = o.hospital_id AND hosp.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros f ON f.id = o.responsavel_financeiro_id AND f.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.hospitais hospf ON hospf.id = o.responsavel_financeiro_id AND hospf.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros m ON m.id = o.medico_id AND m.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.medicos med ON med.id = o.medico_id AND med.tenant_id = o.tenant_id
            WHERE o.tenant_id = @tenantId
              AND (@busca IS NULL OR o.numero ILIKE '%' || @busca || '%' OR o.procedimento ILIKE '%' || @busca || '%' OR COALESCE(h.nome, hosp.nome, '') ILIKE '%' || @busca || '%')
              AND (@situacao IS NULL OR o.situacao = @situacao)
              AND (@dtInicio::date IS NULL OR o.data_prevista >= @dtInicio::date)
              AND (@dtFim::date IS NULL OR o.data_prevista <= @dtFim::date)
            ORDER BY o.created_at DESC",
            new
            {
                tenantId,
                busca,
                situacao,
                dtInicio = inicio.HasValue ? inicio.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                dtFim = fim.HasValue ? fim.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null
            }, cancellationToken: ct));

        return rows.Select(r => new OrcamentoResumo(
            r.Id, r.Numero, r.Revisao, r.Hospital, r.Medico, r.Procedimento, r.ResponsavelFinanceiro,
            r.DataPrevista, r.Validade, r.Situacao, r.TotalGeral, new DateTimeOffset(r.CriadoEm, TimeSpan.Zero)
        )).ToList();
    }

    public async Task<OrcamentoDetalhes?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        var h = await cn.QuerySingleOrDefaultAsync<OrcamentoHeaderRow>(new CommandDefinition(@"
            SELECT o.id AS Id, o.numero AS Numero, o.revisao AS Revisao, o.hospital_id AS HospitalId,
                   COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital,
                   o.medico_id AS MedicoId, COALESCE(m.nome, med.nome, '') AS Medico,
                   o.procedimento AS Procedimento,
                   o.responsavel_financeiro_id AS ResponsavelFinanceiroId,
                   COALESCE(f.nome, hospf.nome, 'Responsável') AS ResponsavelFinanceiro,
                   o.vendedor_id AS VendedorId, o.data_prevista AS DataPrevista, o.validade AS Validade,
                   o.situacao AS Situacao, o.total_produtos AS TotalProdutos, o.desconto_geral AS DescontoGeral,
                   o.total_geral AS TotalGeral, o.observacoes AS Observacoes, o.aprovado_em AS AprovadoEm, o.versao AS Versao
            FROM plantaopro.adm360_orcamentos o
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = o.hospital_id AND h.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = o.hospital_id AND hosp.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros f ON f.id = o.responsavel_financeiro_id AND f.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.hospitais hospf ON hospf.id = o.responsavel_financeiro_id AND hospf.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros m ON m.id = o.medico_id AND m.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.medicos med ON med.id = o.medico_id AND med.tenant_id = o.tenant_id
            WHERE o.id = @id AND o.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (h is null) return null;

        var rawItems = (await cn.QueryAsync<OrcamentoItemQueryRow>(new CommandDefinition(@"
            SELECT i.id AS Id, i.produto_id AS ProdutoId, p.sku AS Sku, p.nome AS Produto, p.unidade AS Unidade,
                   i.quantidade AS Quantidade, i.preco_unitario AS PrecoUnitario, i.desconto AS Desconto, i.total AS Total
            FROM plantaopro.adm360_orcamento_itens i
            JOIN plantaopro.adm360_produtos p ON p.id = i.produto_id AND p.tenant_id = i.tenant_id
            WHERE i.orcamento_id = @id AND i.tenant_id = @tenantId
            ORDER BY p.nome",
            new { id, tenantId }, cancellationToken: ct))).ToList();

        var itensDetalhe = new List<OrcamentoItemDetalhe>();
        foreach (var it in rawItems)
        {
            var reservada = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                SELECT COALESCE(SUM(quantidade), 0)
                FROM plantaopro.adm360_reservas
                WHERE tenant_id = @tenantId AND origem_tipo = 'ORCAMENTO_CIRURGICO' AND origem_id = @id
                  AND produto_id = @produtoId AND situacao = 'ATIVA'",
                new { tenantId, id, produtoId = it.ProdutoId }, cancellationToken: ct));

            var falta = Math.Max(0m, it.Quantidade - reservada);
            itensDetalhe.Add(new OrcamentoItemDetalhe(
                it.Id, it.ProdutoId, it.Sku, it.Produto, it.Unidade, it.Quantidade,
                it.PrecoUnitario, it.Desconto, it.Total, reservada, falta));
        }

        return new OrcamentoDetalhes(
            h.Id, h.Numero, h.Revisao, h.HospitalId, h.Hospital, h.MedicoId, h.Medico,
            h.Procedimento, h.ResponsavelFinanceiroId, h.ResponsavelFinanceiro, h.VendedorId,
            h.DataPrevista, h.Validade, h.Situacao, h.TotalProdutos, h.DescontoGeral,
            h.TotalGeral, h.Observacoes, h.AprovadoEm, h.Versao, itensDetalhe);
    }

    public async Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarOrcamentoCommand c, CancellationToken ct)
    {
        if (c.Itens.Count == 0)
            throw new ArgumentException("O orçamento deve conter ao menos um produto.");

        // Validação e cálculo determinístico no servidor com decimal
        var totaisItens = new List<(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario, decimal Desconto, decimal Total)>();
        foreach (var item in c.Itens)
        {
            var itemTotal = OrcamentoCirurgicoRegras.CalcularTotalItem(item.Quantidade, item.PrecoUnitario, item.Desconto);
            totaisItens.Add((item.ProdutoId, item.Quantidade, item.PrecoUnitario, item.Desconto, itemTotal));
        }

        var (totalProdutos, totalGeral) = OrcamentoCirurgicoRegras.CalcularTotais(totaisItens.Select(x => x.Total), 0);

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        // Valida parceiros/hospitais/médicos existentes no tenant
        var hospValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @id AND tenant_id = @tenantId AND ativo
                UNION ALL
                SELECT 1 FROM plantaopro.hospitais WHERE id = @id AND tenant_id = @tenantId AND (status IS NULL OR status IN ('A', 'ATIVO'))
                UNION ALL
                SELECT 1 FROM plantaopro.adm360_locais WHERE id = @id AND tenant_id = @tenantId AND tipo = 'EXTERNO' AND ativo
            )", new { id = c.HospitalId, tenantId }, tx, cancellationToken: ct));
        if (!hospValido) throw new ArgumentException("Hospital informado é inválido ou inativo.");

        var respValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @id AND tenant_id = @tenantId AND ativo
                UNION ALL
                SELECT 1 FROM plantaopro.hospitais WHERE id = @id AND tenant_id = @tenantId
            )", new { id = c.ResponsavelFinanceiroId, tenantId }, tx, cancellationToken: ct));
        if (!respValido) throw new ArgumentException("Responsável financeiro informado é inválido ou inativo.");

        if (c.MedicoId.HasValue)
        {
            var medValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS (
                    SELECT 1 FROM plantaopro.medicos WHERE id = @id AND tenant_id = @tenantId
                    UNION ALL
                    SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @id AND tenant_id = @tenantId AND ativo
                )", new { id = c.MedicoId.Value, tenantId }, tx, cancellationToken: ct));
            if (!medValido) throw new ArgumentException("Médico informado é inválido.");
        }

        var id = Guid.NewGuid();
        var numeroSeq = await cn.ExecuteScalarAsync<long>("SELECT nextval('plantaopro.adm360_orcamento_numero')", transaction: tx);
        var numero = $"ORC-{numeroSeq:D6}";

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_orcamentos(
                id, tenant_id, numero, revisao, hospital_id, medico_id, procedimento,
                responsavel_financeiro_id, vendedor_id, data_prevista, validade,
                situacao, total_produtos, desconto_geral, total_geral, observacoes, created_by
            ) VALUES(
                @id, @tenantId, @numero, 1, @HospitalId, @MedicoId, @Procedimento,
                @ResponsavelFinanceiroId, @VendedorId, @DataPrevista, @Validade,
                'RASCUNHO', @totalProdutos, 0, @totalGeral, @Observacoes, @usuarioId
            )",
            new
            {
                id, tenantId, numero, c.HospitalId, c.MedicoId, c.Procedimento,
                c.ResponsavelFinanceiroId, c.VendedorId,
                DataPrevista = c.DataPrevista.ToDateTime(TimeOnly.MinValue),
                Validade = c.Validade.ToDateTime(TimeOnly.MinValue),
                totalProdutos, totalGeral, c.Observacoes, usuarioId
            }, tx, cancellationToken: ct));

        foreach (var it in totaisItens)
        {
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_orcamento_itens(
                    id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total
                ) VALUES(
                    gen_random_uuid(), @tenantId, @id, @ProdutoId, @Quantidade, @PrecoUnitario, @Desconto, @Total
                )",
                new { tenantId, id, it.ProdutoId, it.Quantidade, it.PrecoUnitario, it.Desconto, it.Total }, tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task AtualizarAsync(Guid tenantId, Guid usuarioId, AtualizarOrcamentoCommand c, CancellationToken ct)
    {
        if (c.Itens.Count == 0)
            throw new ArgumentException("O orçamento deve conter ao menos um item.");

        var totaisItens = new List<(Guid ProdutoId, decimal Quantidade, decimal PrecoUnitario, decimal Desconto, decimal Total)>();
        foreach (var item in c.Itens)
        {
            var itemTotal = OrcamentoCirurgicoRegras.CalcularTotalItem(item.Quantidade, item.PrecoUnitario, item.Desconto);
            totaisItens.Add((item.ProdutoId, item.Quantidade, item.PrecoUnitario, item.Desconto, itemTotal));
        }

        var (totalProdutos, totalGeral) = OrcamentoCirurgicoRegras.CalcularTotais(totaisItens.Select(x => x.Total), 0);

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var atual = await cn.QuerySingleOrDefaultAsync<OrcamentoHeaderRow>(new CommandDefinition(@"
            SELECT id AS Id, numero AS Numero, revisao AS Revisao, situacao AS Situacao, versao AS Versao
            FROM plantaopro.adm360_orcamentos
            WHERE id = @OrcamentoId AND tenant_id = @tenantId
            FOR UPDATE",
            new { c.OrcamentoId, tenantId }, tx, cancellationToken: ct));

        if (atual is null) throw new InvalidOperationException("Orçamento não encontrado.");

        if (OrcamentoCirurgicoRegras.PodeEditar(atual.Situacao))
        {
            // Rascunho: edita diretamente na mesma revisão
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_orcamentos
                SET hospital_id = @HospitalId, medico_id = @MedicoId, procedimento = @Procedimento,
                    responsavel_financeiro_id = @ResponsavelFinanceiroId, vendedor_id = @VendedorId,
                    data_prevista = @DataPrevista, validade = @Validade, total_produtos = @totalProdutos,
                    total_geral = @totalGeral, observacoes = @Observacoes, versao = versao + 1, updated_at = now()
                WHERE id = @OrcamentoId AND tenant_id = @tenantId",
                new
                {
                    c.HospitalId, c.MedicoId, c.Procedimento, c.ResponsavelFinanceiroId,
                    c.VendedorId,
                    DataPrevista = c.DataPrevista.ToDateTime(TimeOnly.MinValue),
                    Validade = c.Validade.ToDateTime(TimeOnly.MinValue),
                    totalProdutos, totalGeral,
                    c.Observacoes, c.OrcamentoId, tenantId
                }, tx, cancellationToken: ct));

            await cn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM plantaopro.adm360_orcamento_itens WHERE orcamento_id = @OrcamentoId AND tenant_id = @tenantId",
                new { c.OrcamentoId, tenantId }, tx, cancellationToken: ct));

            foreach (var it in totaisItens)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_orcamento_itens(id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total)
                    VALUES(gen_random_uuid(), @tenantId, @OrcamentoId, @ProdutoId, @Quantidade, @PrecoUnitario, @Desconto, @Total)",
                    new { tenantId, c.OrcamentoId, it.ProdutoId, it.Quantidade, it.PrecoUnitario, it.Desconto, it.Total }, tx, cancellationToken: ct));
            }
        }
        else
        {
            // Alteração após aprovação ou envio: cria nova revisão histórica
            var itensAntigos = (await cn.QueryAsync<dynamic>(new CommandDefinition(
                "SELECT produto_id, quantidade, preco_unitario, desconto, total FROM plantaopro.adm360_orcamento_itens WHERE orcamento_id = @OrcamentoId AND tenant_id = @tenantId",
                new { c.OrcamentoId, tenantId }, tx, cancellationToken: ct))).ToList();

            var snapshot = JsonSerializer.Serialize(new
            {
                RevisaoAnterior = atual.Revisao,
                Situacao = atual.Situacao,
                Itens = itensAntigos
            });

            var motivo = string.IsNullOrWhiteSpace(c.MotivoRevisao)
                ? "Revisão comercial de orçamento"
                : c.MotivoRevisao.Trim();

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_orcamento_revisoes(id, tenant_id, orcamento_id, revisao, motivo, snapshot_json, criado_por)
                VALUES(gen_random_uuid(), @tenantId, @OrcamentoId, @Revisao, @motivo, @snapshot, @usuarioId)",
                new { tenantId, c.OrcamentoId, Revisao = atual.Revisao, motivo, snapshot, usuarioId }, tx, cancellationToken: ct));

            var novaRevisao = atual.Revisao + 1;
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_orcamentos
                SET revisao = @novaRevisao, hospital_id = @HospitalId, medico_id = @MedicoId, procedimento = @Procedimento,
                    responsavel_financeiro_id = @ResponsavelFinanceiroId, vendedor_id = @VendedorId,
                    data_prevista = @DataPrevista, validade = @Validade, total_produtos = @totalProdutos,
                    total_geral = @totalGeral, observacoes = @Observacoes, situacao = 'RASCUNHO',
                    aprovado_em = null, aprovado_por = null, versao = versao + 1, updated_at = now()
                WHERE id = @OrcamentoId AND tenant_id = @tenantId",
                new
                {
                    novaRevisao, c.HospitalId, c.MedicoId, c.Procedimento, c.ResponsavelFinanceiroId,
                    c.VendedorId,
                    DataPrevista = c.DataPrevista.ToDateTime(TimeOnly.MinValue),
                    Validade = c.Validade.ToDateTime(TimeOnly.MinValue),
                    totalProdutos, totalGeral,
                    c.Observacoes, c.OrcamentoId, tenantId
                }, tx, cancellationToken: ct));

            await cn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM plantaopro.adm360_orcamento_itens WHERE orcamento_id = @OrcamentoId AND tenant_id = @tenantId",
                new { c.OrcamentoId, tenantId }, tx, cancellationToken: ct));

            foreach (var it in totaisItens)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_orcamento_itens(id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total)
                    VALUES(gen_random_uuid(), @tenantId, @OrcamentoId, @ProdutoId, @Quantidade, @PrecoUnitario, @Desconto, @Total)",
                    new { tenantId, c.OrcamentoId, it.ProdutoId, it.Quantidade, it.PrecoUnitario, it.Desconto, it.Total }, tx, cancellationToken: ct));
            }
        }

        await tx.CommitAsync(ct);
    }

    public async Task AprovarAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, string key, CancellationToken ct)
    {
        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var atual = await cn.QuerySingleOrDefaultAsync<OrcamentoHeaderRow>(new CommandDefinition(@"
            SELECT id AS Id, situacao AS Situacao, versao AS Versao
            FROM plantaopro.adm360_orcamentos
            WHERE id = @orcamentoId AND tenant_id = @tenantId
            FOR UPDATE",
            new { orcamentoId, tenantId }, tx, cancellationToken: ct));

        if (atual is null) throw new InvalidOperationException("Orçamento não encontrado.");

        if (atual.Situacao == "APROVADO")
        {
            await tx.CommitAsync(ct);
            return; // Idempotente
        }

        if (!OrcamentoCirurgicoRegras.PodeAprovar(atual.Situacao))
            throw new InvalidOperationException($"Orçamento em situação '{atual.Situacao}' não pode ser aprovado.");

        // Congela versão comercial. Não movimenta estoque nem gera financeiro.
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_orcamentos
            SET situacao = 'APROVADO', aprovado_em = now(), aprovado_por = @usuarioId,
                idempotency_key = @key, versao = versao + 1, updated_at = now()
            WHERE id = @orcamentoId AND tenant_id = @tenantId",
            new { orcamentoId, tenantId, usuarioId, key }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task RejeitarAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, string motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Motivo da rejeição é obrigatório.");

        await using var cn = Connection();
        var affected = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_orcamentos
            SET situacao = 'REJEITADO', observacoes = COALESCE(observacoes || E'\n', '') || 'Rejeição: ' || @motivo,
                versao = versao + 1, updated_at = now()
            WHERE id = @orcamentoId AND tenant_id = @tenantId AND situacao IN ('RASCUNHO', 'ENVIADO')",
            new { orcamentoId, tenantId, motivo }, cancellationToken: ct));

        if (affected == 0) throw new InvalidOperationException("Orçamento não encontrado ou em situação incompatível.");
    }

    public async Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, string motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Motivo do cancelamento é obrigatório.");

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var affected = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_orcamentos
            SET situacao = 'CANCELADO', observacoes = COALESCE(observacoes || E'\n', '') || 'Cancelamento: ' || @motivo,
                versao = versao + 1, updated_at = now()
            WHERE id = @orcamentoId AND tenant_id = @tenantId AND situacao <> 'CANCELADO'",
            new { orcamentoId, tenantId, motivo }, tx, cancellationToken: ct));

        if (affected == 0) throw new InvalidOperationException("Orçamento não encontrado ou já cancelado.");

        // Cancelamento do orçamento cancela apenas reservas ainda ATIVAS
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_reservas
            SET situacao = 'CANCELADA'
            WHERE tenant_id = @tenantId AND origem_tipo = 'ORCAMENTO_CIRURGICO' AND origem_id = @orcamentoId AND situacao = 'ATIVA'",
            new { tenantId, orcamentoId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task<PlanejamentoReservaOrcamento?> ObterPlanejamentoReservaAsync(Guid tenantId, Guid orcamentoId, CancellationToken ct)
    {
        await using var cn = Connection();
        var header = await cn.QuerySingleOrDefaultAsync<OrcamentoHeaderRow>(new CommandDefinition(@"
            SELECT o.id AS Id, o.numero AS Numero, o.situacao AS Situacao, o.data_prevista AS DataPrevista,
                   COALESCE(h.nome, hosp.nome, 'Hospital') AS Hospital
            FROM plantaopro.adm360_orcamentos o
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = o.hospital_id AND h.tenant_id = o.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = o.hospital_id AND hosp.tenant_id = o.tenant_id
            WHERE o.id = @orcamentoId AND o.tenant_id = @tenantId",
            new { orcamentoId, tenantId }, cancellationToken: ct));

        if (header is null) return null;

        var items = (await cn.QueryAsync<OrcamentoItemPlanejamentoRow>(new CommandDefinition(@"
            SELECT i.id AS ItemId, i.produto_id AS ProdutoId, p.sku AS Sku, p.nome AS Produto, i.quantidade AS Quantidade
            FROM plantaopro.adm360_orcamento_itens i
            JOIN plantaopro.adm360_produtos p ON p.id = i.produto_id AND p.tenant_id = i.tenant_id
            WHERE i.orcamento_id = @orcamentoId AND i.tenant_id = @tenantId
            ORDER BY p.nome, i.id",
            new { orcamentoId, tenantId }, cancellationToken: ct))).ToList();

        var planejados = new List<ItemReservaPlanejamento>();

        foreach (var it in items)
        {
            var reservada = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                SELECT COALESCE(SUM(quantidade), 0)
                FROM plantaopro.adm360_reservas
                WHERE tenant_id = @tenantId AND origem_tipo = 'ORCAMENTO_CIRURGICO' AND origem_id = @orcamentoId
                  AND (orcamento_item_id = @itemId OR (orcamento_item_id IS NULL AND produto_id = @produtoId))
                  AND situacao = 'ATIVA'",
                new { tenantId, orcamentoId, itemId = it.ItemId, produtoId = it.ProdutoId }, cancellationToken: ct));

            var falta = Math.Max(0m, it.Quantidade - reservada);

            // Sugestão FEFO (First Expired, First Out) de lotes liberados com saldo
            var lotesRaw = (await cn.QueryAsync<LoteQueryRow>(new CommandDefinition(@"
                SELECT lote_id AS LoteId, lote AS Lote, validade AS Validade, local_id AS LocalId,
                       local AS Local, fisico AS Fisico, reservado AS Reservado, disponivel AS Disponivel
                FROM plantaopro.adm360_saldos
                WHERE tenant_id = @tenantId AND produto_id = @produtoId AND condicao = 'LIBERADO' AND fisico > 0
                ORDER BY validade ASC NULLS LAST",
                new { tenantId, produtoId = it.ProdutoId }, cancellationToken: ct))).ToList();

            var lotesElegiveis = lotesRaw.Select(l =>
            {
                var compativel = !l.Validade.HasValue || (l.Validade.Value >= header.DataPrevista && l.Validade.Value >= DateOnly.FromDateTime(DateTime.UtcNow));
                return new LoteElegivelReserva(
                    l.LoteId, l.Lote, l.Validade, l.LocalId, l.Local, l.Fisico, l.Reservado, l.Disponivel, compativel);
            }).ToList();

            planejados.Add(new ItemReservaPlanejamento(it.ProdutoId, it.Produto, it.Sku, it.Quantidade, reservada, falta, lotesElegiveis, it.ItemId));
        }

        return new PlanejamentoReservaOrcamento(header.Id, header.Numero, header.Situacao, header.DataPrevista, header.Hospital, planejados);
    }

    public async Task ReservarItemAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, ReservarCommand command, CancellationToken ct)
    {
        Estoque.ValidarQuantidade(command.Quantidade);

        var payloadHash = IdempotenciaHelper.CalcularHash(
            "RESERVA",
            tenantId,
            orcamentoId,
            command.ProdutoId,
            command.LoteId,
            command.LocalId,
            command.Quantidade.ToString("0.0000", CultureInfo.InvariantCulture),
            command.OrcamentoItemId?.ToString() ?? string.Empty);

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            // 1.2: Idempotência ANTES de revalidar saldo consumido pela própria operação
            var opExistente = await cn.QuerySingleOrDefaultAsync<OperacaoExistenteRow>(new CommandDefinition(
                "SELECT id, payload_hash AS PayloadHash, resultado FROM plantaopro.adm360_operacoes WHERE tenant_id = @tenantId AND idempotency_key = @key",
                new { tenantId, key = command.IdempotencyKey }, tx, cancellationToken: ct));

            if (opExistente is not null)
            {
                if (string.Equals(opExistente.PayloadHash, payloadHash, StringComparison.OrdinalIgnoreCase))
                {
                    return; // Retry com mesmo conteúdo: devolução do sucesso sem reexecutar
                }
                throw new InvalidOperationException("Conflito de idempotência: a mesma chave foi utilizada com conteúdo divergente.");
            }

            var orc = await cn.QuerySingleOrDefaultAsync<OrcamentoHeaderRow>(new CommandDefinition(@"
                SELECT id AS Id, situacao AS Situacao, data_prevista AS DataPrevista
                FROM plantaopro.adm360_orcamentos
                WHERE id = @orcamentoId AND tenant_id = @tenantId
                FOR UPDATE",
                new { orcamentoId, tenantId }, tx, cancellationToken: ct));

            if (orc is null) throw new InvalidOperationException("Orçamento não encontrado.");
            if (!OrcamentoCirurgicoRegras.PodeReservar(orc.Situacao))
                throw new InvalidOperationException($"Somente orçamento APROVADO pode receber reservas de materiais. Situação atual: {orc.Situacao}.");

            // Validação de local ativo do tenant
            var localAtivo = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM plantaopro.adm360_locais WHERE id = @LocalId AND tenant_id = @tenantId AND ativo)",
                new { command.LocalId, tenantId }, tx, cancellationToken: ct));
            if (!localAtivo) throw new ArgumentException("Local de estoque informado é inválido ou inativo.");

            // Validação do lote pertencente ao produto e tenant
            var loteRow = await cn.QuerySingleOrDefaultAsync<LoteValidacaoRow>(new CommandDefinition(
                "SELECT id, produto_id AS ProdutoId, validade AS Validade FROM plantaopro.adm360_lotes WHERE id = @LoteId AND tenant_id = @tenantId",
                new { command.LoteId, tenantId }, tx, cancellationToken: ct));
            if (loteRow is null || loteRow.ProdutoId != command.ProdutoId)
                throw new ArgumentException("O lote informado não pertence ao produto especificado ou não existe.");

            // 1.1: Identidade do item e validação de limite do orçamento
            OrcamentoItemRow? orcItem = null;
            if (command.OrcamentoItemId.HasValue)
            {
                orcItem = await cn.QuerySingleOrDefaultAsync<OrcamentoItemRow>(new CommandDefinition(@"
                    SELECT id AS Id, orcamento_id AS OrcamentoId, produto_id AS ProdutoId, quantidade AS Quantidade
                    FROM plantaopro.adm360_orcamento_itens
                    WHERE id = @ItemId AND orcamento_id = @orcamentoId AND tenant_id = @tenantId",
                    new { ItemId = command.OrcamentoItemId.Value, orcamentoId, tenantId }, tx, cancellationToken: ct));

                if (orcItem is null)
                    throw new ArgumentException("Item de orçamento especificado não foi encontrado no orçamento aprovado.");
                if (orcItem.ProdutoId != command.ProdutoId)
                    throw new ArgumentException("O produto da reserva diverge do produto cadastrado na linha do orçamento.");
            }
            else
            {
                var itensProduto = (await cn.QueryAsync<OrcamentoItemRow>(new CommandDefinition(@"
                    SELECT id AS Id, orcamento_id AS OrcamentoId, produto_id AS ProdutoId, quantidade AS Quantidade
                    FROM plantaopro.adm360_orcamento_itens
                    WHERE orcamento_id = @orcamentoId AND produto_id = @ProdutoId AND tenant_id = @tenantId
                    ORDER BY id",
                    new { orcamentoId, command.ProdutoId, tenantId }, tx, cancellationToken: ct))).ToList();

                if (itensProduto.Count == 0)
                    throw new ArgumentException("O produto informado não pertence a este orçamento.");

                foreach (var candidate in itensProduto)
                {
                    var jaReservadoCand = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                        SELECT COALESCE(SUM(quantidade), 0)
                        FROM plantaopro.adm360_reservas
                        WHERE tenant_id = @tenantId AND origem_tipo = 'ORCAMENTO_CIRURGICO' AND origem_id = @orcamentoId
                          AND (orcamento_item_id = @itemId OR (orcamento_item_id IS NULL AND produto_id = @ProdutoId))
                          AND situacao = 'ATIVA'",
                        new { tenantId, orcamentoId, itemId = candidate.Id, command.ProdutoId }, tx, cancellationToken: ct));

                    if (candidate.Quantidade - jaReservadoCand >= command.Quantidade)
                    {
                        orcItem = candidate;
                        break;
                    }
                }
                orcItem ??= itensProduto[0];
            }

            var totalJaReservadoItem = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                SELECT COALESCE(SUM(quantidade), 0)
                FROM plantaopro.adm360_reservas
                WHERE tenant_id = @tenantId AND origem_tipo = 'ORCAMENTO_CIRURGICO' AND origem_id = @orcamentoId
                  AND (orcamento_item_id = @itemId OR (orcamento_item_id IS NULL AND produto_id = @ProdutoId))
                  AND situacao = 'ATIVA'",
                new { tenantId, orcamentoId, itemId = orcItem.Id, command.ProdutoId }, tx, cancellationToken: ct));

            var saldoNecessidade = orcItem.Quantidade - totalJaReservadoItem;
            if (command.Quantidade > saldoNecessidade)
            {
                throw new InvalidOperationException($"Quantidade solicitada ({command.Quantidade}) excede a necessidade ainda não atendida do item do orçamento ({saldoNecessidade}).");
            }

            var lockKey = $"{tenantId}:{command.ProdutoId}:{command.LoteId}:{command.LocalId}";
            await BloquearChavesDeterministasAsync(cn, tx, new[] { lockKey }, ct);

            // Bloqueio de inventário no local
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, command.LocalId, ct);

            // Validar compatibilidade de data de validade com a data prevista da cirurgia
            OrcamentoCirurgicoRegras.ValidarDataCirurgiaEValidadeLote(orc.DataPrevista, loteRow.Validade);

            // Validar quantidade disponível
            var available = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                SELECT COALESCE(disponivel, 0)
                FROM plantaopro.adm360_saldos
                WHERE tenant_id = @tenantId AND produto_id = @ProdutoId AND lote_id = @LoteId AND local_id = @LocalId AND condicao = 'LIBERADO'",
                new { tenantId, command.ProdutoId, command.LoteId, command.LocalId }, tx, cancellationToken: ct));

            if (available < command.Quantidade)
                throw new InvalidOperationException("Quantidade solicitada excede o estoque disponível do lote.");

            var opId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_operacoes(id, tenant_id, tipo, idempotency_key, payload_hash, resultado, created_by)
                VALUES(@opId, @tenantId, 'RESERVA', @key, @payloadHash, 'CONCLUIDO', @usuarioId)",
                new { opId, tenantId, key = command.IdempotencyKey, payloadHash, usuarioId }, tx, cancellationToken: ct));

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_reservas(
                    id, tenant_id, produto_id, lote_id, local_id, quantidade, situacao, origem_tipo, origem_id, orcamento_item_id, idempotency_key, created_by
                ) VALUES(
                    gen_random_uuid(), @tenantId, @ProdutoId, @LoteId, @LocalId, @Quantidade, 'ATIVA', 'ORCAMENTO_CIRURGICO', @orcamentoId, @orcamentoItemId, @key, @usuarioId
                )",
                new
                {
                    tenantId,
                    command.ProdutoId,
                    command.LoteId,
                    command.LocalId,
                    command.Quantidade,
                    orcamentoId,
                    orcamentoItemId = orcItem.Id,
                    key = command.IdempotencyKey,
                    usuarioId
                }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task CancelarReservaAsync(Guid tenantId, Guid usuarioId, Guid orcamentoId, Guid reservaId, string motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Motivo do cancelamento de reserva é obrigatório.");

        await using var cn = Connection();
        var affected = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_reservas
            SET situacao = 'CANCELADA'
            WHERE id = @reservaId AND tenant_id = @tenantId AND origem_tipo = 'ORCAMENTO_CIRURGICO' AND origem_id = @orcamentoId AND situacao = 'ATIVA'",
            new { reservaId, tenantId, orcamentoId }, cancellationToken: ct));

        if (affected == 0)
            throw new InvalidOperationException("Reserva não encontrada ou já encerrada/cancelada.");
    }

    public async Task<IReadOnlyList<OrcamentoRevisaoHistorico>> ObterRevisoesAsync(Guid tenantId, Guid orcamentoId, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<OrcamentoRevisaoHistorico>(new CommandDefinition(@"
            SELECT id AS Id, revisao AS Revisao, motivo AS Motivo, snapshot_json AS SnapshotJson, criado_em AS CriadoEm
            FROM plantaopro.adm360_orcamento_revisoes
            WHERE orcamento_id = @orcamentoId AND tenant_id = @tenantId
            ORDER BY revisao DESC",
            new { orcamentoId, tenantId }, cancellationToken: ct))).AsList();
    }
}
