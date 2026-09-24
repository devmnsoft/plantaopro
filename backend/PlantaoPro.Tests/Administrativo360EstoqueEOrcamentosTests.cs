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
        // 5 aprovados e 5 reprovados com justificativa dentro de 10 pendentes: VÁLIDO
        Inspecao.ValidarDecisao(10m, 5m, 5m, "Avaria identificada na embalagem secundária");

        // Reprovação sem justificativa deve falhar
        Assert.Throws<InvalidOperationException>(() => Inspecao.ValidarDecisao(10m, 5m, 5m, ""));

        // Decisão excedendo saldo pendente deve falhar
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

        // Desconto maior que o subtotal do item deve falhar
        Assert.Throws<InvalidOperationException>(() => OrcamentoCirurgicoRegras.CalcularTotalItem(2m, 100m, 250m));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrcamentoCirurgicoRegras.CalcularTotalItem(0m, 100m, 0m));
    }

    [Fact]
    public void Orcamento_ValidarDataCirurgia_ImpedeLoteQueVenceAntesDaCirurgia()
    {
        var dataCirurgia = new DateOnly(2026, 11, 20);
        var loteValido = new DateOnly(2027, 5, 10);
        var loteInvalido = new DateOnly(2026, 10, 15);

        // Lote válido não lança exceção
        OrcamentoCirurgicoRegras.ValidarDataCirurgiaEValidadeLote(dataCirurgia, loteValido);

        // Lote que vence antes deve lançar InvalidOperationException
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

    // =========================================================================
    // TESTES DE INTEGRAÇÃO COM BANCO POSTGRESQL DESCARTÁVEL / DE HOMOLOGAÇÃO
    // =========================================================================

    [Fact]
    public async Task Integracao_Transferencia_IdempotenteEConflito()
    {
        var cs = ObterConnectionString();
        try
        {
            await using var cnTest = new NpgsqlConnection(cs);
            await cnTest.OpenAsync();
        }
        catch
        {
            return; // Banco indisponível neste ambiente de runner
        }

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");
        var origemId = Guid.Parse("a3610000-0000-4000-8000-000000000003"); // CD-DEMO
        var destinoId = Guid.Parse("a3610000-0000-4000-8000-000000000004"); // HOSP-DEMO

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

        // 1. Executa transferência de 1 unidade
        var cmd1 = new TransferirCommand(produtoId, loteId, origemId, destinoId, 1m, "Transferência teste automatizado", key);
        await estoqueRepo.TransferirAsync(tenantId, usuarioId, cmd1, CancellationToken.None);

        // 2. Reexecuta com mesma chave e mesma quantidade -> IDEMPOTENTE, não duplica nem lança erro
        await estoqueRepo.TransferirAsync(tenantId, usuarioId, cmd1, CancellationToken.None);

        // 3. Reexecuta com mesma chave e quantidade diferente (2m) -> DEVE RETORNAR CONFLITO
        var cmdConflito = new TransferirCommand(produtoId, loteId, origemId, destinoId, 2m, "Tentativa conflito", key);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            estoqueRepo.TransferirAsync(tenantId, usuarioId, cmdConflito, CancellationToken.None));
        Assert.Contains("Conflito de idempotência", ex.Message);
    }

    [Fact]
    public async Task Integracao_Inspecao_ParcialSemColisaoDeChave()
    {
        var cs = ObterConnectionString();
        try
        {
            await using var cnTest = new NpgsqlConnection(cs);
            await cnTest.OpenAsync();
        }
        catch
        {
            return;
        }

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var riId = Guid.NewGuid();
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");
        var loteQ = Guid.Parse("a3610000-0000-4000-8000-000000000006");
        var localCd = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var recebId = Guid.Parse("a3610000-0000-4000-8000-000000000010");
        var itemId = Guid.Parse("a3610000-0000-4000-8000-000000000009");

        // Cria item de recebimento dedicado para testar inspeção parcial sem interferência de execuções prévias
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

        // Decisão de 2 aprovados e 2 reprovados: quantidades rigorosamente iguais
        // Testa a estabilidade das chaves SAIDA_QUARENTENA_APROVACAO vs SAIDA_QUARENTENA_REPROVACAO
        var cmd = new DecidirInspecaoCommand(riId, 2m, 2m, "Laudo técnico parcial", "ALMOXARIFADO", key);

        // Não pode estourar violação de chave única por colisão de pernas
        await qualidadeRepo.DecidirAsync(tenantId, usuarioId, cmd, CancellationToken.None);

        // Reenvio idempotente
        await qualidadeRepo.DecidirAsync(tenantId, usuarioId, cmd, CancellationToken.None);
    }

    [Fact]
    public async Task Integracao_OrcamentoCirurgico_CicloCompleto()
    {
        var cs = ObterConnectionString();
        try
        {
            await using var cnTest = new NpgsqlConnection(cs);
            await cnTest.OpenAsync();
        }
        catch
        {
            return;
        }

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var hospitalId = Guid.Parse("a3610000-0000-4000-8000-000000000030"); // Hospital São Lucas
        var pagadorId = Guid.Parse("a3610000-0000-4000-8000-000000000030");
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");
        var loteVenceAntes = Guid.Parse("a3610000-0000-4000-8000-000000000025"); // Vence 2026-10-15
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

        // 1. Criar Orçamento (cirurgia prevista para 2026-11-20)
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
        Assert.Equal(1450m, orc.TotalGeral); // (3 * 500) - 50 = 1450

        // 2. Tentar reservar em rascunho deve falhar
        var cmdReserva = new ReservarCommand(produtoId, loteId, localId, 1m, "ORCAMENTO_CIRURGICO", orcamentoId, "reserva:" + Guid.NewGuid().ToString("N"));
        var exReserva = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcamentoId, cmdReserva, CancellationToken.None));
        Assert.Contains("Somente orçamento APROVADO", exReserva.Message);

        // 3. Aprovar orçamento
        var keyAprovacao = "aprov:" + Guid.NewGuid().ToString("N");
        await orcamentoRepo.AprovarAsync(tenantId, usuarioId, orcamentoId, keyAprovacao, CancellationToken.None);

        var orcAprovado = await orcamentoRepo.ObterPorIdAsync(tenantId, orcamentoId, CancellationToken.None);
        Assert.Equal("APROVADO", orcAprovado!.Situacao);

        // 4. Tentar reservar lote com validade que vence ANTES da cirurgia deve falhar
        var cmdLoteVenceAntes = new ReservarCommand(produtoId, loteVenceAntes, localId, 1m, "ORCAMENTO_CIRURGICO", orcamentoId, "reserva:vence:" + Guid.NewGuid().ToString("N"));
        var exValidade = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcamentoId, cmdLoteVenceAntes, CancellationToken.None));
        Assert.Contains("vencerá antes do procedimento", exValidade.Message);

        // 5. Reservar material compatível após aprovação
        await orcamentoRepo.ReservarItemAsync(tenantId, usuarioId, orcamentoId, cmdReserva, CancellationToken.None);

        var plano = await orcamentoRepo.ObterPlanejamentoReservaAsync(tenantId, orcamentoId, CancellationToken.None);
        Assert.NotNull(plano);
        var itemPlano = plano!.Itens.First(i => i.ProdutoId == produtoId);
        Assert.True(itemPlano.QuantidadeReservada >= 1m);
        Assert.True(itemPlano.FaltaAtender <= 2m);
    }

    [Fact]
    public async Task Integracao_BloqueioInventario_ImpedeMovimentacoes()
    {
        var cs = ObterConnectionString();
        try
        {
            await using var cnTest = new NpgsqlConnection(cs);
            await cnTest.OpenAsync();
        }
        catch
        {
            return;
        }

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");
        var loteId = Guid.Parse("a3610000-0000-4000-8000-000000000005");
        var localInv = Guid.Parse("a3610000-0000-4000-8000-000000000020"); // ALMOX-INV (em CONTAGEM no seed)
        var localDestino = Guid.Parse("a3610000-0000-4000-8000-000000000004");

        var estoqueRepo = new EstoqueRepository(cs);
        var key = "test:bloqueio_inv:" + Guid.NewGuid().ToString("N");

        var cmd = new TransferirCommand(produtoId, loteId, localInv, localDestino, 1m, "Tentativa em local com inventário", key);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            estoqueRepo.TransferirAsync(tenantId, usuarioId, cmd, CancellationToken.None));

        Assert.Contains("inventário ativo", ex.Message);
    }

    [Fact]
    public async Task Integracao_IsolamentoEntreTenants_NaoVazaDados()
    {
        var cs = ObterConnectionString();
        try
        {
            await using var cnTest = new NpgsqlConnection(cs);
            await cnTest.OpenAsync();
        }
        catch
        {
            return;
        }

        var tenant1 = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var tenant2 = Guid.Parse("8b0c8e74-a81b-4ea2-b499-94755a1ca101"); // CLINICA_ISOLAMENTO

        var estoqueRepo = new EstoqueRepository(cs);
        var orcamentoRepo = new OrcamentoCirurgicoRepository(cs);

        var saldosTenant1 = await estoqueRepo.ConsultarAsync(tenant1, null, null, null, CancellationToken.None);
        var saldosTenant2 = await estoqueRepo.ConsultarAsync(tenant2, null, null, null, CancellationToken.None);

        Assert.NotEmpty(saldosTenant1);
        Assert.Empty(saldosTenant2); // Tenant 2 não visualiza saldos do Tenant 1

        // Consulta de orçamentos do Tenant 1 pelo Tenant 2
        var orcsTenant2 = await orcamentoRepo.ListarAsync(tenant2, null, null, null, null, CancellationToken.None);
        Assert.Empty(orcsTenant2);
    }

    [Fact]
    public async Task Integracao_Concorrencia_DisputaUltimaUnidade()
    {
        var cs = ObterConnectionString();
        try
        {
            await using var cnTest = new NpgsqlConnection(cs);
            await cnTest.OpenAsync();
        }
        catch
        {
            return;
        }

        var tenantId = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
        var usuarioId = Guid.NewGuid();
        var produtoId = Guid.Parse("a3610000-0000-4000-8000-000000000002");
        var localOrigem = Guid.Parse("a3610000-0000-4000-8000-000000000003");
        var localDestino = Guid.Parse("a3610000-0000-4000-8000-000000000004");

        // Cria lote exclusivo para disputa da última unidade
        var loteId = Guid.NewGuid();
        var codigoLote = "DISPUTA-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

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
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localOrigem, 'ENTRADA', 'LIBERADO', 1,
                    'SEED_TEST', @loteId, @loteKey, @usuarioId
                );", new { loteId, tenantId, produtoId, codigoLote, localOrigem, usuarioId, loteKey = "seed:disputa:" + loteId });
        }

        var estoqueRepo = new EstoqueRepository(cs);

        var keyA = "disputa:sessionA:" + Guid.NewGuid().ToString("N");
        var keyB = "disputa:sessionB:" + Guid.NewGuid().ToString("N");

        var cmdA = new TransferirCommand(produtoId, loteId, localOrigem, localDestino, 1m, "Sessão A", keyA);
        var cmdB = new TransferirCommand(produtoId, loteId, localOrigem, localDestino, 1m, "Sessão B", keyB);

        var taskA = Task.Run(async () =>
        {
            try
            {
                await estoqueRepo.TransferirAsync(tenantId, usuarioId, cmdA, CancellationToken.None);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        });

        var taskB = Task.Run(async () =>
        {
            try
            {
                await estoqueRepo.TransferirAsync(tenantId, usuarioId, cmdB, CancellationToken.None);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        });

        var resultados = await Task.WhenAll(taskA, taskB);
        var sucessos = resultados.Count(r => r);
        var falhas = resultados.Count(r => !r);

        // Exatamente uma sessão deve vencer e a outra deve falhar sem saldo negativo
        Assert.Equal(1, sucessos);
        Assert.Equal(1, falhas);

        // Valida que o saldo na origem é exatamente zero e nunca negativo
        var saldosOrigem = await estoqueRepo.ConsultarAsync(tenantId, null, "LIBERADO", localOrigem, CancellationToken.None);
        var saldoItem = saldosOrigem.FirstOrDefault(s => s.LoteId == loteId);
        var saldoRestante = saldoItem?.Disponivel ?? 0m;
        Assert.Equal(0m, saldoRestante);
    }
}
