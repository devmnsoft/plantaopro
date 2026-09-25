using System.Globalization;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class Administrativo360ContasPagarEFechamentoTests
{
    private static string ObterConnectionString()
    {
        return Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name=PlantaoPro.tests";
    }

    private static async Task GarantirConexaoBancoAsync(string cs)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para a suíte de testes do Administrativo 360 não está acessível: {ex.Message}");
        }
    }

    // =========================================================================
    // BLOCO A (3) — PARCELAS E ARREDONDAMENTO DETERMINÍSTICO
    // =========================================================================

    [Fact]
    public void VendaRegras_R100_Em3Parcelas_DistribuiCentavosExatos()
    {
        var parcelas = VendaRegras.GerarParcelas(100.00m, 3, new DateOnly(2026, 1, 15));
        Assert.Equal(3, parcelas.Count);
        Assert.Equal(100.00m, parcelas.Sum(p => p.Valor));
        Assert.All(parcelas, p => Assert.True(p.Valor > 0));
        Assert.Equal(33.34m, parcelas[0].Valor);
        Assert.Equal(33.33m, parcelas[1].Valor);
        Assert.Equal(33.33m, parcelas[2].Valor);
    }

    [Fact]
    public void VendaRegras_R010_Em6Parcelas_DistribuiCentavosExatos()
    {
        // 10 centavos para 6 parcelas distribui em 4 de 2 centavos e 2 de 1 centavo
        var parcelas = VendaRegras.GerarParcelas(0.10m, 6, new DateOnly(2026, 1, 15));
        Assert.Equal(6, parcelas.Count);
        Assert.Equal(0.10m, parcelas.Sum(p => p.Valor));
        Assert.All(parcelas, p => Assert.True(p.Valor > 0m));
        Assert.Equal(0.02m, parcelas[0].Valor);
        Assert.Equal(0.01m, parcelas[5].Valor);
    }

    [Fact]
    public void VendaRegras_R001_Em2Parcelas_RejeitaPorNaoGerarParcelasMinimasDeCentavo()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            VendaRegras.GerarParcelas(0.01m, 2, new DateOnly(2026, 1, 15)));
        Assert.Contains("não é possível dividir", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public void VendaRegras_QuantidadeParcelasInvalida_LancaExcecao(int parcelasInvalidas)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            VendaRegras.GerarParcelas(100.00m, parcelasInvalidas, new DateOnly(2026, 1, 15)));
    }

    [Fact]
    public void VendaRegras_VencimentosFimDoMes_NaoLancaErroEProjetaMesesSubsequentes()
    {
        var inicio = new DateOnly(2026, 1, 31);
        var parcelas = VendaRegras.GerarParcelas(300.00m, 3, inicio);

        Assert.Equal(3, parcelas.Count);
        Assert.Equal(300.00m, parcelas.Sum(p => p.Valor));
        Assert.Equal(new DateOnly(2026, 2, 28), parcelas[0].Vencimento);
        Assert.Equal(new DateOnly(2026, 3, 31), parcelas[1].Vencimento);
        Assert.Equal(new DateOnly(2026, 4, 30), parcelas[2].Vencimento);
    }

    // =========================================================================
    // BLOCO A (2) — REGRAS DE VALORIZAÇÃO E DESCONTO
    // =========================================================================

    [Fact]
    public void ValorizacaoRegras_ValidarDesconto_RejeitaNegativoOuMaiorQueBase()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ValorizacaoRegras.ValidarDesconto(100m, -5m));

        Assert.Throws<InvalidOperationException>(() =>
            ValorizacaoRegras.ValidarDesconto(100m, 105m));

        // Desconto válido não lança exceção
        ValorizacaoRegras.ValidarDesconto(100m, 20m);
    }

    // =========================================================================
    // BLOCO B & C — REGRAS DE CONTAS A PAGAR E CAIXA
    // =========================================================================

    [Fact]
    public void ContasPagarRegras_ValidarAprovacao_ExigeEstadoPendente()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ContasPagarRegras.ValidarAprovacao("APROVADO"));

        Assert.Throws<InvalidOperationException>(() =>
            ContasPagarRegras.ValidarAprovacao("PAGO"));

        // Válido se pendente
        ContasPagarRegras.ValidarAprovacao("PENDENTE_APROVACAO");
    }

    [Fact]
    public void ContasPagarRegras_ValidarPagamento_RejeitaValoresInvalidosOuSaldoInsuficiente()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContasPagarRegras.ValidarPagamento("APROVADO", 100m, 0m));

        Assert.Throws<InvalidOperationException>(() =>
            ContasPagarRegras.ValidarPagamento("PENDENTE_APROVACAO", 100m, 50m));

        Assert.Throws<InvalidOperationException>(() =>
            ContasPagarRegras.ValidarPagamento("PAGO", 100m, 50m));

        Assert.Throws<InvalidOperationException>(() =>
            ContasPagarRegras.ValidarPagamento("APROVADO", 100m, 150m));

        // Válido
        ContasPagarRegras.ValidarPagamento("APROVADO", 100m, 50m);
    }

    [Fact]
    public void CaixaRegras_ValidarSaldoSuficiente_BloqueiaSeSaldoContaMenorQueSaida()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CaixaRegras.ValidarSaldoSuficiente(100m, 150m));

        // Não lança se saldo for suficiente
        CaixaRegras.ValidarSaldoSuficiente(150m, 100m);
    }

    [Fact]
    public void CaixaRegras_ValidarDataBloqueioFechamento_BloqueiaLancamentoRetroativo()
    {
        var dataFechamento = new DateOnly(2026, 9, 20);

        Assert.Throws<InvalidOperationException>(() =>
            CaixaRegras.ValidarDataBloqueioFechamento(new DateOnly(2026, 9, 15), dataFechamento));

        Assert.Throws<InvalidOperationException>(() =>
            CaixaRegras.ValidarDataBloqueioFechamento(new DateOnly(2026, 9, 20), dataFechamento));

        // Lançamento com data posterior é permitido
        CaixaRegras.ValidarDataBloqueioFechamento(new DateOnly(2026, 9, 21), dataFechamento);
    }

    // =========================================================================
    // BLOCO E (18) — CENÁRIO FINANCEIRO DE ACEITE (PASSOS A ATÉ G) EM POSTGRESQL REAL
    // =========================================================================

    [Fact]
    public async Task CenarioFinanceiroDeAceite_PassosA_a_G_ExecutaComExatidaoMatematica()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var caixaRepo = new CaixaRepository(cs);
        var contasPagarRepo = new ContasPagarRepository(cs);
        var contasReceberRepo = new ContasReceberRepository(cs);

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        // 1. Setup do Tenant e Usuário
        var email = $"operador-{tenantId:N}@teste.com";
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants (id, tenant_id, codigo, nome, status)
            VALUES (@tenantId, @tenantId, @codigo, 'Tenant Aceite Financeiro', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.usuarios (id, tenant_id, nome, email, email_normalizado, senha_hash, status)
            VALUES (@usuarioId, @tenantId, 'Operador Teste', @email, @emailNormalizado, 'hash', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;
        ", new
        {
            tenantId,
            usuarioId,
            codigo = $"aceite-{tenantId:N}",
            email,
            emailNormalizado = email.ToUpperInvariant()
        });

        // 2. Setup Fornecedor e Cliente Parceiros
        var fornecedorId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, fornecedor, ativo)
            VALUES (@fornecedorId, @tenantId, 'Fornecedor Alpha', true, true);

            INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, fornecedor, ativo)
            VALUES (@clienteId, @tenantId, 'Cliente Beta Hospital', false, true);
        ", new { tenantId, fornecedorId, clienteId });

        // 3. Setup Conta Financeira com Saldo Inicial R$ 0,00
        var contaId = await caixaRepo.CriarContaAsync(tenantId, usuarioId, new CriarContaFinanceiraCommand(
            "Conta Corrente Principal",
            "CORRENTE",
            "001 - Banco do Brasil",
            "1234",
            "56789-0",
            0.00m,
            new DateOnly(2026, 9, 1)),
            CancellationToken.None);

        // --- PASSO A: Compra recebida de R$ 1.000 -> AP R$ 1.000, Caixa R$ 0 ---
        var tituloPagarId = await contasPagarRepo.CriarDespesaManualAsync(tenantId, usuarioId, new CriarDespesaManualCommand(
            fornecedorId,
            "NF-1000",
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            1000.00m,
            "COMPRAS",
            "Compra recebida para conferência",
            $"idemp-compra-{tenantId:N}"),
            CancellationToken.None);

        // Aprovar a obrigação para ficar elegível a pagamento
        await contasPagarRepo.AprovarAsync(tenantId, usuarioId, new AprovarTituloPagarCommand(
            tituloPagarId,
            $"idemp-aprov-{tenantId:N}"),
            CancellationToken.None);

        var apA = await contasPagarRepo.ObterPorIdAsync(tenantId, tituloPagarId, CancellationToken.None);
        var contasA = await caixaRepo.ListarContasAsync(tenantId, CancellationToken.None);
        var contaA = Assert.Single(contasA, c => c.Id == contaId);

        Assert.NotNull(apA);
        Assert.Equal(1000.00m, apA.SaldoAberto);
        Assert.Equal(0.00m, apA.ValorPago);
        Assert.Equal(0.00m, contaA.SaldoAtual);

        // --- PASSO B: Venda confirmada de R$ 600 -> AR R$ 600, Caixa R$ 0 ---
        var vendaId = Guid.NewGuid();
        var tituloReceberId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_vendas (id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia, total_bruto, desconto, total_liquido, total_custo, situacao)
            VALUES (@vendaId, @tenantId, 'VEN-001', 'VENDA_DIRETA', @vendaId, @clienteId, @clienteId, '2026-09-01', 600.00, 0, 600.00, 200.00, 'CONFIRMADA');

            INSERT INTO plantaopro.adm360_titulos_receber (id, tenant_id, numero, venda_id, pagador_id, parcela, total_parcelas, data_emissao, data_vencimento, valor_principal, saldo_aberto, situacao)
            VALUES (@tituloReceberId, @tenantId, 'REC-001', @vendaId, @clienteId, 1, 1, '2026-09-01', '2026-09-30', 600.00, 600.00, 'ABERTO');
        ", new { tenantId, vendaId, tituloReceberId, clienteId });

        var contasB = await caixaRepo.ListarContasAsync(tenantId, CancellationToken.None);
        var contaB = Assert.Single(contasB, c => c.Id == contaId);
        Assert.Equal(0.00m, contaB.SaldoAtual);

        // --- PASSO C: Receber R$ 300 do cliente -> AR R$ 300, Caixa R$ 300 ---
        await contasReceberRepo.ReceberAsync(tenantId, usuarioId, new ReceberTituloCommand(
            tituloReceberId,
            contaId,
            new DateOnly(2026, 9, 2),
            300.00m,
            "PIX",
            "REC-300",
            $"idemp-rec-{tenantId:N}"),
            CancellationToken.None);

        var arC = await contasReceberRepo.ObterPorIdAsync(tenantId, tituloReceberId, CancellationToken.None);
        var contasC = await caixaRepo.ListarContasAsync(tenantId, CancellationToken.None);
        var contaC = Assert.Single(contasC, c => c.Id == contaId);

        Assert.NotNull(arC);
        Assert.Equal(300.00m, arC.SaldoAberto);
        Assert.Equal(300.00m, arC.ValorRecebido);
        Assert.Equal(300.00m, contaC.SaldoAtual);

        // --- PASSO D: Pagar R$ 200 ao fornecedor -> AP R$ 800, Caixa R$ 100 ---
        var idempPagamento = $"idemp-pag-{tenantId:N}";
        var pagamentoId = await contasPagarRepo.PagarAsync(tenantId, usuarioId, new PagarTituloCommand(
            tituloPagarId,
            contaId,
            new DateOnly(2026, 9, 10),
            200.00m,
            "TED",
            "PAG-200",
            idempPagamento),
            CancellationToken.None);

        var apD = await contasPagarRepo.ObterPorIdAsync(tenantId, tituloPagarId, CancellationToken.None);
        var contasD = await caixaRepo.ListarContasAsync(tenantId, CancellationToken.None);
        var contaD = Assert.Single(contasD, c => c.Id == contaId);

        Assert.NotNull(apD);
        Assert.Equal(800.00m, apD.SaldoAberto);
        Assert.Equal(200.00m, apD.ValorPago);
        Assert.Equal(100.00m, contaD.SaldoAtual);

        // --- PASSO E: Reenviar o pagamento com mesmo idempotency key -> AP R$ 800, Caixa R$ 100 ---
        var pagamentoIdRetry = await contasPagarRepo.PagarAsync(tenantId, usuarioId, new PagarTituloCommand(
            tituloPagarId,
            contaId,
            new DateOnly(2026, 9, 10),
            200.00m,
            "TED",
            "PAG-200",
            idempPagamento),
            CancellationToken.None);

        Assert.Equal(pagamentoId, pagamentoIdRetry);

        var apE = await contasPagarRepo.ObterPorIdAsync(tenantId, tituloPagarId, CancellationToken.None);
        var contasE = await caixaRepo.ListarContasAsync(tenantId, CancellationToken.None);
        var contaE = Assert.Single(contasE, c => c.Id == contaId);

        Assert.NotNull(apE);
        Assert.Equal(800.00m, apE.SaldoAberto);
        Assert.Equal(200.00m, apE.ValorPago);
        Assert.Equal(100.00m, contaE.SaldoAtual);

        // --- PASSO F: Estornar esse pagamento -> AP R$ 1.000, Caixa R$ 300 ---
        var idempEstorno = $"idemp-estorno-{tenantId:N}";
        var estornoId = await contasPagarRepo.EstornarPagamentoAsync(tenantId, usuarioId, new EstornarPagamentoCommand(
            pagamentoId,
            "Erro operacional de lançamento",
            idempEstorno),
            CancellationToken.None);

        var apF = await contasPagarRepo.ObterPorIdAsync(tenantId, tituloPagarId, CancellationToken.None);
        var contasF = await caixaRepo.ListarContasAsync(tenantId, CancellationToken.None);
        var contaF = Assert.Single(contasF, c => c.Id == contaId);

        Assert.NotNull(apF);
        Assert.Equal(1000.00m, apF.SaldoAberto);
        Assert.Equal(0.00m, apF.ValorPago);
        Assert.Equal(300.00m, contaF.SaldoAtual);

        // --- PASSO G: Reenviar o estorno com mesmo idempotency key -> valores permanecem iguais ---
        var estornoIdRetry = await contasPagarRepo.EstornarPagamentoAsync(tenantId, usuarioId, new EstornarPagamentoCommand(
            pagamentoId,
            "Erro operacional de lançamento",
            idempEstorno),
            CancellationToken.None);

        Assert.Equal(estornoId, estornoIdRetry);

        var apG = await contasPagarRepo.ObterPorIdAsync(tenantId, tituloPagarId, CancellationToken.None);
        var contasG = await caixaRepo.ListarContasAsync(tenantId, CancellationToken.None);
        var contaG = Assert.Single(contasG, c => c.Id == contaId);

        Assert.NotNull(apG);
        Assert.Equal(1000.00m, apG.SaldoAberto);
        Assert.Equal(0.00m, apG.ValorPago);
        Assert.Equal(300.00m, contaG.SaldoAtual);
    }

    // =========================================================================
    // BLOCO E (18) — VALIDAÇÃO DO FLUXO DE CAIXA POR PERÍODO E SALDO DE ABERTURA
    // =========================================================================

    [Fact]
    public async Task FluxoCaixa_MovimentoAnteriorAoPeriodo_CompoeAberturaESemDuplicidade()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var caixaRepo = new CaixaRepository(cs);
        var contasPagarRepo = new ContasPagarRepository(cs);
        var contasReceberRepo = new ContasReceberRepository(cs);

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        var emailPeriodo = $"operador-periodo-{tenantId:N}@teste.com";
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants (id, tenant_id, codigo, nome, status)
            VALUES (@tenantId, @tenantId, @codigo, 'Tenant Período Caixa', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.usuarios (id, tenant_id, nome, email, email_normalizado, senha_hash, status)
            VALUES (@usuarioId, @tenantId, 'Operador Período', @email, @emailNormalizado, 'hash', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;
        ", new
        {
            tenantId,
            usuarioId,
            codigo = $"periodo-{tenantId:N}",
            email = emailPeriodo,
            emailNormalizado = emailPeriodo.ToUpperInvariant()
        });

        var fornecedorId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, fornecedor, ativo)
            VALUES (@fornecedorId, @tenantId, 'Fornecedor Gama', true, true);

            INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, fornecedor, ativo)
            VALUES (@clienteId, @tenantId, 'Cliente Delta', false, true);
        ", new { tenantId, fornecedorId, clienteId });

        var contaId = await caixaRepo.CriarContaAsync(tenantId, usuarioId, new CriarContaFinanceiraCommand(
            "Conta Movimento",
            "CORRENTE",
            "Banco Teste",
            "100",
            "200",
            0.00m,
            new DateOnly(2026, 9, 1)),
            CancellationToken.None);

        // Recebimento de R$ 300,00 ocorrido ANTES do período (Data: 2026-09-02)
        var vendaAntId = Guid.NewGuid();
        var tituloReceberId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_vendas (id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia, total_bruto, desconto, total_liquido, total_custo, situacao)
            VALUES (@vendaAntId, @tenantId, 'VEN-ANT-001', 'VENDA_DIRETA', @vendaAntId, @clienteId, @clienteId, '2026-09-01', 300.00, 0, 300.00, 100.00, 'CONFIRMADA');

            INSERT INTO plantaopro.adm360_titulos_receber (id, tenant_id, numero, venda_id, pagador_id, parcela, total_parcelas, data_emissao, data_vencimento, valor_principal, saldo_aberto, situacao)
            VALUES (@tituloReceberId, @tenantId, 'REC-PER-1', @vendaAntId, @clienteId, 1, 1, '2026-09-01', '2026-09-02', 300.00, 300.00, 'ABERTO');
        ", new { tenantId, vendaAntId, tituloReceberId, clienteId });

        await contasReceberRepo.ReceberAsync(tenantId, usuarioId, new ReceberTituloCommand(
            tituloReceberId,
            contaId,
            new DateOnly(2026, 9, 2),
            300.00m,
            "PIX",
            "REC-300-ANT",
            $"idemp-rec-ant-{tenantId:N}"),
            CancellationToken.None);

        // Pagamento de R$ 200,00 ocorrido DENTRO do período (Data: 2026-09-10)
        var tituloPagarId = await contasPagarRepo.CriarDespesaManualAsync(tenantId, usuarioId, new CriarDespesaManualCommand(
            fornecedorId,
            "NF-PER-PAG",
            new DateOnly(2026, 9, 5),
            new DateOnly(2026, 9, 10),
            200.00m,
            "ADMINISTRATIVO",
            "Pagamento dentro do período",
            $"idemp-pag-desp-{tenantId:N}"),
            CancellationToken.None);

        await contasPagarRepo.AprovarAsync(tenantId, usuarioId, new AprovarTituloPagarCommand(
            tituloPagarId,
            $"idemp-aprov-per-{tenantId:N}"),
            CancellationToken.None);

        await contasPagarRepo.PagarAsync(tenantId, usuarioId, new PagarTituloCommand(
            tituloPagarId,
            contaId,
            new DateOnly(2026, 9, 10),
            200.00m,
            "TED",
            "PAG-200-PER",
            $"idemp-pag-dentro-{tenantId:N}"),
            CancellationToken.None);

        // Consulta do Fluxo de Caixa no período 2026-09-05 a 2026-09-15:
        // - Abertura: R$ 300,00 (recebimento do dia 02)
        // - Entradas Realizadas do período: R$ 0,00 (não soma novamente o dia 02)
        // - Saídas Realizadas do período: R$ 200,00 (pagamento do dia 10)
        // - Fechamento: R$ 100,00
        var fluxo = await caixaRepo.FluxoCaixaAsync(
            tenantId,
            new DateOnly(2026, 9, 5),
            new DateOnly(2026, 9, 15),
            CancellationToken.None);

        Assert.Equal(300.00m, fluxo.SaldoAberturaPeriodo);
        Assert.Equal(0.00m, fluxo.TotalEntradasRealizadas);
        Assert.Equal(200.00m, fluxo.TotalSaidasRealizadas);
        Assert.Equal(100.00m, fluxo.SaldoFechamentoPeriodo);
        Assert.Equal(100.00m, fluxo.SaldoAtualContas);
    }

    // =========================================================================
    // BLOCO C (12) — FECHAMENTO DE CAIXA E BLOQUEIO RETROATIVO
    // =========================================================================

    [Fact]
    public async Task FechamentoCaixa_BloqueiaLancamentoRetroativo_EReaberturaFunciona()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var caixaRepo = new CaixaRepository(cs);
        var contasPagarRepo = new ContasPagarRepository(cs);

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        var emailFech = $"operador-fech-{tenantId:N}@teste.com";
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants (id, tenant_id, codigo, nome, status)
            VALUES (@tenantId, @tenantId, @codigo, 'Tenant Fechamento', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.usuarios (id, tenant_id, nome, email, email_normalizado, senha_hash, status)
            VALUES (@usuarioId, @tenantId, 'Operador Fechamento', @email, @emailNormalizado, 'hash', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;
        ", new
        {
            tenantId,
            usuarioId,
            codigo = $"fechamento-{tenantId:N}",
            email = emailFech,
            emailNormalizado = emailFech.ToUpperInvariant()
        });

        var fornecedorId = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, fornecedor, ativo)
            VALUES (@fornecedorId, @tenantId, 'Fornecedor Fechamento', true, true);
        ", new { tenantId, fornecedorId });

        var contaId = await caixaRepo.CriarContaAsync(tenantId, usuarioId, new CriarContaFinanceiraCommand(
            "Conta Fechamento",
            "CORRENTE",
            "Banco Seguro",
            "999",
            "888",
            500.00m,
            new DateOnly(2026, 9, 1)),
            CancellationToken.None);

        // Fechar período da conta de 2026-09-01 até 2026-09-15
        var fechamentoId = await caixaRepo.FecharCaixaAsync(tenantId, usuarioId, new FecharCaixaCommand(
            contaId,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 15),
            500.00m,
            null,
            $"idemp-fech-{tenantId:N}"),
            CancellationToken.None);

        // Criar e aprovar obrigação a pagar
        var tituloPagarId = await contasPagarRepo.CriarDespesaManualAsync(tenantId, usuarioId, new CriarDespesaManualCommand(
            fornecedorId,
            "NF-FECH",
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 10),
            100.00m,
            "ADM",
            "Tentativa de pagamento em período fechado",
            $"idemp-desp-fech-{tenantId:N}"),
            CancellationToken.None);

        await contasPagarRepo.AprovarAsync(tenantId, usuarioId, new AprovarTituloPagarCommand(
            tituloPagarId,
            $"idemp-aprov-fech-{tenantId:N}"),
            CancellationToken.None);

        // Tentativa de pagamento com data 2026-09-10 (DENTRO do período fechado) DEVE falhar
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await contasPagarRepo.PagarAsync(tenantId, usuarioId, new PagarTituloCommand(
                tituloPagarId,
                contaId,
                new DateOnly(2026, 9, 10),
                100.00m,
                "PIX",
                "PAG-BLOQUEADO",
                $"idemp-tentativa-bloq-{tenantId:N}"),
                CancellationToken.None);
        });
        Assert.Contains("fechamento de caixa", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Reabertura auditada do período fechado
        await caixaRepo.ReabrirCaixaAsync(
            tenantId,
            usuarioId,
            new ReabrirCaixaCommand(
                fechamentoId,
                "Ajuste autorizado pela gerência para lançamento retroativo de despesa"),
            CancellationToken.None);

        // Agora a operação deve ter sucesso
        var pagamentoId = await contasPagarRepo.PagarAsync(tenantId, usuarioId, new PagarTituloCommand(
            tituloPagarId,
            contaId,
            new DateOnly(2026, 9, 10),
            100.00m,
            "PIX",
            "PAG-LIBERADO",
            $"idemp-tentativa-pos-reabertura-{tenantId:N}"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, pagamentoId);
    }

    // =========================================================================
    // ISOLAMENTO MULTI-TENANT
    // =========================================================================

    [Fact]
    public async Task IsolamentoTenant_ImpedeAcessoOuModificacaoDeOutroTenant()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var usuarioA = Guid.NewGuid();

        var contasPagarRepo = new ContasPagarRepository(cs);
        var caixaRepo = new CaixaRepository(cs);

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants (id, tenant_id, codigo, nome, status)
            VALUES (@tenantA, @tenantA, @codigoA, 'Tenant A', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.tenants (id, tenant_id, codigo, nome, status)
            VALUES (@tenantB, @tenantB, @codigoB, 'Tenant B', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.usuarios (id, tenant_id, nome, email, email_normalizado, senha_hash, status)
            VALUES (@usuarioA, @tenantA, 'User A', @emailA, @emailNormalizadoA, 'hash', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;
        ", new
        {
            tenantA,
            tenantB,
            usuarioA,
            codigoA = $"ten-a-{tenantA:N}",
            codigoB = $"ten-b-{tenantB:N}",
            emailA = $"user-a-{tenantA:N}@teste.com",
            emailNormalizadoA = $"user-a-{tenantA:N}@teste.com".ToUpperInvariant()
        });

        var fornecedorA = Guid.NewGuid();
        await conn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, fornecedor, ativo)
            VALUES (@fornecedorA, @tenantA, 'Fornecedor A', true, true);
        ", new { tenantA, fornecedorA });

        var tituloIdA = await contasPagarRepo.CriarDespesaManualAsync(tenantA, usuarioA, new CriarDespesaManualCommand(
            fornecedorA,
            "DOC-TEN-A",
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            500.00m,
            "ADM",
            "Despesa do Tenant A",
            $"idemp-a-{tenantA:N}"),
            CancellationToken.None);

        // Tenant B tenta consultar o detalhe do Tenant A
        var detalheB = await contasPagarRepo.ObterPorIdAsync(tenantB, tituloIdA, CancellationToken.None);
        Assert.Null(detalheB);

        // Tenant B tenta aprovar o título do Tenant A
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await contasPagarRepo.AprovarAsync(tenantB, Guid.NewGuid(), new AprovarTituloPagarCommand(
                tituloIdA,
                $"idemp-inv-{tenantB:N}"),
                CancellationToken.None);
        });
    }
}
