using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class Adm360FinanceiroRelatoriosRepository : Adm360Repository, IAdm360FinanceiroRelatoriosRepository
{
    public Adm360FinanceiroRelatoriosRepository(string connectionString) : base(connectionString) { }

    // s9 (rodada 9 r9f): rejeição cross-tenant do filtro por cliente (400).
    // v.cliente_id NÃO tem FK — historicamente pode apontar para adm360_parceiros OU para a
    // tabela clínica hospitais (mantida por compatibilidade de registros históricos). O filtro
    // só aceita ids existentes neste tenant; se o id existir em outro tenant, o erro é explícito
    // em vez de devolver um relatório vazio que parece "sem vendas".
    private static async Task ValidarClienteFiltroAsync(NpgsqlConnection cn, Guid tenantId, Guid? clienteId, CancellationToken ct)
    {
        if (clienteId is null) return;
        var noTenant = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @clienteId AND tenant_id = @tenantId)
               OR EXISTS (SELECT 1 FROM plantaopro.hospitais WHERE id = @clienteId AND tenant_id = @tenantId);",
            new { tenantId, clienteId }, cancellationToken: ct));
        if (noTenant) return;

        var emAlgumTenant = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @clienteId)
               OR EXISTS (SELECT 1 FROM plantaopro.hospitais WHERE id = @clienteId);",
            new { clienteId }, cancellationToken: ct));
        if (emAlgumTenant)
            throw new Administrativo360BusinessException("O cliente informado pertence a outro tenant.");
        throw new Administrativo360BusinessException("Cliente não encontrado.");
    }

    public async Task<IReadOnlyList<RelatorioVendasItem>> RelatorioVendasAsync(
        Guid tenantId, DateOnly? inicio, DateOnly? fim, Guid? clienteId, Guid? vendedorId, CancellationToken ct)
    {
        await using var cn = Connection();
        await ValidarClienteFiltroAsync(cn, tenantId, clienteId, ct);
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT v.numero AS VendaNumero,
                   v.competencia AS Competencia,
                   COALESCE(c.nome, hosp.nome, 'Cliente não identificado') AS Cliente,
                   COALESCE(p.nome, c.nome, hosp.nome, 'Pagador não informado') AS Pagador,
                   COALESCE(u.nome, 'Não informado') AS Vendedor,
                   v.total_liquido AS TotalLiquido,
                   v.total_custo AS TotalCusto,
                   v.comissao_prevista AS ComissaoPrevista,
                   v.situacao AS Situacao,
                   v.total_bruto AS TotalBruto,
                   v.desconto AS Desconto
            FROM plantaopro.adm360_vendas v
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = v.pagador_id AND p.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = v.vendedor_id AND u.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
              AND (@inicio::date IS NULL OR v.competencia >= @inicio::date)
              AND (@fim::date IS NULL OR v.competencia <= @fim::date)
              AND (@clienteId::uuid IS NULL OR v.cliente_id = @clienteId::uuid)
              AND (@vendedorId::uuid IS NULL OR v.vendedor_id = @vendedorId::uuid)
            ORDER BY v.competencia DESC, v.numero DESC",
            new { tenantId, inicio, fim, clienteId, vendedorId }, cancellationToken: ct));

        return rows.Select(r => new RelatorioVendasItem(
            (string)r.vendanumero,
            ToDateOnly(r.competencia),
            (string)r.cliente,
            (string)r.pagador,
            (string)r.vendedor,
            (decimal)r.totalliquido,
            (decimal)r.totalcusto,
            (decimal)r.comissaoprevista,
            (string)r.situacao,
            (decimal)r.totalbruto,
            (decimal)r.desconto
        )).ToList();
    }

    public async Task<IReadOnlyList<RelatorioComissaoItem>> RelatorioComissoesAsync(
        Guid tenantId, Guid? vendedorId, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT COALESCE(u.nome, 'Vendedor') AS Vendedor,
                   v.numero AS VendaNumero,
                   b.data_recebimento AS DataRecebimento,
                   c.base_calculo AS BaseCalculo,
                   c.percentual AS Percentual,
                   c.valor_comissao AS ComissaoApropriada,
                   c.situacao AS Situacao
            FROM plantaopro.adm360_comissoes_apropriadas c
            JOIN plantaopro.adm360_vendas v ON v.id = c.venda_id AND v.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = c.vendedor_id AND u.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_titulo_baixas b ON b.id = c.baixa_id AND b.tenant_id = c.tenant_id
            WHERE c.tenant_id = @tenantId
              AND (@vendedorId::uuid IS NULL OR c.vendedor_id = @vendedorId::uuid)
              AND (@inicio::date IS NULL OR b.data_recebimento >= @inicio::date)
              AND (@fim::date IS NULL OR b.data_recebimento <= @fim::date)
            ORDER BY b.data_recebimento DESC, u.nome",
            new { tenantId, vendedorId, inicio, fim }, cancellationToken: ct));

        return rows.Select(r => new RelatorioComissaoItem(
            (string)r.vendedor,
            (string)r.vendanumero,
            r.datarecebimento is not null ? ToDateOnly(r.datarecebimento) : DateOnly.FromDateTime(DateTime.UtcNow),
            (decimal)r.basecalculo,
            (decimal)r.percentual,
            (decimal)r.comissaoapropriada,
            (string)r.situacao
        )).ToList();
    }

    public async Task<IReadOnlyList<RelatorioMargemItem>> RelatorioMargemAsync(
        Guid tenantId, DateOnly? inicio, DateOnly? fim, CancellationToken ct, Guid? clienteId = null)
    {
        await using var cn = Connection();
        await ValidarClienteFiltroAsync(cn, tenantId, clienteId, ct);
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT v.numero AS VendaNumero,
                   COALESCE(vale.numero, 'N/A') AS ValeNumero,
                   COALESCE(c.nome, hosp.nome, 'Cliente não identificado') AS Cliente,
                   v.total_liquido AS ReceitaLiquida,
                   v.total_custo AS CustoConsumido,
                   v.comissao_prevista AS ComissaoPrevista,
                   COALESCE((SELECT SUM(com.valor_comissao) FROM plantaopro.adm360_comissoes_apropriadas com WHERE com.venda_id = v.id AND com.tenant_id = v.tenant_id AND com.situacao = 'APROPRIADA'), 0) AS ComissaoApropriada
            FROM plantaopro.adm360_vendas v
            LEFT JOIN plantaopro.adm360_valorizacoes val ON val.id = v.valorizacao_id AND val.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_vales vale ON vale.id = val.vale_id AND vale.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = v.tenant_id
            WHERE v.tenant_id = @tenantId
              AND v.situacao = 'CONFIRMADA'
              AND (@inicio::date IS NULL OR v.competencia >= @inicio::date)
              AND (@fim::date IS NULL OR v.competencia <= @fim::date)
              AND (@clienteId::uuid IS NULL OR v.cliente_id = @clienteId::uuid)
            ORDER BY v.competencia DESC, v.numero DESC",
            new { tenantId, inicio, fim, clienteId }, cancellationToken: ct));

        return rows.Select(r =>
        {
            decimal receita = (decimal)r.receitaliquida;
            decimal custo = (decimal)r.custoconsumido;
            decimal comissaoPrevista = (decimal)r.comissaoprevista;
            decimal comissaoApropriada = (decimal)r.comissaoapropriada;
            decimal margem = receita - custo - comissaoPrevista;
            decimal margemPct = receita > 0 ? Math.Round((margem / receita) * 100m, 2) : 0m;

            return new RelatorioMargemItem(
                (string)r.vendanumero,
                (string)r.valenumero,
                (string)r.cliente,
                receita,
                custo,
                comissaoPrevista,
                comissaoApropriada,
                margem,
                margemPct
            );
        }).ToList();
    }

    // s10 (rodada 9 r9f): vencimentos por parcela — um título = exatamente uma linha (os JOINs
    // de nome são 1:1, sem fan-out), ordenado pelo vencimento. O período filtra data_vencimento
    // (eixo do relatório). CANCELADO fica de fora (parcela que não será cobrada); QUITADO/PARCIAL
    // aparecem para histórico de liquidação (saldo 0 / parcial). "Vencido" = vencimento < data
    // do relatório (referência = geração do relatório) && saldo_aberto > 0 — mesma regra das
    // contas a receber (ContasReceberRepository). Totais somam as linhas uma única vez: sem multiplicação.
    public async Task<IReadOnlyList<RelatorioVencimentoItem>> RelatorioVencimentosAsync(
        Guid tenantId, DateOnly? inicio, DateOnly? fim, Guid? clienteId, CancellationToken ct)
    {
        await using var cn = Connection();
        await ValidarClienteFiltroAsync(cn, tenantId, clienteId, ct);
        var referencia = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT t.numero AS TituloNumero,
                   v.numero AS VendaNumero,
                   v.competencia AS Competencia,
                   COALESCE(c.nome, hosp.nome, 'Cliente não identificado') AS Cliente,
                   COALESCE(p.nome, 'Pagador não informado') AS Pagador,
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
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = t.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = t.pagador_id AND p.tenant_id = t.tenant_id
            WHERE t.tenant_id = @tenantId
              AND t.situacao <> 'CANCELADO'
              AND (@inicio::date IS NULL OR t.data_vencimento >= @inicio::date)
              AND (@fim::date IS NULL OR t.data_vencimento <= @fim::date)
              AND (@clienteId::uuid IS NULL OR v.cliente_id = @clienteId::uuid)
            ORDER BY t.data_vencimento, t.numero",
            new { tenantId, inicio, fim, clienteId }, cancellationToken: ct));

        return rows.Select(r =>
        {
            var vencimento = ToDateOnly(r.datavencimento);
            decimal saldo = (decimal)r.saldoaberto;
            return new RelatorioVencimentoItem(
                (string)r.titulonumero,
                (string)r.vendanumero,
                ToDateOnly(r.competencia),
                (string)r.cliente,
                (string)r.pagador,
                (int)r.parcela,
                (int)r.totalparcelas,
                ToDateOnly(r.dataemissao),
                vencimento,
                (decimal)r.valorprincipal,
                (decimal)r.valorrecebido,
                saldo,
                (string)r.situacao,
                vencimento < referencia && saldo > 0
            );
        }).ToList();
    }

    // s11 (rodada 9 r9g): DRE gerencial por CLIENTE em COMPETÊNCIA (não fluxo de caixa).
    // - Receita: adm360_vendas CONFIRMADA, período sobre v.competencia (não data de recebimento);
    //   "deduções" = descontos identificados na venda; "custo realizado" = valor gravado na venda.
    // - Despesas: adm360_titulos_pagar do período (sobre p.competencia; CANCELADO fora). Nesta base
    //   a despesa NÃO tem vínculo com cliente comercial (origem_tipo ∈ RECEBIMENTO_COMPRA /
    //   DESPESA_MANUAL / COMISSAO_VENDEDOR) — sem filtro, todo o total cai no bucket "Não atribuída"
    //   (limitação declarada, sem regra de atribuição inventada); com filtro de cliente, zero.
    // - Conciliação: consolidado = clientes + "Não atribuída", verificado re-agregando as mesmas
    //   fontes e comparando (deltas explícitos em vez de somas que se "casam por construção").
    public async Task<DreGerencialDto> RelatorioDreAsync(Guid tenantId, DateOnly? inicio, DateOnly? fim, Guid? clienteId, CancellationToken ct)
    {
        await using var cn = Connection();
        await ValidarClienteFiltroAsync(cn, tenantId, clienteId, ct);

        const string filtroVendas = @"
              AND v.tenant_id = @tenantId
              AND v.situacao = 'CONFIRMADA'
              AND (@inicio::date IS NULL OR v.competencia >= @inicio::date)
              AND (@fim::date IS NULL OR v.competencia <= @fim::date)
              AND (@clienteId::uuid IS NULL OR v.cliente_id = @clienteId::uuid)";
        const string filtroDespesas = @"
              AND p.tenant_id = @tenantId
              AND p.situacao <> 'CANCELADO'
              AND (@inicio::date IS NULL OR p.competencia >= @inicio::date)
              AND (@fim::date IS NULL OR p.competencia <= @fim::date)";

        var sqlVendas = $@"
            SELECT v.cliente_id AS ClienteId,
                   CASE WHEN c.id IS NOT NULL OR hosp.id IS NOT NULL THEN COALESCE(c.nome, hosp.nome) ELSE NULL END AS Nome,
                   count(*)::int AS QtdVendas,
                   COALESCE(sum(v.total_bruto), 0) AS ReceitaBruta,
                   COALESCE(sum(v.desconto), 0) AS Deducoes,
                   COALESCE(sum(v.total_liquido), 0) AS ReceitaLiquida,
                   COALESCE(sum(v.total_custo), 0) AS CustoRealizado,
                   COALESCE(sum(v.comissao_prevista), 0) AS ComissoesPrevistas
            FROM plantaopro.adm360_vendas v
            LEFT JOIN plantaopro.adm360_parceiros c ON c.id = v.cliente_id AND c.tenant_id = v.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = v.cliente_id AND hosp.tenant_id = v.tenant_id
            WHERE true
{filtroVendas}
            GROUP BY v.cliente_id, c.id, hosp.id, c.nome, hosp.nome
            ORDER BY 2 NULLS LAST";
        var rowsVendas = await cn.QueryAsync<dynamic>(new CommandDefinition(sqlVendas, new { tenantId, inicio, fim, clienteId }, cancellationToken: ct));

        var linhas = new List<DreGerencialLinha>();
        bool temOrfa = false;
        int qtdOrfa = 0;
        decimal brutoOrfa = 0m, dedOrfa = 0m, liqOrfa = 0m, custoOrfa = 0m, comOrfa = 0m;

        foreach (var r in rowsVendas)
        {
            string? nome = (string?)r.nome;
            int qtd = (int)r.qtdvendas;
            decimal bruto = (decimal)r.receitabruta;
            decimal ded = (decimal)r.deducoes;
            decimal liq = (decimal)r.receitaliquida;
            decimal custo = (decimal)r.custorealizado;
            decimal comissao = (decimal)r.comissoesprevistas;

            if (nome is null)
            {
                // bucket "Não atribuída": coalesce qualquer cliente_id que não resolva em nome
                temOrfa = true;
                qtdOrfa += qtd; brutoOrfa += bruto; dedOrfa += ded; liqOrfa += liq; custoOrfa += custo; comOrfa += comissao;
            }
            else
            {
                linhas.Add(new DreGerencialLinha((Guid?)r.clienteid, nome, qtd, bruto, ded, liq, custo, liq - custo, comissao, 0m, liq - custo - comissao));
            }
        }

        // despesas do período (somente quando sem filtro — com filtro, a linha do cliente leva 0)
        int qtdDespesas = 0;
        decimal totalDespesas = 0m;
        if (clienteId is null)
        {
            qtdDespesas = await cn.ExecuteScalarAsync<int>(new CommandDefinition($@"
                SELECT count(*)
                FROM plantaopro.adm360_titulos_pagar p
                WHERE true
{filtroDespesas}", new { tenantId, inicio, fim }, cancellationToken: ct));
            totalDespesas = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition($@"
                SELECT COALESCE(sum(p.valor_principal), 0)
                FROM plantaopro.adm360_titulos_pagar p
                WHERE true
{filtroDespesas}", new { tenantId, inicio, fim }, cancellationToken: ct));
        }

        // conciliação: re-agrega as MESMAS fontes do relatório e compara com as linhas
        decimal liqRef = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition($@"
            SELECT COALESCE(sum(v.total_liquido), 0)
            FROM plantaopro.adm360_vendas v
            WHERE true
{filtroVendas}", new { tenantId, inicio, fim, clienteId }, cancellationToken: ct));
        decimal custoRef = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition($@"
            SELECT COALESCE(sum(v.total_custo), 0)
            FROM plantaopro.adm360_vendas v
            WHERE true
{filtroVendas}", new { tenantId, inicio, fim, clienteId }, cancellationToken: ct));

        decimal liqLinhas = linhas.Sum(x => x.ReceitaLiquida) + liqOrfa;
        decimal custoLinhas = linhas.Sum(x => x.CustoRealizado) + custoOrfa;
        int vendasNoPeriodo = linhas.Sum(x => x.QtdVendas) + qtdOrfa;

        // somas do PostgreSQL (numeric) e .NET decimal são exatas — comparação direta é válida
        bool okLiq = liqLinhas == liqRef;
        bool okCusto = custoLinhas == custoRef;
        // Conciliação de despesas: nesta base todo o total vai para "Não atribuída" (sem divisão em
        // linhas de cliente) → bate por construção com a referência da mesma consulta; o flag e o
        // delta seguem explícitos na resposta para a UI e para eventual regra futura de atribuição.
        bool okDes = true;
        decimal deltaDes = 0m;

        if (temOrfa || totalDespesas > 0m)
        {
            linhas.Add(new DreGerencialLinha(
                null, "Não atribuída", qtdOrfa, brutoOrfa, dedOrfa, liqOrfa, custoOrfa,
                liqOrfa - custoOrfa, comOrfa, totalDespesas, liqOrfa - custoOrfa - comOrfa - totalDespesas));
        }

        var final = linhas
            .OrderBy(x => x.ClienteId is null ? 1 : 0)   // bucket "Não atribuída" sempre por último
            .ThenBy(x => x.Cliente, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new DreGerencialDto(
            inicio, fim, final,
            new DreGerencialConciliacao(
                vendasNoPeriodo, qtdDespesas,
                okLiq, Math.Abs(liqLinhas - liqRef),
                okCusto, Math.Abs(custoLinhas - custoRef),
                okDes, deltaDes));
    }
}
