using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class CaixaRepository : Adm360Repository, ICaixaRepository
{
    public CaixaRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<ContaFinanceiraDto>> ListarContasAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT c.id AS Id,
                   c.nome AS Nome,
                   c.tipo AS Tipo,
                   c.banco AS Banco,
                   c.agencia AS Agencia,
                   c.conta AS Conta,
                   c.saldo_inicial AS SaldoInicial,
                   c.data_saldo_inicial AS DataSaldoInicial,
                   (c.saldo_inicial +
                    COALESCE((SELECT SUM(m.valor) FROM plantaopro.adm360_movimentos_financeiros m WHERE m.conta_id = c.id AND m.tenant_id = c.tenant_id AND m.tipo = 'ENTRADA'), 0) -
                    COALESCE((SELECT SUM(m.valor) FROM plantaopro.adm360_movimentos_financeiros m WHERE m.conta_id = c.id AND m.tenant_id = c.tenant_id AND m.tipo = 'SAIDA'), 0)
                   ) AS SaldoAtual,
                   c.ativo AS Ativo
            FROM plantaopro.adm360_contas_financeiras c
            WHERE c.tenant_id = @tenantId
            ORDER BY c.nome",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new ContaFinanceiraDto(
            (Guid)r.id,
            (string)r.nome,
            (string)r.tipo,
            (string?)r.banco,
            (string?)r.agencia,
            (string?)r.conta,
            (decimal)r.saldoinicial,
            r.datasaldoinicial is not null ? ToDateOnly(r.datasaldoinicial) : null,
            (decimal)r.saldoatual,
            (bool)r.ativo
        )).ToList();
    }

    public async Task<ContaFinanceiraDto?> ObterContaPorIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        var r = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT c.id AS Id,
                   c.nome AS Nome,
                   c.tipo AS Tipo,
                   c.banco AS Banco,
                   c.agencia AS Agencia,
                   c.conta AS Conta,
                   c.saldo_inicial AS SaldoInicial,
                   c.data_saldo_inicial AS DataSaldoInicial,
                   (c.saldo_inicial +
                    COALESCE((SELECT SUM(m.valor) FROM plantaopro.adm360_movimentos_financeiros m WHERE m.conta_id = c.id AND m.tenant_id = c.tenant_id AND m.tipo = 'ENTRADA'), 0) -
                    COALESCE((SELECT SUM(m.valor) FROM plantaopro.adm360_movimentos_financeiros m WHERE m.conta_id = c.id AND m.tenant_id = c.tenant_id AND m.tipo = 'SAIDA'), 0)
                   ) AS SaldoAtual,
                   c.ativo AS Ativo
            FROM plantaopro.adm360_contas_financeiras c
            WHERE c.id = @id AND c.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (r is null) return null;

        return new ContaFinanceiraDto(
            (Guid)r.id,
            (string)r.nome,
            (string)r.tipo,
            (string?)r.banco,
            (string?)r.agencia,
            (string?)r.conta,
            (decimal)r.saldoinicial,
            r.datasaldoinicial is not null ? ToDateOnly(r.datasaldoinicial) : null,
            (decimal)r.saldoatual,
            (bool)r.ativo
        );
    }

    private static string NormalizarTipoConta(string? tipo)
    {
        var t = (tipo ?? "BANCO").Trim().ToUpperInvariant();
        if (t == "CORRENTE" || t == "BANCO") return "BANCO";
        if (t == "CAIXA" || t == "CAIXA_FISICO") return "CAIXA";
        if (t == "APLICACAO") return "APLICACAO";
        return "BANCO";
    }

    public async Task<Guid> CriarContaAsync(Guid tenantId, Guid usuarioId, CriarContaFinanceiraCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nome))
            throw new ArgumentException("Nome da conta financeira é obrigatório.");

        var tipo = NormalizarTipoConta(command.Tipo);
        var id = Guid.NewGuid();
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_contas_financeiras(
                id, tenant_id, nome, tipo, banco, agencia, conta, saldo_inicial, data_saldo_inicial, ativo
            ) VALUES(
                @id, @tenantId, @Nome, @tipo, @Banco, @Agencia, @Conta, @SaldoInicial, @DataSaldoInicial, true
            )",
            new
            {
                id, tenantId, command.Nome, tipo,
                command.Banco, command.Agencia, command.Conta,
                command.SaldoInicial, command.DataSaldoInicial
            }, cancellationToken: ct));

        return id;
    }

    public async Task AtualizarContaAsync(Guid tenantId, Guid usuarioId, AtualizarContaFinanceiraCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nome))
            throw new ArgumentException("Nome da conta financeira é obrigatório.");

        var tipo = NormalizarTipoConta(command.Tipo);
        await using var cn = Connection();
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_contas_financeiras
            SET nome = @Nome,
                tipo = @tipo,
                banco = @Banco,
                agencia = @Agencia,
                conta = @Conta,
                data_saldo_inicial = @DataSaldoInicial,
                updated_at = now()
            WHERE id = @ContaId AND tenant_id = @tenantId",
            new
            {
                command.ContaId, tenantId, command.Nome, tipo,
                command.Banco, command.Agencia, command.Conta, command.DataSaldoInicial
            }, cancellationToken: ct));
    }

    public async Task InativarContaAsync(Guid tenantId, Guid usuarioId, Guid contaId, CancellationToken ct)
    {
        await using var cn = Connection();
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_contas_financeiras
            SET ativo = false, updated_at = now()
            WHERE id = @contaId AND tenant_id = @tenantId",
            new { contaId, tenantId }, cancellationToken: ct));
    }

    public async Task<ExtratoContaDto> ExtratoContaAsync(Guid tenantId, Guid contaId, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        if (inicio.HasValue && fim.HasValue && inicio.Value > fim.Value)
            throw new ArgumentException("Data inicial do extrato não pode ser maior que a data final.");

        var conta = await ObterContaPorIdAsync(tenantId, contaId, ct);
        if (conta is null) throw new InvalidOperationException("Conta financeira não encontrada.");

        await using var cn = Connection();

        // Saldo de abertura do período = saldo inicial documentado (se a data base for atingida) + movimentos anteriores ao início
        decimal saldoAbertura = 0m;
        if (inicio.HasValue)
        {
            var saldoInicialDoc = (conta.DataSaldoInicial == null || conta.DataSaldoInicial <= inicio.Value)
                ? conta.SaldoInicial
                : 0m;

            var movsAnteriores = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                SELECT COALESCE(SUM(CASE WHEN tipo = 'ENTRADA' THEN valor ELSE -valor END), 0)
                FROM plantaopro.adm360_movimentos_financeiros
                WHERE conta_id = @contaId AND tenant_id = @tenantId AND data_movimento < @inicio",
                new { contaId, tenantId, inicio = inicio.Value }, cancellationToken: ct));

            saldoAbertura = saldoInicialDoc + movsAnteriores;
        }
        else
        {
            saldoAbertura = conta.SaldoInicial;
        }

        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT m.id AS Id,
                   m.conta_id AS ContaId,
                   c.nome AS ContaNome,
                   m.tipo AS Tipo,
                   m.valor AS Valor,
                   m.data_movimento AS DataMovimento,
                   m.descricao AS Descricao,
                   m.origem_tipo AS OrigemTipo,
                   m.created_at AS CriadoEm
            FROM plantaopro.adm360_movimentos_financeiros m
            JOIN plantaopro.adm360_contas_financeiras c ON c.id = m.conta_id AND c.tenant_id = m.tenant_id
            WHERE m.conta_id = @contaId AND m.tenant_id = @tenantId
              AND (@inicio IS NULL OR m.data_movimento >= @inicio)
              AND (@fim IS NULL OR m.data_movimento <= @fim)
            ORDER BY m.data_movimento ASC, m.created_at ASC, m.id ASC",
            new { contaId, tenantId, inicio, fim }, cancellationToken: ct));

        var movimentos = rows.Select(r => new MovimentoFinanceiroDto(
            (Guid)r.id,
            (Guid)r.contaid,
            (string)r.contanome,
            (string)r.tipo,
            (decimal)r.valor,
            ToDateOnly(r.datamovimento),
            (string)r.descricao,
            (string)r.origemtipo,
            (DateTime)r.criadoem
        )).ToList();

        decimal totalEntradas = movimentos.Where(m => m.Tipo == "ENTRADA").Sum(m => m.Valor);
        decimal totalSaidas = movimentos.Where(m => m.Tipo == "SAIDA").Sum(m => m.Valor);
        decimal saldoFechamento = saldoAbertura + totalEntradas - totalSaidas;

        return new ExtratoContaDto(
            conta,
            saldoAbertura,
            totalEntradas,
            totalSaidas,
            saldoFechamento,
            conta.SaldoAtual,
            movimentos
        );
    }

    public async Task<FluxoCaixaDto> FluxoCaixaAsync(Guid tenantId, DateOnly inicio, DateOnly fim, CancellationToken ct)
    {
        CaixaRegras.ValidarPeriodo(inicio, fim);

        await using var cn = Connection();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        // Saldo atual de todas as contas
        var contas = await ListarContasAsync(tenantId, ct);
        decimal saldoAtualContas = contas.Sum(c => c.SaldoAtual);

        // Saldo de abertura consolidado do período =
        // Soma dos saldos iniciais das contas (se existentes até o início) + movimentos consolidados anteriores ao início
        decimal saldoInicialContasAbertura = contas
            .Where(c => c.DataSaldoInicial == null || c.DataSaldoInicial <= inicio)
            .Sum(c => c.SaldoInicial);

        decimal movsAnterioresConsolidados = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
            SELECT COALESCE(SUM(CASE WHEN tipo = 'ENTRADA' THEN valor ELSE -valor END), 0)
            FROM plantaopro.adm360_movimentos_financeiros
            WHERE tenant_id = @tenantId AND data_movimento < @inicio",
            new { tenantId, inicio }, cancellationToken: ct));

        decimal saldoAberturaPeriodo = saldoInicialContasAbertura + movsAnterioresConsolidados;

        // Movimentos realizados no período
        var realizadosRows = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT m.data_movimento AS Data,
                   m.descricao AS Descricao,
                   m.origem_tipo AS Origem,
                   m.tipo AS Tipo,
                   m.valor AS Valor
            FROM plantaopro.adm360_movimentos_financeiros m
            WHERE m.tenant_id = @tenantId
              AND m.data_movimento >= @inicio AND m.data_movimento <= @fim
            ORDER BY m.data_movimento ASC, m.created_at ASC",
            new { tenantId, inicio, fim }, cancellationToken: ct))).ToList();

        decimal totalEntradasRealizadas = realizadosRows.Where(r => (string)r.tipo == "ENTRADA").Sum(r => (decimal)r.valor);
        decimal totalSaidasRealizadas = realizadosRows.Where(r => (string)r.tipo == "SAIDA").Sum(r => (decimal)r.valor);
        decimal saldoFechamentoPeriodo = saldoAberturaPeriodo + totalEntradasRealizadas - totalSaidasRealizadas;

        // Previsões de Contas a Receber no período
        var arRows = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT t.data_vencimento AS Data,
                   'Recebimento Previsto: Título ' || t.numero || ' (' || COALESCE(p.nome, 'Cliente') || ')' AS Descricao,
                   'TITULO_RECEBER' AS Origem,
                   t.saldo_aberto AS Valor
            FROM plantaopro.adm360_titulos_receber t
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = t.pagador_id AND p.tenant_id = t.tenant_id
            WHERE t.tenant_id = @tenantId
              AND t.situacao IN ('ABERTO', 'PARCIAL')
              AND t.data_vencimento >= @inicio AND t.data_vencimento <= @fim
            ORDER BY t.data_vencimento ASC, t.numero ASC",
            new { tenantId, inicio, fim }, cancellationToken: ct))).ToList();

        decimal totalEntradasPrevistas = arRows.Sum(r => (decimal)r.valor);

        // Previsões de Contas a Pagar no período
        var apRows = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT t.data_vencimento AS Data,
                   'Pagamento Previsto: Título ' || t.numero || ' (' || COALESCE(f.nome, 'Fornecedor') || ')' AS Descricao,
                   'TITULO_PAGAR' AS Origem,
                   t.saldo_aberto AS Valor
            FROM plantaopro.adm360_titulos_pagar t
            LEFT JOIN plantaopro.adm360_parceiros f ON f.id = t.fornecedor_id AND f.tenant_id = t.tenant_id
            WHERE t.tenant_id = @tenantId
              AND t.situacao IN ('PENDENTE_APROVACAO', 'APROVADO', 'PARCIAL')
              AND t.data_vencimento >= @inicio AND t.data_vencimento <= @fim
            ORDER BY t.data_vencimento ASC, t.numero ASC",
            new { tenantId, inicio, fim }, cancellationToken: ct))).ToList();

        decimal totalSaidasPrevistas = apRows.Sum(r => (decimal)r.valor);

        // Vencidos (fora ou dentro do período com vencimento anterior à data base)
        decimal totalVencidosReceber = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
            SELECT COALESCE(SUM(saldo_aberto), 0)
            FROM plantaopro.adm360_titulos_receber
            WHERE tenant_id = @tenantId AND situacao IN ('ABERTO', 'PARCIAL') AND data_vencimento < @hoje",
            new { tenantId, hoje }, cancellationToken: ct));

        decimal totalVencidosPagar = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
            SELECT COALESCE(SUM(saldo_aberto), 0)
            FROM plantaopro.adm360_titulos_pagar
            WHERE tenant_id = @tenantId AND situacao IN ('PENDENTE_APROVACAO', 'APROVADO', 'PARCIAL') AND data_vencimento < @hoje",
            new { tenantId, hoje }, cancellationToken: ct));

        // Itens combinados para a linha do tempo do fluxo de caixa
        var itensBrutos = new List<(DateOnly Data, string Descricao, string Origem, decimal PrevEntrada, decimal PrevSaida, decimal RealEntrada, decimal RealSaida, int Ordem)>();

        foreach (var r in realizadosRows)
        {
            string tipo = (string)r.tipo;
            decimal valor = (decimal)r.valor;
            itensBrutos.Add((
                ToDateOnly(r.data),
                (string)r.descricao,
                (string)r.origem,
                0m,
                0m,
                tipo == "ENTRADA" ? valor : 0m,
                tipo == "SAIDA" ? valor : 0m,
                1 // Realizados têm precedência determinística no mesmo dia
            ));
        }

        foreach (var p in arRows)
        {
            itensBrutos.Add((
                ToDateOnly(p.data),
                (string)p.descricao,
                (string)p.origem,
                (decimal)p.valor,
                0m,
                0m,
                0m,
                2
            ));
        }

        foreach (var a in apRows)
        {
            itensBrutos.Add((
                ToDateOnly(a.data),
                (string)a.descricao,
                (string)a.origem,
                0m,
                (decimal)a.valor,
                0m,
                0m,
                3
            ));
        }

        var itensOrdenados = itensBrutos
            .OrderBy(i => i.Data)
            .ThenBy(i => i.Ordem)
            .ThenBy(i => i.Descricao)
            .ToList();

        var resultadoItens = new List<FluxoCaixaItemDto>();
        decimal saldoAcumulado = saldoAberturaPeriodo;

        foreach (var it in itensOrdenados)
        {
            saldoAcumulado += (it.RealEntrada - it.RealSaida);
            resultadoItens.Add(new FluxoCaixaItemDto(
                it.Data,
                it.Descricao,
                it.Origem,
                it.PrevEntrada,
                it.PrevSaida,
                it.RealEntrada,
                it.RealSaida,
                saldoAcumulado
            ));
        }

        return new FluxoCaixaDto(
            hoje,
            saldoAtualContas,
            saldoAberturaPeriodo,
            saldoFechamentoPeriodo,
            totalEntradasPrevistas,
            totalSaidasPrevistas,
            totalEntradasRealizadas,
            totalSaidasRealizadas,
            totalVencidosReceber,
            totalVencidosPagar,
            resultadoItens
        );
    }

    public async Task<IReadOnlyList<CaixaFechamentoDto>> ListarFechamentosAsync(Guid tenantId, Guid? contaId, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT f.id AS Id,
                   f.conta_id AS ContaId,
                   c.nome AS ContaNome,
                   f.data_inicio AS DataInicio,
                   f.data_fim AS DataFim,
                   f.saldo_abertura AS SaldoAbertura,
                   f.total_entradas AS TotalEntradas,
                   f.total_saidas AS TotalSaidas,
                   f.saldo_calculado AS SaldoCalculado,
                   f.saldo_conferido AS SaldoConferido,
                   f.diferenca AS Diferenca,
                   f.justificativa AS Justificativa,
                   f.situacao AS Situacao,
                   f.motivo_reabertura AS MotivoReabertura,
                   f.reaberto_em AS ReabertoEm,
                   u_reab.nome AS ReabertoPor,
                   u_fech.nome AS FechadoPor,
                   f.created_at AS CriadoEm
            FROM plantaopro.adm360_caixa_fechamentos f
            JOIN plantaopro.adm360_contas_financeiras c ON c.id = f.conta_id AND c.tenant_id = f.tenant_id
            JOIN plantaopro.usuarios u_fech ON u_fech.id = f.fechado_por AND u_fech.tenant_id = f.tenant_id
            LEFT JOIN plantaopro.usuarios u_reab ON u_reab.id = f.reaberto_por AND u_reab.tenant_id = f.tenant_id
            WHERE f.tenant_id = @tenantId
              AND (@contaId::uuid IS NULL OR f.conta_id = @contaId::uuid)
            ORDER BY f.data_fim DESC, f.created_at DESC",
            new { tenantId, contaId }, cancellationToken: ct));

        return rows.Select(r => new CaixaFechamentoDto(
            (Guid)r.id,
            (Guid)r.contaid,
            (string)r.contanome,
            ToDateOnly(r.datainicio),
            ToDateOnly(r.datafim),
            (decimal)r.saldoabertura,
            (decimal)r.totalentradas,
            (decimal)r.totalsaidas,
            (decimal)r.saldocalculado,
            (decimal)r.saldoconferido,
            (decimal)r.diferenca,
            (string?)r.justificativa,
            (string)r.situacao,
            (string?)r.motivoreabertura,
            r.reabertoem is not null ? (DateTime?)r.reabertoem : null,
            (string?)r.reabertopor,
            (string)r.fechadopor,
            (DateTime)r.criadoem
        )).ToList();
    }

    public async Task<CaixaFechamentoDto?> ObterFechamentoAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        var r = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT f.id AS Id,
                   f.conta_id AS ContaId,
                   c.nome AS ContaNome,
                   f.data_inicio AS DataInicio,
                   f.data_fim AS DataFim,
                   f.saldo_abertura AS SaldoAbertura,
                   f.total_entradas AS TotalEntradas,
                   f.total_saidas AS TotalSaidas,
                   f.saldo_calculado AS SaldoCalculado,
                   f.saldo_conferido AS SaldoConferido,
                   f.diferenca AS Diferenca,
                   f.justificativa AS Justificativa,
                   f.situacao AS Situacao,
                   f.motivo_reabertura AS MotivoReabertura,
                   f.reaberto_em AS ReabertoEm,
                   u_reab.nome AS ReabertoPor,
                   u_fech.nome AS FechadoPor,
                   f.created_at AS CriadoEm
            FROM plantaopro.adm360_caixa_fechamentos f
            JOIN plantaopro.adm360_contas_financeiras c ON c.id = f.conta_id AND c.tenant_id = f.tenant_id
            JOIN plantaopro.usuarios u_fech ON u_fech.id = f.fechado_por AND u_fech.tenant_id = f.tenant_id
            LEFT JOIN plantaopro.usuarios u_reab ON u_reab.id = f.reaberto_por AND u_reab.tenant_id = f.tenant_id
            WHERE f.id = @id AND f.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (r is null) return null;

        return new CaixaFechamentoDto(
            (Guid)r.id,
            (Guid)r.contaid,
            (string)r.contanome,
            ToDateOnly(r.datainicio),
            ToDateOnly(r.datafim),
            (decimal)r.saldoabertura,
            (decimal)r.totalentradas,
            (decimal)r.totalsaidas,
            (decimal)r.saldocalculado,
            (decimal)r.saldoconferido,
            (decimal)r.diferenca,
            (string?)r.justificativa,
            (string)r.situacao,
            (string?)r.motivoreabertura,
            r.reabertoem is not null ? (DateTime?)r.reabertoem : null,
            (string?)r.reabertopor,
            (string)r.fechadopor,
            (DateTime)r.criadoem
        );
    }

    public async Task<Guid> FecharCaixaAsync(Guid tenantId, Guid usuarioId, FecharCaixaCommand command, CancellationToken ct)
    {
        CaixaRegras.ValidarPeriodo(command.DataInicio, command.DataFim);

        var extrato = await ExtratoContaAsync(tenantId, command.ContaId, command.DataInicio, command.DataFim, ct);
        decimal saldoCalculado = extrato.SaldoFechamento;
        decimal diferenca = command.SaldoConferido - saldoCalculado;

        CaixaRegras.ValidarFechamento(saldoCalculado, command.SaldoConferido, command.Justificativa);

        var id = Guid.NewGuid();
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_caixa_fechamentos(
                id, tenant_id, conta_id, data_inicio, data_fim, saldo_abertura,
                total_entradas, total_saidas, saldo_calculado, saldo_conferido,
                diferenca, justificativa, situacao, fechado_por
            ) VALUES(
                @id, @tenantId, @ContaId, @DataInicio, @DataFim, @saldoAbertura,
                @totalEntradas, @totalSaidas, @saldoCalculado, @SaldoConferido,
                @diferenca, @Justificativa, 'FECHADO', @usuarioId
            )",
            new
            {
                id, tenantId, command.ContaId, command.DataInicio, command.DataFim,
                saldoAbertura = extrato.SaldoAbertura, totalEntradas = extrato.TotalEntradas,
                totalSaidas = extrato.TotalSaidas, saldoCalculado, command.SaldoConferido,
                diferenca, command.Justificativa, usuarioId
            }, cancellationToken: ct));

        return id;
    }

    public async Task ReabrirCaixaAsync(Guid tenantId, Guid usuarioId, ReabrirCaixaCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Motivo))
            throw new ArgumentException("Motivo da reabertura do caixa é obrigatório.");

        await using var cn = Connection();
        var rows = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_caixa_fechamentos
            SET situacao = 'REABERTO',
                motivo_reabertura = @Motivo,
                reaberto_em = now(),
                reaberto_por = @usuarioId
            WHERE id = @FechamentoId AND tenant_id = @tenantId AND situacao = 'FECHADO'",
            new { command.FechamentoId, tenantId, command.Motivo, usuarioId }, cancellationToken: ct));

        if (rows == 0)
            throw new InvalidOperationException("Fechamento não encontrado ou não está fechado.");
    }

    public async Task<DateOnly?> ObterUltimaDataFechamentoAsync(Guid tenantId, Guid contaId, CancellationToken ct)
    {
        await using var cn = Connection();
        var dt = await cn.ExecuteScalarAsync<DateTime?>(new CommandDefinition(@"
            SELECT MAX(data_fim)
            FROM plantaopro.adm360_caixa_fechamentos
            WHERE tenant_id = @tenantId AND conta_id = @contaId AND situacao = 'FECHADO'",
            new { tenantId, contaId }, cancellationToken: ct));

        return dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null;
    }
}
