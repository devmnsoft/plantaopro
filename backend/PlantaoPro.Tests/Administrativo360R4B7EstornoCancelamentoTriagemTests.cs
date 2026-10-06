using System.Diagnostics;
using System.Globalization;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// B7 (Rodada 4) — ciclo de vida da cotação no Administrativo 360:
/// 1. Cancelamento de cotação com motivo obrigatório, momento registrado e idempotência (evento com chave).
/// 2. Estorno de resposta aceita: devolve à fila (NA_FILA/AGUARDANDO_DECISAO) preservando protocolo,
///    tentativas, enviado_em e o histórico de mensagens (apêndice "Estorno (B7)").
/// 3. Anti-duplicidade na aprovação: reaprovar REUSA a mesma resposta (mesmo ID, 1 linha);
///    segunda aprovação pós-aceite é bloqueada pela situação terminal da cotação.
/// 4. Guarda de transmissão em andamento: estorno/cancelamento/transmissão bloqueados enquanto há
///    resposta ENVIANDO (bloqueio transitório, liberado após a conciliação do estado).
/// 5. Expiração lazy por prazo excedido: leitura marca EXPIRADA de forma idempotente e a transmissão
///    bloqueia ANTES de abrir qualquer envio.
/// 6. Finalização honesta: se a resposta foi conciliada por outro escritor durante a transmissão,
///    nenhum campo da resposta é sobrescrito; o envio logado carrega o retorno real do conector +
///    nota de conciliação e a cotação não é marcada RESPONDIDA sem linha efetivamente escrita.
/// 7. Triagem de documentos fiscais em quarentena: responsável + prazo UTC + flag de vencida,
///    reabertura, e resolução que tira da quarentena SEM substituir a conferência (decisões separadas).
/// 8. Revisão de orçamento: nova versão (-V{n}), cotação volta a EM_ORCAMENTO e a aprovação prossegue
///    pela transição EM_ORCAMENTO -> PRONTA_PARA_ENVIO do domínio (sem voltar ao fluxo antigo).
/// Todos os fluxos de escrita lockam na ordem cotação -> resposta (evita deadlock) e registram
/// eventos imutáveis em adm360_eventos.
/// </summary>
[Collection("A360Transmissao")]
public sealed class Administrativo360R4B7EstornoCancelamentoTriagemTests
{
    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid UsuarioGestor = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    // Produto demo do tenant Santa Casa usado para relacionamento De/Para (seed fixo).
    private static readonly Guid ProdutoImpDemo = Guid.Parse("a3610000-0000-4000-8000-000000000002");

    /// <summary>
    /// Provedor de teste: valor válido pelo CHECK da tabela de contas (OPMENEXO/INPART/IMPORTACAO_MANUAL).
    /// A conta é isolada por identificador_externo único por teste (UNIQUE tenant+provedor+identificador).
    /// </summary>
    private const string ProvedorTeste = "OPMENEXO";

    /// <summary>Chave CT-e 57 do fixture b1-d-ct57.xml — fonte para gerar chaves únicas por teste.</summary>
    private const string ChaveCteFixture = "35260911222333000181570010000003030000000303";

    // =====================================================================
    // Garantia de schema: advisory lock serializa migrations entre classes
    // paralelas; retry 40P01 tolera impasse de catálogo durante DDL.
    // =====================================================================
    private static async Task GarantirSchemaAsync(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('adm360_eventos_tests_schema'));");
        try
        {
            // Marcador B7 (v2314). Se ausente, aplica a sequência completa (todas idempotentes);
            // se presente, as anteriores já foram aplicadas pelo runner/outros testes.
            var temV2314 = await cn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS(
                    SELECT 1 FROM information_schema.columns
                     WHERE table_schema = 'plantaopro'
                       AND table_name = 'adm360_documentos_recebidos'
                       AND column_name = 'triagem_responsavel_id'
                );");

            if (!temV2314)
            {
                foreach (var arquivo in new[]
                {
                    "database/migrations/2026_09_v2300_administrativo360_status_transmissao_exportacao.sql",
                    "database/migrations/2026_09_v2304_administrativo360_transmissao_tentativas.sql",
                    "database/migrations/2026_10_v2314_adm360_b7_cotacao_cancelamento_estorno_triagem.sql"
                })
                {
                    var caminho = Path.Combine(RepositoryPathResolver.RepoRoot, arquivo);
                    Assert.True(File.Exists(caminho), $"Migration não encontrada: {arquivo}");
                    await ExecutarMigrationComRetryAsync(cn, await File.ReadAllTextAsync(caminho));
                }
            }
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

    // =====================================================================
    // Stubs de conector: comportamento determinístico por fluxo testado
    // =====================================================================

    /// <summary>Retorno fixo configurável (aceite/rejeição com protocolo).</summary>
    private sealed class StubPortaL : IPortalCotacaoConnector
    {
        public StubPortaL(bool sucesso, string statusTransmissao, string? protocolo, string mensagem)
        {
            Sucesso = sucesso; StatusTransmissao = statusTransmissao; Protocolo = protocolo; Mensagem = mensagem;
        }

        public bool Sucesso { get; }
        public string StatusTransmissao { get; }
        public string? Protocolo { get; }
        public string Mensagem { get; }

        public string Provedor => ProvedorTeste;

        public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default) =>
            Task.FromResult(new PortalConexaoStatusResult(true, "CONFIGURADA", null, DateTime.UtcNow));

        public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());

