using System.Globalization;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class Administrativo360FinanceiroEVendasTests
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
            Assert.Fail($"PostgreSQL obrigatório para a suíte de integração não está acessível: {ex.Message}");
        }
    }

    // =========================================================================
    // BLOCO A / GATE A — REGRAS CANÔNICAS DE EXPEDIÇÃO, RESERVA E QUALIDADE
    // =========================================================================

    [Theory]
    [InlineData("RASCUNHO", false)]
    [InlineData("EM_SEPARACAO", false)]
    [InlineData("PRONTO_PARA_EXPEDICAO", true)]
    [InlineData("EXPEDIDO", false)]
    [InlineData("CANCELADO", false)]
    public void GateA_Expedicao_ExigeEstritamenteProntoParaExpedicao(string situacao, bool esperado)
    {
        var pode = ValeConsignacaoRegras.PodeExpedir(situacao);
        Assert.Equal(esperado, pode);
    }

    [Fact]
    public void GateA_Custodia_RejeitaValoresNegativosOuInconsistentes()
    {
        // Rejeita quantidades negativas
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConsignacaoCirurgicaRegras.CalcularPendenteCustodia(-1m, 0m, 0m, 0m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConsignacaoCirurgicaRegras.CalcularPendenteCustodia(10m, -2m, 0m, 0m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConsignacaoCirurgicaRegras.CalcularPendenteCustodia(10m, 0m, -1m, 0m));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConsignacaoCirurgicaRegras.CalcularPendenteCustodia(10m, 0m, 0m, -1m));

        // Rejeita soma atendida maior que expedida (não transforma em zero silenciosamente)
        Assert.Throws<InvalidOperationException>(() =>
            ConsignacaoCirurgicaRegras.CalcularPendenteCustodia(10m, 8m, 4m, 0m));

        // Equação exata: expedido = consumido + devolvido + perda + pendente
        var pendente = ConsignacaoCirurgicaRegras.CalcularPendenteCustodia(10m, 4m, 2m, 1m);
        Assert.Equal(3m, pendente);
    }

    [Fact]
    public void BlocoB_ValorizacaoRegras_ExigeValeReconciliado()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ValorizacaoRegras.ValidarElegibilidadeVale("EXPEDIDO", "PENDENTE_VALORIZACAO"));

        Assert.Throws<InvalidOperationException>(() =>
            ValorizacaoRegras.ValidarElegibilidadeVale("RECONCILIADO", "VALORIZADO"));

        // Válido se reconciliado e pendente
        ValorizacaoRegras.ValidarElegibilidadeVale("RECONCILIADO", "PENDENTE_VALORIZACAO");
    }

    [Fact]
    public void BlocoC_VendaRegras_DistribuicaoParcelasPreservaCentavos()
    {
        // R$ 100,00 em 3 parcelas: 33,33 + 33,33 + 33,34 = 100,00 exatos
        var parcelas = VendaRegras.GerarParcelas(100.00m, 3, DateOnly.FromDateTime(DateTime.UtcNow));
        Assert.Equal(3, parcelas.Count);
        Assert.Equal(100.00m, parcelas.Sum(p => p.Valor));
    }

    [Fact]
    public void BlocoD_ComissaoRegras_CalculoProporcionalEvitaDiferencaCentavos()
    {
        // Venda R$ 100,00 com comissão 5% (Total previsto = R$ 5,00)
        // 1ª baixa de R$ 33,34: round(33.34 * 0.05) = 1,67 - 0 = 1,67
        var c1 = ComissaoRegras.CalcularComissaoApropriada(0m, 33.34m, 5.0m, 0m);
        Assert.Equal(1.67m, c1);

        // 2ª baixa de R$ 33,33: round((33.34 + 33.33) * 0.05) = round(3.3335) = 3.33 - 1.67 = 1,66
        var c2 = ComissaoRegras.CalcularComissaoApropriada(33.34m, 33.33m, 5.0m, 1.67m);
        Assert.Equal(1.66m, c2);

        // 3ª baixa de R$ 33,33: round(100.00 * 0.05) = 5.00 - 3.33 = 1,67
        var c3 = ComissaoRegras.CalcularComissaoApropriada(66.67m, 33.33m, 5.0m, 3.33m);
        Assert.Equal(1.67m, c3);

        Assert.Equal(5.00m, c1 + c2 + c3);
    }

    // =========================================================================
    // SEÇÃO 20 — CENÁRIO NUMÉRICO OBRIGATÓRIO EM POSTGRESQL REAL
    // =========================================================================
    [Fact]
    public async Task Secao20_CenarioNumericoCompleto_ExecutaComExatidaoNoPostgres()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var pagadorId = Guid.NewGuid();
        var vendedorId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var loteId = Guid.NewGuid();
        var localCdId = Guid.NewGuid();
        var localHospId = Guid.NewGuid();
        var orcamentoId = Guid.NewGuid();
        var cirurgiaId = Guid.NewGuid();
        var valeId = Guid.NewGuid();
        var valeItemId = Guid.NewGuid();
        var contaBancariaId = Guid.NewGuid();

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // 1. Setup da estrutura necessária
        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants(id, tenant_id, codigo, nome, status)
            VALUES(@tenantId, @tenantId, 'tenant-secao-20', 'Tenant Teste Seção 20', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES
                (@hospitalId, @tenantId, 'Hospital Geral Seção 20', '11111111000111', false, true),
                (@pagadorId, @tenantId, 'Convênio Pagador Seção 20', '22222222000122', false, true);

            INSERT INTO plantaopro.adm360_locais(id, tenant_id, codigo, nome, tipo)
            VALUES
                (@localCdId, @tenantId, 'CD-S20', 'Centro Distribuicao S20', 'INTERNO'),
                (@localHospId, @tenantId, 'HOSP-S20', 'Hospital Custodia S20', 'EXTERNO');

            INSERT INTO plantaopro.adm360_produtos(id, tenant_id, sku, nome, unidade, preco_custo, controla_lote)
            VALUES(@produtoId, @tenantId, 'SKU-S20', 'Material S20', 'UN', 100.00, true);

            INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade, custo_unitario)
            VALUES(@loteId, @tenantId, @produtoId, 'LOTE-S20', date '2028-12-31', 100.00);

            INSERT INTO plantaopro.adm360_orcamentos(
                id, tenant_id, numero, revisao, hospital_id, procedimento, responsavel_financeiro_id,
                vendedor_id, data_prevista, validade, situacao, total_produtos, desconto_geral, total_geral, created_by
            ) VALUES(
                @orcamentoId, @tenantId, 'ORC-S20-001', 1, @hospitalId, 'Procedimento Seção 20', @pagadorId,
                @usuarioId, date '2026-10-10', date '2026-10-20', 'APROVADO', 900.00, 0, 900.00, @usuarioId
            );

            INSERT INTO plantaopro.adm360_orcamento_itens(
                id, tenant_id, orcamento_id, produto_id, quantidade, preco_unitario, desconto, total
            ) VALUES(
                gen_random_uuid(), @tenantId, @orcamentoId, @produtoId, 6, 150.00, 0, 900.00
            );

            INSERT INTO plantaopro.adm360_cirurgias(
                id, tenant_id, numero, hospital_id, procedimento, data_prevista, hora_prevista,
                local_destino_id, orcamento_id, orcamento_revisao, responsavel_id, situacao, created_by
            ) VALUES(
                @cirurgiaId, @tenantId, 'CIR-S20-001', @hospitalId, 'Procedimento Seção 20', date '2026-10-10', time '08:00',
                @localHospId, @orcamentoId, 1, @usuarioId, 'REALIZADA', @usuarioId
            );

            -- Vale: 6 expedidas, 4 consumidas, 2 devolvidas -> RECONCILIADO
            INSERT INTO plantaopro.adm360_vales(
                id, tenant_id, numero, cirurgia_id, orcamento_id, orcamento_revisao, hospital_id,
                local_origem_id, local_destino_id, data_saida_prevista, data_saida_efetiva, data_reconciliacao,
                situacao, situacao_financeira, created_by
            ) VALUES(
                @valeId, @tenantId, 'VAL-S20-001', @cirurgiaId, @orcamentoId, 1, @hospitalId,
                @localCdId, @localHospId, date '2026-10-09', now() - interval '2 days', now() - interval '1 day',
                'RECONCILIADO', 'PENDENTE_VALORIZACAO', @usuarioId
            );

            INSERT INTO plantaopro.adm360_vale_itens(
                id, tenant_id, vale_id, produto_id, lote_id, quantidade_solicitada, quantidade_separada,
                quantidade_expedida, quantidade_consumida, quantidade_devolvida, quantidade_perda, preco_unitario
            ) VALUES(
                @valeItemId, @tenantId, @valeId, @produtoId, @loteId, 6, 6, 6, 4, 2, 0, 150.00
            );

            -- Conta Bancária inicial com saldo R$ 0,00
            INSERT INTO plantaopro.adm360_contas_financeiras(
                id, tenant_id, nome, tipo, banco, agencia, conta, saldo_inicial, ativo, created_at
            ) VALUES(
                @contaBancariaId, @tenantId, 'Conta Movimento Seção 20', 'BANCO', '001', '123', '456', 0.00, true, now()
            );
        ", new
        {
            tenantId, usuarioId, hospitalId, pagadorId, vendedorId,
            produtoId, loteId, localCdId, localHospId, orcamentoId, cirurgiaId, valeId, valeItemId, contaBancariaId
        });

        var valorizacaoRepo = new ValorizacaoRepository(cs);
        var vendaRepo = new VendaRepository(cs);
        var contasReceberRepo = new ContasReceberRepository(cs);
        var caixaRepo = new CaixaRepository(cs);
        var relatoriosRepo = new Adm360FinanceiroRelatoriosRepository(cs);

        // ---------------------------------------------------------------------
        // PASSO A. Valorizar Vale:
        // Venda R$ 600; Custo consumido R$ 400; Nenhuma nova saída de estoque.
        // ---------------------------------------------------------------------
        var previa = await valorizacaoRepo.ObterPreviaAsync(tenantId, valeId, CancellationToken.None);
        Assert.NotNull(previa);
        Assert.Empty(previa.Pendencias);
        Assert.Equal(600.00m, previa.TotalBruto);
        Assert.Equal(600.00m, previa.TotalLiquido);
        Assert.Equal(400.00m, previa.TotalCusto);
        Assert.Single(previa.Itens);
        Assert.Equal(4.00m, previa.Itens[0].QuantidadeConsumida);
        Assert.Equal(150.00m, previa.Itens[0].PrecoUnitario);
        Assert.Equal(100.00m, previa.Itens[0].CustoUnitario);

        var valCmd = new ValorizarValeCommand(
            valeId, pagadorId, usuarioId,
            ComissaoPercentual: 5.0m,
            DescontoGeral: 0m,
            IdempotencyKey: $"VAL-S20-{Guid.NewGuid():N}",
            Observacoes: "Valorização teste Seção 20"
        );

        var valorizacaoId = await valorizacaoRepo.ValorizarAsync(tenantId, usuarioId, valCmd, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, valorizacaoId);

        // Verifica que o vale agora está VALORIZADO
        var valSitFin = await cn.ExecuteScalarAsync<string>(
            "SELECT situacao_financeira FROM plantaopro.adm360_vales WHERE id = @valeId", new { valeId });
        Assert.Equal("VALORIZADO", valSitFin);

        // ---------------------------------------------------------------------
        // PASSO B. Confirmar Venda:
        // Contas a receber R$ 600; Caixa permanece inalterado.
        // ---------------------------------------------------------------------
        var vendaCmd = new ConfirmarVendaCommand(
            valorizacaoId,
            CondicaoPagamento: "A_VISTA",
            QuantidadeParcelas: 1,
            IdempotencyKey: $"VEN-S20-{Guid.NewGuid():N}",
            Observacoes: "Venda teste Seção 20"
        );

        var vendaId = await vendaRepo.ConfirmarVendaAsync(tenantId, usuarioId, vendaCmd, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, vendaId);

        var venda = await vendaRepo.ObterPorIdAsync(tenantId, vendaId, CancellationToken.None);
        Assert.NotNull(venda);
        Assert.Equal(600.00m, venda.TotalLiquido);
        Assert.Equal(400.00m, venda.TotalCusto);
        Assert.Equal(30.00m, venda.ComissaoPrevista);
        Assert.Single(venda.Titulos);

        var titulo = venda.Titulos[0];
        Assert.Equal(600.00m, titulo.ValorPrincipal);
        Assert.Equal(0.00m, titulo.ValorRecebido);
        Assert.Equal(600.00m, titulo.SaldoAberto);

        // Caixa permanece zerado
        var contaAntes = await caixaRepo.ObterContaPorIdAsync(tenantId, contaBancariaId, CancellationToken.None);
        Assert.NotNull(contaAntes);
        Assert.Equal(0.00m, contaAntes.SaldoAtual);

        // ---------------------------------------------------------------------
        // PASSO C. Receber R$ 300:
        // Saldo a receber R$ 300; Caixa aumenta R$ 300; Comissão apropriada R$ 15.
        // ---------------------------------------------------------------------
        var baixa1Key = $"REC-1-{Guid.NewGuid():N}";
        var rec1Cmd = new ReceberTituloCommand(
            titulo.Id, contaBancariaId, DateOnly.FromDateTime(DateTime.UtcNow),
            Valor: 300.00m,
            MeioPagamento: "PIX",
            Referencia: "COMP-001",
            IdempotencyKey: baixa1Key
        );

        var baixa1Id = await contasReceberRepo.ReceberAsync(tenantId, usuarioId, rec1Cmd, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, baixa1Id);

        var titApos1 = await contasReceberRepo.ObterPorIdAsync(tenantId, titulo.Id, CancellationToken.None);
        Assert.NotNull(titApos1);
        Assert.Equal(300.00m, titApos1.ValorRecebido);
        Assert.Equal(300.00m, titApos1.SaldoAberto);
        Assert.Equal("PARCIAL", titApos1.Situacao);

        var contaApos1 = await caixaRepo.ObterContaPorIdAsync(tenantId, contaBancariaId, CancellationToken.None);
        Assert.NotNull(contaApos1);
        Assert.Equal(300.00m, contaApos1.SaldoAtual);

        var comissoes1 = await relatoriosRepo.RelatorioComissoesAsync(tenantId, usuarioId, null, null, CancellationToken.None);
        Assert.Single(comissoes1);
        Assert.Equal(15.00m, comissoes1[0].ComissaoApropriada);

        // ---------------------------------------------------------------------
        // PASSO D. Repetir a mesma baixa (idempotência):
        // Todos os valores permanecem exatamente iguais.
        // ---------------------------------------------------------------------
        var baixa1RepetidaId = await contasReceberRepo.ReceberAsync(tenantId, usuarioId, rec1Cmd, CancellationToken.None);
        Assert.Equal(baixa1Id, baixa1RepetidaId);

        var titAposRetry = await contasReceberRepo.ObterPorIdAsync(tenantId, titulo.Id, CancellationToken.None);
        Assert.NotNull(titAposRetry);
        Assert.Equal(300.00m, titAposRetry.ValorRecebido);
        Assert.Equal(300.00m, titAposRetry.SaldoAberto);

        var contaAposRetry = await caixaRepo.ObterContaPorIdAsync(tenantId, contaBancariaId, CancellationToken.None);
        Assert.NotNull(contaAposRetry);
        Assert.Equal(300.00m, contaAposRetry.SaldoAtual);

        // ---------------------------------------------------------------------
        // PASSO E. Receber os R$ 300 restantes:
        // Título quitado; Caixa acumulado +R$ 600; Comissão apropriada total R$ 30.
        // ---------------------------------------------------------------------
        var baixa2Key = $"REC-2-{Guid.NewGuid():N}";
        var rec2Cmd = new ReceberTituloCommand(
            titulo.Id, contaBancariaId, DateOnly.FromDateTime(DateTime.UtcNow),
            Valor: 300.00m,
            MeioPagamento: "TED",
            Referencia: "COMP-002",
            IdempotencyKey: baixa2Key
        );

        var baixa2Id = await contasReceberRepo.ReceberAsync(tenantId, usuarioId, rec2Cmd, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, baixa2Id);

        var titQuitado = await contasReceberRepo.ObterPorIdAsync(tenantId, titulo.Id, CancellationToken.None);
        Assert.NotNull(titQuitado);
        Assert.Equal(600.00m, titQuitado.ValorRecebido);
        Assert.Equal(0.00m, titQuitado.SaldoAberto);
        Assert.Equal("QUITADO", titQuitado.Situacao);

        var contaQuitada = await caixaRepo.ObterContaPorIdAsync(tenantId, contaBancariaId, CancellationToken.None);
        Assert.NotNull(contaQuitada);
        Assert.Equal(600.00m, contaQuitada.SaldoAtual);

        var comissoes2 = await relatoriosRepo.RelatorioComissoesAsync(tenantId, usuarioId, null, null, CancellationToken.None);
        Assert.Equal(2, comissoes2.Count);
        Assert.Equal(30.00m, comissoes2.Sum(c => c.ComissaoApropriada));

        // ---------------------------------------------------------------------
        // PASSO F. Estornar a segunda baixa:
        // Saldo a receber R$ 300; Caixa acumulado +R$ 300; Comissão apropriada líquida R$ 15.
        // ---------------------------------------------------------------------
        var estornoCmd = new EstornarBaixaCommand(
            baixa2Id,
            Motivo: "Estorno de teste da 2ª baixa",
            IdempotencyKey: $"EST-2-{Guid.NewGuid():N}"
        );

        var estornoId = await contasReceberRepo.EstornarAsync(tenantId, usuarioId, estornoCmd, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, estornoId);

        var titAposEstorno = await contasReceberRepo.ObterPorIdAsync(tenantId, titulo.Id, CancellationToken.None);
        Assert.NotNull(titAposEstorno);
        Assert.Equal(300.00m, titAposEstorno.ValorRecebido);
        Assert.Equal(300.00m, titAposEstorno.SaldoAberto);
        Assert.Equal("PARCIAL", titAposEstorno.Situacao);

        var contaAposEstorno = await caixaRepo.ObterContaPorIdAsync(tenantId, contaBancariaId, CancellationToken.None);
        Assert.NotNull(contaAposEstorno);
        Assert.Equal(300.00m, contaAposEstorno.SaldoAtual);

        var comissoes3 = await relatoriosRepo.RelatorioComissoesAsync(tenantId, usuarioId, null, null, CancellationToken.None);
        var comissaoLiquida = comissoes3.Where(c => c.Situacao == "APROPRIADA").Sum(c => c.ComissaoApropriada);
        Assert.Equal(15.00m, comissaoLiquida);

        // Verificação da Margem Prevista no relatório:
        // Receita líquida = 600, Custo materiais = 400, Comissão prevista = 30 -> Margem = 170.
        var relMargem = await relatoriosRepo.RelatorioMargemAsync(tenantId, null, null, CancellationToken.None);
        Assert.Single(relMargem);
        Assert.Equal(600.00m, relMargem[0].ReceitaLiquida);
        Assert.Equal(400.00m, relMargem[0].CustoConsumido);
        Assert.Equal(30.00m, relMargem[0].ComissaoPrevista);
        Assert.Equal(170.00m, relMargem[0].MargemContribuicao);
    }

    // =========================================================================
    // SEÇÃO 21 — CONCORRÊNCIA, IDEMPOTÊNCIA E SEGURANÇA MULTI-TENANT
    // =========================================================================

    [Fact]
    public async Task Secao21_Recebimento_BloqueiaValorAcimaDoSaldo()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var pagadorId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var tituloId = Guid.NewGuid();

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants(id, tenant_id, codigo, nome, status)
            VALUES(@tenantId, @tenantId, 'tenant-conc', 'Tenant Teste Concorrência', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES
                (@hospitalId, @tenantId, 'Hosp Conc', '11111111000101', false, true),
                (@pagadorId, @tenantId, 'Pag Conc', '22222222000102', false, true);

            INSERT INTO plantaopro.adm360_contas_financeiras(id, tenant_id, nome, tipo, saldo_inicial, ativo, created_at)
            VALUES(@contaId, @tenantId, 'Conta Teste Conc', 'BANCO', 0, true, now());

            INSERT INTO plantaopro.adm360_vendas(
                id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia,
                total_bruto, desconto, total_liquido, total_custo, comissao_percentual, comissao_prevista,
                condicao_pagamento, quantidade_parcelas, situacao, idempotency_key, created_by
            ) VALUES(
                @vendaId, @tenantId, 'VEN-CONC-01', 'DIRETA', @vendaId, @hospitalId, @pagadorId, date '2026-10-01',
                100, 0, 100, 50, 5, 5, 'A_VISTA', 1, 'CONFIRMADA', 'key-venda-conc', @usuarioId
            );

            INSERT INTO plantaopro.adm360_titulos_receber(
                id, tenant_id, venda_id, numero, pagador_id, parcela, total_parcelas,
                data_emissao, data_vencimento, valor_principal, valor_recebido, saldo_aberto, situacao, created_by
            ) VALUES(
                @tituloId, @tenantId, @vendaId, 'TIT-CONC-01', @pagadorId, 1, 1,
                date '2026-10-01', date '2026-10-31', 100.00, 0.00, 100.00, 'ABERTO', @usuarioId
            );
        ", new { tenantId, usuarioId, hospitalId, pagadorId, contaId, vendaId, tituloId });

        var repo = new ContasReceberRepository(cs);

        // Tentativa de receber R$ 101,00 em um título com saldo de R$ 100,00 deve falhar
        var cmdExcesso = new ReceberTituloCommand(
            tituloId, contaId, DateOnly.FromDateTime(DateTime.UtcNow),
            Valor: 101.00m,
            MeioPagamento: "PIX",
            Referencia: null,
            IdempotencyKey: "key-excesso"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ReceberAsync(tenantId, usuarioId, cmdExcesso, CancellationToken.None));
    }

    [Fact]
    public async Task Secao21_Idempotencia_MesmaChaveComPayloadDiferenteRetornaConflito()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var pagadorId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var vendaId = Guid.NewGuid();
        var tituloId = Guid.NewGuid();

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants(id, tenant_id, codigo, nome, status)
            VALUES(@tenantId, @tenantId, 'tenant-idemp', 'Tenant Teste Idemp', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES
                (@hospitalId, @tenantId, 'Hosp Idemp', '11111111000103', false, true),
                (@pagadorId, @tenantId, 'Pag Idemp', '22222222000104', false, true);

            INSERT INTO plantaopro.adm360_contas_financeiras(id, tenant_id, nome, tipo, saldo_inicial, ativo, created_at)
            VALUES(@contaId, @tenantId, 'Conta Teste Idemp', 'BANCO', 0, true, now());

            INSERT INTO plantaopro.adm360_vendas(
                id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia,
                total_bruto, desconto, total_liquido, total_custo, comissao_percentual, comissao_prevista,
                condicao_pagamento, quantidade_parcelas, situacao, idempotency_key, created_by
            ) VALUES(
                @vendaId, @tenantId, 'VEN-IDEMP-01', 'DIRETA', @vendaId, @hospitalId, @pagadorId, date '2026-10-01',
                200, 0, 200, 100, 5, 10, 'A_VISTA', 1, 'CONFIRMADA', 'key-venda-idemp', @usuarioId
            );

            INSERT INTO plantaopro.adm360_titulos_receber(
                id, tenant_id, venda_id, numero, pagador_id, parcela, total_parcelas,
                data_emissao, data_vencimento, valor_principal, valor_recebido, saldo_aberto, situacao, created_by
            ) VALUES(
                @tituloId, @tenantId, @vendaId, 'TIT-IDEMP-01', @pagadorId, 1, 1,
                date '2026-10-01', date '2026-10-31', 200.00, 0.00, 200.00, 'ABERTO', @usuarioId
            );
        ", new { tenantId, usuarioId, hospitalId, pagadorId, contaId, vendaId, tituloId });

        var repo = new ContasReceberRepository(cs);
        var mesmaChave = $"KEY-CONFLITO-{Guid.NewGuid():N}";

        // 1ª execução com valor R$ 50,00
        var cmd1 = new ReceberTituloCommand(
            tituloId, contaId, DateOnly.FromDateTime(DateTime.UtcNow),
            Valor: 50.00m,
            MeioPagamento: "PIX",
            Referencia: "REF-1",
            IdempotencyKey: mesmaChave
        );
        var baixaId1 = await repo.ReceberAsync(tenantId, usuarioId, cmd1, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, baixaId1);

        // 2ª execução com a MESMA chave porém com valor R$ 60,00 -> DEVE RETORNAR CONFLITO
        var cmd2 = new ReceberTituloCommand(
            tituloId, contaId, DateOnly.FromDateTime(DateTime.UtcNow),
            Valor: 60.00m,
            MeioPagamento: "TED",
            Referencia: "REF-2",
            IdempotencyKey: mesmaChave
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ReceberAsync(tenantId, usuarioId, cmd2, CancellationToken.None));
        Assert.Contains("Conflito de idempotência", ex.Message);
    }

    [Fact]
    public async Task Secao21_MultiTenant_IsolamentoRigidoImpedeAcessoCruzado()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        var h1 = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var conta1 = Guid.NewGuid();
        var venda1 = Guid.NewGuid();
        var titulo1 = Guid.NewGuid();

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.tenants(id, tenant_id, codigo, nome, status)
            VALUES
                (@t1, @t1, 't1-cross', 'Tenant 1 Cross', 'ATIVO'),
                (@t2, @t2, 't2-cross', 'Tenant 2 Cross', 'ATIVO')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES
                (@h1, @t1, 'Hosp T1', '11111111000111', false, true),
                (@p1, @t1, 'Pag T1', '22222222000122', false, true);

            INSERT INTO plantaopro.adm360_contas_financeiras(id, tenant_id, nome, tipo, saldo_inicial, ativo, created_at)
            VALUES(@conta1, @t1, 'Conta T1', 'BANCO', 0, true, now());

            INSERT INTO plantaopro.adm360_vendas(
                id, tenant_id, numero, origem_tipo, origem_id, cliente_id, pagador_id, competencia,
                total_bruto, desconto, total_liquido, total_custo, comissao_percentual, comissao_prevista,
                condicao_pagamento, quantidade_parcelas, situacao, idempotency_key, created_by
            ) VALUES(
                (@venda1), @t1, 'VEN-T1-01', 'DIRETA', @venda1, @h1, @p1, date '2026-10-01',
                500, 0, 500, 200, 5, 25, 'A_VISTA', 1, 'CONFIRMADA', 'key-t1', @u1
            );

            INSERT INTO plantaopro.adm360_titulos_receber(
                id, tenant_id, venda_id, numero, pagador_id, parcela, total_parcelas,
                data_emissao, data_vencimento, valor_principal, valor_recebido, saldo_aberto, situacao, created_by
            ) VALUES(
                @titulo1, @t1, @venda1, 'TIT-T1-01', @p1, 1, 1,
                date '2026-10-01', date '2026-10-31', 500.00, 0.00, 500.00, 'ABERTO', @u1
            );
        ", new { t1, t2, u1, u2, h1, p1, conta1, venda1, titulo1 });

        var repoT2 = new ContasReceberRepository(cs);

        // Tenant 2 tentando ler título do Tenant 1 deve retornar null (não encontrado)
        var titCross = await repoT2.ObterPorIdAsync(t2, titulo1, CancellationToken.None);
        Assert.Null(titCross);

        // Tenant 2 tentando baixar título do Tenant 1 deve falhar
        var cmdCross = new ReceberTituloCommand(
            titulo1, conta1, DateOnly.FromDateTime(DateTime.UtcNow),
            Valor: 100.00m,
            MeioPagamento: "PIX",
            Referencia: null,
            IdempotencyKey: "key-cross"
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repoT2.ReceberAsync(t2, u2, cmdCross, CancellationToken.None));
    }
}
