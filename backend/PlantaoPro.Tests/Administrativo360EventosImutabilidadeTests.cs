using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R4-A5: guarda de fim de coleção — se alguma limpeza de jornada/evento/documento
/// falhou durante a coleção A360Transmissao (antes engolidas em silêncio), a rodada falha
/// aqui explicitamente em vez de virar flake na rodada seguinte.
/// </summary>
internal sealed class GuardaFalhasDeLimpezaColecao : IDisposable
{
    public void Dispose()
    {
        var falhas = Administrativo360EventosImutabilidadeTests.FalhasDeLimpeza.ToList();
        if (falhas.Count > 0)
        {
            throw new AggregateException(
                $"{falhas.Count} falha(s) de limpeza silenciosa(s) detectada(s) na coleção A360Transmissao:",
                falhas);
        }
    }
}

/// <summary>
/// R4-A5: registra a guarda de fim de coleção no A360Transmissao (a definição anterior
/// não existia; a coleção era implícita). Dispose com falha = rodada falha explicitamente.
/// </summary>
[CollectionDefinition("A360Transmissao")]
public sealed class A360TransmissaoCollectionDefinition : ICollectionFixture<GuardaFalhasDeLimpezaColecao>
{
}

/// <summary>
/// WP4 (requisito P4) — eventos de negócio imutáveis com hash, downloads idempotentes e
/// atomicidade da transmissão no Administrativo 360.
///
/// Regras validadas:
///  1. Jornada completa manual (captura com anexo → relacionamento → orçamento → aprovação →
///     transmissão) gera os 4 tipos de evento (ARQUIVO/COTACAO_ANEXO, APROVACAO,
///     RETORNO_EXTERNO e ARQUIVO/EXPORTACAO), cada um com hash SHA-256 recomputável em C#
///     (mesma forma canônica do serviço) e chaves de idempotência corretas.
///  2. Captura sem anexos NÃO gera evento ARQUIVO (sem registro vazio).
///  3. Idempotência do log: mesma chave NÃO repete linha; chaves nulas coexistem.
///  4. Hash canônico é determinístico e sensível a tenant/tipo/descrição/dados (null ≡ {}).
///  5. Importação de XML gera DECLARACAO_MANUAL recomputável e evento de documento fiscal com
///     hash preenchido pelo trigger (fórmula do backfill, recomputada na mesma sessão);
///     destinatário não autorizado cai em quarentena DESTINATARIO_NAO_AUTORIZADO.
///  6. adm360_eventos é append-only: UPDATE/DELETE sem bypass → P0001; o bypass GUC vale
///     somente na transação onde foi ativado (set_config local à transação).
///  7. Herança histórica: adm360_documento_eventos (linhas backfilladas) também bloqueia
///     UPDATE/DELETE sem bypass.
///  8. vale_eventos: DELETE bloqueado; apenas o progresso da decisão (quantidade_decidida)
///     pode ser atualizado.
///  9. Recuperação oficial: ENVIANDO antigo → RESULTADO_DESCONHECIDO com marcação textual;
///     ENVIANDO recente permanece (queda em andamento não é conciliada).
/// 10. Falha técnica no conector → RESULTADO_DESCONHECIDO honesto (sem ENVIANDO órfão),
///     cotação segue PRONTA_PARA_ENVIO e RETORNO_EXTERNO registra o status final.
/// 11/12. HTTP real: download de anexo e de exportação com ETag derivado do SHA-256 +
///     304 idempotente e X-File-SHA256 na exportação.
/// </summary>
// Mesma coleção das demais classes que escrevem status_transmissao='ENVIANDO':
// serializa entre si para que assert de estado global não enxergue janela de teste concorrente.
[Collection("A360Transmissao")]
public sealed class Administrativo360EventosImutabilidadeTests : IClassFixture<PlantaoProApiFactory>
{
    private readonly PlantaoProApiFactory _factory;

    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid UsuarioGestor = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    // Produto demo do tenant Santa Casa usado para o relacionamento do item (seed fixo).
    private static readonly Guid ProdutoImpDemo = Guid.Parse("a3610000-0000-4000-8000-000000000002");

    private const string GestorEmail = "gestor@santacasa-demo.example";
    private const string GestorSenha = "SantaCasa!Demo2026#Gestor";

    // R4-A5: falhas de limpeza deixam de ser engolidas em silêncio. Não lançamos na hora —
    // a limpeza roda em finally e não pode mascarar a asserção do próprio teste — mas
    // registramos: a GuardaFalhasDeLimpezaColecao falha a rodada no fim da coleção se algo
    // escapou. Foi exatamente este catch mudo que escondeu o orfao ENVIANDO que quebrou a
    // rodada seguinte após execução interrompida.
    internal static readonly ConcurrentBag<Exception> FalhasDeLimpeza = new();

    public Administrativo360EventosImutabilidadeTests(PlantaoProApiFactory factory) => _factory = factory;

    // ------------------------------------------------------------------
    // Testes
    // ------------------------------------------------------------------

    [Fact]
    public async Task JornadaCompletaManual_GeraOsQuatroEventosComHashRecomputavelERegistrosImutaveis()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        var repo = new CotacoesRepository(cs);

        var bytesAnexo = Encoding.UTF8.GetBytes("anexo real da cotação WP4 (hash e ETag conferidos)");
        var shaAnexo = Sha256Hex(bytesAnexo);
        var (cotacaoId, contaCriadaAgora) = await CapturarBaseAsync(repo, "IMPORTACAO_MANUAL", "W4A",
            new[] { new CapturarCotacaoAnexoCommand("anexo_wp4.txt", bytesAnexo.Length, "text/plain", shaAnexo, bytesAnexo) });

        Guid respostaId = Guid.Empty, orcamentoId = Guid.Empty;
        try
        {
            // 1) ARQUIVO na captura: 1 linha, chave idempotente, hash recomputável
            var evArquivo = await BuscarEventosAsync(cs, cotacaoId, Adm360TipoEvento.Arquivo);
            Assert.Single(evArquivo);
            var linhaArquivo = evArquivo[0];
            Assert.Equal("COTACAO_ANEXO", linhaArquivo.Entidade);
            Assert.Equal("Anexo(s) recebido(s) com a captura da cotação (1 arquivo(s))", linhaArquivo.Descricao);
            Assert.Equal($"arquivo:captura:{TenantSantaCasa:N}:{cotacaoId:N}", linhaArquivo.IdempotencyKey);
            AssertHex64(linhaArquivo.Sha256Hash);
            Assert.Equal(
                Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Arquivo, "COTACAO_ANEXO", cotacaoId, linhaArquivo.Descricao,
                    new { total_anexos = 1, anexos = new[] { new { nome_arquivo = "anexo_wp4.txt", tamanho_bytes = bytesAnexo.Length, sha256_hash = shaAnexo } } }),
                linhaArquivo.Sha256Hash);

