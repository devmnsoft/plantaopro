using Dapper;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// WS-A3 — Identidade documental por família fiscal e unicidade de sequencia_evento.
/// Cenários:
///   T1: N importações concorrentes da MESMA chave -> exatamente 1 documento/1 evento (sem erro).
///   T2: concorrência divergente da MESMA chave -> 1 vencedor + falha de negócio explícita (nenhuma exceção não tratada).
///   T3: importar -> conferir -> vincular -> sequências 1,2,3 contíguas + status VINCULADO.
///   T4: recebimento físico (gate) faz write-back do vínculo canônico no documento SEM mudar o status (G5).
///   T5: segundo recebimento do mesmo documento/pedido REUSA o recebimento canônico (1 recebimento, 2 movimentos, 2 títulos).
///   T6: documento já vinculado a outro pedido -> erro de negócio explícito (sem violação de constraint).
///   T7: colisão de identidade fiscal (5-tupla) com bytes distintos -> erro "Identidade fiscal" (sem duplicar registro).
///   T8: dois XMLs MALFORMADO distintos coexistem em quarentena (identidade fora do índice parcial).
/// Padrão A3: tenant fixo compartilhado, chaves geradas por execução, purge escopado em try/finally.
/// </summary>
[Collection("A360Transmissao")]
public sealed class Administrativo360DocumentosWsA3Tests
{
    private static string ObterConnectionString() => TestDatabase.ConnectionString;

    private static async Task GarantirConexaoBancoAsync(string cs)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();

