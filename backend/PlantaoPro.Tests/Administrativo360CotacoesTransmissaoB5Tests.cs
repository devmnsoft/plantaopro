using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// B5 (Exportação/Sincronização) — Transmissão de resposta de cotação do Administrativo 360.
/// Regras validadas:
/// 1. Tentativa PERSISTIDA: cada transmissão grava uma linha em adm360_cotacao_envios
///    (UNIQUE tenant+resposta+tentativa) com canal, estado final, evidência e datas.
/// 2. Rede FORA de transação: marcação (INICIADA + ENVIANDO com SELECT ... FOR UPDATE) e
///    finalização são duas transações curtas; a chamada externa acontece entre elas.
/// 3. Concorrência: duas transmissões simultâneas para a mesma resposta — apenas UMA avança;
///    a segunda lê ENVIANDO sob lock e é rejeitada ("em andamento"), sem duplicar envio.
/// 4. Falha técnica do conector vira estado honesto RESULTADO_DESCONHECIDO (nunca ENVIANDO
///    órfão); repetir sem confirmação explícita é bloqueado (anti-duplicidade); com a flag
///    de confirmação, nova tentativa é criada e persistida (idempotência controlada).
/// 5. ACEITA_PELO_PORTAL é terminal: bloqueia qualquer retransmissão citando o protocolo.
/// 6. Recuperação de boot: ENVIANDO antigo vira RESULTADO_DESCONHECIDO e a tentativa
///    INICIADA correspondente é marcada INTERRUPTA; ENVIANDO recente permanece intocado.
/// 7. Isolamento multi-tenant: outro tenant não lê nem cria envio da resposta.
/// </summary>
// Mesma coleção das demais classes que escrevem status_transmissao='ENVIANDO':
// serializa entre si para que assert de estado global não enxergue janela de teste concorrente.
[Collection("A360Transmissao")]
public sealed class Administrativo360CotacoesTransmissaoB5Tests
{
    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid TenantIsolado = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647599");
    private static readonly Guid UsuarioGestor = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    // Produto demo do tenant Santa Casa usado para relacionamento De/Para (seed fixo).
    private static readonly Guid ProdutoImpDemo = Guid.Parse("a3610000-0000-4000-8000-000000000002");

    // ------------------------- Conectores de teste (fakes) -------------------------

