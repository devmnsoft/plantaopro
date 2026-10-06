using Dapper;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// A3 — Central de documentos XML (continuação do B1): parsing por BYTES respeitando o encoding
/// declarado (ISO-8859-1), múltiplos documentos por arquivo (infNFe &gt; ABRASF &gt; NFS-e),
/// taxonomia de quarentena (XML_MALFORMADO / MODELO_NAO_SUPORTADO / DOCUMENTO_INCOMPLETO /
/// DESTINATARIO_NAO_AUTORIZADO) com chaves sintéticas determinísticas, ABRASF clínico sem validação
/// de destinatário, falha por unidade SEM exceção derrubando o arquivo, conferência autorizada como
/// gate de recebimento de estoque e nome do arquivo original preservado.
/// Padrão B1: tenant fixo compartilhado, chaves geradas por execução, purge escopado em try/finally.
/// </summary>
public sealed class Administrativo360DocumentosXmlA3Tests
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

    /// <summary>Chave fixa característica da prescrição ABRASF deste teste (dígitos do atributo Id).</summary>
    private const string ChaveAbrsfComId = "442610987654321";

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

    // =========================================================================
    // Helpers de fixture
    // =========================================================================

    /// <summary>
    /// Chave de acesso NF-e única por execução: 44 dígitos com o modelo '55' na posição
    /// exigida por XmlDocumentoRegras.ValidarChaveAcesso (SUBSTRING(chave, 20, 2) = '55').
    /// </summary>
    private static string ChaveNfeUnica()
    {
        string D(int n) => string.Concat(Enumerable.Range(0, n).Select(_ => (char)Random.Shared.Next('0', '9')));
        return "442609" + D(14) + "55" + D(22); // 6 + 14 + 2 + 22 = 44
    }

    /// <summary>Corpo interno do &lt;infNFe&gt; (ide/emit/dest/total) ajustável aos cenários A3.</summary>
    private static string CorpoInfNfe(string chaveAcesso, string? modIde = null, bool incluirDest = true,
        string xNomeEmitente = "Emitente Teste A3 LTDA")
    {
        var mod = modIde ?? chaveAcesso.Substring(20, 2);
        var dest = incluirDest
            ? $@"<dest>
              <CNPJ>{CnpjDestinoAutorizado}</CNPJ>
              <xNome>Destinatario Santa Casa SA</xNome>
              <enderDest><xLgr>Rua B</xLgr><xMun>Cidade</xMun><UF>MG</UF></enderDest>
            </dest>"
            : string.Empty;
        return $@"<ide>
    <cUF>44</cUF>
    <mod>{mod}</mod>
    <serie>1</serie>
    <nNF>{chaveAcesso.Substring(25, 9)}</nNF>
    <dhEmi>2026-10-03T10:00:00-03:00</dhEmi>
  </ide>
  <emit>
    <CNPJ>11222333000181</CNPJ>
    <xNome>{xNomeEmitente}</xNome>
    <enderEmit><xLgr>Rua A</xLgr><xMun>Cidade</xMun><UF>MG</UF></enderEmit>
  </emit>
  {dest}
  <total>
    <ICMSTot>
      <vBC>100.00</vBC>
      <vNF>110.00</vNF>
      <vProd>100.00</vProd>
    </ICMSTot>
  </total>";
    }

    /// <summary>NF-e/NFC-e minimalista completa (com declaração XML) para importação direta.</summary>
    private static string NfeXml(string chaveAcesso, string? modIde = null, bool incluirDest = true,
        bool comAtributoId = true)
    {
        var attrs = comAtributoId ? $"Id=\"NFe{chaveAcesso}\" " : string.Empty;
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<infNFe {attrs}versao=""4.00"">
{CorpoInfNfe(chaveAcesso, modIde, incluirDest)}
</infNFe>";
    }

    /// <summary>Purge escopado do cenário de gate de estoque (IDs exclusivos deste teste).</summary>
    private static async Task PurgeGateCompraAsync(string cs, Guid parceiroId, Guid produtoId, Guid localId, Guid pedidoId)
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
            DELETE FROM plantaopro.adm360_pedidos WHERE id=@pedidoId;
            DELETE FROM plantaopro.adm360_lotes WHERE tenant_id=@tenantId AND produto_id=@produtoId;
            DELETE FROM plantaopro.adm360_locais WHERE id=@localId;
            DELETE FROM plantaopro.adm360_produtos WHERE id=@produtoId;
            DELETE FROM plantaopro.adm360_parceiros WHERE id=@parceiroId;",
            new { tenantId = TenantSantaCasa, parceiroId, produtoId, localId, pedidoId }, tx);
        await tx.CommitAsync();
    }

    // =========================================================================
    // G1 — parsing por bytes respeitando o encoding declarado (ISO-8859-1)
    // =========================================================================
    [Fact(DisplayName = "A3/G1: XML ISO-8859-1 — texto decodificado pelo encoding declarado, hash sobre os bytes exatos")]
    public async Task G1_EncodingIso88591_RespeitadoNosBytes()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);
        try
        {
                    const string xNome = "HOSPITAL SãO CASA DEMO";
            var xmlTexto = $@"<?xml version=""1.0"" encoding=""ISO-8859-1""?>
<infNFe Id=""NFe{chave}"" versao=""4.00"">
{CorpoInfNfe(chave, xNomeEmitente: xNome)}
</infNFe>";

            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(xmlTexto);
            // 'ã' em Latin-1 é o byte 0xE3 (em UTF-8 viraria o par C3 A3).
            Assert.Contains((byte)0xE3, bytes);

            var repo = new DocumentosXmlRepository(cs);
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xmlTexto) { NomeArquivo = "a3-latin1.xml", XmlBytes = bytes },
                CancellationToken.None);

            Assert.Equal(1, res.TotalUnidades);
            Assert.Equal(1, res.Importados);
            Assert.Equal(0, res.Falhas);
            var id = res.Documentos[0].DocumentoId!.Value;

            var d = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, id);
            Assert.NotNull(d);
            // Texto decodificado com o encoding declarado — sem mojibake ("HOSPITAL Ã£o CASA").
            Assert.Equal(xNome, d!.EmitenteNome);
            Assert.Contains(xNome, d.XmlConteudo);
            // Hash calculado sobre os BYTES LATIN-1 exatos, não sobre a re-encoding UTF-8.
            Assert.Equal(Sha256Hex(bytes), d.XmlHash);
            Assert.NotEqual(Sha256Hex(Encoding.UTF8.GetBytes(xmlTexto)), d.XmlHash);
            Assert.Equal("a3-latin1.xml", d.NomeArquivo); // A3/G6

            var baixado = await repo.ObterXmlBytesAsync(TenantSantaCasa, id);
            Assert.NotNull(baixado);
            Assert.True(baixado!.Value.Bytes.AsSpan().SequenceEqual(bytes.AsSpan()),
                "O download deve devolver os bytes exatos do arquivo recebido (ISO-8859-1 incluído).");
        }
        finally
        {
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // G2 — múltiplos documentos em um único arquivo
    // =========================================================================
    [Fact(DisplayName = "A3/G2: arquivo com dois nfeProc — importa as DUAS unidades com o mesmo nome de arquivo")]
    public async Task G2_MultiDocumento_DuasUnidadesInfNFe_MesmoArquivo()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var k1 = ChaveNfeUnica();
        var k2 = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, k1, k2);
        try
        {
            var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<exportador>
  <nfeProc versao=""4.00""><NFe><infNFe Id=""NFe{k1}"">{CorpoInfNfe(k1)}</infNFe></NFe></nfeProc>
  <nfeProc versao=""4.00""><NFe><infNFe Id=""NFe{k2}"">{CorpoInfNfe(k2)}</infNFe></NFe></nfeProc>
</exportador>";

            var repo = new DocumentosXmlRepository(cs);
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xml) { NomeArquivo = "a3-multidoc.xml" }, CancellationToken.None);

            Assert.Equal(2, res.TotalUnidades);
            Assert.Equal(2, res.Importados);
            Assert.Equal(0, res.EmQuarentena);
            Assert.Equal(0, res.DuplicadosIgnorados);
            Assert.Equal(0, res.Falhas);
            Assert.Equal(2, res.Documentos.Count);
            Assert.All(res.Documentos, r => Assert.False(r.Quarentena));

            var id1 = res.Documentos[0].DocumentoId!.Value;
            var id2 = res.Documentos[1].DocumentoId!.Value;
            Assert.NotEqual(id1, id2);

            // Ambas as unidades persistiram o MESMO nome de arquivo original (A3/G6).
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var comNome = await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_documentos_recebidos
                WHERE tenant_id=@t AND chave_acesso = ANY(@chaves) AND nome_arquivo='a3-multidoc.xml'",
                new { t = TenantSantaCasa, chaves = new[] { k1, k2 } });
            Assert.Equal(2, comNome);
        }
        finally
        {
            await PurgePorChavesAsync(cs, k1, k2);
        }
    }

    [Fact(DisplayName = "A3/G2: chave duplicada DENTRO do arquivo — 2ª ocorrência vira duplicado idempotente sem novo registro")]
    public async Task G2_DuplicadoIntraArquivo_SegundaOcorrenciaIdempotente()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);
        try
        {
            var xml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<exportador>
  <nfeProc versao=""4.00""><NFe><infNFe Id=""NFe{chave}"">{CorpoInfNfe(chave)}</infNFe></NFe></nfeProc>
  <nfeProc versao=""4.00""><NFe><infNFe Id=""NFe{chave}"">{CorpoInfNfe(chave)}</infNFe></NFe></nfeProc>
</exportador>";

            var repo = new DocumentosXmlRepository(cs);
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xml) { NomeArquivo = "a3-dupintra.xml" }, CancellationToken.None);

            Assert.Equal(2, res.TotalUnidades);
            Assert.Equal(1, res.Importados);
            Assert.Equal(1, res.DuplicadosIgnorados);
            Assert.Equal(0, res.EmQuarentena);
            Assert.Equal(0, res.Falhas);

            Assert.False(res.Documentos[0].DuplicadoIdempotente);
            var idPrimeira = res.Documentos[0].DocumentoId!.Value;
            Assert.True(res.Documentos[1].DuplicadoIdempotente);
            Assert.Equal(idPrimeira, res.Documentos[1].DocumentoId!.Value);

            // Um único registro e um único evento de importação para a chave.
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var linhas = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso=@k",
                new { t = TenantSantaCasa, k = chave });
            Assert.Equal(1, linhas);
            var eventos = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documento_eventos WHERE documento_id=(SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso=@k)",
                new { t = TenantSantaCasa, k = chave });
            Assert.Equal(1, eventos);
        }
        finally
        {
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // G4 — ABRASF (prescrição eletrônica clínica)
    // =========================================================================
    [Fact(DisplayName = "A3/G4: ABRASF — tipo/modelo próprios, valor zero, destinatário NÃO validado, chave do Id (ou sintética sem Id)")]
    public async Task G4_Abrsf_Clinico_SemValidacaoDeDestinatario()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        const string cnpjForaDoTenant = "99887766000155";

        var xmlComId = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<ABRASF Id=""ABR-{ChaveAbrsfComId}X"" versao=""1.0"">
  <pRes Id=""RES-98765"">
    <nNumero>12345</nNumero>
    <dhEmi>2026-10-01T08:00:00-03:00</dhEmi>
    <emit><CNPJ>11222333000181</CNPJ><xNome>MEDCLINICA CLINICA MEDICA LTDA</xNome></emit>
    <tomador><CNPJ>{cnpjForaDoTenant}</CNPJ><xNome>DESTINATARIO FORA DO TENANT</xNome></tomador>
  </pRes>
</ABRASF>";

        var xmlSemId = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<pRes>
  <nNumero>777</nNumero>
  <emit><xNome>MEDCLINICA CLINICA MEDICA LTDA</xNome></emit>
</pRes>";
        var chaveSemId = "ABR" + Sha256Hex(Encoding.UTF8.GetBytes(xmlSemId)).Substring(0, 33);

        await PurgePorChavesAsync(cs, ChaveAbrsfComId, chaveSemId);
        try
        {
            var repo = new DocumentosXmlRepository(cs);

            // 1) Com Id: chave = dígitos do Id; destinatário FORA do tenant e nem por isso vai a quarentena.
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xmlComId) { NomeArquivo = "a3-abr-comid.xml" }, CancellationToken.None);
            Assert.Equal(1, res.TotalUnidades);
            Assert.Equal(1, res.Importados);
            Assert.Equal(0, res.EmQuarentena);
            var id = res.Documentos[0].DocumentoId!.Value;

            var d = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, id);
            Assert.NotNull(d);
            Assert.False(d!.Quarentena);
            Assert.Equal("ABRASF", d.TipoDocumento);
            Assert.Equal("ABR", d.Modelo);
            Assert.Equal(ChaveAbrsfComId, d.ChaveAcesso);
            Assert.Equal("12345", d.Numero);
            Assert.Equal(0m, d.ValorTotal);
            Assert.Equal(cnpjForaDoTenant, d.DestinatarioCnpj);
            Assert.Equal(new DateTime(2026, 10, 1, 11, 0, 0, DateTimeKind.Utc), d.DataEmissao);

            // 2) Sem Id: chave sintética determinística por conteúdo (reimportável/idempotente).
            var res2 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xmlSemId) { NomeArquivo = "a3-abr-semid.xml" }, CancellationToken.None);
            Assert.Equal(1, res2.TotalUnidades);
            Assert.Equal(1, res2.Importados);
            Assert.Equal(chaveSemId, res2.Documentos[0].ChaveAcesso);
            var d2 = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, res2.Documentos[0].DocumentoId!.Value);
            Assert.Equal("ABRASF", d2!.TipoDocumento);
            Assert.Equal("777", d2.Numero);
        }
        finally
        {
            await PurgePorChavesAsync(cs, ChaveAbrsfComId, chaveSemId);
        }
    }

    // =========================================================================
    // G3 — DOCUMENTO_INCOMPLETO (chave preservada quando couber; sintética quando ausente)
    // =========================================================================
    [Fact(DisplayName = "A3/G3: INCOMPLETO preserva a chave de acesso quando existente; sintética determinística sem chave")]
    public async Task G3_Incompleto_PreservaChave_E_SinteticaQuandoAusente()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var kComChave = ChaveNfeUnica();
        var kBaseSemChave = ChaveNfeUnica(); // usada só nos campos internos (nNF); o atributo Id é omitido
        var xmlSemId = NfeXml(kBaseSemChave, comAtributoId: false);
        var chaveSintetica = "INC" + Sha256Hex(Encoding.UTF8.GetBytes(xmlSemId)).Substring(0, 33);

        await PurgePorChavesAsync(cs, kComChave, chaveSintetica);
        try
        {
            var repo = new DocumentosXmlRepository(cs);

            // (a) Seções ausentes (<dest>) com chave válida de 44 dígitos -> chave PRESERVADA como diagnóstico.
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(kComChave, incluirDest: false)), CancellationToken.None);
            Assert.Equal(1, res.TotalUnidades);
            Assert.Equal(0, res.Importados);
            Assert.Equal(1, res.EmQuarentena);
            var r = res.Documentos[0];
            Assert.True(r.Quarentena);
            Assert.Equal("DOCUMENTO_INCOMPLETO", r.MotivoQuarentena);
            Assert.Equal(kComChave, r.ChaveAcesso); // 44 <= 60 -> preservada
            Assert.NotNull(r.DocumentoId); // quarentena também persiste (para triagem posterior)

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var status = await cn.ExecuteScalarAsync<string>(
                "SELECT status_conferencia FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso=@k",
                new { t = TenantSantaCasa, k = kComChave });
            Assert.Equal("DIVERGENTE", status);

            // (b) Todas as seções presentes mas SEM atributo Id -> chave vazia -> sintética determinística.
            var res2 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xmlSemId), CancellationToken.None);
            Assert.Equal(1, res2.EmQuarentena);
            Assert.Equal("DOCUMENTO_INCOMPLETO", res2.Documentos[0].MotivoQuarentena);
            Assert.Equal(chaveSintetica, res2.Documentos[0].ChaveAcesso);
            Assert.True(res2.Documentos[0].ChaveAcesso.Length <= 60);
        }
        finally
        {
            await PurgePorChavesAsync(cs, kComChave, chaveSintetica);
        }
    }

    // =========================================================================
    // G3/G1 — XML_MALFORMADO (chave sintética determinística; reimportação idempotente)
    // =========================================================================
    [Fact(DisplayName = "A3/G3: MALFORMADO — chave sintética determinística por conteúdo, reimportação idempotente; raiz desconhecida segue a mesma regra")]
    public async Task G3_Malformado_ChaveSinteticaDeterministica_Idempotente()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var bytesTrunc = Encoding.UTF8.GetBytes("<exportador><documento>conteudo truncado sem fechar");
        var chaveMalf = "MALF" + Sha256Hex(bytesTrunc).Substring(0, 33);
        var xmlFoo = "<foo bar=\"1\"/>";
        var chaveFoo = "MALF" + Sha256Hex(Encoding.UTF8.GetBytes(xmlFoo)).Substring(0, 33);

        await PurgePorChavesAsync(cs, chaveMalf, chaveFoo);
        try
        {
            var repo = new DocumentosXmlRepository(cs);

            // Arquivo ilegível: UMA unidade em quarentena XML_MALFORMADO, SEM exceção no retorno.
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(Encoding.UTF8.GetString(bytesTrunc)) { NomeArquivo = "a3-truncado.xml", XmlBytes = bytesTrunc },
                CancellationToken.None);
            Assert.Equal(1, res.TotalUnidades);
            Assert.Equal(0, res.Importados);
            Assert.Equal(1, res.EmQuarentena);
            Assert.Equal(0, res.Falhas);
            var r = res.Documentos[0];
            Assert.Equal(chaveMalf, r.ChaveAcesso);
            Assert.Equal("XML_MALFORMADO", r.MotivoQuarentena);
            var id = r.DocumentoId!.Value;

            // Reimportar o MESMO arquivo corrompido é idempotente (mesma chave, mesmo documento).
            var res2 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(Encoding.UTF8.GetString(bytesTrunc)) { NomeArquivo = "a3-truncado.xml", XmlBytes = bytesTrunc },
                CancellationToken.None);
            Assert.Equal(1, res2.DuplicadosIgnorados);
            Assert.Equal(id, res2.Documentos[0].DocumentoId!.Value);
            Assert.True(res2.Documentos[0].DuplicadoIdempotente);

            // Legível porém com raiz não reconhecida por nenhuma família -> mesma classificação.
            var res3 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xmlFoo), CancellationToken.None);
            Assert.Equal(1, res3.EmQuarentena);
            Assert.Equal("XML_MALFORMADO", res3.Documentos[0].MotivoQuarentena);
            Assert.NotEqual(chaveMalf, chaveFoo);
            Assert.Equal(chaveFoo, res3.Documentos[0].ChaveAcesso);
        }
        finally
        {
            await PurgePorChavesAsync(cs, chaveMalf, chaveFoo);
        }
    }

    // =========================================================================
    // G3 — MODELO_NAO_SUPORTADO (<ide mod> divergente da chave)
    // =========================================================================
    [Fact(DisplayName = "A3/G3: <ide mod> divergente da chave -> MODELO_NAO_SUPORTADO em quarentena com a chave preservada")]
    public async Task G3_ModeloNaoSuportado_ModDivergenteDaChave()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var chave = ChaveNfeUnica(); // modelo '55' na posição canônica
        await PurgePorChavesAsync(cs, chave);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chave, modIde: "57")), CancellationToken.None);

            Assert.Equal(1, res.TotalUnidades);
            Assert.Equal(1, res.EmQuarentena);
            Assert.Equal(0, res.Importados);
            Assert.Equal("MODELO_NAO_SUPORTADO", res.Documentos[0].MotivoQuarentena);
            Assert.Equal(chave, res.Documentos[0].ChaveAcesso); // preservada (44 <= 60)

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var motivo = await cn.ExecuteScalarAsync<string>(
                "SELECT motivo_quarentena FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id=@t AND chave_acesso=@k",
                new { t = TenantSantaCasa, k = chave });
            Assert.Equal("MODELO_NAO_SUPORTADO", motivo);
        }
        finally
        {
            await PurgePorChavesAsync(cs, chave);
        }
    }

    // =========================================================================
    // G5 — conferência autorizada: idempotente e bloqueada em quarentena
    // =========================================================================
    [Fact(DisplayName = "A3/G5: conferir — autoriza (status CONFERIDO + evento seq 2 + evento global), é idempotente e quarentena bloqueia")]
    public async Task G5_Conferir_Idempotente_QuarentenaBloqueia()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var kOk = ChaveNfeUnica();
        var kQuar = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, kOk, kQuar);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var idOk = (await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(kOk)), CancellationToken.None)).Documentos[0].DocumentoId!.Value;
            var idQuar = (await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(kQuar, incluirDest: false)), CancellationToken.None)).Documentos[0].DocumentoId!.Value;

            await repo.ConferirDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ConferirDocumentoCommand(idOk), CancellationToken.None);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            Assert.Equal("CONFERIDO", await cn.ExecuteScalarAsync<string>(
                "SELECT status_conferencia FROM plantaopro.adm360_documentos_recebidos WHERE id=@id", new { id = idOk }));
            Assert.Equal(2, await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documento_eventos WHERE documento_id=@id", new { id = idOk }));
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_documento_eventos
                WHERE documento_id=@id AND tipo_evento='CONFIRMACAO_CONFERENCIA' AND sequencia_evento=2", new { id = idOk }));
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_eventos
                WHERE tenant_id=@t AND entidade_id=@id AND tipo_evento='CONFIRMACAO_CONFERENCIA'",
                new { t = TenantSantaCasa, id = idOk }));

            // Conferir de novo: silêncio (sem novo evento, sem novo status).
            await repo.ConferirDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ConferirDocumentoCommand(idOk), CancellationToken.None);
            Assert.Equal(2, await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documento_eventos WHERE documento_id=@id", new { id = idOk }));
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_eventos
                WHERE tenant_id=@t AND entidade_id=@id AND tipo_evento='CONFIRMACAO_CONFERENCIA'",
                new { t = TenantSantaCasa, id = idOk }));

            // Documento em quarentena NÃO pode ser conferido.
            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.ConferirDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ConferirDocumentoCommand(idQuar), CancellationToken.None));
            Assert.Contains("quarentena", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await PurgePorChavesAsync(cs, kOk, kQuar);
        }
    }

    // =========================================================================
    // G5 — gate de estoque: pendente/quarentena bloqueiam, conferido libera, legado (sem doc) passa
    // =========================================================================
    [Fact(DisplayName = "A3/G5: gate de recebimento — PENDENTE e QUARENTENA bloqueiam estoque; CONFERIDO libera (lote+movimento); sem documento = legado")]
    public async Task G5_GateEstoque_ConferenciaAutorizada()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var kDoc = ChaveNfeUnica();
        var kQuar = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, kDoc, kQuar);

        var parceiroId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var localId = Guid.NewGuid();
        var pedidoId = Guid.Empty;
        try
        {
            await using (var cnSeed = new NpgsqlConnection(cs))
            {
                await cnSeed.OpenAsync();
                await cnSeed.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, documento, fornecedor, ativo)
                    VALUES(@id, @tenantId, 'Fornecedor Gate A3 LTDA', @doc, true, true)",
                    new { id = parceiroId, tenantId = TenantSantaCasa, doc = "A3GATE-" + Sufixo() });
                await cnSeed.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_produtos(id, tenant_id, sku, nome, unidade, controla_lote, exige_inspecao, preco_custo, ativo)
                    VALUES(@id, @tenantId, @sku, 'Produto Gate A3', 'UN', false, false, 10.00, true)",
                    new { id = produtoId, tenantId = TenantSantaCasa, sku = "SKU-A3GATE-" + Sufixo() });
                await cnSeed.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_locais(id, tenant_id, codigo, nome, tipo, ativo)
                    VALUES(@id, @tenantId, @codigo, 'Local Gate A3', 'INTERNO', true)",
                    new { id = localId, tenantId = TenantSantaCasa, codigo = "LG-A3-" + Sufixo() });
            }

            var comprasRepo = new ComprasRepository(cs);
            var xmlRepo = new DocumentosXmlRepository(cs);
            var ct = CancellationToken.None;

            // Documentos: um válido (destinatário autorizado) e um incompleto (quarentena).
            var docId = (await xmlRepo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(kDoc)), ct)).Documentos[0].DocumentoId!.Value;
            var docQuarId = (await xmlRepo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(kQuar, incluirDest: false)), ct)).Documentos[0].DocumentoId!.Value;

            // Pedido aprovado com saldo 10 (dois recebimentos de 5).
            pedidoId = await comprasRepo.CriarAsync(TenantSantaCasa, UsuarioGestor,
                new CriarPedidoCommand(parceiroId, null, 0m,
                    new[] { new PedidoItemCommand(produtoId, 10m, 20m, 0m) }), ct);
            await comprasRepo.AprovarAsync(TenantSantaCasa, UsuarioGestor, pedidoId, "APR-A3-" + Guid.NewGuid().ToString("N"), ct);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var pedidoItemId = await cn.ExecuteScalarAsync<Guid>(
                "SELECT id FROM plantaopro.adm360_pedido_itens WHERE pedido_id=@p AND tenant_id=@t",
                new { p = pedidoId, t = TenantSantaCasa });

            // 1) Documento PENDENTE: bloqueio antes da conferência autorizada.
            var exPend = await Assert.ThrowsAsync<Administrativo360BusinessException>(() => comprasRepo.ReceberAsync(
                TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoId, "NF-e GATE-A3-1", "REC-A3P-" + Guid.NewGuid().ToString("N"),
                    new[] { new ReceberItemCommand(pedidoItemId, 5m, null, null, localId) }, docId), ct));
            Assert.Contains("confirme a conferência", exPend.Message, StringComparison.OrdinalIgnoreCase);

            // 2) Documento em QUARENTENA: bloqueio específico.
            var exQuar = await Assert.ThrowsAsync<Administrativo360BusinessException>(() => comprasRepo.ReceberAsync(
                TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoId, "NF-e GATE-A3-2", "REC-A3Q-" + Guid.NewGuid().ToString("N"),
                    new[] { new ReceberItemCommand(pedidoItemId, 5m, null, null, localId) }, docQuarId), ct));
            Assert.Contains("quarentena", exQuar.Message, StringComparison.OrdinalIgnoreCase);

            // 3) Conferido: o recebimento LIBERA — lote + movimento de entrada criados.
            await xmlRepo.ConferirDocumentoAsync(TenantSantaCasa, UsuarioGestor, new ConferirDocumentoCommand(docId), ct);
            var keyRec = "REC-A3-" + Guid.NewGuid().ToString("N");
            var recId = await comprasRepo.ReceberAsync(TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoId, "NF-e GATE-A3-3", keyRec,
                    new[] { new ReceberItemCommand(pedidoItemId, 5m, null, null, localId) }, docId), ct);
            Assert.NotEqual(Guid.Empty, recId);

            // 4) Legado: recebimento SEM documento XML (nulo) passa sem gate (comportamento anterior).
            var keyLeg = "REC-A3L-" + Guid.NewGuid().ToString("N");
            var recLegId = await comprasRepo.ReceberAsync(TenantSantaCasa, UsuarioGestor,
                new ConfirmarRecebimentoCommand(pedidoId, "NF-e GATE-A3-4", keyLeg,
                    new[] { new ReceberItemCommand(pedidoItemId, 5m, null, null, localId) }, null), ct);
            Assert.NotEqual(Guid.Empty, recLegId);
            Assert.NotEqual(recId, recLegId);

            // Efeitos reais no estoque: UM lote (SEM-LOTE, upsert), DOIS movimentos ENTRADA, itens LIBERADOS, pedido RECEBIDO.
            Assert.Equal(1, await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_lotes WHERE tenant_id=@t AND produto_id=@p AND codigo='SEM-LOTE'",
                new { t = TenantSantaCasa, p = produtoId }));
            Assert.Equal(2, await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*) FROM plantaopro.adm360_movimentos
                WHERE tenant_id=@t AND idempotency_key IN (@k1, @k2)",
                new { t = TenantSantaCasa, k1 = keyRec + ":" + pedidoItemId.ToString("D"), k2 = keyLeg + ":" + pedidoItemId.ToString("D") }));
            var condicoes = (await cn.QueryAsync<string>(
                "SELECT DISTINCT condicao FROM plantaopro.adm360_recebimento_itens WHERE recebimento_id IN (@r1, @r2)",
                new { r1 = recId, r2 = recLegId })).ToArray();
            Assert.Equal(new[] { "LIBERADO" }, condicoes);
            Assert.Equal("RECEBIDO", await cn.ExecuteScalarAsync<string>(
                "SELECT situacao FROM plantaopro.adm360_pedidos WHERE id=@p", new { p = pedidoId }));

            // O gate NÃO muda o status do documento (VINCULADO é exclusivo do vínculo explícito).
            Assert.Equal("CONFERIDO", await cn.ExecuteScalarAsync<string>(
                "SELECT status_conferencia FROM plantaopro.adm360_documentos_recebidos WHERE id=@id", new { id = docId }));
        }
        finally
        {
            if (pedidoId != Guid.Empty)
                await PurgeGateCompraAsync(cs, parceiroId, produtoId, localId, pedidoId);
            await PurgePorChavesAsync(cs, kDoc, kQuar);
        }
    }
}
