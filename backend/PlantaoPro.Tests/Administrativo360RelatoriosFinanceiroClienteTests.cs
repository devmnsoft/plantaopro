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
}