            var hasModuloId = await cn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'plantaopro' AND table_name = 'tenant_modulos' AND column_name = 'modulo_id'
                );");

            if (!hasModuloId)
            {
                var migrationPath = Path.Combine(RepositoryPathResolver.RepoRoot, "database/migrations/2026_09_v2197_reconciliar_contratos_modulos.sql");
                if (File.Exists(migrationPath))
                {
                    var sql = await File.ReadAllTextAsync(migrationPath);
                    await cn.ExecuteAsync(sql);
                }
            }

            var hasAdm360 = await cn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM plantaopro.tenant_modulos tm
                    JOIN plantaopro.modulos_sistema ms ON ms.id = tm.modulo_id
                    WHERE tm.tenant_id = @TenantSantaCasa AND ms.codigo = 'ADM360' AND tm.reg_status = 'A' AND tm.habilitado = true
                );", new { TenantSantaCasa });

            if (!hasAdm360)
            {
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.tenant_modulos (id, tenant_id, modulo_id, codigo, codigo_modulo, habilitado, status, origem, reg_status, reg_date)
                    SELECT gen_random_uuid(), @TenantSantaCasa, m.id, m.codigo, m.codigo, true, 'ATIVO', 'DEMO_MIGRATION', 'A', now()
                    FROM plantaopro.modulos_sistema m
                    WHERE upper(m.codigo) = 'ADM360' AND m.reg_status = 'A'
                    ON CONFLICT DO NOTHING;", new { TenantSantaCasa });
            }
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para a suíte de testes do Administrativo 360 não está acessível: {ex.Message}");
        }
    }

    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid UsuarioGestor = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    /// <summary>CNPJ do estabelecimento ativo do tenant (autorizado para receber NF-e/NFC-e).</summary>
    private const string CnpjDestinoAutorizado = "46389044000130";

    private static string Sha256Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Purge escopado por chave (não afeta seeds de outros testes); GUC bypass em transação.</summary>
    private static async Task PurgePorChavesAsync(string cs, params string[] chaves)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync("SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true);", transaction: tx);
        await cn.ExecuteAsync(@"
            DELETE FROM plantaopro.adm360_documento_eventos
            WHERE documento_id IN (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = ANY(@chaves));
            DELETE FROM plantaopro.adm360_documento_itens
            WHERE documento_id IN (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = ANY(@chaves));
            DELETE FROM plantaopro.adm360_documentos_recebidos
            WHERE chave_acesso = ANY(@chaves);", new { chaves }, tx);
        await tx.CommitAsync();
    }

    /// <summary>
    /// Chave NF-e única por execução: 44 dígitos, modelo '55' na posição 20-21.
    /// </summary>
    private static string ChaveNfeUnica()
    {
        string D(int n) => string.Concat(Enumerable.Range(0, n).Select(_ => (char)Random.Shared.Next('0', '9')));
        return "442609" + D(14) + "55" + D(22); // 6 + 14 + 2 + 22 = 44
    }

    /// <summary>
    /// Chave NF-e cujo NÚMERO extraído (posições 25-33) é o informadO — permite forçar
    /// colisão de identidade fiscal (mesmo emitente/série/modelo/número) com chaves distintas.
    /// </summary>
    private static string ChaveNfeComNumeroCompartilhado(string numeroComum)
    {
        string D(int n) => string.Concat(Enumerable.Range(0, n).Select(_ => (char)Random.Shared.Next('0', '9')));
        return "442609" + D(14) + "55" + D(3) + numeroComum + D(10); // 6+14+2+3+9+10 = 44
    }

    /// <summary>Corpo interno do &lt;infNFe&gt; com vNF ajustável (para bytes divergentes com a mesma chave).</summary>
    private static string CorpoInfNfe(string chaveAcesso, string vNf = "110.00")
    {
        return $@"<ide>
    <cUF>44</cUF>
    <mod>55</mod>
    <serie>1</serie>
    <nNF>{chaveAcesso.Substring(25, 9)}</nNF>
    <dhEmi>2026-10-06T10:00:00-03:00</dhEmi>
  </ide>
  <emit>
    <CNPJ>11222333000181</CNPJ>
    <xNome>Emitente Teste WsA3 LTDA</xNome>
    <enderEmit><xLgr>Rua A</xLgr><xMun>Cidade</xMun><UF>MG</UF></enderEmit>
  </emit>
  <dest>
    <CNPJ>{CnpjDestinoAutorizado}</CNPJ>
    <xNome>Destinatario Santa Casa SA</xNome>
    <enderDest><xLgr>Rua B</xLgr><xMun>Cidade</xMun><UF>MG</UF></enderDest>
  </dest>
  <total>
    <ICMSTot>
      <vBC>100.00</vBC>
      <vNF>{vNf}</vNF>
      <vProd>100.00</vProd>
    </ICMSTot>
  </total>";
    }

    /// <summary>NF-e minimalista completa (com declaração XML) para importação direta.</summary>
    private static string NfeXml(string chaveAcesso, string vNf = "110.00")
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<infNFe Id=""NFe{chaveAcesso}"" versao=""4.00"">
{CorpoInfNfe(chaveAcesso, vNf)}
</infNFe>";
    }

    /// <summary>Purge escopado do cenário de gate de estoque (IDs exclusivos deste teste).</summary>
    private static async Task PurgeGateCompraAsync(string cs, Guid parceiroId, Guid produtoId, Guid localId, Guid pedidoId, bool limparCadastroCompartilhado = true)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync("SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true);", transaction: tx);
        await cn.ExecuteAsync(@"
            -- WS-A3: o gate de recebimento faz write-back do vinculo no documento fiscal
            -- (FK recebimento_id/titulo_pagar_id) -> zerar antes de excluir as origens.
            UPDATE plantaopro.adm360_documentos_recebidos d
            SET recebimento_id = NULL, pedido_id = NULL, titulo_pagar_id = NULL
            WHERE d.recebimento_id IN (SELECT id FROM plantaopro.adm360_recebimentos WHERE pedido_id=@pedidoId);
            DELETE FROM plantaopro.adm360_movimentos
            WHERE tenant_id=@tenantId AND origem_tipo='RECEBIMENTO'
              AND origem_id IN (SELECT ri.id FROM plantaopro.adm360_recebimento_itens ri
                                JOIN plantaopro.adm360_recebimentos r ON r.id = ri.recebimento_id
                                WHERE r.pedido_id=@pedidoId AND r.tenant_id=@tenantId);
            DELETE FROM plantaopro.adm360_recebimento_itens
            WHERE recebimento_id IN (SELECT id FROM plantaopro.adm360_recebimentos WHERE pedido_id=@pedidoId);
            DELETE FROM plantaopro.adm360_titulos_pagar
            WHERE origem_id IN (SELECT id FROM plantaopro.adm360_recebimentos WHERE pedido_id=@pedidoId);
            DELETE FROM plantaopro.adm360_recebimentos WHERE pedido_id=@pedidoId;
            DELETE FROM plantaopro.adm360_pedido_itens WHERE pedido_id=@pedidoId;
            DELETE FROM plantaopro.adm360_pedidos WHERE id=@pedidoId;",
            new { tenantId = TenantSantaCasa, pedidoId }, tx);
        if (limparCadastroCompartilhado)
        {
            await cn.ExecuteAsync(@"
                DELETE FROM plantaopro.adm360_lotes WHERE tenant_id=@tenantId AND produto_id=@produtoId;
                DELETE FROM plantaopro.adm360_locais WHERE id=@localId;
                DELETE FROM plantaopro.adm360_produtos WHERE id=@produtoId;
                DELETE FROM plantaopro.adm360_parceiros WHERE id=@parceiroId;",
                new { tenantId = TenantSantaCasa, parceiroId, produtoId, localId }, tx);
        }
        await tx.CommitAsync();
    }

    /// <summary>Seed mínimo da cadeia de compras (parceiro/produto/local) para o cenário de estoque.</summary>
    private static async Task<(Guid ParceiroId, Guid ProdutoId, Guid LocalId)> SeedCadeiaCompraAsync(string cs, string tag)
    {
        var parceiroId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var localId = Guid.NewGuid();
        await using var cnSeed = new NpgsqlConnection(cs);
        await cnSeed.OpenAsync();
        await cnSeed.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES(@id, @tenantId, @nome, @doc, true, true)",
            new { id = parceiroId, tenantId = TenantSantaCasa, nome = $"Fornecedor WsA3 {tag} LTDA", doc = $"WSA3-{tag}-" + Sufixo() });
        await cnSeed.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_produtos(id, tenant_id, sku, nome, unidade, controla_lote, exige_inspecao, preco_custo, ativo)
            VALUES(@id, @tenantId, @sku, 'Produto WsA3', 'UN', false, false, 10.00, true)",
            new { id = produtoId, tenantId = TenantSantaCasa, sku = $"SKU-WSA3-{tag}-" + Sufixo() });
        await cnSeed.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_locais(id, tenant_id, codigo, nome, tipo, ativo)
            VALUES(@id, @tenantId, @codigo, 'Local WsA3', 'INTERNO', true)",
            new { id = localId, tenantId = TenantSantaCasa, codigo = $"LG-WSA3-{tag}-" + Sufixo() });
        return (parceiroId, produtoId, localId);
    }

    // =========================================================================
    // T1 — concorrência: N imports da MESMA chave (arquivo idêntico)
    // =========================================================================
    [Fact(DisplayName = "WS-A3/T1: N importações concorrentes da MESMA chave -> exatamente 1 documento e 1 evento, sem falha")]
    public async Task T1_ConcorrenciaMesmaChave_IdempotenciaExata()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        const int N = 8;
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var xml = NfeXml(chave);
            var tarefas = Enumerable.Range(0, N)
                .Select(_ => Task.Run(() => repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                    new ImportarXmlManualCommand(xml) { NomeArquivo = "wsa3-t1.xml" }, CancellationToken.None)))
                .ToArray();
            var resultados = await Task.WhenAll(tarefas);

            // Nenhuma unidade pode falhar: quem perde a corrida encontra o registro e segue idempotente.
            Assert.All(resultados, r => Assert.Equal(0, r.Falhas));
            var ids = resultados.Select(r => r.Documentos[0].DocumentoId).Distinct().ToArray();
            Assert.Single(ids);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso=@k",
                new { t = TenantSantaCasa, k = chave }));
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_documento_eventos
                WHERE documento_id = (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso=@k)",
                new { t = TenantSantaCasa, k = chave }));
        }
        finally
        {
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // T2 — concorrência: mesma chave com CONTEÚDO DIVERgente
    // =========================================================================
    [Fact(DisplayName = "WS-A3/T2: importação concorrente divergente da mesma chave -> 1 vencedor + erro de negócio explícito")]
    public async Task T2_ConcorrenciaMesmaChaveDivergente_UnicoVencedor_ErroExplicito()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var xmlA = NfeXml(chave, vNf: "110.00");
            var xmlB = NfeXml(chave, vNf: "220.00");
            var tarefaA = repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xmlA) { NomeArquivo = "wsa3-t2-a.xml" }, CancellationToken.None);
            var tarefaB = repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xmlB) { NomeArquivo = "wsa3-t2-b.xml" }, CancellationToken.None);
            await Task.WhenAll(tarefaA, tarefaB);
            var resultados = new[] { await tarefaA, await tarefaB };
            Assert.Equal(1, resultados.Sum(r => r.Importados));
            Assert.Equal(1, resultados.Sum(r => r.Falhas));
            var falho = resultados.Single(r => r.Falhas == 1);
            // Erro de negócio claro (conteúdo divergente OU identidade já registrada) — nunca exceção técnica vazia.
            var msgPerdedor = falho.Documentos[0].MensagemErro ?? string.Empty;
            Assert.True(Regex.IsMatch(msgPerdedor, @"divergente|Identidade fiscal", RegexOptions.IgnoreCase),
                $"Mensagem de falha inesperada na unidade perdedora: {msgPerdedor}");

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso=@k",
                new { t = TenantSantaCasa, k = chave }));
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_documento_eventos
                WHERE documento_id = (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso=@k)",
                new { t = TenantSantaCasa, k = chave }));
        }
        finally
        {
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // T3 — cadeia import -> conferir -> vincular = sequências 1,2,3 + VINCULADO
    // =========================================================================
    [Fact(DisplayName = "WS-A3/T3: importar -> conferir -> vincular -> sequências 1,2,3 contíguas e status VINCULADO")]
    public async Task T3_CadeiaCompleta_SequenciasContiguas_StatusVinculado()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);

        var (parceiroId, produtoId, localId) = await SeedCadeiaCompraAsync(cs, "T3");
        var pedidoId = Guid.Empty;
        try
        {
            var ct = CancellationToken.None;
            var xmlRepo = new DocumentosXmlRepository(cs);
            var comprasRepo = new ComprasRepository(cs);

            var docId = (await xmlRepo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chave)), ct)).Documentos[0].DocumentoId!.Value;

            await xmlRepo.ConferirDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ConferirDocumentoCommand(docId), ct);

            pedidoId = await comprasRepo.CriarAsync(TenantSantaCasa, UsuarioGestor,
                new CriarPedidoCommand(parceiroId, null, 0m,
                    new[] { new PedidoItemCommand(produtoId, 10m, 20m, 0m) }), ct);
            await comprasRepo.AprovarAsync(TenantSantaCasa, UsuarioGestor, pedidoId, "APR-WSA3-" + Guid.NewGuid().ToString("N"), ct);

            await xmlRepo.VincularRecebimentoAsync(TenantSantaCasa, UsuarioGestor,
                new VincularDocumentoRecebimentoCommand(docId, pedidoId, localId, "VNC-WSA3-" + Guid.NewGuid().ToString("N")), ct);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            Assert.Equal("VINCULADO", await cn.ExecuteScalarAsync<string>(
                "SELECT status_conferencia FROM plantaopro.adm360_documentos_recebidos WHERE id=@id", new { id = docId }));
            Assert.True(await cn.ExecuteScalarAsync<bool>(
                "SELECT recebimento_id IS NOT NULL AND pedido_id = @pedidoId FROM plantaopro.adm360_documentos_recebidos WHERE id=@id",
                new { id = docId, pedidoId }));

            var eventos = (await cn.QueryAsync<dynamic>(@"
                SELECT sequencia_evento AS seq, tipo_evento AS tipo
                FROM plantaopro.adm360_documento_eventos
                WHERE documento_id=@id ORDER BY sequencia_evento", new { id = docId })).ToArray();
            Assert.Equal(3, eventos.Length);
            Assert.Equal(new[] { 1, 2, 3 }, eventos.Select(e => (int)e.seq).ToArray());
            Assert.Equal("IMPORTACAO_MANUAL", (string)eventos[0].tipo);
            Assert.Equal("CONFIRMACAO_CONFERENCIA", (string)eventos[1].tipo);
            Assert.Equal("VINCULACAO_RECEBIMENTO", (string)eventos[2].tipo);
        }
        finally
        {
            if (pedidoId != Guid.Empty)
                await PurgeGateCompraAsync(cs, parceiroId, produtoId, localId, pedidoId);
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // T4 — recebimento físico: write-back do vínculo SEM alterar o status (G5)
    // =========================================================================
    [Fact(DisplayName = "WS-A3/T4: recebimento físico -> write-back (recebimento/pedido/título) no doc, status segue CONFERIDO, evento seq 3")]
    public async Task T4_RecebimentoFisico_WriteBackSemMudarStatus()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);

        var (parceiroId, produtoId, localId) = await SeedCadeiaCompraAsync(cs, "T4");
        var pedidoId = Guid.Empty;
        try
        {
            var ct = CancellationToken.None;
            var xmlRepo = new DocumentosXmlRepository(cs);
            var comprasRepo = new ComprasRepository(cs);

            var docId = (await xmlRepo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chave)), ct)).Documentos[0].DocumentoId!.Value;
            await xmlRepo.ConferirDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ConferirDocumentoCommand(docId), ct);

            pedidoId = await comprasRepo.CriarAsync(TenantSantaCasa, UsuarioGestor,
                new CriarPedidoCommand(parceiroId, null, 0m,
                    new[] { new PedidoItemCommand(produtoId, 10m, 20m, 0m) }), ct);
            await comprasRepo.AprovarAsync(TenantSantaCasa, UsuarioGestor, pedidoId, "APR-WSA3-" + Guid.NewGuid().ToString("N"), ct);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var pedidoItemId = await cn.ExecuteScalarAsync<Guid>(
                "SELECT id FROM plantaopro.adm360_pedido_itens WHERE pedido_id=@p AND tenant_id=@t",
                new { p = pedidoId, t = TenantSantaCasa });

            var recId = await comprasRepo.ReceberAsync(TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoId, "NF-e WSA3-T4", "REC-T4-" + Guid.NewGuid().ToString("N"),
                    new[] { new ReceberItemCommand(pedidoItemId, 5m, null, null, localId) }, docId), ct);
            Assert.NotEqual(Guid.Empty, recId);

            // Write-back do vínculo canônico: recebimento/pedido/título preenchidos…
            Assert.True(await cn.ExecuteScalarAsync<bool>(@"
                SELECT recebimento_id = @recId AND pedido_id = @pedidoId AND titulo_pagar_id IS NOT NULL
                FROM plantaopro.adm360_documentos_recebidos WHERE id=@id",
                new { id = docId, recId, pedidoId }));
            // …mas o status NÃO muda (VINCULADO continua exclusivo do vínculo explícito — teste G5).
            Assert.Equal("CONFERIDO", await cn.ExecuteScalarAsync<string>(
                "SELECT status_conferencia FROM plantaopro.adm360_documentos_recebidos WHERE id=@id", new { id = docId }));
            // Trilha auditável: evento VINCULACAO_RECEBIMENTO na sequência 3 (após import 1 + confer 2).
            var ev = (await cn.QueryAsync<dynamic>(@"
                SELECT sequencia_evento AS seq, tipo_evento AS tipo, descricao_evento AS desc
                FROM plantaopro.adm360_documento_eventos WHERE documento_id=@id ORDER BY sequencia_evento",
                new { id = docId })).ToArray();
            Assert.Equal(3, ev.Length);
            Assert.Equal(3, (int)ev[2].seq);
            Assert.Equal("VINCULACAO_RECEBIMENTO", (string)ev[2].tipo);
            Assert.Contains("físico", (string)ev[2].desc, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (pedidoId != Guid.Empty)
                await PurgeGateCompraAsync(cs, parceiroId, produtoId, localId, pedidoId);
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // T5 — segundo recebimento do mesmo documento/pedido REUSA o recebimento canônico
    // =========================================================================
    [Fact(DisplayName = "WS-A3/T5: 2º recebimento do mesmo documento/pedido reusa o recebimento canônico (1 recebimento, 2 movimentos, 1 título acumulado, sem novo evento de vínculo)")]
    public async Task T5_RecebimentoParcialSobreoRecebimentoCanonica()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);

        var (parceiroId, produtoId, localId) = await SeedCadeiaCompraAsync(cs, "T5");
        var pedidoId = Guid.Empty;
        try
        {
            var ct = CancellationToken.None;
            var xmlRepo = new DocumentosXmlRepository(cs);
            var comprasRepo = new ComprasRepository(cs);

            var docId = (await xmlRepo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chave)), ct)).Documentos[0].DocumentoId!.Value;
            await xmlRepo.ConferirDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ConferirDocumentoCommand(docId), ct);

            pedidoId = await comprasRepo.CriarAsync(TenantSantaCasa, UsuarioGestor,
                new CriarPedidoCommand(parceiroId, null, 0m,
                    new[] { new PedidoItemCommand(produtoId, 10m, 20m, 0m) }), ct);
            await comprasRepo.AprovarAsync(TenantSantaCasa, UsuarioGestor, pedidoId, "APR-WSA3-" + Guid.NewGuid().ToString("N"), ct);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var pedidoItemId = await cn.ExecuteScalarAsync<Guid>(
                "SELECT id FROM plantaopro.adm360_pedido_itens WHERE pedido_id=@p AND tenant_id=@t",
                new { p = pedidoId, t = TenantSantaCasa });

            var k1 = "REC-T5A-" + Guid.NewGuid().ToString("N");
            var recId1 = await comprasRepo.ReceberAsync(TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoId, "NF-e WSA3-T5A", k1,
                    new[] { new ReceberItemCommand(pedidoItemId, 5m, null, null, localId) }, docId), ct);

            var k2 = "REC-T5B-" + Guid.NewGuid().ToString("N");
            var recId2 = await comprasRepo.ReceberAsync(TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoId, "NF-e WSA3-T5B", k2,
                    new[] { new ReceberItemCommand(pedidoItemId, 5m, null, null, localId) }, docId), ct);

            // Resolução placeholder-vs-físico: o 2º recebimento SOMA sobre o recebimento canônico.
            Assert.Equal(recId1, recId2);
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_recebimentos WHERE tenant_id=@t AND pedido_id=@p",
                new { t = TenantSantaCasa, p = pedidoId }));
            Assert.Equal(2, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_movimentos
                WHERE tenant_id=@t AND idempotency_key IN (@k1, @k2)",
                new { t = TenantSantaCasa, k1 = k1 + ":" + pedidoItemId.ToString("D"), k2 = k2 + ":" + pedidoItemId.ToString("D") }));
            // WS-A3: um único título por origem canônica (ux_adm360_titulos_pagar_origem);
            // o 2º recebimento ACUMULA (5x20 + 5x20 = 200) em vez de criar outro registro.
            var tituloValor = await cn.ExecuteScalarAsync<decimal>(@"
                SELECT valor_principal FROM plantaopro.adm360_titulos_pagar WHERE tenant_id=@t AND origem_id=@r",
                new { t = TenantSantaCasa, r = recId1 });
            Assert.Equal(200m, tituloValor);
            // Sem evento de vínculo duplicado no documento (write-back só na 1ª vez).
            Assert.Equal(3, await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documento_eventos WHERE documento_id=@id", new { id = docId }));
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_documento_eventos
                WHERE documento_id=@id AND tipo_evento='VINCULACAO_RECEBIMENTO'", new { id = docId }));
            Assert.Equal("RECEBIDO", await cn.ExecuteScalarAsync<string>(
                "SELECT situacao FROM plantaopro.adm360_pedidos WHERE id=@p", new { p = pedidoId }));
        }
        finally
        {
            if (pedidoId != Guid.Empty)
                await PurgeGateCompraAsync(cs, parceiroId, produtoId, localId, pedidoId);
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // T6 — documento já vinculado a OUTRO pedido -> erro de negócio explícito
    // =========================================================================
    [Fact(DisplayName = "WS-A3/T6: receber em outro pedido usando o mesmo documento -> erro explícito (sem violação de constraint)")]
    public async Task T6_OutroPedido_ErroExplicito()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);

        var (parceiroId, produtoId, localId) = await SeedCadeiaCompraAsync(cs, "T6");
        var pedidoAId = Guid.Empty;
        var pedidoBId = Guid.Empty;
        try
        {
            var ct = CancellationToken.None;
            var xmlRepo = new DocumentosXmlRepository(cs);
            var comprasRepo = new ComprasRepository(cs);

            var docId = (await xmlRepo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chave)), ct)).Documentos[0].DocumentoId!.Value;
            await xmlRepo.ConferirDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ConferirDocumentoCommand(docId), ct);

            pedidoAId = await comprasRepo.CriarAsync(TenantSantaCasa, UsuarioGestor,
                new CriarPedidoCommand(parceiroId, null, 0m,
                    new[] { new PedidoItemCommand(produtoId, 10m, 20m, 0m) }), ct);
            await comprasRepo.AprovarAsync(TenantSantaCasa, UsuarioGestor, pedidoAId, "APR-WSA3-" + Guid.NewGuid().ToString("N"), ct);
            pedidoBId = await comprasRepo.CriarAsync(TenantSantaCasa, UsuarioGestor,
                new CriarPedidoCommand(parceiroId, null, 0m,
                    new[] { new PedidoItemCommand(produtoId, 10m, 20m, 0m) }), ct);
            await comprasRepo.AprovarAsync(TenantSantaCasa, UsuarioGestor, pedidoBId, "APR-WSA3-" + Guid.NewGuid().ToString("N"), ct);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var itemIdA = await cn.ExecuteScalarAsync<Guid>(
                "SELECT id FROM plantaopro.adm360_pedido_itens WHERE pedido_id=@p AND tenant_id=@t LIMIT 1",
                new { p = pedidoAId, t = TenantSantaCasa });
            var itemIdB = await cn.ExecuteScalarAsync<Guid>(
                "SELECT id FROM plantaopro.adm360_pedido_itens WHERE pedido_id=@p AND tenant_id=@t LIMIT 1",
                new { p = pedidoBId, t = TenantSantaCasa });

            // 1º recebimento (pedido A) cria o vínculo canônico no documento.
            await comprasRepo.ReceberAsync(TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoAId, "NF-e WSA3-T6A", "REC-T6A-" + Guid.NewGuid().ToString("N"),
                    new[] { new ReceberItemCommand(itemIdA, 5m, null, null, localId) }, docId), ct);

            // 2º recebimento (pedido B) com o MESMO documento: conflito explícito de negócio.
            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() => comprasRepo.ReceberAsync(
                TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoBId, "NF-e WSA3-T6B", "REC-T6B-" + Guid.NewGuid().ToString("N"),
                    new[] { new ReceberItemCommand(itemIdB, 5m, null, null, localId) }, docId), ct));
            Assert.Contains("outro pedido", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (pedidoAId != Guid.Empty)
                await PurgeGateCompraAsync(cs, parceiroId, produtoId, localId, pedidoAId, limparCadastroCompartilhado: false);
            if (pedidoBId != Guid.Empty)
                await PurgeGateCompraAsync(cs, parceiroId, produtoId, localId, pedidoBId);
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // T7 — colisão de identidade fiscal (5-tupla) com BYTES DIFERENTES (sequencial)
    // =========================================================================
    [Fact(DisplayName = "WS-A3/T7: mesmo emitente/modelo/número/série com chaves e bytes distintos -> erro 'Identidade fiscal', sem duplicar")]
    public async Task T7_ColisaoIdentidadeFiscal_BytesDistintos()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        string D(int n) => string.Concat(Enumerable.Range(0, n).Select(_ => (char)Random.Shared.Next('1', '9')));
        var numeroComum = D(9);
        var k1 = ChaveNfeComNumeroCompartilhado(numeroComum);
        var k2 = ChaveNfeComNumeroCompartilhado(numeroComum);
        Assert.NotEqual(k1, k2);
        await PurgePorChavesAsync(cs, k1, k2);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var ct = CancellationToken.None;

            var r1 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(k1, vNf: "110.00")), ct);
            Assert.Equal(1, r1.Importados);
            Assert.Equal(0, r1.Falhas);

            // Bytes distintos (vNF diferente) -> o documento NÃO é idempotente: a identidade
            // fiscal já ocupada produz erro de negócio explícito (nunca duplicata silenciosa).
            var r2 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(k2, vNf: "220.00")), ct);
            Assert.Equal(0, r2.Importados);
            Assert.Equal(1, r2.Falhas);
            Assert.Contains("Identidade fiscal", r2.Documentos[0].MensagemErro!, StringComparison.OrdinalIgnoreCase);
            Assert.Null(r2.Documentos[0].DocumentoId);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso = ANY(@ks)",
                new { t = TenantSantaCasa, ks = new[] { k1, k2 } }));
        }
        finally
        {
            await PurgePorChavesAsync(cs, k1, k2);
        }
    }

    // =========================================================================
    // T8 — quarentena: MALFORMADOs distintos coexistem (identidade fora do índice parcial)
    // =========================================================================
    [Fact(DisplayName = "WS-A3/T8: dois XMLs MALFORMADO distintos coexistem em quarentena (identidade fiscal fora do índice)")]
    public async Task T8_QuarentenaMalformados_CoexistemSemColisao()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var bytesA = Encoding.UTF8.GetBytes("<exportador><documento>conteudo truncado A sem fechar");
        var bytesB = Encoding.UTF8.GetBytes("<foo bar=\"2\"/>");
        var kA = "MALF" + Sha256Hex(bytesA).Substring(0, 33);
        var kB = "MALF" + Sha256Hex(bytesB).Substring(0, 33);
        Assert.NotEqual(kA, kB);
        await PurgePorChavesAsync(cs, kA, kB);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var ct = CancellationToken.None;

            var rA = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(Encoding.UTF8.GetString(bytesA)) { NomeArquivo = "wsa3-t8-a.xml", XmlBytes = bytesA }, ct);
            Assert.Equal(1, rA.EmQuarentena);
            Assert.Equal("XML_MALFORMADO", rA.Documentos[0].MotivoQuarentena);

            var rB = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(Encoding.UTF8.GetString(bytesB)) { NomeArquivo = "wsa3-t8-b.xml", XmlBytes = bytesB }, ct);
            Assert.Equal(1, rB.EmQuarentena);
            Assert.Equal("XML_MALFORMADO", rB.Documentos[0].MotivoQuarentena);

            // Os dois documentos ficam persistidos em quarentena — o índice parcial de
            // identidade fiscal (WHERE quarentena = false) não bloqueia triagem posterior.
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            Assert.Equal(2, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_documentos_recebidos
                WHERE tenant_id=@t AND chave_acesso = ANY(@ks) AND quarentena = true",
                new { t = TenantSantaCasa, ks = new[] { kA, kB } }));
        }
        finally
        {
            await PurgePorChavesAsync(cs, kA, kB);
        }
    }
}