        public Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default) =>
            Task.FromResult(new EnvioRespostaPortalResult(Sucesso, StatusTransmissao, Protocolo, Mensagem));
    }

    /// <summary>Bloqueia no meio da "chamada externa": permite observar a janela ENVIANDO persistida.</summary>
    private sealed class StubGate : IPortalCotacaoConnector
    {
        private readonly TaskCompletionSource<bool> _entrouNaChamada = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _liberar = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completa quando o conector entra na chamada externa (após ENVIANDO commitado).</summary>
        public Task EntrouNaChamadaExterna => _entrouNaChamada.Task;

        /// <summary>Libera a chamada bloqueada; o conector então devolve resultado desconhecido.</summary>
        public void Liberar() => _liberar.TrySetResult(true);

        public string Provedor => ProvedorTeste;

        public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default) =>
            Task.FromResult(new PortalConexaoStatusResult(true, "CONFIGURADA", null, DateTime.UtcNow));

        public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());

        public async Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default)
        {
            _entrouNaChamada.TrySetResult(true);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20)); // válvula de segurança contra hang de teste
            try { await _liberar.Task.WaitAsync(cts.Token); } catch (OperationCanceledException) { }
            return new EnvioRespostaPortalResult(false, "RESULTADO_DESCONHECIDO", null, "Simulacao de retorno desconhecido apos liberacao do gate.");
        }
    }

    /// <summary>
    /// Finalização honesta (T17): durante a chamada ao portal (fora da transação), um conciliador
    /// concorrente muda a resposta de ENVIANDO para RESULTADO_DESCONHECIDO; o portal então devolve
    /// o aceite — a transação final não pode sobrescrever a resposta nem marcar a cotação RESPONDIDA.
    /// </summary>
    private sealed class StubConcorrenciaHonesto : IPortalCotacaoConnector
    {
        private readonly string _cs;
        private readonly Guid _tenantId;

        public StubConcorrenciaHonesto(string cs, Guid tenantId) { _cs = cs; _tenantId = tenantId; }

        public string Provedor => ProvedorTeste;

        public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default) =>
            Task.FromResult(new PortalConexaoStatusResult(true, "CONFIGURADA", null, DateTime.UtcNow));

        public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());

        public async Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default)
        {
            // O UPDATE roda quando a resposta JÁ está ENVIANDO (txMarca foi commitada antes da chamada).
            await using var cn = new NpgsqlConnection(_cs);
            await cn.OpenAsync(ct);
            await cn.ExecuteAsync(@"
                UPDATE plantaopro.adm360_cotacao_respostas
                   SET status_transmissao = 'RESULTADO_DESCONHECIDO', updated_at = now()
                 WHERE id = @id AND tenant_id = @tenantId AND status_transmissao = 'ENVIANDO'",
                new { id = resposta.Id, tenantId = _tenantId });
            return new EnvioRespostaPortalResult(true, "ACEITA_PELO_PORTAL", "PROTO-HONESTO-B7", "Aceite confirmado pelo portal (retorno chegou durante a transmissao).");
        }
    }

    // =====================================================================
    // Helpers de cenário: conta/cotação únicas por teste (sem interferência)
    // =====================================================================

    private sealed class CenarioB7
    {
        public CenarioB7(CotacoesRepository repo, Guid estabelecimentoId, Guid contaId, string identificador)
        {
            Repo = repo;
            EstabelecimentoId = estabelecimentoId;
            ContaId = contaId;
            Identificador = identificador;
        }

        public CotacoesRepository Repo { get; }
        public Guid EstabelecimentoId { get; }
        public Guid ContaId { get; }
        public string Identificador { get; }

        public Task<Guid> CapturarAsync(DateTime prazoRespostaUtc) =>
            Repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CapturarCotacaoCommand(
                EstabelecimentoId, ContaId, ProvedorTeste, Identificador, 1,
                "Hospital R4 B7", "T.B.", "Cirurgia R4 B7",
                DateOnly.FromDateTime(DateTime.Today.AddDays(10)), prazoRespostaUtc,
                "PORTAL_OFICIAL", "{\"bloco\":\"r4b7\"}",
                new[]
                {
                    new CapturarCotacaoItemCommand(1, "IMP-DEMO", "Produto Importacao Demo", "Fornecedor Demo", "STD", 1, "UN")
                }));

        public async Task<Guid> RelacionarEOrcarAsync(Guid cotacaoId)
        {
            var detalhe = await Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.NotNull(detalhe);
            Assert.Single(detalhe!.Itens);
            await Repo.RelacionarItemAsync(TenantSantaCasa, UsuarioGestor, new RelacionarItemCotacaoCommand(
                detalhe.Itens[0].Id, ProdutoImpDemo, 1.0000m, 120.50m, 0m, "Placa Demo", null, "RELACIONADO", null));
            return await Repo.GerarOrcamentoCirurgicoAsync(TenantSantaCasa, UsuarioGestor, new GerarOrcamentoDaCotacaoCommand(cotacaoId));
        }

        public Task<Guid> AprovarAsync(Guid cotacaoId) =>
            Repo.AprovarRespostaAsync(TenantSantaCasa, UsuarioGestor, new AprovarRespostaCotacaoCommand(cotacaoId));
    }

    private sealed record JornadaB7(CotacoesRepository Repo, Guid ContaId, Guid CotacaoId, Guid RespostaId, Guid OrcamentoId, string Sufixo);

    private static async Task<CenarioB7> CriarCenarioAsync(string cs, IPortalCotacaoConnector conector, string sufixo)
    {
        await GarantirSchemaAsync(cs);
        var repo = new CotacoesRepository(cs, new[] { conector });
        var estabelecimentos = await repo.ListarEstabelecimentosAsync(TenantSantaCasa);
        Assert.NotEmpty(estabelecimentos);

        // Usuário de acesso presente => conta sai CONFIGURADA no upsert.
        var identConta = $"CONTAB7-{sufixo}";
        await repo.ConfigurarContaPortalAsync(TenantSantaCasa, UsuarioGestor, new ConfigurarPortalContaCommand(
            estabelecimentos[0].Id, ProvedorTeste, "Conta Teste R4 B7", identConta, "user-b7-teste", null));

        var contas = await repo.ListarContasPortalAsync(TenantSantaCasa);
        var conta = contas.Single(c => c.Provedor == ProvedorTeste && c.IdentificadorExterno == identConta);
        Assert.Equal("CONFIGURADA", conta.StatusIntegracao);

        return new CenarioB7(repo, estabelecimentos[0].Id, conta.Id, $"B7T-{sufixo}");
    }

    /// <summary>Jornada até a proposta ACEITA pelo portal stub (RESPONDIDA), para os fatos de terminal/bloqueio.</summary>
    private static async Task<JornadaB7> PrepararJornadaAceitaAsync(string cs, string sufixo)
    {
        var cenario = await CriarCenarioAsync(cs, new StubPortaL(true, "ACEITA_PELO_PORTAL", $"PROTO-B7-{sufixo}", "Aceite confirmado pelo portal de teste."), sufixo);
        var cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));
        var orcamentoId = await cenario.RelacionarEOrcarAsync(cotacaoId);
        var respostaId = await cenario.AprovarAsync(cotacaoId);
        await cenario.Repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));
        return new JornadaB7(cenario.Repo, cenario.ContaId, cotacaoId, respostaId, orcamentoId, sufixo);
    }

    /// <summary>Limpeza escopada (best-effort): filhos primeiro; REPLICA ignora triggers de imutabilidade/FK.</summary>
    private static async Task LimparAsync(string cs, Guid contaId, Guid cotacaoId, Guid respostaId, params Guid[] orcamentoIds)
    {
        var oids = orcamentoIds.Where(o => o != Guid.Empty).ToArray();
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            await cn.ExecuteAsync("SET LOCAL session_replication_role = 'REPLICA'", transaction: tx);
            if (respostaId != Guid.Empty)
            {
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @id", new { id = respostaId }, tx);
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_cotacao_respostas WHERE id = @id", new { id = respostaId }, tx);
            }
            if (cotacaoId != Guid.Empty)
            {
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_cotacao_anexos WHERE cotacao_id = @id", new { id = cotacaoId }, tx);
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_cotacao_itens WHERE cotacao_id = @id", new { id = cotacaoId }, tx);
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_cotacoes WHERE id = @id", new { id = cotacaoId }, tx);
            }
            if (oids.Length > 0)
            {
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_orcamento_itens WHERE orcamento_id = ANY(@oids)", new { oids }, tx);
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_orcamentos WHERE id = ANY(@oids)", new { oids }, tx);
            }
            if (contaId != Guid.Empty)
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_portal_contas WHERE id = @id", new { id = contaId }, tx);
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* best-effort */ }
        }
    }

    // =====================================================================
    // Helpers de consulta/estado
    // =====================================================================

    private static async Task AguardarStatusRespostaAsync(string cs, Guid respostaId, string esperado, int timeoutMs = 10000)
    {
        var limite = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < limite)
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var atual = await cn.ExecuteScalarAsync<string?>(@"
                SELECT status_transmissao FROM plantaopro.adm360_cotacao_respostas
                 WHERE id = @id AND tenant_id = @t",
                new { id = respostaId, t = TenantSantaCasa });
            if (atual == esperado) return;
            await Task.Delay(50);
        }
        throw new Xunit.Sdk.XunitException($"Timeout aguardando status_transmissao '{esperado}' da resposta {respostaId}.");
    }

    private static async Task<long> ContarEventosAsync(string cs, Guid entidadeId, string tipoEvento, string entidade, string? idempotencyKey = null)
    {
        var sql = idempotencyKey is null
            ? @"SELECT count(*) FROM plantaopro.adm360_eventos
                 WHERE entidade_id = @e AND tipo_evento = @tipo AND entidade = @ent AND tenant_id = @t AND idempotency_key IS NULL"
            : @"SELECT count(*) FROM plantaopro.adm360_eventos
                 WHERE entidade_id = @e AND tipo_evento = @tipo AND entidade = @ent AND tenant_id = @t AND idempotency_key = @chave";
        object parametros = idempotencyKey is null
            ? new { e = entidadeId, tipo = tipoEvento, ent = entidade, t = TenantSantaCasa }
            : new { e = entidadeId, tipo = tipoEvento, ent = entidade, t = TenantSantaCasa, chave = idempotencyKey };
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<long>(sql, parametros);
    }

    private static async Task<long> ContarRespostasAsync(string cs, Guid cotacaoId)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<long>(@"
            SELECT count(*) FROM plantaopro.adm360_cotacao_respostas
             WHERE cotacao_id = @id AND tenant_id = @t",
            new { id = cotacaoId, t = TenantSantaCasa });
    }

    private static async Task<long> ContarEnviosAsync(string cs, Guid respostaId)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<long>(@"
            SELECT count(*) FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @id",
            new { id = respostaId });
    }

    private static async Task<int> ExecSqlAsync(string cs, string sql, object? param = null)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.ExecuteAsync(sql, param);
    }

    // =====================================================================
    // Documentos em quarentena (triagem): fixture CT-e 57 com chave única por
    // teste — o parser deriva o modelo das posições 20-21 da chave, então 57
    // mantém a quarentena MODELO_NAO_SUPORTADO sem colidir entre testes/classes.
    // =====================================================================

    private static string ChaveCteUnica(string sufixoHex8)
    {
        var ncte = 100_000_000 + (int)(long.Parse(sufixoHex8, NumberStyles.HexNumber) % 900_000_000);
        var chave = $"3526091122233300018157001{ncte:D9}0000000303";
        Debug.Assert(chave.Length == 44);
        return chave;
    }

    private static async Task<(Guid docId, string chave)> ImportarDocumentoQuarentenaAsync(DocumentosXmlRepository repo, string sufixoHex8)
    {
        var textoOriginal = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Administrativo360Xml", "b1-d-ct57.xml"));
        var texto = textoOriginal.Replace(ChaveCteFixture, ChaveCteUnica(sufixoHex8));
        var resumo = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
            new ImportarXmlManualCommand(texto) { NomeArquivo = $"b7-triagem-{sufixoHex8}.xml" });
        Assert.Equal(1, resumo.TotalUnidades);
        Assert.Equal(1, resumo.EmQuarentena);
        var unidade = Assert.Single(resumo.Documentos);
        Assert.True(unidade.Quarentena);
        Assert.Equal("MODELO_NAO_SUPORTADO", unidade.MotivoQuarentena);
        return (unidade.DocumentoId!.Value, unidade.ChaveAcesso);
    }

    private static async Task PurgeDocumentosAsync(string cs, params string[] chaves)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            await cn.ExecuteAsync("SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true);", transaction: tx);
            foreach (var chave in chaves)
            {
                await cn.ExecuteAsync(@"
                    DELETE FROM plantaopro.adm360_documento_eventos
                     WHERE documento_id IN (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = @chave);
                    DELETE FROM plantaopro.adm360_documento_itens
                     WHERE documento_id IN (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = @chave);
                    DELETE FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = @chave;",
                    new { chave }, tx);
            }
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* best-effort */ }
        }
    }

    // =====================================================================
    // Fatos
    // =====================================================================

    [Fact(DisplayName = "B7/T01: jornada aceitar -> estornar -> reaproveitar a MESMA resposta -> retransmitir (2 tentativas) -> RESPONDIDA, com auditoria de eventos")]
    public async Task B7_T01_Jornada_Aceitar_Estornar_Reaproveitar_RetornaAMesmaRespostaECicloCompleto()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty, respostaId = Guid.Empty;
        try
        {
            var cenario = await CriarCenarioAsync(cs, new StubPortaL(true, "ACEITA_PELO_PORTAL", $"PROTO-B7-{sufixo}", "Aceite confirmado pelo portal de teste."), sufixo);
            contaId = cenario.ContaId;

            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));
            var orcamentoId = await cenario.RelacionarEOrcarAsync(cotacaoId);

            // Aprovação cria a ÚNICA resposta em NA_FILA e move a cotação para PRONTA_PARA_ENVIO.
            respostaId = await cenario.AprovarAsync(cotacaoId);
            var r1 = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.NotNull(r1);
            Assert.Equal("NA_FILA", r1!.StatusTransmissao);
            Assert.Equal(orcamentoId, r1.OrcamentoId);
            Assert.Equal("PRONTA_PARA_ENVIO", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

            // Transmissão via portal stub: aceite + cotação RESPONDIDA.
            await cenario.Repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));
            var r2 = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.Equal("ACEITA_PELO_PORTAL", r2!.StatusTransmissao);
            Assert.Equal($"PROTO-B7-{sufixo}", r2.ProtocoloExterno);
            Assert.Equal(1, r2.Tentativas);
            Assert.NotNull(r2.EnviadoEm);
            Assert.Equal("RESPONDIDA", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

            // ESTORNO: volta a NA_FILA preservando histórico (protocolo, tentativas, enviado_em; mensagem apendida).
            await cenario.Repo.EstornarRespostaAsync(TenantSantaCasa, UsuarioGestor, new EstornarRespostaCommand(respostaId, "Estorno para novo ciclo de teste."));
            var r3 = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.Equal("NA_FILA", r3!.StatusTransmissao);
            Assert.Equal("AGUARDANDO_DECISAO", r3.StatusComercialExterno);
            Assert.Equal($"PROTO-B7-{sufixo}", r3.ProtocoloExterno);
            Assert.Equal(1, r3.Tentativas);
            Assert.Contains("Estorno (B7): Estorno para novo ciclo de teste.", r3.MensagemRetorno);
            Assert.Equal("PRONTA_PARA_ENVIO", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

            // Reaprovação REUSA a mesma resposta (anti-duplicidade): mesmo ID, continua 1 linha.
            var respostaReaprovada = await cenario.AprovarAsync(cotacaoId);
            Assert.Equal(respostaId, respostaReaprovada);
            Assert.Equal(1L, await ContarRespostasAsync(cs, cotacaoId));
            Assert.Equal("NA_FILA", (await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId))!.StatusTransmissao);

            // Retransmissão volta a fechar: tentativas incrementa e cotação RESPONDIDA de novo.
            await cenario.Repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));
            var r5 = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.Equal("ACEITA_PELO_PORTAL", r5!.StatusTransmissao);
            Assert.Equal(2, r5.Tentativas);
            Assert.Equal("RESPONDIDA", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

            // Auditoria imutável: 1 APROVACAO com chave (criação) + 1 sem chave (reuso);
            // 1 ESTORNO sem chave; 2 RETORNO_EXTERNO (um por tentativa).
            Assert.Equal(1L, await ContarEventosAsync(cs, respostaId, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", $"aprovacao:resposta:{respostaId:N}"));
            Assert.Equal(1L, await ContarEventosAsync(cs, respostaId, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", null));
            Assert.Equal(1L, await ContarEventosAsync(cs, respostaId, Adm360TipoEvento.Estorno, "COTACAO_RESPOSTA", null));
            Assert.Equal(2L, await ContarEventosAsync(cs, respostaId, Adm360TipoEvento.RetornoExterno, "COTACAO_RESPOSTA", null));
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, respostaId);
        }
    }

    [Fact(DisplayName = "B7/T02: estorno bloqueado durante ENVIANDO (transitório) e liberado após a conciliação do estado")]
    public async Task B7_T02_Estorno_BloqueadoEnquantoTransmissaoEnviando_E_LiberadoDepois()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty, respostaId = Guid.Empty;
        try
        {
            var gate = new StubGate();
            var cenario = await CriarCenarioAsync(cs, gate, sufixo);
            contaId = cenario.ContaId;
            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));
            await cenario.RelacionarEOrcarAsync(cotacaoId);
            respostaId = await cenario.AprovarAsync(cotacaoId);

            var emTransito = cenario.Repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));
            await gate.EntrouNaChamadaExterna;
            await AguardarStatusRespostaAsync(cs, respostaId, "ENVIANDO");

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                cenario.Repo.EstornarRespostaAsync(TenantSantaCasa, UsuarioGestor, new EstornarRespostaCommand(respostaId, "Estorno durante voo.")));
            Assert.StartsWith("Esta resposta está com transmissão em andamento", ex.Message);

            gate.Liberar();
            await emTransito;

            var r = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.Equal("RESULTADO_DESCONHECIDO", r!.StatusTransmissao);
            Assert.DoesNotContain("Estorno (B7)", r.MensagemRetorno ?? string.Empty);

            // Estado conciliado (não é mais ENVIANDO): o estorno volta a ser permitido.
            await cenario.Repo.EstornarRespostaAsync(TenantSantaCasa, UsuarioGestor, new EstornarRespostaCommand(respostaId, "Estorno apos conciliacao."));
            var r2 = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.Equal("NA_FILA", r2!.StatusTransmissao);
            Assert.Contains("Estorno (B7): Estorno apos conciliacao.", r2.MensagemRetorno);
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, respostaId);
        }
    }

    [Fact(DisplayName = "B7/T03: motivo do cancelamento e justificativa do estorno são validados ANTES de tocar no banco")]
    public async Task B7_T03_CancelarSemMotivo_E_EstornarSemJustificativa_FalhaSemTocarNoBanco()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        var repo = new CotacoesRepository(cs, new[] { new StubPortaL(true, "ACEITA_PELO_PORTAL", "PROTO-X", "ok") });

        var exC = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            repo.CancelarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CancelarCotacaoCommand(Guid.NewGuid(), "   ")));
        Assert.Equal("O motivo do cancelamento da cotação é obrigatório.", exC.Message);

        var exE = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            repo.EstornarRespostaAsync(TenantSantaCasa, UsuarioGestor, new EstornarRespostaCommand(Guid.NewGuid(), "")));
        Assert.Equal("A justificativa do estorno é obrigatória.", exE.Message);
    }

    [Fact(DisplayName = "B7/T04: cancelar cotação RECEBIDA registra CANCELADA + motivo + momento; repetir é idempotente (1 evento com chave, sem sobrescrever)")]
    public async Task B7_T04_CancelarCotacaoRecebida_RegistraEstadoTerminale_Idempotente()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty;
        try
        {
            var cenario = await CriarCenarioAsync(cs, new StubPortaL(true, "ACEITA_PELO_PORTAL", $"PROTO-B7-{sufixo}", "ok"), sufixo);
            contaId = cenario.ContaId;

            // Cotação inexistente: 404 antes de qualquer escrita.
            var ex404 = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                cenario.Repo.CancelarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CancelarCotacaoCommand(Guid.NewGuid(), "Nao existe.")));
            Assert.Contains("não encontrada", ex404.Message);

            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2)); // RECEBIDA

            await cenario.Repo.CancelarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CancelarCotacaoCommand(cotacaoId, "Cirurgia desmarcada pelo hospital."));
            var d = await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.Equal("CANCELADA", d!.StatusInterno);
            Assert.Equal("Cirurgia desmarcada pelo hospital.", d.MotivoCancelamento);
            Assert.NotNull(d.CanceladoEm);

            // Re-cancelamento: idempotente — sem exceção, sem sobrescrita, sem evento duplicado.
            var primeiroCancelamentoEm = d.CanceladoEm!.Value;
            await cenario.Repo.CancelarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CancelarCotacaoCommand(cotacaoId, "Outro motivo (nao deve sobrescrever)."));
            var d2 = await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.Equal(primeiroCancelamentoEm, d2!.CanceladoEm);
            Assert.Equal("Cirurgia desmarcada pelo hospital.", d2.MotivoCancelamento);

            Assert.Equal(1L, await ContarEventosAsync(cs, cotacaoId, Adm360TipoEvento.Cancelamento, "COTACAO", $"cancelamento:cotacao:{cotacaoId:N}"));
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, Guid.Empty);
        }
    }

    [Fact(DisplayName = "B7/T05: cancelamento é bloqueado em situação terminal (RESPONDIDA)")]
    public async Task B7_T05_CancelarBloqueadoQuandoCotacaoRespondida()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var jornada = await PrepararJornadaAceitaAsync(cs, sufixo);
        try
        {
            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                jornada.Repo.CancelarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CancelarCotacaoCommand(jornada.CotacaoId, "Tarde demais.")));
            Assert.Equal("Cotação na situação 'RESPONDIDA' não permite alteração de status.", ex.Message);

            var d = await jornada.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, jornada.CotacaoId);
            Assert.Equal("RESPONDIDA", d!.StatusInterno);
            Assert.Null(d.CanceladoEm);
            Assert.Null(d.MotivoCancelamento);
        }
        finally
        {
            await LimparAsync(cs, jornada.ContaId, jornada.CotacaoId, jornada.RespostaId, jornada.OrcamentoId);
        }
    }

    [Fact(DisplayName = "B7/T06: cancelamento bloqueado com resposta ENVIANDO (transitório) e liberado após a conciliação")]
    public async Task B7_T06_CancelarBloqueadoComRespostaEnviando_E_LiberadoAposConciliacao()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty, respostaId = Guid.Empty;
        try
        {
            var gate = new StubGate();
            var cenario = await CriarCenarioAsync(cs, gate, sufixo);
            contaId = cenario.ContaId;
            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));
            var orcamentoId = await cenario.RelacionarEOrcarAsync(cotacaoId);
            respostaId = await cenario.AprovarAsync(cotacaoId);

            var emTransito = cenario.Repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));
            await gate.EntrouNaChamadaExterna;
            await AguardarStatusRespostaAsync(cs, respostaId, "ENVIANDO");

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                cenario.Repo.CancelarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CancelarCotacaoCommand(cotacaoId, "Cancelar durante voo.")));
            Assert.StartsWith("Não é possível cancelar a cotação com transmissão em andamento", ex.Message);

            var d = await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.Equal("PRONTA_PARA_ENVIO", d!.StatusInterno);
            Assert.Null(d.CanceladoEm);

            gate.Liberar();
            await emTransito;

            // Estado conciliado (RESULTADO_DESCONHECIDO, não ENVIANDO): o cancelamento desbloqueia.
            await cenario.Repo.CancelarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CancelarCotacaoCommand(cotacaoId, "Cancelado apos conciliacao do envio."));
            Assert.Equal("CANCELADA", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);
            await LimparAsync(cs, contaId, cotacaoId, respostaId, orcamentoId);
            contaId = cotacaoId = respostaId = Guid.Empty; // já limpo
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, respostaId);
        }
    }

    [Fact(DisplayName = "B7/T07: prazo excedido marca EXPIRADA na leitura (lazy) e a operação é idempotente")]
    public async Task B7_T07_PrazoExcedido_MarcaExpiradaLazyNoLeituraE_Idempotente()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty;
        try
        {
            var cenario = await CriarCenarioAsync(cs, new StubPortaL(true, "ACEITA_PELO_PORTAL", $"PROTO-B7-{sufixo}", "ok"), sufixo);
            contaId = cenario.ContaId;
            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2)); // prazo futuro
            Assert.Equal("RECEBIDA", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

            // Simula o decurso do prazo sem tocar na coluna de status.
            await ExecSqlAsync(cs,
                "UPDATE plantaopro.adm360_cotacoes SET prazo_resposta = now() - interval '5 minutes', updated_at = now() WHERE id = @id",
                new { id = cotacaoId });

            var d = await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.Equal("EXPIRADA", d!.StatusInterno);

            var d2 = await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.Equal("EXPIRADA", d2.StatusInterno);
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, Guid.Empty);
        }
    }

    [Fact(DisplayName = "B7/T08: transmissão é bloqueada em cotação CANCELADA (antes de abrir qualquer envio)")]
    public async Task B7_T08_TransmitirBloqueadoQuandoCotacaoCancelada()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty, respostaId = Guid.Empty;
        try
        {
            var cenario = await CriarCenarioAsync(cs, new StubPortaL(true, "ACEITA_PELO_PORTAL", $"PROTO-B7-{sufixo}", "ok"), sufixo);
            contaId = cenario.ContaId;
            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));
            var orcamentoId = await cenario.RelacionarEOrcarAsync(cotacaoId);
            respostaId = await cenario.AprovarAsync(cotacaoId);

            await cenario.Repo.CancelarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CancelarCotacaoCommand(cotacaoId, "Desmarcada."));

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                cenario.Repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId)));
            Assert.Equal("A cotação vinculada foi cancelada internamente e não pode ser transmitida.", ex.Message);

            // Resposta intacta e nenhum envio aberto.
            var r = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.Equal("NA_FILA", r!.StatusTransmissao);
            Assert.Equal(0, r.Tentativas);
            Assert.Equal(0L, await ContarEnviosAsync(cs, respostaId));
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, respostaId);
        }
    }

    [Fact(DisplayName = "B7/T09: transmissão bloqueia em prazo excedido (EXPIRADA marcada) ANTES de abrir envio; resposta intacta")]
    public async Task B7_T09_TransmitirBloqueadoQuandoPrazoExcedeu()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty, respostaId = Guid.Empty;
        try
        {
            var cenario = await CriarCenarioAsync(cs, new StubPortaL(true, "ACEITA_PELO_PORTAL", $"PROTO-B7-{sufixo}", "ok"), sufixo);
            contaId = cenario.ContaId;
            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));
            await cenario.RelacionarEOrcarAsync(cotacaoId);
            respostaId = await cenario.AprovarAsync(cotacaoId);

            await ExecSqlAsync(cs,
                "UPDATE plantaopro.adm360_cotacoes SET prazo_resposta = now() - interval '5 minutes', updated_at = now() WHERE id = @id",
                new { id = cotacaoId });

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                cenario.Repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId)));
            Assert.StartsWith("O prazo de resposta da cotação expirou em", ex.Message);
            Assert.EndsWith("UTC.", ex.Message);

            // Expiração lazy registrada no caminho do próprio envio.
            Assert.Equal("EXPIRADA", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

            var r = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.Equal("NA_FILA", r!.StatusTransmissao);
            Assert.Equal(0, r.Tentativas);
            Assert.Equal(0L, await ContarEnviosAsync(cs, respostaId));
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, respostaId);
        }
    }

    [Fact(DisplayName = "B7/T10: anti-duplicidade sequencial na aprovação — mesmo ID, 1 linha, evento de criação com chave + reuso sem chave")]
    public async Task B7_T10_AprovarDuasVezes_RetornaAMesmaRespostaESemDuplicar()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty, respostaId = Guid.Empty;
        try
        {
            var cenario = await CriarCenarioAsync(cs, new StubPortaL(true, "ACEITA_PELO_PORTAL", $"PROTO-B7-{sufixo}", "ok"), sufixo);
            contaId = cenario.ContaId;
            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));
            await cenario.RelacionarEOrcarAsync(cotacaoId);

            var r1 = await cenario.AprovarAsync(cotacaoId);
            var r2 = await cenario.AprovarAsync(cotacaoId);
            Assert.Equal(r1, r2);
            Assert.Equal(1L, await ContarRespostasAsync(cs, cotacaoId));

            Assert.Equal(1L, await ContarEventosAsync(cs, r1, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", $"aprovacao:resposta:{r1:N}"));
            Assert.Equal(1L, await ContarEventosAsync(cs, r1, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", null));

            respostaId = r1;
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, respostaId);
        }
    }

    [Fact(DisplayName = "B7/T11: segunda aprovação após aceite é bloqueada pela situação terminal (RESPONDIDA), sem duplicar resposta")]
    public async Task B7_T11_AprovarApósAceite_E_BloqueadaPelaSituacaoTerminal()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var jornada = await PrepararJornadaAceitaAsync(cs, sufixo);
        try
        {
            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                jornada.Repo.AprovarRespostaAsync(TenantSantaCasa, UsuarioGestor, new AprovarRespostaCotacaoCommand(jornada.CotacaoId)));
            Assert.Equal("Cotação na situação 'RESPONDIDA' não permite alteração de status.", ex.Message);
            Assert.Equal(1L, await ContarRespostasAsync(cs, jornada.CotacaoId));
        }
        finally
        {
            await LimparAsync(cs, jornada.ContaId, jornada.CotacaoId, jornada.RespostaId, jornada.OrcamentoId);
        }
    }

    [Fact(DisplayName = "B7/T12: estorno isolado de resposta ACEITA — preserva protocolo/tentativas/enviado_em e apêndice de histórico")]
    public async Task B7_T12_EstornoDeRespostaAceita_PreservaHistoricoEAbreNovoCiclo()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var jornada = await PrepararJornadaAceitaAsync(cs, sufixo);
        try
        {
            await jornada.Repo.EstornarRespostaAsync(TenantSantaCasa, UsuarioGestor, new EstornarRespostaCommand(jornada.RespostaId, "Erro na proposta aceita."));
            var r = await jornada.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, jornada.RespostaId);
            Assert.Equal("NA_FILA", r!.StatusTransmissao);
            Assert.Equal("AGUARDANDO_DECISAO", r.StatusComercialExterno);
            Assert.Equal($"PROTO-B7-{sufixo}", r.ProtocoloExterno);            // preservado
            Assert.NotNull(r.EnviadoEm);                                      // preservado
            Assert.Equal(1, r.Tentativas);                                    // não zerado
            Assert.Contains("Aceite confirmado pelo portal de teste.", r.MensagemRetorno);   // história anterior
            Assert.Contains("| Estorno (B7): Erro na proposta aceita.", r.MensagemRetorno);   // apêndice

            Assert.Equal("PRONTA_PARA_ENVIO", (await jornada.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, jornada.CotacaoId))!.StatusInterno);
            Assert.Equal(1L, await ContarEventosAsync(cs, jornada.RespostaId, Adm360TipoEvento.Estorno, "COTACAO_RESPOSTA", null));
        }
        finally
        {
            await LimparAsync(cs, jornada.ContaId, jornada.CotacaoId, jornada.RespostaId, jornada.OrcamentoId);
        }
    }

    [Fact(DisplayName = "B7/T13: ciclo completo de triagem — abrir -> listar -> reabrir -> vencida -> resolver (sai da quarentena, conferência intacta, 3 eventos)")]
    public async Task B7_T13_Triagem_CicloCompleto_AbrirListarReabrirVencidaResolver()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var (docId, chave) = (Guid.Empty, string.Empty);
        try
        {
            await GarantirSchemaAsync(cs);
            var repoDoc = new DocumentosXmlRepository(cs);
            (docId, chave) = await ImportarDocumentoQuarentenaAsync(repoDoc, sufixo);

            // Abertura: responsável + prazo futuro + observação.
            await repoDoc.AbrirTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                new AbrirTriagemDocumentoCommand(docId, UsuarioGestor, DateTime.UtcNow.AddHours(1), "Conferir com o fornecedor."));

            var lista1 = (await repoDoc.ListarTriagensAbertasAsync(TenantSantaCasa))
                .Where(x => x.DocumentoId == docId).ToList();
            var item = Assert.Single(lista1);
            Assert.False(item.Vencida);
            Assert.NotNull(item.TriagemAbertaEm);
            Assert.NotNull(item.TriagemPrazo);
            Assert.NotNull(item.ResponsavelNome);
            Assert.Null(item.NomeArquivo); // a central não persiste nome de arquivo para estes documentos

            // Reabertura: fato distinto (atualiza campos, limpa resolvida_em, novo evento).
            await repoDoc.AbrirTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                new AbrirTriagemDocumentoCommand(docId, UsuarioGestor, DateTime.UtcNow.AddHours(2), null));

            // Vencida derivada: SQL empurra o prazo para o passado.
            await ExecSqlAsync(cs,
                "UPDATE plantaopro.adm360_documentos_recebidos SET triagem_prazo = now() - interval '1 hour', updated_at = now() WHERE id = @id",
                new { id = docId });
            var lista2 = (await repoDoc.ListarTriagensAbertasAsync(TenantSantaCasa)).Single(x => x.DocumentoId == docId);
            Assert.True(lista2.Vencida);

            // O importador grava DIVERGENTE para documentos em quarentena; a resolução NÃO toca nela.
            var conferenciaInicial = "DIVERGENTE";

            // Resolução: sai da quarentena; status_conferencia permanece inalterada (decisão separada).
            await repoDoc.ResolverTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                new ResolverTriagemDocumentoCommand(docId, "Documento verificado pelo responsável."));

            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var final = await cn.QuerySingleAsync<dynamic>(@"
                    SELECT d.quarentena, d.motivo_quarentena, d.triagem_resolvida_em, d.status_conferencia
                      FROM plantaopro.adm360_documentos_recebidos d
                     WHERE d.id = @id AND d.tenant_id = @t",
                    new { id = docId, t = TenantSantaCasa });
                Assert.False((bool)final.quarentena);
                Assert.Null(final.motivo_quarentena);
                Assert.NotNull(final.triagem_resolvida_em);
                Assert.Equal(conferenciaInicial, (string)final.status_conferencia);
            }

            // Documente resolvido desaparece da lista aberta.
            var lista3 = await repoDoc.ListarTriagensAbertasAsync(TenantSantaCasa);
            Assert.DoesNotContain(lista3, x => x.DocumentoId == docId);

            // 3 eventos TRIAGEM sem chave (abrir, reabrir, resolver).
            Assert.Equal(3L, await ContarEventosAsync(cs, docId, Adm360TipoEvento.Triagem, "DOCUMENTO_XML", null));
        }
        finally
        {
            if (chave.Length > 0) await PurgeDocumentosAsync(cs, chave);
        }
    }

    [Fact(DisplayName = "B7/T14: guards negativos da triagem — resolver sem responsável, responsável desconhecido, prazo no passado, fora de quarentena")]
    public async Task B7_T14_Triagem_GuardsNegativos_BloqueiamComMensagemEspecifica()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixoA = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var sufixoB = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var (docA, chaveA) = (Guid.Empty, string.Empty);
        var (docB, chaveB) = (Guid.Empty, string.Empty);
        try
        {
            await GarantirSchemaAsync(cs);
            var repoDoc = new DocumentosXmlRepository(cs);
            (docA, chaveA) = await ImportarDocumentoQuarentenaAsync(repoDoc, sufixoA);
            (docB, chaveB) = await ImportarDocumentoQuarentenaAsync(repoDoc, sufixoB);

            // Resolver sem responsável atribuído.
            var exSemResp = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repoDoc.ResolverTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ResolverTriagemDocumentoCommand(docA, "Sem responsavel.")));
            Assert.Equal("Apenas triagem com responsável atribuído pode ser resolvida (abra a triagem antes).", exSemResp.Message);

            // Responsável desconhecido (GUID aleatório não existe no tenant).
            var exResp = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repoDoc.AbrirTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                    new AbrirTriagemDocumentoCommand(docB, Guid.NewGuid(), DateTime.UtcNow.AddHours(1), null)));
            Assert.Equal("O responsável informado não existe ou não pertence ao tenant atual.", exResp.Message);

            // Prazo no passado.
            var exPrazo = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repoDoc.AbrirTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                    new AbrirTriagemDocumentoCommand(docB, UsuarioGestor, DateTime.UtcNow.AddMinutes(-5), null)));
            Assert.Equal("O prazo da triagem deve ser um momento futuro em UTC.", exPrazo.Message);

            // Abrir + resolver docB; depois ambos fora de quarentena devem bloquear.
            await repoDoc.AbrirTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                new AbrirTriagemDocumentoCommand(docB, UsuarioGestor, DateTime.UtcNow.AddHours(1), null));
            await repoDoc.ResolverTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                new ResolverTriagemDocumentoCommand(docB, "Ok."));

            var exReabrir = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repoDoc.AbrirTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                    new AbrirTriagemDocumentoCommand(docB, UsuarioGestor, DateTime.UtcNow.AddHours(2), null)));
            Assert.Equal("A triagem só pode ser aberta para documentos em quarentena.", exReabrir.Message);

            var exResolver = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repoDoc.ResolverTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ResolverTriagemDocumentoCommand(docB, "De novo.")));
            Assert.Equal("O documento não está em quarentena; não há triagem para resolver.", exResolver.Message);
        }
        finally
        {
            var chaves = new[] { chaveA, chaveB }.Where(c => c.Length > 0).ToArray();
            if (chaves.Length > 0) await PurgeDocumentosAsync(cs, chaves);
        }
    }

    [Fact(DisplayName = "B7/T15: lista de triagens ordena por prazo (menor primeiro) e deriva a flag de vencida")]
    public async Task B7_T15_Triagem_ListagemOrdenaPorPrazo()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixoA = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var sufixoB = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var (docA, chaveA) = (Guid.Empty, string.Empty);
        var (docB, chaveB) = (Guid.Empty, string.Empty);
        try
        {
            await GarantirSchemaAsync(cs);
            var repoDoc = new DocumentosXmlRepository(cs);
            (docA, chaveA) = await ImportarDocumentoQuarentenaAsync(repoDoc, sufixoA);
            (docB, chaveB) = await ImportarDocumentoQuarentenaAsync(repoDoc, sufixoB);

            await repoDoc.AbrirTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                new AbrirTriagemDocumentoCommand(docA, UsuarioGestor, DateTime.UtcNow.AddHours(3), null));
            await repoDoc.AbrirTriagemDocumentoAsync(TenantSantaCasa, UsuarioGestor,
                new AbrirTriagemDocumentoCommand(docB, UsuarioGestor, DateTime.UtcNow.AddHours(1), null));

            var meus = (await repoDoc.ListarTriagensAbertasAsync(TenantSantaCasa))
                .Where(x => x.DocumentoId == docA || x.DocumentoId == docB).ToList();
            Assert.Equal(2, meus.Count);
            Assert.Equal(docB, meus[0].DocumentoId); // menor prazo primeiro
            Assert.Equal(docA, meus[1].DocumentoId);
            Assert.All(meus, x => Assert.False(x.Vencida));
        }
        finally
        {
            var chaves = new[] { chaveA, chaveB }.Where(c => c.Length > 0).ToArray();
            if (chaves.Length > 0) await PurgeDocumentosAsync(cs, chaves);
        }
    }

    [Fact(DisplayName = "B7/T16: revisão de orçamento gera -V2 + EM_ORCAMENTO; aprovação segue pela transição EM_ORCAMENTO->PRONTA_PARA_ENVIO; revisão fora de fase bloqueia")]
    public async Task B7_T16_RevisaoDeOrcamento_NovaVersaoETransicaoDiretaParaAprovacao()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty, respostaId = Guid.Empty, orcamentoId1 = Guid.Empty, orcamentoId2 = Guid.Empty;
        try
        {
            var cenario = await CriarCenarioAsync(cs, new StubPortaL(true, "ACEITA_PELO_PORTAL", $"PROTO-B7-{sufixo}", "ok"), sufixo);
            contaId = cenario.ContaId;
            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));

            orcamentoId1 = await cenario.RelacionarEOrcarAsync(cotacaoId);
            var d1 = await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.Equal("AGUARDANDO_APROVACAO", d1!.StatusInterno);
            Assert.Equal($"ORC-COT-B7T-{sufixo}-R1", d1.OrcamentoNumero);

            // Revisão: nova versão, cotação volta a EM_ORCAMENTO para ajuste.
            orcamentoId2 = await cenario.Repo.GerarOrcamentoCirurgicoAsync(TenantSantaCasa, UsuarioGestor,
                new GerarOrcamentoDaCotacaoCommand(cotacaoId, Revisar: true));
            Assert.NotEqual(orcamentoId1, orcamentoId2);
            var d2 = await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.Equal("EM_ORCAMENTO", d2!.StatusInterno);
            Assert.Equal(orcamentoId2, d2.OrcamentoId);
            Assert.Equal($"ORC-COT-B7T-{sufixo}-R1-V2", d2.OrcamentoNumero);

            // Transição B7: aprovar diretamente de EM_ORCAMENTO; snapshot vincula a nova versão.
            respostaId = await cenario.AprovarAsync(cotacaoId);
            Assert.Equal("PRONTA_PARA_ENVIO", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);
            Assert.Equal(orcamentoId2, (await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId))!.OrcamentoId);

            // Revisão fora de fase (PRONTA_PARA_ENVIO já não permite ajuste).
            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                cenario.Repo.GerarOrcamentoCirurgicoAsync(TenantSantaCasa, UsuarioGestor,
                    new GerarOrcamentoDaCotacaoCommand(cotacaoId, Revisar: true)));
            Assert.Equal("Revisão de orçamento somente é permitida nas fases AGUARDANDO_APROVACAO ou EM_ORCAMENTO (fase atual: PRONTA_PARA_ENVIO).", ex.Message);
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, respostaId, orcamentoId1, orcamentoId2);
        }
    }

    [Fact(DisplayName = "B7/T17: finalização honesta — resposta conciliada por outro escritor durante a transmissão não é sobrescrita; envio loga o retorno real + cotação não vira RESPONDIDA")]
    public async Task B7_T17_FinalizacaoHonesta_NaRespostaConciliadaDuranteATransmissao()
    {
        var cs = TestDatabase.ConnectionString;
        var sufixo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid contaId = Guid.Empty, cotacaoId = Guid.Empty, respostaId = Guid.Empty;
        try
        {
            var stub = new StubConcorrenciaHonesto(cs, TenantSantaCasa);
            var cenario = await CriarCenarioAsync(cs, stub, sufixo);
            contaId = cenario.ContaId;
            cotacaoId = await cenario.CapturarAsync(DateTime.UtcNow.AddDays(2));
            await cenario.RelacionarEOrcarAsync(cotacaoId);
            respostaId = await cenario.AprovarAsync(cotacaoId);

            await cenario.Repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));

            // A resposta manteve o estado conciliado — nenhum campo foi sobrescrito pelo txFinal.
            var r = await cenario.Repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.Equal("RESULTADO_DESCONHECIDO", r!.StatusTransmissao);
            Assert.Null(r.ProtocoloExterno); // o atrasado não chegou à resposta

            // O envio logado registra o retorno REAL do conector + a nota de conciliação.
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var envio = await cn.QuerySingleAsync<dynamic>(@"
                    SELECT e.status_transmissao, e.mensagem
                      FROM plantaopro.adm360_cotacao_envios e
                     WHERE e.resposta_id = @id
                     ORDER BY e.tentativa DESC LIMIT 1",
                    new { id = respostaId });
                Assert.Equal("ACEITA_PELO_PORTAL", (string)envio.status_transmissao);
                Assert.Contains("Finalizacao honesta (B7)", (string)envio.mensagem);
                Assert.Contains("RESULTADO_DESCONHECIDO", (string)envio.mensagem);
                Assert.Contains("nenhum campo da resposta foi sobrescrito", (string)envio.mensagem);
            }

            // Cotação NÃO virou RESPONDIDA (nenhuma linha de resposta foi efetivamente escrita).
            Assert.Equal("PRONTA_PARA_ENVIO", (await cenario.Repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

            // Evento RETORNO_EXTERNO carrega o estado conciliado para auditoria.
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var dados = await cn.ExecuteScalarAsync<string?>(@"
                    SELECT dados::text FROM plantaopro.adm360_eventos
                     WHERE entidade_id = @r AND tipo_evento = 'RETORNO_EXTERNO' AND tenant_id = @t",
                    new { r = respostaId, t = TenantSantaCasa });
                Assert.NotNull(dados);
                // Parsed (sem depender do formato exato de renderização do jsonb).
                using var doc = System.Text.Json.JsonDocument.Parse(dados!);
                Assert.Equal("RESULTADO_DESCONHECIDO", doc.RootElement.GetProperty("estado_resposta_conciliado").GetString());
            }
        }
        finally
        {
            await LimparAsync(cs, contaId, cotacaoId, respostaId);
        }
    }

    [Fact(DisplayName = "B7/T18 (unit): matriz de transições — EM_ORCAMENTO->PRONTA_PARA_ENVIO liberada; terminais e saltos inválidos bloqueiam")]
    public void B7_T18_Unit_MatrizDeTransicoes_EM_OrcamentoParaProntaE_TerminaisBloqueiam()
    {
        // B7: a revisão de orçamento mantém a fase de ajuste e libera a aprovação direta.
        CotacaoRegras.ValidarTransicaoStatus("EM_ORCAMENTO", "PRONTA_PARA_ENVIO");

        var exTerminal = Assert.Throws<Administrativo360BusinessException>(() =>
            CotacaoRegras.ValidarTransicaoStatus("RESPONDIDA", "PRONTA_PARA_ENVIO"));
        Assert.Equal("Cotação na situação 'RESPONDIDA' não permite alteração de status.", exTerminal.Message);

        var exInvalida = Assert.Throws<Administrativo360BusinessException>(() =>
            CotacaoRegras.ValidarTransicaoStatus("RECEBIDA", "PRONTA_PARA_ENVIO"));
        Assert.Contains("'RECEBIDA' -> 'PRONTA_PARA_ENVIO'", exInvalida.Message);
    }
}
