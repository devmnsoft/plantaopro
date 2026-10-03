using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Bloco 7 (Exportação e Integrações) — Administrativo 360.
/// Regras validadas:
/// 1. Canal IMPORTACAO_MANUAL: transmitir gera arquivo REAL da proposta aprovada (JSON com snapshot),
///    persistido em adm360_cotacao_exportacoes com SHA-256, imutável por trigger e por UNIQUE.
/// 2. Retransmissão é idempotente: mantém 1 linha, mesmo hash e mesmo nome de arquivo.
/// 3. Stubs honestos: OPMENEXO/INPART permanecem CONFIGURACAO_PENDENTE, sem protocolo,
///    sem arquivo exportado e sem marcar a cotação como RESPONDIDA.
/// 4. Isolamento multi-tenant na leitura do arquivo.
/// </summary>
// Mesma coleção das demais classes que escrevem status_transmissao='ENVIANDO':
// serializa entre si para que assert de estado global não enxergue janela de teste concorrente.
[Collection("A360Transmissao")]
public sealed class Administrativo360Bloco7ExportacaoPropostaTests
{
    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid TenantIsolado = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647599");
    private static readonly Guid UsuarioGestor = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    // Produto demo do tenant Santa Casa usado para relacionamento De/Para (seed fixo).
    private static readonly Guid ProdutoImpDemo = Guid.Parse("a3610000-0000-4000-8000-000000000002");