            // 2) Aprovação: APROVACAO imutável na mesma transação
            (orcamentoId, respostaId) = await RelacionarOrcamentarAprovarAsync(repo, cotacaoId);
            var evAprovacao = await BuscarEventosAsync(cs, respostaId, Adm360TipoEvento.Aprovacao);
            Assert.Single(evAprovacao);
            var linhaAprovacao = evAprovacao[0];
            Assert.Equal("COTACAO_RESPOSTA", linhaAprovacao.Entidade);
            Assert.Equal("Proposta aprovada para envio ao portal", linhaAprovacao.Descricao);
            Assert.Equal($"aprovacao:resposta:{respostaId:N}", linhaAprovacao.IdempotencyKey);
            AssertHex64(linhaAprovacao.Sha256Hash);
            Assert.Equal(
                Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", respostaId, linhaAprovacao.Descricao,
                    new { cotacao_id = cotacaoId, orcamento_id = orcamentoId, revisao = 1 }),
                linhaAprovacao.Sha256Hash);

            // 3) Transmissão manual: RETORNO_EXTERNO + ARQUIVO da exportação
            await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));
            var enviada = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.NotNull(enviada);
            Assert.Equal("EXPORTADA_MANUALMENTE", enviada!.StatusTransmissao);
            Assert.NotNull(enviada.EnviadoEm);

            var evRetorno = await BuscarEventosAsync(cs, respostaId, Adm360TipoEvento.RetornoExterno);
            Assert.Single(evRetorno);
            var linhaRetorno = evRetorno[0];
            Assert.Equal("COTACAO_RESPOSTA", linhaRetorno.Entidade);
            Assert.Equal("Retorno da transmissão da resposta: EXPORTADA_MANUALMENTE", linhaRetorno.Descricao);
            Assert.Equal("", linhaRetorno.IdempotencyKey);
            var jRetorno = JsonDocument.Parse(linhaRetorno.DadosJson).RootElement;
            var statusFinal = jRetorno.GetProperty("status_final").GetString();
            var protocolo = jRetorno.GetProperty("protocolo_externo").ValueKind == JsonValueKind.Null
                ? null
                : jRetorno.GetProperty("protocolo_externo").GetString();
            var msgRetorno = jRetorno.GetProperty("mensagem_retorno").GetString();
            AssertHex64(linhaRetorno.Sha256Hash);
            Assert.Equal(
                Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.RetornoExterno, "COTACAO_RESPOSTA", respostaId, linhaRetorno.Descricao,
                    new { status_final = statusFinal, protocolo_externo = protocolo, mensagem_retorno = msgRetorno }),
                linhaRetorno.Sha256Hash);

            var arquivo = await repo.ObterExportacaoPorRespostaAsync(TenantSantaCasa, respostaId);
            Assert.NotNull(arquivo);
            var evExportacao = await BuscarEventosAsync(cs, arquivo!.Id, Adm360TipoEvento.Arquivo);
            Assert.Single(evExportacao);
            var linhaExportacao = evExportacao[0];
            Assert.Equal("EXPORTACAO", linhaExportacao.Entidade);
            Assert.Equal($"Arquivo de exportação gerado: {arquivo.NomeArquivo}", linhaExportacao.Descricao);
            Assert.Equal($"arquivo:exportacao:{respostaId:N}", linhaExportacao.IdempotencyKey);
            AssertHex64(linhaExportacao.Sha256Hash);
            Assert.Equal(
                Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Arquivo, "EXPORTACAO", arquivo.Id, linhaExportacao.Descricao,
                    new { nome_arquivo = arquivo.NomeArquivo, tamanho_bytes = arquivo.TamanhoBytes, sha256_hash = arquivo.Sha256Hash }),
                linhaExportacao.Sha256Hash);

            // 4) Imutabilidade: as 4 linhas da jornada rejeitam UPDATE e DELETE (P0001)
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            foreach (var linha in new[] { linhaArquivo, linhaAprovacao, linhaRetorno, linhaExportacao })
            {
                var exU = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                    "UPDATE plantaopro.adm360_eventos SET descricao = descricao || ' (tato)' WHERE id = @i", new { i = linha.Id }));
                Assert.Equal("P0001", exU.SqlState);
                Assert.Contains("imutavel", exU.Message, StringComparison.OrdinalIgnoreCase);

                var exD = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_eventos WHERE id = @i", new { i = linha.Id }));
                Assert.Equal("P0001", exD.SqlState);
            }
        }
        finally
        {
            await LimparJornadaAsync(cs, respostaId, orcamentoId, cotacaoId, contaCriadaAgora);
        }
    }

    [Fact]
    public async Task CapturarSemAnexos_NaoGeraEventoArquivo()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        var repo = new CotacoesRepository(cs);

        var (cotacaoId, contaCriadaAgora) = await CapturarBaseAsync(repo, "IMPORTACAO_MANUAL", "W4S");
        try
        {
            Assert.Empty(await BuscarEventosAsync(cs, cotacaoId, Adm360TipoEvento.Arquivo));
        }
        finally
        {
            await LimparJornadaAsync(cs, Guid.Empty, Guid.Empty, cotacaoId, contaCriadaAgora);
        }
    }

    [Fact]
    public async Task IdempotenciaDoLog_MesmaChaveNaoRepeteLinha_ChavesNulasCoexistem()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);

        var entidadeId = Guid.NewGuid();
        var chave = $"wp4:idempotencia:{entidadeId:N}";
        try
        {
            var svc = new Adm360EventService(cs);
            var primeira = await svc.RegistrarAsync(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "TESTE_W4", entidadeId, UsuarioGestor,
                "evento de integridade WP4", new { ok = true }, chave);
            var segunda = await svc.RegistrarAsync(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "TESTE_W4", entidadeId, UsuarioGestor,
                "evento de integridade WP4", new { ok = true }, chave);
            Assert.True(primeira, "Primeiro registro com a chave deve gravar.");
            Assert.False(segunda, "Segundo registro com a MESMA chave deve ser idempotente.");

            var nulaA = await svc.RegistrarAsync(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "TESTE_W4", entidadeId, UsuarioGestor,
                "chave nula A", new { n = 1 });
            var nulaB = await svc.RegistrarAsync(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "TESTE_W4", entidadeId, UsuarioGestor,
                "chave nula B", new { n = 2 });
            Assert.True(nulaA);
            Assert.True(nulaB, "Chaves nulas coexistem (registro por tentativa de evento).");

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var linhas = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_eventos WHERE tenant_id = @t AND entidade_id = @e",
                new { t = TenantSantaCasa, e = entidadeId });
            Assert.Equal(3, linhas);
            var porChave = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_eventos WHERE tenant_id = @t AND idempotency_key = @k",
                new { t = TenantSantaCasa, k = chave });
            Assert.Equal(1, porChave);
        }
        finally
        {
            await LimparEventosAsync(cs, entidadeId);
        }
    }

    [Fact]
    public void HashCanonical_DeterministicoESensivelAoConteudo()
    {
        var entidadeId = Guid.NewGuid();
        var base1 = Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", entidadeId, "decisão", new { a = 1 });
        var base2 = Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", entidadeId, "decisão", new { a = 1 });
        AssertHex64(base1);
        Assert.Equal(base1, base2);

        Assert.NotEqual(base1, Adm360EventService.ComputarHash(Guid.NewGuid(), Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", entidadeId, "decisão", new { a = 1 }));
        Assert.NotEqual(base1, Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "OUTRA_ENTIDADE", entidadeId, "decisão", new { a = 1 }));
        Assert.NotEqual(base1, Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", entidadeId, "outra descrição", new { a = 1 }));
        Assert.NotEqual(base1, Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", entidadeId, "decisão", new { a = 2 }));
        Assert.NotEqual(base1, Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Arquivo, "COTACAO_RESPOSTA", entidadeId, "decisão", new { a = 1 }));

        // null equivale ao objeto vazio (forma canônica documentada do serviço)
        var hNull = Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", entidadeId, "decisão", null);
        Assert.Equal(hNull, Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", entidadeId, "decisão", new { }));
        Assert.NotEqual(base1, hNull);
    }

    [Fact]
    public async Task ImportacaoXml_GeraDeclaracaoManualRecomputavelEHASHDeDocumentoPeloTrigger()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        var docs = new DocumentosXmlRepository(cs);

        Guid docAutorizado = Guid.Empty, docRestrito = Guid.Empty;
        try
        {
            // Pré-condição: CNPJ autorizado (estabelecimento ativo do tenant) e um CNPJ de fora
            string cnpjAutorizado;
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                cnpjAutorizado = await cn.ExecuteScalarAsync<string?>(
                    "SELECT cnpj FROM plantaopro.adm360_estabelecimentos WHERE tenant_id = @t AND ativo = true ORDER BY id LIMIT 1",
                    new { t = TenantSantaCasa });
                Assert.False(string.IsNullOrWhiteSpace(cnpjAutorizado),
                    "Pré-condição do ambiente: tenant Santa Casa precisa de um estabelecimento ativo com CNPJ.");
            }

            // A) Destinatário autorizado: sem quarentena + DECLARACAO_MANUAL + hash do trigger
            var chaveOk = ChaveNfeUnica();
            docAutorizado = (await docs.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chaveOk, cnpjAutorizado), "wp4_autorizado.xml"))).Documentos[0].DocumentoId!.Value;

            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var st = await cn.QueryFirstAsync<dynamic>(
                    "SELECT quarentena, motivo_quarentena FROM plantaopro.adm360_documentos_recebidos WHERE id = @d",
                    new { d = docAutorizado });
                Assert.False((bool)st.quarentena);
                Assert.Null((string?)st.motivo_quarentena);
            }

            var evs = await BuscarEventosAsync(cs, docAutorizado, Adm360TipoEvento.DeclaracaoManual);
            Assert.Single(evs);
            var ev = evs[0];
            Assert.Equal("DOCUMENTO_XML", ev.Entidade);
            Assert.Equal($"declaracao:documento:{docAutorizado:N}", ev.IdempotencyKey);
            var j = JsonDocument.Parse(ev.DadosJson).RootElement;
            Assert.Equal(chaveOk, j.GetProperty("chave_acesso").GetString());
            Assert.False(j.GetProperty("quarentena").GetBoolean());
            Assert.Equal(JsonValueKind.Null, j.GetProperty("motivo_quarentena").ValueKind);
            AssertHex64(ev.Sha256Hash);
            Assert.Equal(
                Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.DeclaracaoManual, "DOCUMENTO_XML", docAutorizado, ev.Descricao,
                    new { chave_acesso = chaveOk, quarentena = false, motivo_quarentena = (string?)null }),
                ev.Sha256Hash);

            // Evento de documento fiscal: hash do trigger recomputado na MESMA sessão (fórmula do backfill)
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var consistente = await cn.QueryFirstAsync<bool>(new CommandDefinition(@"
                    SELECT sha256_hash = encode(digest(
                        coalesce(id::text, '') || '|' || coalesce(tenant_id::text, '') || '|' ||
                        coalesce(tipo_evento, '') || '|' || coalesce(sequencia_evento::text, '') || '|' ||
                        coalesce(descricao_evento, '') || '|' || coalesce(data_evento::text, '') || '|' ||
                        coalesce(protocolo, '') || '|' || coalesce(detalhes, ''),
                        'sha256'), 'hex')
                    FROM plantaopro.adm360_documento_eventos
                    WHERE tenant_id = @t AND documento_id = @d
                    ORDER BY sequencia_evento DESC LIMIT 1",
                    new { t = TenantSantaCasa, d = docAutorizado }));
                Assert.True(consistente, "Hash do novo documento_eventos diverge da fórmula do trigger/backfill.");
            }

            // B) Destinatário NÃO autorizado: quarentena DESTINATARIO_NAO_AUTORIZADO + evento coerente
            var chaveRestrita = ChaveNfeUnica();
            docRestrito = (await docs.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chaveRestrita, "99999999000199"), "wp4_restrito.xml"))).Documentos[0].DocumentoId!.Value;

            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var stR = await cn.QueryFirstAsync<dynamic>(
                    "SELECT quarentena, motivo_quarentena FROM plantaopro.adm360_documentos_recebidos WHERE id = @d",
                    new { d = docRestrito });
                Assert.True((bool)stR.quarentena);
                Assert.Equal("DESTINATARIO_NAO_AUTORIZADO", (string)stR.motivo_quarentena);
            }

            var evR = await BuscarEventosAsync(cs, docRestrito, Adm360TipoEvento.DeclaracaoManual);
            Assert.Single(evR);
            var jr = JsonDocument.Parse(evR[0].DadosJson).RootElement;
            Assert.Equal(chaveRestrita, jr.GetProperty("chave_acesso").GetString());
            Assert.True(jr.GetProperty("quarentena").GetBoolean());
            Assert.Equal("DESTINATARIO_NAO_AUTORIZADO", jr.GetProperty("motivo_quarentena").GetString());
            AssertHex64(evR[0].Sha256Hash);
            Assert.Equal(
                Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.DeclaracaoManual, "DOCUMENTO_XML", docRestrito, evR[0].Descricao,
                    new { chave_acesso = chaveRestrita, quarentena = true, motivo_quarentena = "DESTINATARIO_NAO_AUTORIZADO" }),
                evR[0].Sha256Hash);
        }
        finally
        {
            await LimparDocumentosAsync(cs, docAutorizado, docRestrito);
        }
    }

    [Fact]
    public async Task EventosAdm360_AppendOnly_BypassGUCLiberaSomenteNaMesmaTransacao()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);

        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var entidadeIdA = Guid.NewGuid();
        var entidadeIdB = Guid.NewGuid();
        var hashA = Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "TESTE_W6", entidadeIdA, "linha A", new { });
        var hashB = Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.Aprovacao, "TESTE_W6", entidadeIdB, "linha B", new { });

        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();

            var sqlInsert = @"
                INSERT INTO plantaopro.adm360_eventos(id, tenant_id, tipo_evento, entidade, entidade_id, usuario_id, descricao, dados, sha256_hash)
                VALUES (@id, @t, 'APROVACAO', 'TESTE_W6', @e, @u, @d, '{}'::jsonb, @h)";
            await cn.ExecuteAsync(sqlInsert, new { id = idA, t = TenantSantaCasa, e = entidadeIdA, u = UsuarioGestor, d = "linha A", h = hashA });
            await cn.ExecuteAsync(sqlInsert, new { id = idB, t = TenantSantaCasa, e = entidadeIdB, u = UsuarioGestor, d = "linha B", h = hashB });

            // Sem bypass: UPDATE e DELETE bloqueados (P0001)
            var exU = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                "UPDATE plantaopro.adm360_eventos SET descricao = 'trocada' WHERE id = @i", new { i = idA }));
            Assert.Equal("P0001", exU.SqlState);
            Assert.Contains("imutavel", exU.Message, StringComparison.OrdinalIgnoreCase);

            var exD = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                "DELETE FROM plantaopro.adm360_eventos WHERE id = @i", new { i = idA }));
            Assert.Equal("P0001", exD.SqlState);

            // Bypass GUC local à transação: na MESMA tx permite UPDATE + DELETE (manutenção oficial)
            await using var tx = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync("SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true)", transaction: tx);
            await cn.ExecuteAsync("UPDATE plantaopro.adm360_eventos SET descricao = 'mantencao autorizada' WHERE id = @i", new { i = idA }, transaction: tx);
            await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_eventos WHERE id = @i", new { i = idA }, transaction: tx);
            await tx.CommitAsync();

            var qtdA = await cn.ExecuteScalarAsync<int>("SELECT count(*) FROM plantaopro.adm360_eventos WHERE id = @i", new { i = idA });
            Assert.Equal(0, qtdA);

            // Fora da tx do bypass, a imutabilidade continua valendo (GUC não vazou)
            var exB = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                "UPDATE plantaopro.adm360_eventos SET descricao = 'trocada' WHERE id = @i", new { i = idB }));
            Assert.Equal("P0001", exB.SqlState);
        }
        finally
        {
            // Limpeza: a linha sobrevivente sai sob REPLICA (desliga todos os row triggers)
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SET SESSION session_replication_role = 'REPLICA'");
            try
            {
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_eventos WHERE id IN (@a, @b)", new { a = idA, b = idB });
            }
            finally
            {
                await cn.ExecuteAsync("RESET SESSION_REPLICATION_ROLE");
            }
        }
    }

    [Fact]
    public async Task DocumentoEventosHerdados_BackfillBloqueiaUpdateEDeleteSemBypass()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        var id = await cn.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT id FROM plantaopro.adm360_documento_eventos WHERE tenant_id = @t ORDER BY sequencia_evento, id LIMIT 1",
            new { t = TenantSantaCasa });
        Assert.False(id is null, "Pré-condição: tenant Santa Casa precisa de eventos históricos de documento.");

        var semHash = await cn.ExecuteScalarAsync<bool>(
            "SELECT sha256_hash IS NULL FROM plantaopro.adm360_documento_eventos WHERE id = @i", new { i = id!.Value });
        Assert.False(semHash, "Backfill: toda linha herdada precisa ter sha256_hash.");

        // UPDATE e DELETE bloqueados (sem alterar estado: ambos falham em auto-commit)
        var exU = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
            "UPDATE plantaopro.adm360_documento_eventos SET descricao_evento = descricao_evento || ' (auditoria)' WHERE id = @i",
            new { i = id.Value }));
        Assert.Equal("P0001", exU.SqlState);

        var exD = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
            "DELETE FROM plantaopro.adm360_documento_eventos WHERE id = @i", new { i = id.Value }));
        Assert.Equal("P0001", exD.SqlState);
    }

    [Fact]
    public async Task ValeEventos_DeleteBloqueado_SomenteProgressoDaDecisaoAtualizavel()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);

        var valeId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var eventoId = Guid.NewGuid();
        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.OpenAsync();
            var hospitalId = await cn.ExecuteScalarAsync<Guid>("SELECT id FROM plantaopro.adm360_estabelecimentos WHERE tenant_id = @t ORDER BY id LIMIT 1", new { t = TenantSantaCasa });
            var produtoId = await cn.ExecuteScalarAsync<Guid>("SELECT id FROM plantaopro.adm360_produtos WHERE tenant_id = @t ORDER BY id LIMIT 1", new { t = TenantSantaCasa });
            var loteId = await cn.ExecuteScalarAsync<Guid>("SELECT id FROM plantaopro.adm360_lotes WHERE tenant_id = @t ORDER BY id LIMIT 1", new { t = TenantSantaCasa });
            var locais = (await cn.QueryAsync<Guid>("SELECT id FROM plantaopro.adm360_locais WHERE tenant_id = @t ORDER BY id LIMIT 2", new { t = TenantSantaCasa })).ToList();
            Assert.True(locais.Count >= 2, "Pré-condição: tenant precisa de dois locais para o seed do vale.");

            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_vales(id, tenant_id, numero, hospital_id, local_origem_id, local_destino_id, data_saida_prevista)
                VALUES (@v, @t, @num, @h, @lo, @ld, @ds)",
                new { v = valeId, t = TenantSantaCasa, num = $"V-W4-{Sufixo()}", h = hospitalId, lo = locais[0], ld = locais[1], ds = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) });
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_vale_itens(id, tenant_id, vale_id, produto_id, lote_id, quantidade_solicitada)
                VALUES (@i, @t, @v, @p, @l, 10)",
                new { i = itemId, t = TenantSantaCasa, v = valeId, p = produtoId, l = loteId });
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_vale_eventos(id, tenant_id, vale_id, vale_item_id, tipo, quantidade)
                VALUES (@e, @t, @v, @i, 'CONSUMO', 3)",
                new { e = eventoId, t = TenantSantaCasa, v = valeId, i = itemId });
        }

        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();

            // Progresso da decisão: permitido (única atualização lícita)
            await cn.ExecuteAsync(
                "UPDATE plantaopro.adm360_vale_eventos SET quantidade_decidida = 2.5 WHERE id = @e", new { e = eventoId });
            var decidido = await cn.ExecuteScalarAsync<decimal>(
                "SELECT quantidade_decidida FROM plantaopro.adm360_vale_eventos WHERE id = @e", new { e = eventoId });
            Assert.Equal(2.5m, decidido);

            // Qualquer outra coluna: bloqueado (P0001)
            var exColuna = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                "UPDATE plantaopro.adm360_vale_eventos SET tipo = 'RETORNO' WHERE id = @e", new { e = eventoId }));
            Assert.Equal("P0001", exColuna.SqlState);
            Assert.Contains("progresso", exColuna.Message, StringComparison.OrdinalIgnoreCase);

            var exData = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                "UPDATE plantaopro.adm360_vale_eventos SET data_evento = now() - interval '1 day' WHERE id = @e", new { e = eventoId }));
            Assert.Equal("P0001", exData.SqlState);

            // DELETE: bloqueado
            var exD = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                "DELETE FROM plantaopro.adm360_vale_eventos WHERE id = @e", new { e = eventoId }));
            Assert.Equal("P0001", exD.SqlState);
            Assert.Contains("imutavel", exD.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SET SESSION session_replication_role = 'REPLICA'");
            try
            {
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_vale_eventos WHERE vale_id = @v", new { v = valeId });
                await cn.ExecuteAsync("DELETE FROM plantaopro.adm360_vales WHERE id = @v", new { v = valeId });
            }
            finally
            {
                await cn.ExecuteAsync("RESET SESSION_REPLICATION_ROLE");
            }
        }
    }

    [Fact]
    public async Task RecoveryEnviandoAntiga_ViraResultadoDesconhecidoComMarcacao_RecentePermanece()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        var repo = new CotacoesRepository(cs);

        var (cotacaoId, contaCriadaAgora) = await CapturarBaseAsync(repo, "IMPORTACAO_MANUAL", "W4R");
        Guid respostaId = Guid.Empty, orcamentoId = Guid.Empty;
        try
        {
            (orcamentoId, respostaId) = await RelacionarOrcamentarAprovarAsync(repo, cotacaoId);
            var fila = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.NotNull(fila);
            Assert.Equal("NA_FILA", fila!.StatusTransmissao);

            const string sqlBaseAntigas = @"
                SELECT count(*) FROM plantaopro.adm360_cotacao_respostas
                WHERE status_transmissao = 'ENVIANDO' AND updated_at <= now() - interval '10 minutes'";

            // Simula processo interrompido: resposta em ENVIANDO desde 2h (REPLICA desliga triggers)
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var baseAntigas = await cn.ExecuteScalarAsync<int>(sqlBaseAntigas);

                await cn.ExecuteAsync("SET SESSION session_replication_role = 'REPLICA'");
                await cn.ExecuteAsync(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET status_transmissao = 'ENVIANDO', updated_at = now() - interval '2 hours'
                    WHERE id = @r", new { r = respostaId });
                await cn.ExecuteAsync("RESET SESSION_REPLICATION_ROLE");

                var recuperadas = await Adm360TransmissaoRecovery.ReconciliarEnviandoAncoradosAsync(cs, olderThanMinutes: 10);
                Assert.Equal(baseAntigas + 1, recuperadas);

                var st = await cn.QueryFirstAsync<dynamic>(@"
                    SELECT status_transmissao, mensagem_retorno
                    FROM plantaopro.adm360_cotacao_respostas WHERE id = @r", new { r = respostaId });
                Assert.Equal("RESULTADO_DESCONHECIDO", (string)st.status_transmissao);
                Assert.Contains("Recuperacao oficial", (string)st.mensagem_retorno, StringComparison.Ordinal);
            }

            // ENVIANDO recente (atualização agora) permanece: queda em andamento não é conciliada
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await cn.ExecuteAsync("SET SESSION session_replication_role = 'REPLICA'");
                await cn.ExecuteAsync(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET status_transmissao = 'ENVIANDO', updated_at = now()
                    WHERE id = @r", new { r = respostaId });
                await cn.ExecuteAsync("RESET SESSION_REPLICATION_ROLE");

                var baseAntigas2 = await cn.ExecuteScalarAsync<int>(sqlBaseAntigas);
                var recuperadas2 = await Adm360TransmissaoRecovery.ReconciliarEnviandoAncoradosAsync(cs, olderThanMinutes: 10);
                Assert.Equal(baseAntigas2, recuperadas2);

                var statusFim = await cn.ExecuteScalarAsync<string>(
                    "SELECT status_transmissao FROM plantaopro.adm360_cotacao_respostas WHERE id = @r", new { r = respostaId });
                Assert.Equal("ENVIANDO", statusFim);
            }
        }
        finally
        {
            await LimparJornadaAsync(cs, respostaId, orcamentoId, cotacaoId, contaCriadaAgora);
        }
    }

    [Fact]
    public async Task ConectorComFalhaTecnica_ResultadoDesconhecidoSemOrfaonEmEnviandoERetornoRegistrado()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        var repo = new CotacoesRepository(cs, new IPortalCotacaoConnector[] { new ConectorFalhaSimulada() });

        var (cotacaoId, contaCriadaAgora) = await CapturarBaseAsync(repo, "CONNECTORFALHA", "W4F");
        Guid respostaId = Guid.Empty, orcamentoId = Guid.Empty;
        try
        {
            (orcamentoId, respostaId) = await RelacionarOrcamentarAprovarAsync(repo, cotacaoId);

            // Não lança exceção para o chamador: falha técnica vira estado honesto
            await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));

            var r = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
            Assert.NotNull(r);
            Assert.Equal("RESULTADO_DESCONHECIDO", r!.StatusTransmissao);
            Assert.Equal(1, r.Tentativas);
            Assert.Null(r.ProtocoloExterno);
            Assert.Contains("Falha técnica na transmissão", r.MensagemRetorno, StringComparison.Ordinal);
            Assert.Contains("Timeout simulado", r.MensagemRetorno, StringComparison.Ordinal);

            // Cotação continua encaminhável (não virou RESPONDIDA, não ficou presa)
            var cotacao = await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            Assert.NotNull(cotacao);
            Assert.Equal("PRONTA_PARA_ENVIO", cotacao!.StatusInterno);

            // RETORNO_EXTERNO registra o status final com hash recomputável
            var evs = await BuscarEventosAsync(cs, respostaId, Adm360TipoEvento.RetornoExterno);
            Assert.Single(evs);
            var ev = evs[0];
            Assert.Equal("", ev.IdempotencyKey);
            var j = JsonDocument.Parse(ev.DadosJson).RootElement;
            Assert.Equal("RESULTADO_DESCONHECIDO", j.GetProperty("status_final").GetString());
            Assert.Equal(JsonValueKind.Null, j.GetProperty("protocolo_externo").ValueKind);
            var msgEv = j.GetProperty("mensagem_retorno").GetString();
            Assert.Contains("Timeout simulado", msgEv!, StringComparison.Ordinal);
            AssertHex64(ev.Sha256Hash);
            Assert.Equal(
                Adm360EventService.ComputarHash(TenantSantaCasa, Adm360TipoEvento.RetornoExterno, "COTACAO_RESPOSTA", respostaId, ev.Descricao,
                    new { status_final = "RESULTADO_DESCONHECIDO", protocolo_externo = (string?)null, mensagem_retorno = msgEv }),
                ev.Sha256Hash);

            // Nenhum ENVIANDO órfão no tenant
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var orphanas = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_cotacao_respostas WHERE tenant_id = @t AND status_transmissao = 'ENVIANDO'",
                new { t = TenantSantaCasa });
            Assert.Equal(0, orphanas);
        }
        finally
        {
            await LimparJornadaAsync(cs, respostaId, orcamentoId, cotacaoId, contaCriadaAgora);
        }
    }

    [Fact]
    public async Task HttpAnexo_EtagPorSha256E304Idempotente()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        var repo = new CotacoesRepository(cs);

        var bytesAnexo = Encoding.UTF8.GetBytes("download idempotente WP4 (anexo)");
        var shaAnexo = Sha256Hex(bytesAnexo);
        var (cotacaoId, contaCriadaAgora) = await CapturarBaseAsync(repo, "IMPORTACAO_MANUAL", "W4H",
            new[] { new CapturarCotacaoAnexoCommand("anexo_http_wp4.txt", bytesAnexo.Length, "text/plain", shaAnexo, bytesAnexo) });
        try
        {
            var detalhe = await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
            var anexo = Assert.Single(detalhe!.Anexos, a => a.NomeArquivo == "anexo_http_wp4.txt");
            Assert.Equal(shaAnexo, anexo.Sha256Hash);

            var token = await TokenAsync(GestorEmail, GestorSenha);
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"api/administrativo360/cotacoes/{cotacaoId}/anexos/{anexo.Id}";
            var resp = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var corpo = await resp.Content.ReadAsByteArrayAsync();
            Assert.Equal(bytesAnexo, corpo);
            Assert.Equal($"\"{shaAnexo}\"", resp.Headers.ETag?.Tag);

            // Revalidação com ETag exato → 304 sem corpo
            var req304 = new HttpRequestMessage(HttpMethod.Get, url);
            req304.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{shaAnexo}\""));
            var r304 = await client.SendAsync(req304);
            Assert.Equal(HttpStatusCode.NotModified, r304.StatusCode);
            Assert.Empty(await r304.Content.ReadAsByteArrayAsync());

            // "*" revalida qualquer versão → 304
            var reqEstrela = new HttpRequestMessage(HttpMethod.Get, url);
            reqEstrela.Headers.TryAddWithoutValidation("If-None-Match", "*");
            var rEstrela = await client.SendAsync(reqEstrela);
            Assert.Equal(HttpStatusCode.NotModified, rEstrela.StatusCode);

            // ETag errada → 200 com o corpo completo de novo
            var reqErrada = new HttpRequestMessage(HttpMethod.Get, url);
            reqErrada.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"" + new string('f', 64) + "\""));
            var rErrada = await client.SendAsync(reqErrada);
            Assert.Equal(HttpStatusCode.OK, rErrada.StatusCode);
            Assert.Equal(bytesAnexo.Length, (await rErrada.Content.ReadAsByteArrayAsync()).Length);
        }
        finally
        {
            await LimparJornadaAsync(cs, Guid.Empty, Guid.Empty, cotacaoId, contaCriadaAgora);
        }
    }

    [Fact]
    public async Task HttpExportacao_XFileSha256ETagE304Idempotente()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        var repo = new CotacoesRepository(cs);

        var (cotacaoId, contaCriadaAgora) = await CapturarBaseAsync(repo, "IMPORTACAO_MANUAL", "W4X");
        Guid respostaId = Guid.Empty, orcamentoId = Guid.Empty;
        try
        {
            (orcamentoId, respostaId) = await RelacionarOrcamentarAprovarAsync(repo, cotacaoId);
            await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));

            var arquivo = await repo.ObterExportacaoPorRespostaAsync(TenantSantaCasa, respostaId);
            Assert.NotNull(arquivo);

            var token = await TokenAsync(GestorEmail, GestorSenha);
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"api/administrativo360/cotacoes/respostas/{respostaId}/exportacao";
            var resp = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var corpo = await resp.Content.ReadAsByteArrayAsync();
            Assert.Equal(arquivo!.TamanhoBytes, corpo.Length);
            Assert.Equal(arquivo.Sha256Hash, Sha256Hex(corpo));
            Assert.Equal(arquivo.Sha256Hash, resp.Headers.GetValues("X-File-SHA256")?.FirstOrDefault());
            Assert.Equal($"\"{arquivo.Sha256Hash}\"", resp.Headers.ETag?.Tag);

            var req304 = new HttpRequestMessage(HttpMethod.Get, url);
            req304.Headers.IfNoneMatch.Add(new EntityTagHeaderValue($"\"{arquivo.Sha256Hash}\""));
            var r304 = await client.SendAsync(req304);
            Assert.Equal(HttpStatusCode.NotModified, r304.StatusCode);
            Assert.Empty(await r304.Content.ReadAsByteArrayAsync());
        }
        finally
        {
            await LimparJornadaAsync(cs, respostaId, orcamentoId, cotacaoId, contaCriadaAgora);
        }
    }

    // ------------------------------------------------------------------
    // Jornada (padrão Bloco 7) e helpers
    // ------------------------------------------------------------------

    private async Task<(Guid CotacaoId, Guid? ContaCriadaAgora)> CapturarBaseAsync(
        CotacoesRepository repo, string provedor, string tag, IReadOnlyList<CapturarCotacaoAnexoCommand>? anexos = null)
    {
        var estabelecimentos = await repo.ListarEstabelecimentosAsync(TenantSantaCasa);
        Assert.NotEmpty(estabelecimentos);

        // Conta do canal manual (upsert idempotente por tenant+provedor+identificador)
        var contas = await repo.ListarContasPortalAsync(TenantSantaCasa);
        var contaManual = contas.FirstOrDefault(c => c.Provedor == "IMPORTACAO_MANUAL" && c.IdentificadorExterno == "CONTAMANUAL-WP4");
        Guid? contaCriadaAgora = null;
        if (contaManual is null)
        {
            contaCriadaAgora = await repo.ConfigurarContaPortalAsync(TenantSantaCasa, UsuarioGestor,
                new ConfigurarPortalContaCommand(estabelecimentos[0].Id, "IMPORTACAO_MANUAL", "Canal Manual - WP4", "CONTAMANUAL-WP4", null, null));
            contas = await repo.ListarContasPortalAsync(TenantSantaCasa);
            contaManual = contas.First(c => c.Id == contaCriadaAgora);
        }

        var identificador = $"{tag}-{Sufixo()}";
        var cotacaoId = await repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CapturarCotacaoCommand(
            estabelecimentos[0].Id, contaManual.Id, provedor, identificador, 1,
            "Hospital WP4", "T.W.", "Cirurgia de Teste WP4",
            DateOnly.FromDateTime(DateTime.Today.AddDays(10)), DateTime.UtcNow.AddDays(2),
            "IMPORTACAO_MANUAL", "{\"wp4\":true}",
            new[] { new CapturarCotacaoItemCommand(1, "IMP-DEMO", "Produto Importacao Demo", "Fornecedor Demo", "STD", 2, "UN") },
            anexos));
        return (cotacaoId, contaCriadaAgora);
    }

    private async Task<(Guid OrcamentoId, Guid RespostaId)> RelacionarOrcamentarAprovarAsync(CotacoesRepository repo, Guid cotacaoId)
    {
        var detalhe = await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
        Assert.NotNull(detalhe);

        foreach (var item in detalhe!.Itens.ToList())
        {
            await repo.RelacionarItemAsync(TenantSantaCasa, UsuarioGestor, new RelacionarItemCotacaoCommand(
                item.Id, ProdutoImpDemo, 1.0000m, 150.75m, 0m, "Placa Demo", null, "RELACIONADO", null));
        }

        var orcamentoId = await repo.GerarOrcamentoCirurgicoAsync(TenantSantaCasa, UsuarioGestor,
            new GerarOrcamentoDaCotacaoCommand(cotacaoId));

        var respostaId = await repo.AprovarRespostaAsync(TenantSantaCasa, UsuarioGestor,
            new AprovarRespostaCotacaoCommand(cotacaoId));
        return (orcamentoId, respostaId);
    }

    private static void RegistrarFalhaDeLimpeza(string origem, Exception ex)
    {
        FalhasDeLimpeza.Add(ex);
        Console.Error.WriteLine($"[{origem}] limpeza ADM360 falhou: {ex.Message}");
    }

    private static async Task LimparJornadaAsync(string cs, Guid respostaId, Guid orcamentoId, Guid cotacaoId, Guid? contaCriadaAgora)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SET SESSION session_replication_role = 'REPLICA'");
            try
            {
                // Ordem respeita dependências (FKs continuam válidas mesmo sob REPLICA)
                if (respostaId != Guid.Empty)
                {
                    // adm360_cotacao_envios referencia a resposta; sem remover antes, uma
                    // execucao interrompida em estado ENVIANDO vira orfao que quebra o teste
                    // de "nenhum ENVIANDO no tenant" na rodada seguinte.
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_cotacao_envios WHERE resposta_id = @r AND tenant_id = @t",
                        new { r = respostaId, t = TenantSantaCasa });
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_cotacao_exportacoes WHERE resposta_id = @r AND tenant_id = @t",
                        new { r = respostaId, t = TenantSantaCasa });
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_cotacao_respostas WHERE id = @r AND tenant_id = @t",
                        new { r = respostaId, t = TenantSantaCasa });
                }
                if (cotacaoId != Guid.Empty)
                {
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_cotacao_anexos WHERE cotacao_id = @c AND tenant_id = @t",
                        new { c = cotacaoId, t = TenantSantaCasa });
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_cotacao_itens WHERE cotacao_id = @c AND tenant_id = @t",
                        new { c = cotacaoId, t = TenantSantaCasa });
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
                        "DELETE FROM plantaopro.adm360_cotacoes WHERE id = @c AND tenant_id = @t",
                        new { c = cotacaoId, t = TenantSantaCasa });
                }
                if (contaCriadaAgora is { } cid)
                {
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_portal_contas WHERE id = @k AND tenant_id = @t",
                        new { k = cid, t = TenantSantaCasa });
                }

                // Eventos imutáveis da jornada (REPLICA desliga o trigger append-only)
                var entidadeIds = new List<Guid>();
                if (respostaId != Guid.Empty) entidadeIds.Add(respostaId);
                if (cotacaoId != Guid.Empty) entidadeIds.Add(cotacaoId);
                if (entidadeIds.Count > 0)
                {
                    await cn.ExecuteAsync(@"
                        DELETE FROM plantaopro.adm360_eventos
                        WHERE tenant_id = @t AND (
                            entidade_id = ANY(@ids)
                            OR (idempotency_key IS NOT NULL AND idempotency_key IN (@c1, @c2))
                        )",
                        new
                        {
                            t = TenantSantaCasa, ids = entidadeIds.ToArray(),
                            c1 = respostaId == Guid.Empty ? null : $"aprovacao:resposta:{respostaId:N}",
                            c2 = respostaId == Guid.Empty ? null : $"arquivo:exportacao:{respostaId:N}"
                        });
                }
            }
            finally
            {
                await cn.ExecuteAsync("RESET SESSION_REPLICATION_ROLE");
            }
        }
        catch (Exception ex)
        {
            // Limpeza nunca deve ocultar as asserções do teste — mas também não pode ser
            // silenciosa (R4-A5): registra para a guarda de fim de coleção e para o log.
            RegistrarFalhaDeLimpeza("LimparJornadaAsync", ex);
        }
    }

    private static async Task LimparEventosAsync(string cs, params Guid[] entidadeIds)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SET SESSION session_replication_role = 'REPLICA'");
            try
            {
                await cn.ExecuteAsync(@"
                    DELETE FROM plantaopro.adm360_eventos
                    WHERE tenant_id = @t AND entidade_id = ANY(@ids)",
                    new { t = TenantSantaCasa, ids = entidadeIds });
            }
            finally
            {
                await cn.ExecuteAsync("RESET SESSION_REPLICATION_ROLE");
            }
        }
        catch (Exception ex)
        {
            // best-effort visível (R4-A5): falha silenciosa aqui vira flake na rodada seguinte
            RegistrarFalhaDeLimpeza("LimparEventosAsync", ex);
        }
    }

    private static async Task LimparDocumentosAsync(string cs, params Guid[] documentoIds)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync("SET SESSION session_replication_role = 'REPLICA'");
            try
            {
                foreach (var d in documentoIds.Where(x => x != Guid.Empty))
                {
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_documento_eventos WHERE documento_id = @d AND tenant_id = @t",
                        new { d, t = TenantSantaCasa });
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_documento_itens WHERE documento_id = @d AND tenant_id = @t",
                        new { d, t = TenantSantaCasa });
                    await cn.ExecuteAsync(
                        "DELETE FROM plantaopro.adm360_documentos_recebidos WHERE id = @d AND tenant_id = @t",
                        new { d, t = TenantSantaCasa });
                }

                var ids = documentoIds.Where(x => x != Guid.Empty).ToArray();
                if (ids.Length > 0)
                {
                    await cn.ExecuteAsync(@"
                        DELETE FROM plantaopro.adm360_eventos
                        WHERE tenant_id = @t AND entidade_id = ANY(@ids)",
                        new { t = TenantSantaCasa, ids });
                }
            }
            finally
            {
                await cn.ExecuteAsync("RESET SESSION_REPLICATION_ROLE");
            }
        }
        catch (Exception ex)
        {
            // best-effort visível (R4-A5): falha silenciosa aqui vira flake na rodada seguinte
            RegistrarFalhaDeLimpeza("LimparDocumentosAsync", ex);
        }
    }

    // ------------------------------------------------------------------
    // Consulta e utilidades
    // ------------------------------------------------------------------

    private sealed record EventoLinha(
        Guid Id, string TipoEvento, string Entidade, Guid EntidadeId,
        string Descricao, string DadosJson, string Sha256Hash, string IdempotencyKey);

    private static async Task<List<EventoLinha>> BuscarEventosAsync(string cs, Guid entidadeId, string? tipoEvento = null)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return (await cn.QueryAsync<EventoLinha>(new CommandDefinition(@"
            SELECT id AS Id, tipo_evento AS TipoEvento, entidade AS Entidade,
                   entidade_id AS EntidadeId, descricao AS Descricao,
                   dados::text AS DadosJson, sha256_hash AS Sha256Hash,
                   coalesce(idempotency_key, '') AS IdempotencyKey
            FROM plantaopro.adm360_eventos
            WHERE tenant_id = @t AND entidade_id = @e
              AND (@tipo IS NULL OR tipo_evento = @tipo)
            ORDER BY created_at, id",
            new { t = TenantSantaCasa, e = entidadeId, tipo = tipoEvento }))).ToList();
    }

    private static async Task GarantirSchemaAsync(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // Lock de conselheiro: serializa a garantia de schema entre classes de teste paralelas.
        // As migrations contêm DDL com lock exclusivo de catálogo (ALTER TABLE ... ADD TRIGGER);
        // duas sessões executando os arquivos ao mesmo tempo podem gerar impasse (40P01).
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
                await cn.ExecuteAsync(await File.ReadAllTextAsync(migrationPath));
            }

            // v2300 + v2301 são idempotentes: podem rodar em qualquer ordem/replicação do banco de teste.
            foreach (var arquivo in new[]
            {
                "database/migrations/2026_09_v2300_administrativo360_status_transmissao_exportacao.sql",
                "database/migrations/2026_09_v2301_administrativo360_eventos_imutabilidade.sql"
            })
            {
                var caminho = Path.Combine(RepositoryPathResolver.RepoRoot, arquivo);
                Assert.True(File.Exists(caminho), $"Migration {Path.GetFileName(arquivo)} não encontrada no repositório.");
                await ExecutarMigrationComRetryAsync(cn, await File.ReadAllTextAsync(caminho));
            }
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('adm360_eventos_tests_schema'));");
        }
    }

    /// <summary>
    /// Executa um arquivo de migration tolerando impasse (40P01): o v2301 contém backfill
    /// (UPDATE com lock de linha) em adm360_documento_eventos, tabela que outras classes de
    /// teste escrevem em paralelo. Quando o Postgres escolhe esta sessão como vítima do
    /// impasse, basta repetir — a sessão concorrente segue normalmente.
    /// </summary>
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

    private async Task<string> TokenAsync(string email, string senha)
    {
        using var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("api/auth/login", new { Email = email, Senha = senha });
        resposta.EnsureSuccessStatusCode();
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token), $"Login de {email} não retornou token.");
        return token!;
    }

    private static string Sha256Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Chave de acesso NF-e única por execução: 44 dígitos com o modelo '55' na posição
    /// exigida por XmlDocumentoRegras.ValidarChaveAcesso (SUBSTRING(chave, 20, 2) = '55').
    /// </summary>
    private static string ChaveNfeUnica()
    {
        string D(int n) => string.Concat(Enumerable.Range(0, n).Select(_ => (char)Random.Shared.Next('0', '9')));
        // 44 = cUF(2) + AAMM(4) + CNPJ(14) + modelo(2, indices 20-21 = '55') + serie/nNF...(22)
        return "442609" + D(14) + "55" + D(22); // 6 + 14 + 2 + 22 = 44
    }

    private static string NfeXml(string chaveAcesso, string destCnpj, string emitCnpj = "11222333000181")
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<nfeProc xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
  <NFe xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
    <infNFe Id=""NFe{chaveAcesso}"" versao=""4.00"">
      <ide>
        <cUF>44</cUF>
        <natOp>OPERACAO TESTE WP4</natOp>
        <mod>55</mod>
        <serie>1</serie>
        <nNF>{chaveAcesso.Substring(25, 9)}</nNF>
        <dhEmi>2026-09-29T10:00:00-03:00</dhEmi>
        <tpNF>1</tpNF>
        <idDest>1</idDest>
        <finNFe>1</finNFe>
      </ide>
      <emit>
        <CNPJ>{emitCnpj}</CNPJ>
        <xNome>Emitente Teste WP4 LTDA</xNome>
        <enderEmit><xLgr>Rua A</xLgr><xMun>Cidade</xMun><UF>MG</UF></enderEmit>
      </emit>
      <dest>
        <CNPJ>{destCnpj}</CNPJ>
        <xNome>Destinatario Teste WP4 SA</xNome>
        <enderDest><xLgr>Rua B</xLgr><xMun>Cidade</xMun><UF>MG</UF></enderDest>
      </dest>
      <total>
        <ICMSTot>
          <vBC>100.00</vBC>
          <vNF>110.00</vNF>
          <vProd>100.00</vProd>
        </ICMSTot>
      </total>
    </infNFe>
  </NFe>
</nfeProc>";
    }

    private static void AssertHex64(string hash)
    {
        Assert.False(string.IsNullOrWhiteSpace(hash), "SHA-256 canônico não pode ser vazio.");
        Assert.Equal(64, hash!.Length);
        Assert.All(hash, c => Assert.True(
            Uri.IsHexDigit(c) && (!char.IsLetter(c) || char.IsLower(c)),
            $"'{c}' não é hex minúsculo."));
    }

    /// <summary>Conector de teste cujo envio de proposta sempre falha tecnicamente.</summary>
    private sealed class ConectorFalhaSimulada : IPortalCotacaoConnector
    {
        public string Provedor => "CONNECTORFALHA";

        public Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default)
            => Task.FromResult(new PortalConexaoStatusResult(false, "SIMULADO", "Sem teste de conexão neste cenário.", DateTime.UtcNow));

        public Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CapturarCotacaoCommand>>(Array.Empty<CapturarCotacaoCommand>());

        public Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default)
            => throw new TimeoutException("Timeout simulado na chamada do portal (WP4).");
    }
}
