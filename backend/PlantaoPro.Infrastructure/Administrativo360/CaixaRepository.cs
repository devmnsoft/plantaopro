using Dapper;
using PlantaoPro.Application.Administrativo360;

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
            (decimal)r.saldoatual,
            (bool)r.ativo
        );
    }

    public async Task<Guid> CriarContaAsync(Guid tenantId, Guid usuarioId, CriarContaFinanceiraCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Nome))
            throw new ArgumentException("Nome da conta financeira é obrigatório.");

        var id = Guid.NewGuid();
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_contas_financeiras(
                id, tenant_id, nome, tipo, banco, agencia, conta, saldo_inicial, ativo
            ) VALUES(
                @id, @tenantId, @Nome, @Tipo, @Banco, @Agencia, @Conta, @SaldoInicial, true
            )",
            new
            {
                id, tenantId, command.Nome, command.Tipo,
                command.Banco, command.Agencia, command.Conta, command.SaldoInicial
            }, cancellationToken: ct));

        return id;
    }

    public async Task<ExtratoContaDto> ExtratoContaAsync(Guid tenantId, Guid contaId, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        var conta = await ObterContaPorIdAsync(tenantId, contaId, ct);
        if (conta is null) throw new InvalidOperationException("Conta financeira não encontrada.");

        await using var cn = Connection();
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
            ORDER BY m.data_movimento DESC, m.created_at DESC",
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

        decimal entradas = movimentos.Where(m => m.Tipo == "ENTRADA").Sum(m => m.Valor);
        decimal saidas = movimentos.Where(m => m.Tipo == "SAIDA").Sum(m => m.Valor);

        return new ExtratoContaDto(
            conta,
            entradas,
            saidas,
            conta.SaldoAtual,
            movimentos
        );
    }

    public async Task<FluxoCaixaDto> FluxoCaixaAsync(Guid tenantId, DateOnly inicio, DateOnly fim, CancellationToken ct)
    {
        await using var cn = Connection();

        var contas = await ListarContasAsync(tenantId, ct);
        decimal saldoAtualContas = contas.Sum(c => c.SaldoAtual);

        var totalPrevisto = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
            SELECT COALESCE(SUM(saldo_aberto), 0)
            FROM plantaopro.adm360_titulos_receber
            WHERE tenant_id = @tenantId AND situacao IN ('ABERTO', 'PARCIAL')",
            new { tenantId }, cancellationToken: ct));

        var totalRealizado = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
            SELECT COALESCE(SUM(valor), 0)
            FROM plantaopro.adm360_movimentos_financeiros
            WHERE tenant_id = @tenantId AND tipo = 'ENTRADA'",
            new { tenantId }, cancellationToken: ct));

        var previstosRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT t.data_vencimento AS Data,
                   'Previsão: Título ' || t.numero || ' (' || COALESCE(p.nome, 'Cliente') || ')' AS Descricao,
                   'TITULO_RECEBER' AS Origem,
                   t.saldo_aberto AS Valor
            FROM plantaopro.adm360_titulos_receber t
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = t.pagador_id AND p.tenant_id = t.tenant_id
            WHERE t.tenant_id = @tenantId
              AND t.situacao IN ('ABERTO', 'PARCIAL')
              AND t.data_vencimento >= @inicio AND t.data_vencimento <= @fim",
            new { tenantId, inicio, fim }, cancellationToken: ct));

        var realizadosRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT m.data_movimento AS Data,
                   m.descricao AS Descricao,
                   m.origem_tipo AS Origem,
                   m.tipo AS Tipo,
                   m.valor AS Valor
            FROM plantaopro.adm360_movimentos_financeiros m
            WHERE m.tenant_id = @tenantId
              AND m.data_movimento >= @inicio AND m.data_movimento <= @fim",
            new { tenantId, inicio, fim }, cancellationToken: ct));

        var itens = new List<FluxoCaixaItemDto>();

        foreach (var p in previstosRows)
        {
            itens.Add(new FluxoCaixaItemDto(
                ToDateOnly(p.data),
                (string)p.descricao,
                (string)p.origem,
                (decimal)p.valor,
                0m,
                0m,
                0m
            ));
        }

        foreach (var r in realizadosRows)
        {
            string tipo = (string)r.tipo;
            decimal valor = (decimal)r.valor;
            itens.Add(new FluxoCaixaItemDto(
                ToDateOnly(r.data),
                (string)r.descricao,
                (string)r.origem,
                0m,
                tipo == "ENTRADA" ? valor : 0m,
                tipo == "SAIDA" ? valor : 0m,
                0m
            ));
        }

        var ordenados = itens.OrderBy(i => i.Data).ToList();
        var resultadoItens = new List<FluxoCaixaItemDto>();
        decimal saldoAcumulado = saldoAtualContas;

        foreach (var it in ordenados)
        {
            saldoAcumulado += (it.RealizadoEntrada - it.RealizadoSaida);
            resultadoItens.Add(new FluxoCaixaItemDto(
                it.Data, it.Descricao, it.Origem, it.PrevistoEntrada, it.RealizadoEntrada, it.RealizadoSaida, saldoAcumulado
            ));
        }

        return new FluxoCaixaDto(
            saldoAtualContas,
            totalPrevisto,
            totalRealizado,
            resultadoItens
        );
    }
}