    private static async Task GarantirSchemaAsync(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // Lock de conselheiro: serializa a garantia de schema entre classes de teste paralelas.
        // As migrations contêm DDL com lock exclusivo de catálogo; duas sessões executando os
        // arquivos ao mesmo tempo podem gerar impasse (40P01).
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

            // v2300 — estende o CHECK de status_transmissao com os estados do Bloco 7.
            // Idempotente: pode rodar em qualquer ordem/replicação do banco de teste.
            var migrationV2300 = Path.Combine(RepositoryPathResolver.RepoRoot,
                "database/migrations/2026_09_v2300_administrativo360_status_transmissao_exportacao.sql");
            Assert.True(File.Exists(migrationV2300), "Migration v2300 (status_transmissao) não encontrada.");
            await ExecutarMigrationComRetryAsync(cn, await File.ReadAllTextAsync(migrationV2300));
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('adm360_eventos_tests_schema'));");
        }
    }

    /// <summary>
    /// Executa um arquivo de migration tolerando impasse (40P01): DDL com lock exclusivo de
    /// catálogo pode conflitar com sessões de teste que escrevem nas tabelas adm360 em
    /// paralelo. Quando o Postgres escolhe esta sessão como vítima do impasse, basta repetir.
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

    [Fact]
    public async Task Bloco7_ExportacaoManual_GeraArquivoImutavelComRetransmissaoIdempotente()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await GarantirSchemaAsync(cs);

            var repo = new CotacoesRepository(cs);
            var estabelecimentos = await repo.ListarEstabelecimentosAsync(TenantSantaCasa);
            Assert.NotEmpty(estabelecimentos);

            // Conta do canal manual (upsert idempotente por tenant+provedor+identificador)
            var contas = await repo.ListarContasPortalAsync(TenantSantaCasa);
            var contaManual = contas.FirstOrDefault(c => c.Provedor == "IMPORTACAO_MANUAL" && c.IdentificadorExterno == "CONTAMANUAL-BLOCO7");
            Guid? contaCriadaAgora = null;
            if (contaManual is null)
            {
                contaCriadaAgora = await repo.ConfigurarContaPortalAsync(TenantSantaCasa, UsuarioGestor,
                    new ConfigurarPortalContaCommand(estabelecimentos[0].Id, "IMPORTACAO_MANUAL", "Canal Manual - Bloco 7", "CONTAMANUAL-BLOCO7", null, null));
                contas = await repo.ListarContasPortalAsync(TenantSantaCasa);
                contaManual = contas.First(c => c.Id == contaCriadaAgora);
            }
            Assert.Equal("CONFIGURADA", contaManual.StatusIntegracao);

            // Sufixo curto: o numero do orcamento e ORC-COT-{ident}-R{rev} em varchar(30)
            var sufixo = Guid.NewGuid().ToString("N")[..7];
            var identificador = $"B7M-{sufixo}";

            var cotacaoId = await repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CapturarCotacaoCommand(
                estabelecimentos[0].Id, contaManual.Id, "IMPORTACAO_MANUAL", identificador, 1,
                "Hospital Bloco 7", "T.S.", "Cirurgia Teste Exportacao Manual",
                DateOnly.FromDateTime(DateTime.Today.AddDays(10)), DateTime.UtcNow.AddDays(2),
                "IMPORTACAO_MANUAL", "{\"bloco\":7}",
                new[] { new CapturarCotacaoItemCommand(1, "IMP-DEMO", "Produto Importacao Demo", "Fornecedor Demo", "STD", 2, "UN") }));

            Guid respostaId = Guid.Empty, orcamentoId = Guid.Empty;
            try
            {
                // Relacionamento do item com produto interno
                var detalhe = await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
                Assert.NotNull(detalhe);
                Assert.Single(detalhe!.Itens);

                await repo.RelacionarItemAsync(TenantSantaCasa, UsuarioGestor, new RelacionarItemCotacaoCommand(
                    detalhe.Itens[0].Id, ProdutoImpDemo, 1.0000m, 150.75m, 0m, "Placa Demo", null, "RELACIONADO", null));

                // Orçamento cirúrgico vinculado
                orcamentoId = await repo.GerarOrcamentoCirurgicoAsync(TenantSantaCasa, UsuarioGestor,
                    new GerarOrcamentoDaCotacaoCommand(cotacaoId));
                Assert.Equal("AGUARDANDO_APROVACAO", (await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

                // Aprovação: snapshot imutável + fila (ainda SEM arquivo de exportação)
                respostaId = await repo.AprovarRespostaAsync(TenantSantaCasa, UsuarioGestor,
                    new AprovarRespostaCotacaoCommand(cotacaoId));

                var naFila = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.NotNull(naFila);
                Assert.Equal("NA_FILA", naFila!.StatusTransmissao);
                Assert.Null(naFila.ExportacaoId);
                Assert.Equal("PRONTA_PARA_ENVIO", (await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

                // Transmissão pelo canal manual: gera o arquivo real da proposta aprovada
                await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));

                var enviada = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.NotNull(enviada);
                Assert.Equal("EXPORTADA_MANUALMENTE", enviada!.StatusTransmissao);
                Assert.Null(enviada.ProtocoloExterno);
                Assert.NotNull(enviada.EnviadoEm);
                Assert.Equal(1, enviada.Tentativas);
                Assert.NotNull(enviada.ExportacaoId);
                Assert.Equal("RESPONDIDA", (await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

                // Arquivo real: bytes, tamanho, SHA-256 e conteúdo coerente
                var arquivo = await repo.ObterExportacaoPorRespostaAsync(TenantSantaCasa, respostaId);
                Assert.NotNull(arquivo);
                Assert.Equal("application/json", arquivo!.ContentType);
                Assert.Equal($"COT_{identificador}_R1_PROPOSTA_EXPORTADA.json", arquivo.NomeArquivo);
                Assert.Equal(arquivo.Conteudo.Length, arquivo.TamanhoBytes);
                Assert.Equal(Convert.ToHexString(SHA256.HashData(arquivo.Conteudo)).ToLowerInvariant(), arquivo.Sha256Hash);

                using var doc = JsonDocument.Parse(arquivo.Conteudo);
                Assert.Equal("PROPOSTA_APROVADA_EXPORTACAO_MANUAL", doc.RootElement.GetProperty("tipo_documento").GetString());
                Assert.Equal(identificador, doc.RootElement.GetProperty("cotacao").GetProperty("identificador_externo").GetString());
                var proposta = doc.RootElement.GetProperty("proposta_aprovada");
                Assert.Equal(JsonValueKind.Object, proposta.ValueKind);
                var itensSnapshot = proposta.GetProperty("Itens");
                Assert.Equal(JsonValueKind.Array, itensSnapshot.ValueKind);
                Assert.NotEmpty(itensSnapshot.EnumerateArray().ToArray());

                // Banco: exatamente 1 linha; hash gravado == hash recalculado em C#
                await using var cn = new NpgsqlConnection(cs);
                await cn.OpenAsync();
                var qtdLinhas = await cn.ExecuteScalarAsync<int>(
                    "SELECT count(*) FROM plantaopro.adm360_cotacao_exportacoes WHERE resposta_id = @r", new { r = respostaId });
                Assert.Equal(1, qtdLinhas);
                var shaBanco = await cn.ExecuteScalarAsync<string>(
                    "SELECT sha256_hash FROM plantaopro.adm360_cotacao_exportacoes WHERE resposta_id = @r", new { r = respostaId });
                Assert.Equal(arquivo.Sha256Hash, shaBanco);

                // Imutabilidade no banco: UPDATE e DELETE bloqueados pela trigger
                var exUpdate = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                    "UPDATE plantaopro.adm360_cotacao_exportacoes SET tamanho_bytes = tamanho_bytes + 1 WHERE resposta_id = @r",
                    new { r = respostaId }));
                Assert.Equal("P0001", exUpdate.SqlState);
                Assert.Contains("imut", exUpdate.Message, StringComparison.OrdinalIgnoreCase);

                var exDelete = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_cotacao_exportacoes WHERE resposta_id = @r",
                    new { r = respostaId }));
                Assert.Equal("P0001", exDelete.SqlState);
                Assert.Contains("imut", exDelete.Message, StringComparison.OrdinalIgnoreCase);

                // Retransmissão é idempotente: 1 linha, mesmo arquivo/hash, tentativas incrementadas
                await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));

                var reenviada = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.NotNull(reenviada);
                Assert.Equal("EXPORTADA_MANUALMENTE", reenviada!.StatusTransmissao);
                Assert.Equal(2, reenviada.Tentativas);
                Assert.Equal(arquivo.Sha256Hash, reenviada.ExportacaoSha256Hash);

                var arquivo2 = await repo.ObterExportacaoPorRespostaAsync(TenantSantaCasa, respostaId);
                Assert.NotNull(arquivo2);
                Assert.Equal(arquivo.Id, arquivo2!.Id);
                Assert.Equal(arquivo.NomeArquivo, arquivo2.NomeArquivo);
                Assert.Equal(arquivo.Sha256Hash, arquivo2.Sha256Hash);
                Assert.Equal(1, await cn.ExecuteScalarAsync<int>(
                    "SELECT count(*) FROM plantaopro.adm360_cotacao_exportacoes WHERE resposta_id = @r", new { r = respostaId }));

                // Isolamento multi-tenant: outro tenant não enxerga o arquivo
                Assert.Null(await repo.ObterExportacaoPorRespostaAsync(TenantIsolado, respostaId));
            }
            finally
            {
                await LimparDadosAsync(cs, respostaId, orcamentoId, cotacaoId, contaCriadaAgora);
            }
        }
        catch (NpgsqlException ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o teste do Bloco 7 não está acessível: {ex.Message}");
        }
    }

    [Fact]
    public async Task Bloco7_StubOpmenexo_NaoGeraArquivoNemProtocoloEMarcaCondicaoVerdadeira()
    {
        var cs = TestDatabase.ConnectionString;
        try
        {
            await GarantirSchemaAsync(cs);

            var repo = new CotacoesRepository(cs);
            var estabelecimentos = await repo.ListarEstabelecimentosAsync(TenantSantaCasa);
            Assert.NotEmpty(estabelecimentos);

            var contas = await repo.ListarContasPortalAsync(TenantSantaCasa);
            var contaOpmenexo = contas.FirstOrDefault(c => c.Provedor == "OPMENEXO" && c.Ativo);
            Guid? contaCriadaAgora = null;
            if (contaOpmenexo is null)
            {
                contaCriadaAgora = await repo.ConfigurarContaPortalAsync(TenantSantaCasa, UsuarioGestor,
                    new ConfigurarPortalContaCommand(estabelecimentos[0].Id, "OPMENEXO", "Portal OPMENEXO (Demo)", "OPMENEXO-DEMO", null, null));
                contas = await repo.ListarContasPortalAsync(TenantSantaCasa);
                contaOpmenexo = contas.First(c => c.Id == contaCriadaAgora);
            }

            // Conector é honesto: sem integração real não há conexão
            var status = await new OpmenexoConnector().TestarConexaoAsync(contaOpmenexo);
            Assert.False(status.Conectado);
            Assert.True(status.Status is "NAO_CONFIGURADA" or "CONFIGURACAO_PENDENTE");

            var contaInpart = contas.FirstOrDefault(c => c.Provedor == "INPART" && c.Ativo);
            if (contaInpart is not null)
            {
                var statusInpart = await new InpartConnector().TestarConexaoAsync(contaInpart);
                Assert.False(statusInpart.Conectado);
            }

            var sufixo = Guid.NewGuid().ToString("N")[..7];
            var identificador = $"B7X-{sufixo}";

            var cotacaoId = await repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, new CapturarCotacaoCommand(
                estabelecimentos[0].Id, contaOpmenexo.Id, "OPMENEXO", identificador, 1,
                "Hospital Bloco 7", "J.S.", "Cirurgia Teste Stub OPMENEXO",
                DateOnly.FromDateTime(DateTime.Today.AddDays(10)), DateTime.UtcNow.AddDays(2),
                "PORTAL_OFICIAL", "{\"provedor\":\"OPMENEXO\"}",
                new[] { new CapturarCotacaoItemCommand(1, "OPX-001", "Produto Stub OPMENEXO", null, null, 1, "UN") }));

            Guid respostaId = Guid.Empty, orcamentoId = Guid.Empty;
            try
            {
                var detalhe = await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
                Assert.NotNull(detalhe);
                Assert.Single(detalhe!.Itens);

                await repo.RelacionarItemAsync(TenantSantaCasa, UsuarioGestor, new RelacionarItemCotacaoCommand(
                    detalhe.Itens[0].Id, ProdutoImpDemo, 1.0000m, 120.00m, 0m, "Kit Demo", null, "RELACIONADO", null));

                orcamentoId = await repo.GerarOrcamentoCirurgicoAsync(TenantSantaCasa, UsuarioGestor,
                    new GerarOrcamentoDaCotacaoCommand(cotacaoId));

                respostaId = await repo.AprovarRespostaAsync(TenantSantaCasa, UsuarioGestor,
                    new AprovarRespostaCotacaoCommand(cotacaoId));

                // Transmissão contra stub: condição verdadeira configuração pendente, sem protocolo
                await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(respostaId));

                var resp = await repo.ObterRespostaPorIdAsync(TenantSantaCasa, respostaId);
                Assert.NotNull(resp);
                Assert.Equal("CONFIGURACAO_PENDENTE", resp!.StatusTransmissao);
                Assert.Null(resp.ProtocoloExterno);
                Assert.Null(resp.EnviadoEm);
                Assert.Equal(1, resp.Tentativas);
                Assert.NotNull(resp.MensagemRetorno);
                Assert.Contains("OPMENEXO", resp.MensagemRetorno!);

                // Sem protocolo do portal: a cotação segue aguardando envio (não vira RESPONDIDA)
                Assert.Equal("PRONTA_PARA_ENVIO", (await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId))!.StatusInterno);

                // Nenhum arquivo de exportação é gerado para provedor não integrado
                Assert.Null(await repo.ObterExportacaoPorRespostaAsync(TenantSantaCasa, respostaId));
                await using var cn = new NpgsqlConnection(cs);
                await cn.OpenAsync();
                var qtd = await cn.ExecuteScalarAsync<int>(
                    "SELECT count(*) FROM plantaopro.adm360_cotacao_exportacoes WHERE resposta_id = @r", new { r = respostaId });
                Assert.Equal(0, qtd);
            }
            finally
            {
                await LimparDadosAsync(cs, respostaId, orcamentoId, cotacaoId, contaCriadaAgora);
            }
        }
        catch (NpgsqlException ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para o teste do Bloco 7 não está acessível: {ex.Message}");
        }
    }

    /// <summary>
    /// Limpeza best-effort dos dados criados pelo teste. A tabela de exportações é imutável
    /// por row trigger; a limpeza usa session_replication_role='REPLICA' (técnica padrão para
    /// manutenção/homologação — TRUNCATE/DELETE bypassing row triggers).
    /// </summary>
    private static async Task LimparDadosAsync(string cs, Guid respostaId, Guid orcamentoId, Guid cotacaoId, Guid? contaId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();

            if (respostaId != Guid.Empty)
            {
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

            if (contaId is { } cid)
            {
                await cn.ExecuteAsync(
                    "DELETE FROM plantaopro.adm360_portal_contas WHERE id = @k AND tenant_id = @t",
                    new { k = cid, t = TenantSantaCasa });
            }
        }
        catch
        {
            // Limpeza nunca deve ocultar as asserções do teste
        }
    }
}