    /// <summary>Conector lento: dá tempo para as duas transações de marcação se sobreporarem.</summary>
    private sealed class ConectorLento : IPortalCotacaoConnector
    {
        public string Provedor => "IMPORTACAO_MANUAL";
        public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default)
            => Task.FromResult(new PortalConexaoStatusResult(true, "CONFIGURADA", null, DateTime.UtcNow));
        public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());
        public async Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(
            PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default)
        {
            await Task.Delay(1500, ct);
            return new EnvioRespostaPortalResult(true, "EXPORTADA_MANUALMENTE", null, "Arquivo da proposta gerado (conector de teste lento).");
        }
    }

    /// <summary>Conector que falha sempre na transmissão (exceção artificial do conector).</summary>
    private sealed class ConectorFalha : IPortalCotacaoConnector
    {
        public string Provedor => "IMPORTACAO_MANUAL";
        public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default)
            => Task.FromResult(new PortalConexaoStatusResult(false, "NAO_CONFIGURADA", null, DateTime.UtcNow));
        public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());
        public Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(
            PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default)
            => throw new InvalidOperationException("Falha simulada no conector (exceção artificial do teste).");
    }

    // ------------------------- Infra de schema/jornada -------------------------

    private static async Task GarantirSchemaAsync(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // Lock de conselheiro: serializa a garantia de schema entre classes de teste paralelas.
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('adm360_eventos_tests_schema'));");
        try
        {
            var temTabela = await cn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_schema = 'plantaopro' AND table_name = 'adm360_cotacao_exportacoes'
                );");

            if (!temTabela)
            {
                var migrationPath = Path.Combine(RepositoryPathResolver.RepoRoot,
                    "database/migrations/2026_09_v2200_administrativo360_exportacao_proposta_imutavel.sql");
                Assert.True(File.Exists(migrationPath), "Migration v2200 (exportação de cotação) não encontrada.");
                await ExecutarMigrationComRetryAsync(cn, await File.ReadAllTextAsync(migrationPath));
            }

            // v2300 — CHECK de status_transmissao (idempotente).
            var migrationV2300 = Path.Combine(RepositoryPathResolver.RepoRoot,
                "database/migrations/2026_09_v2300_administrativo360_status_transmissao_exportacao.sql");
            Assert.True(File.Exists(migrationV2300), "Migration v2300 (status_transmissao) não encontrada.");
            await ExecutarMigrationComRetryAsync(cn, await File.ReadAllTextAsync(migrationV2300));

            // v2304 — B5: tabela de tentativas persistidas (IF NOT EXISTS; já aplicada 2x em psql).
            var migrationV2304 = Path.Combine(RepositoryPathResolver.RepoRoot,
                "database/migrations/2026_09_v2304_administrativo360_transmissao_tentativas.sql");
            Assert.True(File.Exists(migrationV2304), "Migration v2304 (tentativas de transmissão) não encontrada.");
            await ExecutarMigrationComRetryAsync(cn, await File.ReadAllTextAsync(migrationV2304));
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('adm360_eventos_tests_schema'));");
        }
    }

    /// <summary>Executa um arquivo de migration tolerando impasse (40P01) entre sessões paralelas.</summary>
    private static async Task ExecutarMigrationComRetryAsync(NpgsqlConnection cn, string sql)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                await cn.ExecuteAsync(sql);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5)
            {
                await Task.Delay(150 * tentativa);
            }
        }
    }

    /// <summary>
    /// Jornada até a resposta em NA_FILA: conta do canal manual (identificador único por teste)
    /// -> captura -> relacionamento -> orçamento -> aprovação. Devolve os IDs para limpeza.
    /// </summary>
    private static async Task<(Guid cotacaoId, Guid orcamentoId, Guid respostaId, Guid contaCriadoAgora)>
        PrepararRespostaNaFilaAsync(CotacoesRepository repo, string prefixo)
    {
        var estabelecimentos = await repo.ListarEstabelecimentosAsync(TenantSantaCasa);
        Assert.NotEmpty(estabelecimentos);

        var sufixo = Guid.NewGuid().ToString("N")[..7];
        var identificador = $"B5-{prefixo}-{sufixo}";

        var contaCriadaAgora = await repo.ConfigurarContaPortalAsync(TenantSantaCasa, UsuarioGestor,
            new ConfigurarPortalContaCommand(estabelecimentos[0].Id, "IMPORTACAO_MANUAL", $"Canal Manual - B5 {prefixo}", identificador, null, null));

        var cotacaoId = await repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CapturarCotacaoCommand(
            estabelecimentos[0].Id, contaCriadaAgora, "IMPORTACAO_MANUAL", identificador, 1,
            "Hospital B5", "P.T.", "Cirurgia Teste Transmissao B5",
            DateOnly.FromDateTime(DateTime.Today.AddDays(10)), DateTime.UtcNow.AddDays(2),
            "IMPORTACAO_MANUAL", "{\"bloco\":5}",
            new[] { new CapturarCotacaoItemCommand(1, "IMP-B5", "Produto Importacao Demo", "Fornecedor Demo", "STD", 1, "UN") }));

        var detalhe = await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
        Assert.NotNull(detalhe);
        Assert.Single(detalhe!.Itens);

        await repo.RelacionarItemAsync(TenantSantaCasa, UsuarioGestor, new RelacionarItemCotacaoCommand(
            detalhe.Itens[0].Id, ProdutoImpDemo, 1.0000m, 100.00m, 0m, "Placa Demo", null, "RELACIONADO", null));

        var orcamentoId = await repo.GerarOrcamentoCirurgicoAsync(TenantSantaCasa, UsuarioGestor,
            new GerarOrcamentoDaCotacaoCommand(cotacaoId));

        var respostaId = await repo.AprovarRespostaAsync(TenantSantaCasa, UsuarioGestor,
            new AprovarRespostaCotacaoCommand(cotacaoId));

        var naFila = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
        Assert.Equal("NA_FILA", naFila!.StatusTransmissao);

        return (cotacaoId, orcamentoId, respostaId, contaCriadaAgora);
    }

    private static async Task<int> ContarEnviosAsync(NpgsqlConnection cn, Guid respostaId, Guid tenantId)
        => await cn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @r AND tenant_id = @t",
            new { r = respostaId, t = tenantId });

    /// <summary>
    /// Limpeza best-effort. Ordem importa: envios (FK p/ respostas) antes de respostas;
    /// exportações são imutáveis por trigger -> session_replication_role='REPLICA'.
    /// </summary>
    private static async Task LimparDadosAsync(string cs, Guid respostaId, Guid orcamentoId, Guid cotacaoId, Guid contaId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();

            if (respostaId != Guid.Empty)
            {
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @r AND tenant_id = @t",
                    new { r = respostaId, t = TenantSantaCasa });
                await cn.ExecuteAsync("SET SESSION session_replication_role = 'REPLICA'");
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_cotacao_exportacoes WHERE resposta_id = @r", new { r = respostaId });
                await cn.ExecuteAsync("RESET SESSION_REPLICATION_ROLE");
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_cotacao_respostas WHERE id = @r AND tenant_id = @t",
                    new { r = respostaId, t = TenantSantaCasa });
            }

            if (orcamentoId != Guid.Empty)
            {
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_orcamento_itens WHERE orcamento_id = @o AND tenant_id = @t",
                    new { o = orcamentoId, t = TenantSantaCasa });
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_orcamentos WHERE id = @o AND tenant_id = @t",
                    new { o = orcamentoId, t = TenantSantaCasa });
            }

            if (cotacaoId != Guid.Empty)
            {
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_cotacao_itens WHERE cotacao_id = @c AND tenant_id = @t",
                    new { c = cotacaoId, t = TenantSantaCasa });
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_cotacao_anexos WHERE cotacao_id = @c AND tenant_id = @t",
                    new { c = cotacaoId, t = TenantSantaCasa });
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_cotacoes WHERE id = @c AND tenant_id = @t",
                    new { c = cotacaoId, t = TenantSantaCasa });
            }

            if (contaId != Guid.Empty)
            {
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_portal_contas WHERE id = @k AND tenant_id = @t",
                    new { k = contaId, t = TenantSantaCasa });
            }
        }
        catch
        {
            // Limpeza nunca deve ocultar as asserções do teste
        }
    }

    // ------------------------------- Facts -------------------------------

    [Fact]
    public async Task TransmissaoManual_PersisteTentaviaComEstadoFinalEEvidencia()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await GarantirSchemaAsync(cs);
            var repo = new CotacoesRepository(cs);
            var (cotacaoId, orcamentoId, respostaId, contaId) = await PrepararRespostaNaFilaAsync(repo, "B5A");

            try
            {
                await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));

                var resp = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.NotNull(resp);
                Assert.Equal("EXPORTADA_MANUALMENTE", resp!.StatusTransmissao);
                Assert.Null(resp.ProtocoloExterno);
                Assert.NotNull(resp.EnviadoEm);
                Assert.Equal(1, resp.Tentativas);
                Assert.NotNull(resp.ExportacaoId);
                Assert.Equal("RESPONDIDA", (await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

                // Evidência persistida: exatamente 1 linha de envio, estado final e canal corretos
                await using var cn = new NpgsqlConnection(cs);
                await cn.OpenAsync();
                Assert.Equal(1, await ContarEnviosAsync(cn, respostaId, TenantSantaCasa));
                var envio = await cn.QuerySingleAsync<dynamic>(@"
                    SELECT * FROM plantaopro.adm360_cotacao_envios
                    WHERE resposta_id = @r AND tenant_id = @t ORDER BY tentativa",
                    new { r = respostaId, t = TenantSantaCasa });
                Assert.Equal(1, (int)envio.tentativa);
                Assert.Equal("EXPORTADA_MANUALMENTE", (string)envio.status_transmissao);
                Assert.Equal("IMPORTACAO_MANUAL", (string)envio.canal);
                Assert.Null((string?)envio.protocolo_externo);
                var finalizadoEm = (DateTimeOffset?)envio.finalizado_em;
                var iniciadoEm = (DateTimeOffset)envio.iniciado_em;
                Assert.NotNull(finalizadoEm);
                Assert.True(finalizadoEm >= iniciadoEm);
                Assert.Equal(UsuarioGestor, (Guid)envio.created_by);
            }
            finally
            {
                await LimparDadosAsync(cs, respostaId, orcamentoId, cotacaoId, contaId);
            }
        }
        catch (NpgsqlException ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o teste B5 não está acessível: {ex.Message}");
        }
    }

    [Fact]
    public async Task TransmissoesSimultaneas_SomenteUmaAvancaENaoDuplicaEnvio()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await GarantirSchemaAsync(cs);
            // Conector lento (1,5 s): o vencedor segura o estado ENVIANDO o tempo suficiente
            // para o perdedor ler a linha sob lock e ser rejeitado, garantindo determinismo.
            var repo = new CotacoesRepository(cs, new IPortalCotacaoConnector[] { new ConectorLento() });
            var (cotacaoId, orcamentoId, respostaId, contaId) = await PrepararRespostaNaFilaAsync(repo, "B5B");

            try
            {
                Task<(bool Ok, string Erro)> Transmitir() => Task.Run(async () =>
                {
                    try
                    {
                        await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));
                        return (Ok: true, Erro: "");
                    }
                    catch (Exception ex)
                    {
                        return (Ok: false, Erro: ex.Message);
                    }
                });

                var tarefas = new[] { Transmitir(), Transmitir() };
                var resultados = await Task.WhenAll(tarefas);

                Assert.True(resultados[0].Ok ^ resultados[1].Ok,
                    $"Esperado exatamente 1 sucesso e 1 rejeição; obtido r1=({resultados[0].Ok}, '{resultados[0].Erro}') r2=({resultados[1].Ok}, '{resultados[1].Erro}')");
                var perdedor = resultados[0].Ok ? resultados[1].Erro : resultados[0].Erro;
                Assert.Contains("em andamento", perdedor, StringComparison.OrdinalIgnoreCase);

                // Estado final: 1 envio persistido, 1 tentativa, canal manual exportado
                var resp = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.True(
                    resp.StatusTransmissao == "EXPORTADA_MANUALMENTE",
                    $"Diagnóstico B5 — Status='{resp.StatusTransmissao}'; MensagemRetorno='{resp.MensagemRetorno}'; Protocolo='{resp.ProtocoloExterno}'; Tentativas={resp.Tentativas}");
                Assert.Equal(1, resp.Tentativas);

                await using var cn = new NpgsqlConnection(cs);
                await cn.OpenAsync();
                Assert.Equal(1, await ContarEnviosAsync(cn, respostaId, TenantSantaCasa));
            }
            finally
            {
                await LimparDadosAsync(cs, respostaId, orcamentoId, cotacaoId, contaId);
            }
        }
        catch (NpgsqlException ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o teste B5 não está acessível: {ex.Message}");
        }
    }

    [Fact]
    public async Task FalhaDeConector_ResultadoDesconhecidoExigeConfirmacaoExplicitaParaRetransmitir()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await GarantirSchemaAsync(cs);
            var repo = new CotacoesRepository(cs, new IPortalCotacaoConnector[] { new ConectorFalha() });
            var (cotacaoId, orcamentoId, respostaId, contaId) = await PrepararRespostaNaFilaAsync(repo, "B5C");

            try
            {
                // 1ª tentativa: exceção do conector vira estado honesto, nunca ENVIANDO órfão
                await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));

                var resp = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.Equal("RESULTADO_DESCONHECIDO", resp!.StatusTransmissao);
                Assert.Null(resp.ProtocoloExterno);
                Assert.Null(resp.EnviadoEm);
                Assert.Contains("Falha técnica na transmissão (exceção do conector): Falha simulada no conector", resp.MensagemRetorno!);
                // Sem protocolo confirmado, a cotação NÃO vira RESPONDIDA
                Assert.Equal("PRONTA_PARA_ENVIO", (await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

                await using var cn = new NpgsqlConnection(cs);
                await cn.OpenAsync();
                Assert.Equal(1, await ContarEnviosAsync(cn, respostaId, TenantSantaCasa));
                var st1 = await cn.ExecuteScalarAsync<string>(
                    "SELECT status_transmissao FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @r AND tentativa = 1",
                    new { r = respostaId });
                Assert.Equal("RESULTADO_DESCONHECIDO", st1);

                // Retransmissão SEM confirmação explícita: regra anti-duplicidade bloqueia e não cria tentativa
                var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                    repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId)));
                Assert.Contains("Confirme explicitamente", ex.Message);
                Assert.Equal(1, await ContarEnviosAsync(cn, respostaId, TenantSantaCasa));

                // Retransmissão COM confirmação explícita: nova tentativa persistida (conector falha de novo)
                await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId, true));

                var resp2 = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.Equal("RESULTADO_DESCONHECIDO", resp2!.StatusTransmissao);
                Assert.Equal(2, resp2.Tentativas);
                Assert.Equal(2, await ContarEnviosAsync(cn, respostaId, TenantSantaCasa));
                var st2 = await cn.ExecuteScalarAsync<string>(
                    "SELECT status_transmissao FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @r AND tentativa = 2",
                    new { r = respostaId });
                Assert.Equal("RESULTADO_DESCONHECIDO", st2);
            }
            finally
            {
                await LimparDadosAsync(cs, respostaId, orcamentoId, cotacaoId, contaId);
            }
        }
        catch (NpgsqlException ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o teste B5 não está acessível: {ex.Message}");
        }
    }

    [Fact]
    public async Task EnvioInterrompido_ReconciliacaoMarcaDesconhecidoETentativaInterrupta_RecentePermanece()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await GarantirSchemaAsync(cs);
            var repo = new CotacoesRepository(cs);
            var (cotacaoId, orcamentoId, respostaId, contaId) = await PrepararRespostaNaFilaAsync(repo, "B5D");

            Guid cotacaoId2 = Guid.Empty, orcamentoId2 = Guid.Empty, respostaId2 = Guid.Empty, contaId2 = Guid.Empty;
            try
            {
                await using var cn = new NpgsqlConnection(cs);
                await cn.OpenAsync();

                // Rodada 1 — crash simulado: resposta ENVIANDO antiga (15 min > limiar 10 min)
                // com a tentativa persistida presa em INICIADA (processo caiu entre marcação e finalização).
                await cn.ExecuteAsync(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET status_transmissao = 'ENVIANDO', tentativas = tentativas + 1, updated_at = now() - interval '15 minutes'
                    WHERE id = @r AND tenant_id = @t", new { r = respostaId, t = TenantSantaCasa });
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_cotacao_envios
                        (tenant_id, resposta_id, tentativa, canal, status_transmissao, iniciado_em, created_by)
                    VALUES (@t, @r, 1, 'IMPORTACAO_MANUAL', 'INICIADA', now() - interval '15 minutes', @u)",
                    new { r = respostaId, t = TenantSantaCasa, u = UsuarioGestor });

                var recuperadas = await Adm360TransmissaoRecovery.ReconciliarEnviandoAncoradosAsync(cs, olderThanMinutes: 10);
                Assert.True(recuperadas >= 1, $"Esperado ao menos 1 resposta recuperada; obtido {recuperadas}");

                var resp = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.Equal("RESULTADO_DESCONHECIDO", resp!.StatusTransmissao);
                Assert.Contains("Recuperacao oficial", resp.MensagemRetorno!);
                var st1 = await cn.ExecuteScalarAsync<string>(
                    "SELECT status_transmissao FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @r AND tentativa = 1",
                    new { r = respostaId });
                Assert.Equal("INTERRUPTA", st1);

                // Rodada 2 — mesmo padrão, segunda tentativa (UNIQUE tenant+resposta+tentativa)
                await cn.ExecuteAsync(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET status_transmissao = 'ENVIANDO', updated_at = now() - interval '12 minutes'
                    WHERE id = @r AND tenant_id = @t", new { r = respostaId, t = TenantSantaCasa });
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_cotacao_envios
                        (tenant_id, resposta_id, tentativa, canal, status_transmissao, iniciado_em, created_by)
                    VALUES (@t, @r, 2, 'IMPORTACAO_MANUAL', 'INICIADA', now() - interval '12 minutes', @u)",
                    new { r = respostaId, t = TenantSantaCasa, u = UsuarioGestor });

                var recuperadas2 = await Adm360TransmissaoRecovery.ReconciliarEnviandoAncoradosAsync(cs, olderThanMinutes: 10);
                Assert.True(recuperadas2 >= 1, $"Esperado ao menos 1 resposta recuperada na rodada 2; obtido {recuperadas2}");
                Assert.Equal("RESULTADO_DESCONHECIDO", (await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId))!.StatusTransmissao);
                var qtdInterruptas = await cn.ExecuteScalarAsync<int>(
                    "SELECT count(*) FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @r AND status_transmissao = 'INTERRUPTA'",
                    new { r = respostaId });
                Assert.Equal(2, qtdInterruptas);

                // Rodada 3 — ENVIANDO recente (transmissão em andamento real) permanece intocado
                (cotacaoId2, orcamentoId2, respostaId2, contaId2) = await PrepararRespostaNaFilaAsync(repo, "B5D2");
                await cn.ExecuteAsync(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET status_transmissao = 'ENVIANDO', tentativas = tentativas + 1, updated_at = now()
                    WHERE id = @r AND tenant_id = @t", new { r = respostaId2, t = TenantSantaCasa });

                await Adm360TransmissaoRecovery.ReconciliarEnviandoAncoradosAsync(cs, olderThanMinutes: 10);
                var statusRecente = await cn.ExecuteScalarAsync<string>(
                    "SELECT status_transmissao FROM plantaopro.adm360_cotacao_respostas WHERE id = @r",
                    new { r = respostaId2 });
                Assert.Equal("ENVIANDO", statusRecente);
                Assert.Equal(0, await ContarEnviosAsync(cn, respostaId2, TenantSantaCasa));
            }
            finally
            {
                await LimparDadosAsync(cs, respostaId2, orcamentoId2, cotacaoId2, contaId2);
                await LimparDadosAsync(cs, respostaId, orcamentoId, cotacaoId, contaId);
            }
        }
        catch (NpgsqlException ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o teste B5 não está acessível: {ex.Message}");
        }
    }

    [Fact]
    public async Task AceitaPeloPortal_BloqueiaRetransmissaoCitandoProtocoloENaoCriaTentativa()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await GarantirSchemaAsync(cs);
            var repo = new CotacoesRepository(cs);
            var (cotacaoId, orcamentoId, respostaId, contaId) = await PrepararRespostaNaFilaAsync(repo, "B5E");

            try
            {
                // Seed via SQL: retorno de aceite registrado pelo portal (estado terminal)
                await using var cn = new NpgsqlConnection(cs);
                await cn.OpenAsync();
                await cn.ExecuteAsync(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET status_transmissao = 'ACEITA_PELO_PORTAL', protocolo_externo = 'PROTO-B5-123',
                        enviado_em = now(), updated_at = now()
                    WHERE id = @r AND tenant_id = @t", new { r = respostaId, t = TenantSantaCasa });

                // Mesmo com confirmação explícita, ACEITA é terminal
                var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                    repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId, true)));
                Assert.Contains("já foi concluída", ex.Message);
                Assert.Contains("PROTO-B5-123", ex.Message);

                // Nenhuma tentativa nova foi criada: o bloqueio acontece antes da persistência
                Assert.Equal(0, await ContarEnviosAsync(cn, respostaId, TenantSantaCasa));
            }
            finally
            {
                await LimparDadosAsync(cs, respostaId, orcamentoId, cotacaoId, contaId);
            }
        }
        catch (NpgsqlException ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o teste B5 não está acessível: {ex.Message}");
        }
    }

    [Fact]
    public async Task OutroTenant_NaoLeNemCriaEnvioDaResposta()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await GarantirSchemaAsync(cs);
            var repo = new CotacoesRepository(cs);
            var (cotacaoId, orcamentoId, respostaId, contaId) = await PrepararRespostaNaFilaAsync(repo, "B5F");

            try
            {
                // Referência: o tenant dono transmite normalmente
                await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));
                Assert.Equal("EXPORTADA_MANUALMENTE", (await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId))!.StatusTransmissao);

                // Outro tenant: resposta fora do escopo -> KeyNotFound, sem nenhum envio criado no seu tenant
                var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                    repo.TransmitirRespostaAsync(TenantIsolado, UsuarioGestor, new TransmitirRespostaCommand(respostaId)));
                Assert.Contains("não encontrado", ex.Message);

                await using var cn = new NpgsqlConnection(cs);
                await cn.OpenAsync();
                Assert.Equal(0, await ContarEnviosAsync(cn, respostaId, TenantIsolado));
                Assert.Equal(1, await ContarEnviosAsync(cn, respostaId, TenantSantaCasa));
            }
            finally
            {
                await LimparDadosAsync(cs, respostaId, orcamentoId, cotacaoId, contaId);
            }
        }
        catch (NpgsqlException ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o teste B5 não está acessível: {ex.Message}");
        }
    }
}
