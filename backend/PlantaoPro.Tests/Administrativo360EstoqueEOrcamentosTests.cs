using System.Globalization;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class Administrativo360EstoqueEOrcamentosTests
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

    [Fact]
    public void Estoque_Disponivel_SubtraiReservasApenasDoLiberado()
    {
        var disp = Estoque.Disponivel(10m, 3m, vencido: false, bloqueado: false);
        Assert.Equal(7m, disp);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Estoque_Disponivel_VencidoOuBloqueado_EhZero(bool vencido, bool bloqueado)
    {
        var disp = Estoque.Disponivel(10m, 0m, vencido, bloqueado);
        Assert.Equal(0m, disp);
    }

    [Fact]
    public void Estoque_ValidarTransferencia_ExigeLocaisDistintosEPositivos()
    {
        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => Estoque.ValidarTransferencia(id, id, 5m, "Motivo válido"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Estoque.ValidarTransferencia(Guid.NewGuid(), Guid.NewGuid(), 0m, "Motivo válido"));
        Assert.Throws<ArgumentException>(() => Estoque.ValidarTransferencia(Guid.NewGuid(), Guid.NewGuid(), 5m, ""));
    }

    [Fact]
    public void Inspecao_ValidarDecisao_ExigeSaldoEPositividade()
    {
        Inspecao.ValidarDecisao(10m, 5m, 5m, "Avaria identificada na embalagem secundária");
        Assert.Throws<InvalidOperationException>(() => Inspecao.ValidarDecisao(10m, 5m, 5m, ""));
        Assert.Throws<InvalidOperationException>(() => Inspecao.ValidarDecisao(10m, 6m, 5m, "Justificativa"));
    }

    [Fact]
    public void Idempotencia_GeraHashEstavelParaMesmoConteudoEDivergenteParaOutro()
    {
        var tenant = Guid.NewGuid();
        var prod = Guid.NewGuid();
        var lote = Guid.NewGuid();
        var orig = Guid.NewGuid();
        var dest = Guid.NewGuid();

        var hash1 = IdempotenciaHelper.CalcularHash("TRANSFERENCIA", tenant, prod, lote, orig, dest, "3.0000");
        var hash2 = IdempotenciaHelper.CalcularHash("TRANSFERENCIA", tenant, prod, lote, orig, dest, "3.0000");
        var hashDivergente = IdempotenciaHelper.CalcularHash("TRANSFERENCIA", tenant, prod, lote, orig, dest, "4.0000");

        Assert.Equal(hash1, hash2);
        Assert.NotEqual(hash1, hashDivergente);
    }

    [Fact]
    public void Orcamento_CalculoItem_AplicaDescontoSemPermitirValorNegativo()
    {
        var total = OrcamentoCirurgicoRegras.CalcularTotalItem(2m, 100m, 30m);
        Assert.Equal(170m, total);
        Assert.Throws<InvalidOperationException>(() => OrcamentoCirurgicoRegras.CalcularTotalItem(2m, 100m, 250m));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrcamentoCirurgicoRegras.CalcularTotalItem(0m, 100m, 0m));
    }

    [Fact]
    public void Orcamento_ValidarDataCirurgia_ImpedeLoteQueVenceAntesDaCirurgia()
    {
        var dataCirurgia = new DateOnly(2026, 11, 20);
        var loteValido = new DateOnly(2027, 5, 10);
        var loteInvalido = new DateOnly(2026, 10, 15);

        OrcamentoCirurgicoRegras.ValidarDataCirurgiaEValidadeLote(dataCirurgia, loteValido);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            OrcamentoCirurgicoRegras.ValidarDataCirurgiaEValidadeLote(dataCirurgia, loteInvalido));

        Assert.Contains("vencerá antes do procedimento", ex.Message);
    }

    [Fact]
    public void Orcamento_EstadosDeTransicao_RespeitamRegrasComerciais()
    {
        Assert.True(OrcamentoCirurgicoRegras.PodeEditar("RASCUNHO"));
        Assert.False(OrcamentoCirurgicoRegras.PodeEditar("APROVADO"));

        Assert.True(OrcamentoCirurgicoRegras.PodeAprovar("RASCUNHO"));
        Assert.True(OrcamentoCirurgicoRegras.PodeAprovar("ENVIADO"));
        Assert.False(OrcamentoCirurgicoRegras.PodeAprovar("REJEITADO"));

        Assert.True(OrcamentoCirurgicoRegras.PodeReservar("APROVADO"));
        Assert.False(OrcamentoCirurgicoRegras.PodeReservar("RASCUNHO"));
    }

    [Fact]
    public void Consignacao_InvariantesDeReconciliacao_CalculamPendenteCorretamente()
    {
        // Expedido: 6 = 4 consumidos + 2 devolvidos + 0 perdas => pendente 0
        var pendente = ValeConsignacaoRegras.CalcularPendenteCustodia(6m, 4m, 2m, 0m);
        Assert.Equal(0m, pendente);

        // Se soma exceder o expedido, deve falhar
        Assert.Throws<InvalidOperationException>(() =>
            ValeConsignacaoRegras.CalcularPendenteCustodia(6m, 4m, 3m, 0m));
    }

    // =========================================================================
    // TESTES DE INTEGRAÇÃO COM BANCO POSTGRESQL REAL
    // =========================================================================

    [Fact]
    public async Task Integracao_Transferencia_IdempotenteEConflito()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");
        var origemId = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var destinoId = Guid.Parse("a3610000-0000-4000-8000-000000000004");

        var loteId = Guid.NewGuid();
        var codigoLote = "TRANSF-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
                VALUES(@loteId, @tenantId, @produtoId, @codigoLote, '2028-01-01');
                
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @origemId, 'ENTRADA', 'LIBERADO', 10,
                    'SEED_TEST', @loteId, @loteKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, origemId, usuarioId, loteKey = "seed:transf:" + loteId });
        }

        var estoqueRepo = new EstoqueRepository(cs);
        var key = "test:transferencia:" + Guid.NewGuid().ToString("N");

        var cmd1 = new TransferirCommand(produtoId, loteId, origemId, destinoId, 1m, "Transferência teste automatizado", key);
        await estoqueRepo.TransferirAsync(tenantId, usuarioId, cmd1, CancellationToken.None);

        // Reexecuta com mesma chave e mesma quantidade -> IDEMPOTENTE
        await estoqueRepo.TransferirAsync(tenantId, usuarioId, cmd1, CancellationToken.None);

        // Reexecuta com mesma chave e quantidade diferente (2m) -> DEVE RETORNAR CONFLITO
        var cmdConflito = new TransferirCommand(produtoId, loteId, origemId, destinoId, 2m, "Tentativa conflito", key);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            estoqueRepo.TransferirAsync(tenantId, usuarioId, cmdConflito, CancellationToken.None));
        Assert.Contains("Conflito de idempotência", ex.Message);
    }

    [Fact]
    public async Task Integracao_Inspecao_ParcialSemColisaoDeChave()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var riId = Guid.NewGuid();
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");
        var loteQ = Guid.Parse("a3610000-0000-4000-8000-000000000006");
        var localCd = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var recebId = Guid.Parse("a3610000-0000-4000-8000-000000000010");
        var itemId = Guid.Parse("a3610000-0000-4000-8000-000000000009");

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_recebimento_itens(id, tenant_id, recebimento_id, pedido_item_id, produto_id, lote_id, local_id, quantidade, condicao)
                VALUES(@riId, @tenantId, @recebId, @itemId, @produtoId, @loteQ, @localCd, 10, 'QUARENTENA');
                
                INSERT INTO plantaopro.adm360_movimentos(id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, origem_tipo, origem_id, idempotency_key, created_by)
                VALUES(gen_random_uuid(), @tenantId, @produtoId, @loteQ, @localCd, 'ENTRADA', 'QUARENTENA', 10, 'RECEBIMENTO', @riId, @mKey, @usuarioId);",
                new { riId, tenantId, recebId, itemId, produtoId, loteQ, localCd, mKey = "seed:inspecao:" + riId, usuarioId });
        }

        var qualidadeRepo = new QualidadeRepository(cs);
        var key = "test:inspecao:" + Guid.NewGuid().ToString("N");
        var cmd = new DecidirInspecaoCommand(riId, 2m, 2m, "Laudo técnico parcial", "ALMOXARIFADO", key);

        await qualidadeRepo.DecidirAsync(tenantId, usuarioId, cmd, CancellationToken.None);
        await qualidadeRepo.DecidirAsync(tenantId, usuarioId, cmd, CancellationToken.None);
    }

    [Fact]
    public async Task Integracao_OrcamentoCirurgico_CicloCompleto()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var pagadorId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");
        var loteVenceAntes = Guid.Parse("a3610000-0000-4000-8000-000000000025");
        var localId = Guid.Parse("a3610000-0000-4000-8000-000000000003");

        var loteId = Guid.NewGuid();
        var codigoLote = "ORC-LOTE-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
                VALUES(@loteId, @tenantId, @produtoId, @codigoLote, '2028-01-01');
                
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localId, 'ENTRADA', 'LIBERADO', 10,
                    'SEED_TEST', @loteId, @loteKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, localId, usuarioId, loteKey = "seed:orc:" + loteId });
        }

        var orcamentoRepo = new OrcamentoCirurgicoRepository(cs);

        var cmdCriar = new CriarOrcamentoCommand(
            hospitalId, null, "Cirurgia Teste de Quadril", pagadorId, null,
            new DateOnly(2026, 11, 20),
            new DateOnly(2026, 11, 5),
            "Observações teste",
            new[] { new OrcamentoItemCommand(produtoId, 3m, 500m, 50m) });

        var orcamentoId = await orcamentoRepo.CriarAsync(tenantId, usuarioId, cmdCriar, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, orcamentoId);

        var orc = await orcamentoRepo.ObterPorIdAsync(tenantId, orcamentoId, CancellationToken.None);
        Assert.NotNull(orc);
        Assert.Equal("RASCUNHO", orc!.Situacao);
        Assert.Equal(1450m, orc.TotalGeral);

        // Reserva em rascunho deve falhar
        var cmdReserva = new ReservarCommand(produtoId, loteId, localId, 1m, "ORCAMENTO_CIRURGICO", orcamentoId, "reserva:" + Guid.NewGuid().ToString("N"));
        var exReserva = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcamentoId, cmdReserva, CancellationToken.None));
        Assert.Contains("Somente orçamento APROVADO", exReserva.Message);

        // Aprovar orçamento
        var keyAprovacao = "aprov:" + Guid.NewGuid().ToString("N");
        await orcamentoRepo.AprovarAsync(tenantId, usuarioId, orcamentoId, keyAprovacao, CancellationToken.None);

        var orcAprovado = await orcamentoRepo.ObterPorIdAsync(tenantId, orcamentoId, CancellationToken.None);
        Assert.Equal("APROVADO", orcAprovado!.Situacao);

        // Lote que vence antes da cirurgia deve falhar
        var cmdLoteVenceAntes = new ReservarCommand(produtoId, loteVenceAntes, localId, 1m, "ORCAMENTO_CIRURGICO", orcamentoId, "reserva:vence:" + Guid.NewGuid().ToString("N"));
        var exValidade = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcamentoId, cmdLoteVenceAntes, CancellationToken.None));
        Assert.Contains("vencerá antes do procedimento", exValidade.Message);

        // Reservar material compatível
        await orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcamentoId, cmdReserva, CancellationToken.None);

        var plano = await orcamentoRepo.ObterPlanejamentoReservaAsync(tenantId, orcamentoId, CancellationToken.None);
        Assert.NotNull(plano);
        var itemPlano = plano!.Itens.First(i => i.ProdutoId == produtoId);
        Assert.True(itemPlano.QuantidadeReservada >= 1m);
        Assert.True(itemPlano.FaltaAtender <= 2m);
    }

    // =========================================================================
    // SEÇÃO 16: ACEITE NUMÉRICO OBRIGATÓRIO (7 ETAPAS EXATAS)
    // =========================================================================

    [Fact]
    public async Task Integracao_AceiteNumerico_JornadaCompleta7Passos()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var localInternoId = Guid.Parse("a3610000-0000-4000-8000-000000000003"); // CD-DEMO
        var localExternoId = Guid.Parse("a3610000-0000-4000-8000-000000000004"); // HOSP-DEMO
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");

        var loteId = Guid.NewGuid();
        var codigoLote = "ACEITE-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        // 1) INICIAL: interno físico 10; reserva 0; disponível 10; externo 0
        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
                VALUES(@loteId, @tenantId, @produtoId, @codigoLote, '2028-12-31');
                
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localInternoId, 'ENTRADA', 'LIBERADO', 10,
                    'TEST_INICIAL', @loteId, @mKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, localInternoId, usuarioId, mKey = "aceite:init:" + loteId });
        }

        var estoqueRepo = new EstoqueRepository(cs);
        var orcamentoRepo = new OrcamentoCirurgicoRepository(cs);
        var cirurgiaRepo = new CirurgiaRepository(cs);
        var valeRepo = new ValeConsignacaoRepository(cs);

        // Valida Passo 1
        var saldos1 = await estoqueRepo.ConsultarAsync(tenantId, null, null, null, CancellationToken.None);
        var saldo1Interno = saldos1.First(s => s.LoteId == loteId && s.LocalId == localInternoId && s.Condicao == "LIBERADO");
        var saldo1Externo = saldos1.FirstOrDefault(s => s.LoteId == loteId && s.LocalId == localExternoId);

        Assert.Equal(10m, saldo1Interno.Fisico);
        Assert.Equal(0m, saldo1Interno.Reservado);
        Assert.Equal(10m, saldo1Interno.Disponivel);
        Assert.Equal(0m, saldo1Externo?.Fisico ?? 0m);

        // 2) RESERVAR 6: interno físico 10; reserva 6; disponível 4
        var cmdCriarOrc = new CriarOrcamentoCommand(
            hospitalId, null, "Procedimento Aceite Numérico", hospitalId, null,
            new DateOnly(2026, 11, 25), new DateOnly(2026, 11, 10), "Aceite",
            new[] { new OrcamentoItemCommand(produtoId, 10m, 500m, 0m) });

        var orcId = await orcamentoRepo.CriarAsync(tenantId, usuarioId, cmdCriarOrc, CancellationToken.None);
        await orcamentoRepo.AprovarAsync(tenantId, usuarioId, orcId, "aprov:aceite:" + orcId, CancellationToken.None);

        var orcDetalhes = await orcamentoRepo.ObterPorIdAsync(tenantId, orcId, CancellationToken.None);
        var orcItemId = orcDetalhes!.Itens.First(i => i.ProdutoId == produtoId).Id;

        var cmdReserva = new ReservarCommand(produtoId, loteId, localInternoId, 6m, "ORCAMENTO_CIRURGICO", orcId, "reserva:aceite:" + Guid.NewGuid().ToString("N"), null, orcItemId);
        await orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcId, cmdReserva, CancellationToken.None);

        // Valida Passo 2
        var saldos2 = await estoqueRepo.ConsultarAsync(tenantId, null, null, null, CancellationToken.None);
        var saldo2Interno = saldos2.First(s => s.LoteId == loteId && s.LocalId == localInternoId && s.Condicao == "LIBERADO");
        Assert.Equal(10m, saldo2Interno.Fisico);
        Assert.Equal(6m, saldo2Interno.Reservado);
        Assert.Equal(4m, saldo2Interno.Disponivel);

        // 3) EXPEDIR 6: interno físico 4; reserva ativa 0; disponível interno 4; externo físico 6
        var cirurgiaId = await cirurgiaRepo.CriarAsync(tenantId, usuarioId, new CriarCirurgiaCommand(
            hospitalId, null, "Procedimento Aceite", new DateOnly(2026, 11, 25), new TimeOnly(8, 0),
            orcId, 1, null, localExternoId, "Cirurgia Aceite"), CancellationToken.None);

        // Localiza a reserva criada
        Guid reservaId;
        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            reservaId = await cn.ExecuteScalarAsync<Guid>(
                "SELECT id FROM plantaopro.adm360_reservas WHERE tenant_id = @tenantId AND lote_id = @loteId AND situacao = 'ATIVA'",
                new { tenantId, loteId });
        }

        var valeId = await valeRepo.CriarAsync(tenantId, usuarioId, new CriarValeCommand(
            cirurgiaId, orcId, 1, hospitalId, null, localInternoId, localExternoId,
            new DateOnly(2026, 11, 24), new DateOnly(2026, 11, 28), "Vale Aceite",
            new[] { new ValeItemCommand(produtoId, loteId, reservaId, 6m, 500m) }), CancellationToken.None);

        var valeAntesExp = await valeRepo.ObterPorIdAsync(tenantId, valeId, CancellationToken.None);
        var valeItemId = valeAntesExp!.Itens.First().Id;

        await valeRepo.SepararItemAsync(tenantId, usuarioId, valeId, new SepararItemValeCommand(valeItemId, 6m), CancellationToken.None);
        await valeRepo.ConcluirSeparacaoAsync(tenantId, usuarioId, valeId, CancellationToken.None);
        await valeRepo.ExpedirAsync(tenantId, usuarioId, new ExpedirValeCommand(valeId, "expedir:aceite:" + valeId), CancellationToken.None);

        // Valida Passo 3
        var saldos3 = await estoqueRepo.ConsultarAsync(tenantId, null, null, null, CancellationToken.None);
        var saldo3Interno = saldos3.First(s => s.LoteId == loteId && s.LocalId == localInternoId && s.Condicao == "LIBERADO");
        var saldo3Externo = saldos3.First(s => s.LoteId == loteId && s.LocalId == localExternoId && s.Condicao == "LIBERADO");

        Assert.Equal(4m, saldo3Interno.Fisico);
        Assert.Equal(0m, saldo3Interno.Reservado);
        Assert.Equal(4m, saldo3Interno.Disponivel);
        Assert.Equal(6m, saldo3Externo.Fisico);

        // 4) CONSUMIR 4: externo físico 2; consumo do vale 4
        await valeRepo.RegistrarConsumoAsync(tenantId, usuarioId, new RegistrarConsumoValeCommand(
            valeId, valeItemId, 4m, "Utilizado no paciente", "consumo:aceite:" + valeId), CancellationToken.None);

        var saldos4 = await estoqueRepo.ConsultarAsync(tenantId, null, null, null, CancellationToken.None);
        var saldo4Externo = saldos4.First(s => s.LoteId == loteId && s.LocalId == localExternoId && s.Condicao == "LIBERADO");
        var valePosConsumo = await valeRepo.ObterPorIdAsync(tenantId, valeId, CancellationToken.None);
        var itemPosConsumo = valePosConsumo!.Itens.First();

        Assert.Equal(2m, saldo4Externo.Fisico);
        Assert.Equal(4m, itemPosConsumo.QuantidadeConsumida);

        // 5) DEVOLVER 2 PARA QUARENTENA: externo físico 0; interno total 6; liberado 4; quarentena 2
        await valeRepo.RegistrarRetornoAsync(tenantId, usuarioId, new RegistrarRetornoValeCommand(
            valeId, valeItemId, 2m, "Sobra cirúrgica", "retorno:aceite:" + valeId), CancellationToken.None);

        var saldos5 = await estoqueRepo.ConsultarAsync(tenantId, null, null, null, CancellationToken.None);
        var saldo5Externo = saldos5.FirstOrDefault(s => s.LoteId == loteId && s.LocalId == localExternoId);
        var saldo5InternoLib = saldos5.FirstOrDefault(s => s.LoteId == loteId && s.LocalId == localInternoId && s.Condicao == "LIBERADO");
        var saldo5InternoQuar = saldos5.FirstOrDefault(s => s.LoteId == loteId && s.LocalId == localInternoId && s.Condicao == "QUARENTENA");

        Assert.Equal(0m, saldo5Externo?.Fisico ?? 0m);
        Assert.Equal(4m, saldo5InternoLib?.Fisico ?? 0m);
        Assert.Equal(2m, saldo5InternoQuar?.Fisico ?? 0m);
        Assert.Equal(6m, (saldo5InternoLib?.Fisico ?? 0m) + (saldo5InternoQuar?.Fisico ?? 0m));

        // 6) QUALIDADE LIBERAR O RETORNO: interno liberado 6; quarentena 0; externo 0
        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            // Simula liberação formal de qualidade da quarentena para liberado
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES
                    (gen_random_uuid(), @tenantId, @produtoId, @loteId, @localInternoId, 'SAIDA', 'QUARENTENA', -2, 'QUALIDADE', @valeId, @k1, @usuarioId),
                    (gen_random_uuid(), @tenantId, @produtoId, @loteId, @localInternoId, 'ENTRADA', 'LIBERADO', 2, 'QUALIDADE', @valeId, @k2, @usuarioId);",
                new { tenantId, produtoId, loteId, localInternoId, valeId, k1 = "qual:sai:" + loteId, k2 = "qual:ent:" + loteId, usuarioId });
        }

        var saldos6 = await estoqueRepo.ConsultarAsync(tenantId, null, null, null, CancellationToken.None);
        var saldo6InternoLib = saldos6.FirstOrDefault(s => s.LoteId == loteId && s.LocalId == localInternoId && s.Condicao == "LIBERADO");
        var saldo6InternoQuar = saldos6.FirstOrDefault(s => s.LoteId == loteId && s.LocalId == localInternoId && s.Condicao == "QUARENTENA");
        var saldo6Externo = saldos6.FirstOrDefault(s => s.LoteId == loteId && s.LocalId == localExternoId);

        Assert.Equal(6m, saldo6InternoLib?.Fisico ?? 0m);
        Assert.Equal(0m, saldo6InternoQuar?.Fisico ?? 0m);
        Assert.Equal(0m, saldo6Externo?.Fisico ?? 0m);

        // 7) RECONCILIAR: expedido 6 = consumido 4 + devolvido 2; pendente 0
        await valeRepo.ReconciliarAsync(tenantId, usuarioId, new ReconciliarValeCommand(
            valeId, "Reconciliação Aceite Numérico", "rec:aceite:" + valeId), CancellationToken.None);

        var valeFinal = await valeRepo.ObterPorIdAsync(tenantId, valeId, CancellationToken.None);
        var itemFinal = valeFinal!.Itens.First();

        Assert.Equal("RECONCILIADO", valeFinal.Situacao);
        Assert.Equal("PENDENTE_VALORIZACAO", valeFinal.SituacaoFinanceira); // Sem faturamento / NF neste ciclo
        Assert.Equal(6m, itemFinal.QuantidadeExpedida);
        Assert.Equal(4m, itemFinal.QuantidadeConsumida);
        Assert.Equal(2m, itemFinal.QuantidadeDevolvida);
        Assert.Equal(0m, itemFinal.PendenteCustodia);
    }

    // =========================================================================
    // SEÇÃO 17: TESTES NEGATIVOS E INVARIANTES
    // =========================================================================

    [Fact]
    public async Task Negativo_ReservaAcimaDoOrcamento_Negada()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var localId = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");

        var loteId = Guid.NewGuid();
        var codigoLote = "NEG-ORC-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
                VALUES(@loteId, @tenantId, @produtoId, @codigoLote, '2028-12-31');
                
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localId, 'ENTRADA', 'LIBERADO', 20,
                    'SEED', @loteId, @mKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, localId, usuarioId, mKey = "neg:orc:" + loteId });
        }

        var orcamentoRepo = new OrcamentoCirurgicoRepository(cs);

        // Orçamento de 3 unidades
        var cmdCriar = new CriarOrcamentoCommand(
            hospitalId, null, "Cirurgia Teste Negativo", hospitalId, null,
            new DateOnly(2026, 12, 1), new DateOnly(2026, 11, 20), "Negativo",
            new[] { new OrcamentoItemCommand(produtoId, 3m, 100m, 0m) });

        var orcId = await orcamentoRepo.CriarAsync(tenantId, usuarioId, cmdCriar, CancellationToken.None);
        await orcamentoRepo.AprovarAsync(tenantId, usuarioId, orcId, "aprov:neg:" + orcId, CancellationToken.None);

        // Tentar reservar 4 unidades (excede 3 do orçamento)
        var cmdReserva = new ReservarCommand(produtoId, loteId, localId, 4m, "ORCAMENTO_CIRURGICO", orcId, "res:neg:" + Guid.NewGuid().ToString("N"));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcId, cmdReserva, CancellationToken.None));

        Assert.Contains("excede a necessidade ainda não atendida", ex.Message);
    }

    [Fact]
    public async Task Negativo_RetryAposConsumirTodoDisponivel_RetornaOperacaoOriginal()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var localId = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");

        var loteId = Guid.NewGuid();
        var codigoLote = "RETRY-TOT-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        // Saldo físico exato de 2 unidades
        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
                VALUES(@loteId, @tenantId, @produtoId, @codigoLote, '2028-12-31');
                
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localId, 'ENTRADA', 'LIBERADO', 2,
                    'SEED', @loteId, @mKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, localId, usuarioId, mKey = "retry:tot:" + loteId });
        }

        var orcamentoRepo = new OrcamentoCirurgicoRepository(cs);

        var cmdCriar = new CriarOrcamentoCommand(
            hospitalId, null, "Cirurgia Teste Retry", hospitalId, null,
            new DateOnly(2026, 12, 1), new DateOnly(2026, 11, 20), "Retry",
            new[] { new OrcamentoItemCommand(produtoId, 5m, 100m, 0m) });

        var orcId = await orcamentoRepo.CriarAsync(tenantId, usuarioId, cmdCriar, CancellationToken.None);
        await orcamentoRepo.AprovarAsync(tenantId, usuarioId, orcId, "aprov:retry:" + orcId, CancellationToken.None);

        var keyRetry = "reserva:retry:key:" + Guid.NewGuid().ToString("N");
        var cmdReserva = new ReservarCommand(produtoId, loteId, localId, 2m, "ORCAMENTO_CIRURGICO", orcId, keyRetry);

        // Primeira chamada consome todo o disponível (disponível vira 0)
        await orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcId, cmdReserva, CancellationToken.None);

        // Retry com a MESMA chave e MESMO payload: NÃO deve falhar por falta de saldo disponível!
        // Deve reconhecer a idempotência e retornar com sucesso.
        await orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcId, cmdReserva, CancellationToken.None);
    }

    [Fact]
    public async Task Negativo_DoisValesNaoUtilizamMesmaAlocacao()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var localOrig = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var localDest = Guid.Parse("a3610000-0000-4000-8000-000000000004");
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");

        var loteId = Guid.NewGuid();
        var codigoLote = "VALE-ALOC-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var reservaId = Guid.NewGuid();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
                VALUES(@loteId, @tenantId, @produtoId, @codigoLote, '2028-12-31');
                
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localOrig, 'ENTRADA', 'LIBERADO', 10,
                    'SEED', @loteId, @mKey, @usuarioId
                );
                
                INSERT INTO plantaopro.adm360_reservas(
                    id, tenant_id, produto_id, lote_id, local_id, quantidade, situacao, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    @reservaId, @tenantId, @produtoId, @loteId, @localOrig, 3, 'ATIVA', 'MANUAL', @reservaId, @rKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, localOrig, usuarioId, mKey = "m:" + loteId, reservaId, rKey = "r:" + reservaId });
        }

        var valeRepo = new ValeConsignacaoRepository(cs);

        // Vale 1 consome a reserva
        var cmdVale1 = new CriarValeCommand(
            null, null, null, hospitalId, null, localOrig, localDest,
            new DateOnly(2026, 12, 1), null, "Vale 1",
            new[] { new ValeItemCommand(produtoId, loteId, reservaId, 3m, 100m) });

        var vale1Id = await valeRepo.CriarAsync(tenantId, usuarioId, cmdVale1, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, vale1Id);

        // Vale 2 tenta consumir a MESMA reserva -> DEVE FALHAR
        var cmdVale2 = new CriarValeCommand(
            null, null, null, hospitalId, null, localOrig, localDest,
            new DateOnly(2026, 12, 1), null, "Vale 2",
            new[] { new ValeItemCommand(produtoId, loteId, reservaId, 3m, 100m) });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            valeRepo.CriarAsync(tenantId, usuarioId, cmdVale2, CancellationToken.None));

        Assert.Contains("já estão vinculadas a outro vale", ex.Message);
    }

    [Fact]
    public async Task Negativo_ConsumoAcimaDoExpedido_Negado()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var localOrig = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var localDest = Guid.Parse("a3610000-0000-4000-8000-000000000004");
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");

        var loteId = Guid.NewGuid();
        var codigoLote = "VALE-CONS-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
                VALUES(@loteId, @tenantId, @produtoId, @codigoLote, '2028-12-31');
                
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localOrig, 'ENTRADA', 'LIBERADO', 10,
                    'SEED', @loteId, @mKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, localOrig, usuarioId, mKey = "m:cons:" + loteId });
        }

        var valeRepo = new ValeConsignacaoRepository(cs);

        var valeId = await valeRepo.CriarAsync(tenantId, usuarioId, new CriarValeCommand(
            null, null, null, hospitalId, null, localOrig, localDest,
            new DateOnly(2026, 12, 1), null, "Vale Consumo",
            new[] { new ValeItemCommand(produtoId, loteId, null, 2m, 100m) }), CancellationToken.None);

        var valeDetalhes = await valeRepo.ObterPorIdAsync(tenantId, valeId, CancellationToken.None);
        var itemId = valeDetalhes!.Itens.First().Id;

        await valeRepo.SepararItemAsync(tenantId, usuarioId, valeId, new SepararItemValeCommand(itemId, 2m), CancellationToken.None);
        await valeRepo.ConcluirSeparacaoAsync(tenantId, usuarioId, valeId, CancellationToken.None);
        await valeRepo.ExpedirAsync(tenantId, usuarioId, new ExpedirValeCommand(valeId, "exp:neg:" + valeId), CancellationToken.None);

        // Tentar consumir 3 quando apenas 2 foram expedidos
        var cmdConsumo = new RegistrarConsumoValeCommand(valeId, itemId, 3m, "Tentativa excessiva", "cons:excess:" + valeId);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            valeRepo.RegistrarConsumoAsync(tenantId, usuarioId, cmdConsumo, CancellationToken.None));

        Assert.Contains("excede o saldo pendente em custódia", ex.Message);
    }

    [Fact]
    public async Task Negativo_CancelamentoAposExpedicao_Negado()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var localOrig = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var localDest = Guid.Parse("a3610000-0000-4000-8000-000000000004");
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");

        var loteId = Guid.NewGuid();
        var codigoLote = "VALE-CANC-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
                VALUES(@loteId, @tenantId, @produtoId, @codigoLote, '2028-12-31');
                
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade,
                    origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES (
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localOrig, 'ENTRADA', 'LIBERADO', 5,
                    'SEED', @loteId, @mKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, localOrig, usuarioId, mKey = "m:canc:" + loteId });
        }

        var valeRepo = new ValeConsignacaoRepository(cs);

        var valeId = await valeRepo.CriarAsync(tenantId, usuarioId, new CriarValeCommand(
            null, null, null, hospitalId, null, localOrig, localDest,
            new DateOnly(2026, 12, 1), null, "Vale Cancelamento",
            new[] { new ValeItemCommand(produtoId, loteId, null, 1m, 100m) }), CancellationToken.None);

        var valeDetalhes = await valeRepo.ObterPorIdAsync(tenantId, valeId, CancellationToken.None);
        var itemId = valeDetalhes!.Itens.First().Id;

        await valeRepo.SepararItemAsync(tenantId, usuarioId, valeId, new SepararItemValeCommand(itemId, 1m), CancellationToken.None);
        await valeRepo.ConcluirSeparacaoAsync(tenantId, usuarioId, valeId, CancellationToken.None);
        await valeRepo.ExpedirAsync(tenantId, usuarioId, new ExpedirValeCommand(valeId, "exp:canc:" + valeId), CancellationToken.None);

        // Cancelamento após expedição não devolve material ficticiamente -> DEVE SER RECUSADO
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            valeRepo.CancelarAsync(tenantId, usuarioId, valeId, "Tentativa cancelamento pós-expedição", CancellationToken.None));

        Assert.Contains("não pode ser cancelado diretamente", ex.Message);
    }
}
