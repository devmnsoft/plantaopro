using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

// s9/s10 (rodada 9 r9f): relatórios financeiros por CLIENTE comercial (rejeição cross-tenant)
// + vencimentos POR PARCELA (uma linha por título, vencido na data do relatório).
public sealed class Administrativo360RelatoriosFinanceiroClienteTests
{
    private static string ObterConnectionString() => TestDatabase.ConnectionString;

    private static async Task GarantirConexaoBancoAsync(string cs)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para a suíte de integração não está acessível: {ex.Message}");
        }
    }

    [Fact]
    public async Task RelatoriosPorCliente_NomeFiltroCrossTenantEVencimentosPorParcela()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var clienteA = Guid.NewGuid();
        var pagadorA = Guid.NewGuid();
        var clienteB = Guid.NewGuid(); // parceiro de OUTRO tenant → rejeição cross-tenant
        var vendaId = Guid.NewGuid();
        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var t3 = Guid.NewGuid();
        var t4 = Guid.NewGuid();

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var vencVencida = hoje.AddDays(-10);   // ABERTO vencida → Vencido = true
        var vencQuitada = hoje.AddDays(-5);    // QUITADA vencida → Vencido = false (saldo 0)
        var vencFuturo = hoje.AddDays(15);     // ABERTA futura → Vencido = false
        var vencCancelada = hoje.AddDays(-3);  // CANCELADA → fora do relatório

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // A construção do repositório dispara o construtor estático de Adm360Repository, que registra
        // o TypeHandler do Dapper para DateOnly — necessário para os parâmetros @venc* abaixo.
        var repo = new Adm360FinanceiroRelatoriosRepository(cs);

        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants(id, tenant_id, codigo, nome, status)
            VALUES
                (@tenantA, @tenantA, 'tenant-r9f-a', 'Tenant R9F A', 'ATIVO'),
                (@tenantB, @tenantB, 'tenant-r9f-b', 'Tenant R9F B', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES
                (@clienteA, @tenantA, 'Cliente Teste Alfa', null, false, true),
                (@pagadorA, @tenantA, 'Pagador Teste Alfa', null, true, true),
                (@clienteB, @tenantB, 'Cliente Outro Tenant', null, false, true);

            INSERT INTO plantaopro.adm360_vendas(
                id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia,
                total_bruto, desconto, total_liquido, total_custo, comissao_percentual, comissao_prevista,
                condicao_pagamento, quantidade_parcelas, situacao, idempotency_key, created_by
            ) VALUES(
                @vendaId, @tenantA, 'VEN-R9F-01', 'DIRETA', @vendaId, @clienteA, @pagadorA, date '2026-10-01',
                120.00, 20.00, 100.00, 50.00, 5, 5.00, 'A_PRAZO', 4, 'CONFIRMADA', 'key-venda-r9f', @usuarioId
            );

            INSERT INTO plantaopro.adm360_titulos_receber(
                id, tenant_id, venda_id, numero, pagador_id, parcela, total_parcelas,
                data_emissao, data_vencimento, valor_principal, valor_recebido, saldo_aberto, situacao, created_by
            ) VALUES
                (@t1, @tenantA, @vendaId, 'TIT-R9F-01', @pagadorA, 1, 4, date '2026-10-01', @vencVencida, 25.00, 0.00, 25.00, 'ABERTO', @usuarioId),
                (@t2, @tenantA, @vendaId, 'TIT-R9F-02', @pagadorA, 2, 4, date '2026-10-01', @vencQuitada, 25.00, 25.00, 0.00, 'QUITADO', @usuarioId),
                (@t3, @tenantA, @vendaId, 'TIT-R9F-03', @pagadorA, 3, 4, date '2026-10-01', @vencFuturo, 25.00, 0.00, 25.00, 'ABERTO', @usuarioId),
                (@t4, @tenantA, @vendaId, 'TIT-R9F-04', @pagadorA, 4, 4, date '2026-10-01', @vencCancelada, 25.00, 0.00, 0.00, 'CANCELADO', @usuarioId);
        ", new { tenantA, tenantB, usuarioId, clienteA, pagadorA, clienteB, vendaId, t1, t2, t3, t4, vencVencida, vencQuitada, vencFuturo, vencCancelada });

        // -------------------------------------------------------------------
        // s9 — dimensão CLIENTE no relatório de vendas:
        // nome resolvido via adm360_parceiros + bruto/desconto transportados.
        // -------------------------------------------------------------------
        var vendas = await repo.RelatorioVendasAsync(tenantA, null, null, null, null, CancellationToken.None);
        Assert.Single(vendas);
        Assert.Equal("Cliente Teste Alfa", vendas[0].Cliente);
        Assert.Equal("Pagador Teste Alfa", vendas[0].Pagador);
        Assert.Equal(120.00m, vendas[0].TotalBruto);
        Assert.Equal(20.00m, vendas[0].Desconto);
        Assert.Equal(100.00m, vendas[0].TotalLiquido);

        // Filtro por cliente do próprio tenant funciona.
        var filtrado = await repo.RelatorioVendasAsync(tenantA, null, null, clienteA, null, CancellationToken.None);
        Assert.Single(filtrado);
        Assert.Equal("VEN-R9F-01", filtrado[0].VendaNumero);

        // Cliente de OUTRO tenant é rejeitado (400 no HTTP — exceção de negócio no repositório).
        await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            repo.RelatorioVendasAsync(tenantA, null, null, clienteB, null, CancellationToken.None));

        // Cliente inexistente também é rejeitado com erro explícito (não relatório vazio).
        await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            repo.RelatorioVendasAsync(tenantA, null, null, Guid.NewGuid(), null, CancellationToken.None));

        // Isolamento por tenant: o tenant A não vê dados do B (nenhuma venda no B).
        var vendasB = await repo.RelatorioVendasAsync(tenantB, null, null, null, null, CancellationToken.None);
        Assert.Empty(vendasB);

        // -------------------------------------------------------------------
        // s10 — vencimentos POR PARCELA: uma linha por título (sem multiplicação);
        // CANCELADA fora; vencido = vencimento < data do relatório && saldo > 0.
        // -------------------------------------------------------------------
        var vencimentos = await repo.RelatorioVencimentosAsync(tenantA, null, null, null, CancellationToken.None);
        Assert.Equal(3, vencimentos.Count); // t4 (CANCELADA) não aparece

        var vencida = Assert.Single(vencimentos, x => x.Vencido);
        Assert.Equal(1, vencida.Parcela);
        Assert.Equal(25.00m, vencida.SaldoAberto);

        // QUITADA com vencimento passado NÃO é vencida (saldo 0).
        var quitada = vencimentos.Single(x => x.Parcela == 2);
        Assert.False(quitada.Vencido);
        Assert.Equal("QUITADO", quitada.Situacao);

        // ABERTA futura não é vencida.
        Assert.False(vencimentos.Single(x => x.Parcela == 3).Vencido);

        // Nome do cliente aparece na linha da parcela.
        Assert.All(vencimentos, x => Assert.Equal("Cliente Teste Alfa", x.Cliente));

        // Período sobre data_vencimento (inclusivo): só a futura entra na janela "amanhã em diante".
        var somenteFuturo = await repo.RelatorioVencimentosAsync(tenantA, hoje.AddDays(1), null, null, CancellationToken.None);
        Assert.Single(somenteFuturo);
        Assert.Equal(3, somenteFuturo[0].Parcela);

        // Filtro por cliente no vencimento + cross-tenant também aqui.
        Assert.Equal(3, (await repo.RelatorioVencimentosAsync(tenantA, null, null, clienteA, CancellationToken.None)).Count);
        await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            repo.RelatorioVencimentosAsync(tenantA, null, null, clienteB, CancellationToken.None));

        // -------------------------------------------------------------------
        // s9 — margem gerencial: dimensão Cliente + filtro opcional.
        // -------------------------------------------------------------------
        var margens = await repo.RelatorioMargemAsync(tenantA, null, null, CancellationToken.None);
        Assert.Single(margens);
        Assert.Equal("Cliente Teste Alfa", margens[0].Cliente);
        Assert.Equal(45.00m, margens[0].MargemContribuicao); // 100 - 50 - 5
        Assert.Equal(45.00m, margens[0].MargemPercentual);    // 45/100 × 100

        Assert.Single(await repo.RelatorioMargemAsync(tenantA, null, null, CancellationToken.None, clienteA));
        await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            repo.RelatorioMargemAsync(tenantA, null, null, CancellationToken.None, clienteB));
    }

    // s11 (rodada 9 r9g): DRE gerencial POR CLIENTE em competência — bucket "Não atribuída"
    // (venda sem nome resolvido + despesas, que não têm vínculo com cliente nesta base),
    // reconciliação consolidado = clientes + não atribuída, e rejeição cross-tenant.
    [Fact]
    public async Task DreGerencial_PorCliente_CompentenciaConciliacaoEOrfa()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var clienteA = Guid.NewGuid();     // parceiro resolvido → linha com nome
        var clienteB = Guid.NewGuid();     // parceiro de OUTRO tenant → rejeição cross-tenant
        var orfaCli = Guid.NewGuid();     // cliente_id aleatório (sem parceiro/hospital) → "Não atribuída"
        var fornecedorA = Guid.NewGuid(); // fornecedor para adm360_titulos_pagar

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var compOk = hoje.AddDays(-5);     // dentro da janela [-30, +30]
        var compFora = hoje.AddDays(40);   // fora da janela principal, dentro da secundária [+31, +60]

        var vendaN1 = Guid.NewGuid();     // venda do cliente nomeado
        var vendaO1 = Guid.NewGuid();     // venda órfã (não atribuída)
        var dp1 = Guid.NewGuid();         // despesa APROVADA na janela → contabilizada
        var dp2 = Guid.NewGuid();         // despesa CANCELADA na janela → fora
        var dp3 = Guid.NewGuid();         // despesa APROVADA fora da janela → fora da principal

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // Repositório antes do SQL bruto (construtor estático registra o TypeHandler de DateOnly).
        var repo = new Adm360FinanceiroRelatoriosRepository(cs);

        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants(id, tenant_id, codigo, nome, status)
            VALUES
                (@tenantA, @tenantA, 'tenant-r9g-a', 'Tenant R9G A', 'ATIVO'),
                (@tenantB, @tenantB, 'tenant-r9g-b', 'Tenant R9G B', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES
                (@clienteA, @tenantA, 'Cliente DRE Alfa', null, false, true),
                (@fornecedorA, @tenantA, 'Fornecedor DRE Alfa', null, true, true),
                (@clienteB, @tenantB, 'Cliente DRE Outro Tenant', null, false, true);

            INSERT INTO plantaopro.adm360_vendas(
                id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia,
                total_bruto, desconto, total_liquido, total_custo, comissao_percentual, comissao_prevista,
                condicao_pagamento, quantidade_parcelas, situacao, idempotency_key, created_by
            ) VALUES
                (@vendaN1, @tenantA, 'VEN-R9G-01', 'DIRETA', @vendaN1, @clienteA, @fornecedorA, @compOk,
                 200.00, 20.00, 180.00, 80.00, 5, 9.00, 'A_PRAZO', 1, 'CONFIRMADA', 'key-ven-r9g-01', @usuarioId),
                (@vendaO1, @tenantA, 'VEN-R9G-02', 'DIRETA', @vendaO1, @orfaCli, @fornecedorA, @compOk,
                 100.00, 0.00, 100.00, 40.00, 4, 4.00, 'A_PRAZO', 1, 'CONFIRMADA', 'key-ven-r9g-02', @usuarioId);

            INSERT INTO plantaopro.adm360_titulos_pagar(
                id, tenant_id, numero, origem_tipo, fornecedor_id, competencia, data_emissao, data_vencimento,
                valor_principal, situacao, saldo_aberto
            ) VALUES
                (@dp1, @tenantA, 'TP-R9G-01', 'DESPESA_MANUAL', @fornecedorA, @compOk, @compOk, @compOk, 30.00, 'APROVADO', 30.00),
                (@dp2, @tenantA, 'TP-R9G-02', 'DESPESA_MANUAL', @fornecedorA, @compOk, @compOk, @compOk, 77.00, 'CANCELADO', 77.00),
                (@dp3, @tenantA, 'TP-R9G-03', 'DESPESA_MANUAL', @fornecedorA, @compFora, @compFora, @compFora, 55.00, 'APROVADO', 55.00);
        ", new { tenantA, tenantB, usuarioId, clienteA, clienteB, orfaCli, fornecedorA, compOk, compFora, vendaN1, vendaO1, dp1, dp2, dp3 });

        var jn = hoje.AddDays(-30);
        var jf = hoje.AddDays(30);

        // -------------------------------------------------------------------
        // Sem filtro: linha do cliente nomeado + bucket "Não atribuída" (órfã + despesas).
        // -------------------------------------------------------------------
        var dre = await repo.RelatorioDreAsync(tenantA, jn, jf, null, CancellationToken.None);
        Assert.Equal(2, dre.Linhas.Count);
        // ordenação: nomeado primeiro, "Não atribuída" sempre por último
        var linhaAlfa = dre.Linhas[0];
        var linhaOrfa = dre.Linhas[1];

        Assert.Equal("Cliente DRE Alfa", linhaAlfa.Cliente);
        Assert.Equal(clienteA, linhaAlfa.ClienteId);
        Assert.Equal(1, linhaAlfa.QtdVendas);
        Assert.Equal(200.00m, linhaAlfa.ReceitaBruta);
        Assert.Equal(20.00m, linhaAlfa.Deducoes);
        Assert.Equal(180.00m, linhaAlfa.ReceitaLiquida);
        Assert.Equal(80.00m, linhaAlfa.CustoRealizado);
        Assert.Equal(100.00m, linhaAlfa.ResultadoBruto);
        Assert.Equal(9.00m, linhaAlfa.ComissoesPrevistas);
        Assert.Equal(0.00m, linhaAlfa.DespesasAtribuidas);   // nenhuma regra de atribuição inventada
        Assert.Equal(91.00m, linhaAlfa.ResultadoGerencial);  // 180 - 80 - 9

        Assert.Equal("Não atribuída", linhaOrfa.Cliente);
        Assert.Null(linhaOrfa.ClienteId);
        Assert.Equal(1, linhaOrfa.QtdVendas);
        Assert.Equal(100.00m, linhaOrfa.ReceitaLiquida);
        Assert.Equal(40.00m, linhaOrfa.CustoRealizado);
        Assert.Equal(60.00m, linhaOrfa.ResultadoBruto);
        Assert.Equal(4.00m, linhaOrfa.ComissoesPrevistas);
        Assert.Equal(30.00m, linhaOrfa.DespesasAtribuidas);  // só a APROVADA na janela (CANCELADA e futura fora)
        Assert.Equal(26.00m, linhaOrfa.ResultadoGerencial);  // 60 - 4 - 30

        // conciliação: consolidado = clientes + não atribuída, re-agregado das mesmas fontes
        Assert.Equal(2, dre.Conciliacao.VendasNoPeriodo);
        Assert.Equal(1, dre.Conciliacao.DespesasNoPeriodo);
        Assert.True(dre.Conciliacao.ReceitaLiquidaBate);
        Assert.True(dre.Conciliacao.CustoBate);
        Assert.True(dre.Conciliacao.DespesasBatem);
        Assert.Equal(0m, dre.Conciliacao.DeltaReceitaLiquida);
        Assert.Equal(0m, dre.Conciliacao.DeltaCusto);
        Assert.Equal(0m, dre.Conciliacao.DeltaDespesas);

        // -------------------------------------------------------------------
        // Filtro por cliente do próprio tenant: só a linha dele, sem despesas (regra declarada).
        // -------------------------------------------------------------------
        var dreFiltrado = await repo.RelatorioDreAsync(tenantA, jn, jf, clienteA, CancellationToken.None);
        var somenteAlfa = Assert.Single(dreFiltrado.Linhas);
        Assert.Equal("Cliente DRE Alfa", somenteAlfa.Cliente);
        Assert.Equal(0.00m, somenteAlfa.DespesasAtribuidas);
        Assert.Equal(91.00m, somenteAlfa.ResultadoGerencial);
        Assert.True(dreFiltrado.Conciliacao.ReceitaLiquidaBate);
        Assert.Equal(1, dreFiltrado.Conciliacao.VendasNoPeriodo);

        // cross-tenant continua sendo rejeição explícita no DRE
        await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            repo.RelatorioDreAsync(tenantA, jn, jf, clienteB, CancellationToken.None));

        // -------------------------------------------------------------------
        // Período secundário sem vendas: só o total de despesas entra (competencia da despesa).
        // -------------------------------------------------------------------
        var dreSemVendas = await repo.RelatorioDreAsync(tenantA, hoje.AddDays(31), hoje.AddDays(60), null, CancellationToken.None);
        var apenasDespesa = Assert.Single(dreSemVendas.Linhas);
        Assert.Equal("Não atribuída", apenasDespesa.Cliente);
        Assert.Equal(0, apenasDespesa.QtdVendas);
        Assert.Equal(0.00m, apenasDespesa.ReceitaLiquida);
        Assert.Equal(55.00m, apenasDespesa.DespesasAtribuidas);
        Assert.Equal(-55.00m, apenasDespesa.ResultadoGerencial);
        Assert.Equal(1, dreSemVendas.Conciliacao.DespesasNoPeriodo);
        Assert.Equal(0, dreSemVendas.Conciliacao.VendasNoPeriodo);

        // isolamento por tenant: B não vê nada do A
        Assert.Empty((await repo.RelatorioDreAsync(tenantB, jn, jf, null, CancellationToken.None)).Linhas);
    }
}
