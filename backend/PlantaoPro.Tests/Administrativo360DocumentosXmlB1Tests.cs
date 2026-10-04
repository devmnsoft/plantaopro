using Dapper;
using System.Security.Cryptography;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// B1 — Central de documentos XML: identificação por CONTEÚDO (NF-e 55 / NFC-e 65 / NFS-e 67),
/// preservação byte-a-byte do arquivo original, DTD/DOCTYPE sem fetch, duplicidade idempotente
/// e quarentena por conteúdo divergente. Fixtures em Fixtures/Administrativo360Xml/.
/// </summary>
public sealed class Administrativo360DocumentosXmlB1Tests
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

    // Chaves fixas dos fixtures (44 dígitos, modelo nas posições 21-22).
    private const string ChaveA = "35260911222333000181550010000001010000000101"; // NF-e 55
    private const string ChaveB = "43261011222333000181650010000002020000000202"; // NFC-e 65
    private const string ChaveC = "1001";                                          // NFS-e (numNFS)
    private const string ChaveD = "35260911222333000181570010000003030000000303"; // mod 57

    private static string CaminhoFixture(string nome) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Administrativo360Xml", nome);

    private static string Sha256Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

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
    // FIXTURE A — NF-e 55 completo (namespace + BOM): bytes preservados byte-a-byte
    // =========================================================================
    [Fact]
    public async Task FixtureA_Nfe55_BomPreservado_PersisteBytesETipoCompleto()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await PurgePorChavesAsync(cs, ChaveA);
        try
        {
            var bytesArquivo = File.ReadAllBytes(CaminhoFixture("b1-a-nfe55.xml"));
            Assert.Equal(0xEF, bytesArquivo[0]); // o arquivo-fonte TEM BOM EF BB BF
            var texto = File.ReadAllText(CaminhoFixture("b1-a-nfe55.xml")); // ReadAllText consome o BOM

            var repo = new DocumentosXmlRepository(cs);
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(texto) { NomeArquivo = "b1-a-nfe55.xml", XmlBytes = bytesArquivo });
            Assert.Equal(1, res.TotalUnidades);
            Assert.Equal(1, res.Importados);
            var id = res.Documentos[0].DocumentoId!.Value;

            var d = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, id);
            Assert.NotNull(d);
            Assert.False(d!.Quarentena);
            Assert.Equal("NFE_COMPLETA", d.TipoDocumento);
            Assert.Equal("b1-a-nfe55.xml", d.NomeArquivo); // A3/G6: nome do arquivo original persistido
            Assert.Equal("55", d.Modelo);
            Assert.Equal(ChaveA, d.ChaveAcesso);
            Assert.Equal("101", d.Numero);
            Assert.Equal("001", d.Serie);
            Assert.Equal(new DateTime(2026, 9, 28, 13, 30, 0, DateTimeKind.Utc), d.DataEmissao);
            Assert.Equal("11222333000181", d.EmitenteCnpj);
            Assert.Equal("HOSPOMED DISTRIBUIDORA DE MEDICAMENTOS LTDA", d.EmitenteNome);
            Assert.Equal("46389044000130", d.DestinatarioCnpj);
            Assert.Equal(125.00m, d.ValorTotal);
            Assert.Single(d.Itens);
            Assert.Equal("SERINGA-777", d.Itens[0].CodigoProdutoEmitente);

            // Fonte da verdade = bytes originais do arquivo (com BOM), não o texto decodificado.
            var baixado = await repo.ObterXmlBytesAsync(TenantSantaCasa, id);
            Assert.NotNull(baixado);
            Assert.True(baixado!.Value.Bytes.AsSpan().SequenceEqual(bytesArquivo.AsSpan()),
                "O download deve devolver os bytes exatos do arquivo recebido (BOM incluído).");
            Assert.Equal(Sha256Hex(bytesArquivo), baixado.Value.Hash);
            Assert.Equal(ChaveA, baixado.Value.ChaveAcesso);
            Assert.Equal(Sha256Hex(bytesArquivo), d.XmlHash);

            // Texto exibição: BOM removido no decode, conteúdo íntegro.
            Assert.False(d.XmlConteudo.StartsWith('\uFEFF'), "O texto de exibição não deve conter BOM.");
            Assert.Contains($"<infNFe Id=\"NFe{ChaveA}\">", d.XmlConteudo);
        }
        finally
        {
            await PurgePorChavesAsync(cs, ChaveA);
        }
    }

    // =========================================================================
    // FIXTURE B — NFC-e 65 com DOCTYPE PUBLIC (sem namespace): DTD aceita SEM fetch
    // =========================================================================
    [Fact]
    public async Task FixtureB_Nfce65_Doctype_SemFetch_TipoNfce()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await PurgePorChavesAsync(cs, ChaveB);
        try
        {
            var texto = File.ReadAllText(CaminhoFixture("b1-b-nfce65.xml"));
            Assert.Contains("DOCTYPE", texto);

            // Importação sem bytes originais: bytes derivam de UTF-8 do texto (API clássica).
            var repo = new DocumentosXmlRepository(cs);
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(texto) { NomeArquivo = "b1-b-nfce65.xml" });
            Assert.Equal(1, res.TotalUnidades);
            Assert.Equal(1, res.Importados);
            var id = res.Documentos[0].DocumentoId!.Value;

            var d = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, id);
            Assert.NotNull(d);
            Assert.False(d!.Quarentena);
            Assert.Equal("NFC_E", d.TipoDocumento);
            Assert.Equal("65", d.Modelo);
            Assert.Equal(ChaveB, d.ChaveAcesso);
            Assert.Equal("202", d.Numero);
            Assert.Equal(new DateTime(2026, 10, 1, 17, 5, 0, DateTimeKind.Utc), d.DataEmissao);
            Assert.Equal(38.90m, d.ValorTotal);

            // DOCTYPE preservado no texto de exibição (parse aceitou sem buscar/validar a DTD).
            Assert.Contains("DOCTYPE", d.XmlConteudo);

            var baixado = await repo.ObterXmlBytesAsync(TenantSantaCasa, id);
            Assert.NotNull(baixado);
            Assert.Equal(Sha256Hex(System.Text.Encoding.UTF8.GetBytes(texto)), baixado!.Value.Hash);
        }
        finally
        {
            await PurgePorChavesAsync(cs, ChaveB);
        }
    }

    // =========================================================================
    // FIXTURE C — NFS-e (procNFS/nfs/infNFS): modelo 67, chave por numNFS, zero itens
    // =========================================================================
    [Fact]
    public async Task FixtureC_Nfse_Modelo67_ChaveNumnfs_SemItens()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await PurgePorChavesAsync(cs, ChaveC);
        try
        {
            var texto = File.ReadAllText(CaminhoFixture("b1-c-nfse.xml"));

            var repo = new DocumentosXmlRepository(cs);
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(texto) { NomeArquivo = "b1-c-nfse.xml" });
            Assert.Equal(1, res.TotalUnidades);
            Assert.Equal(1, res.Importados);
            var id = res.Documentos[0].DocumentoId!.Value;

            var d = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, id);
            Assert.NotNull(d);
            Assert.False(d!.Quarentena);
            Assert.Equal("NFS_E", d.TipoDocumento);
            Assert.Equal("67", d.Modelo);
            Assert.Equal(ChaveC, d.ChaveAcesso);
            Assert.Equal("1001", d.Numero);
            Assert.Equal("1", d.Serie);
            Assert.Equal(new DateTime(2026, 9, 29, 12, 15, 0, DateTimeKind.Utc), d.DataEmissao);
            Assert.Equal("11222333000181", d.EmitenteCnpj);
            // Destinatário vem de <toma> (layout varia por prefeitura).
            Assert.Equal("46389044000130", d.DestinatarioCnpj);
            Assert.Equal("HOSPITAL SANTA CASA DEMO LTDA", d.DestinatarioNome);
            Assert.Equal(980.00m, d.ValorTotal);
            // NFS-e não tem quebra de itens: valor_produtos = valor_total.
            Assert.Equal(980.00m, d.ValorProdutos);
            Assert.Empty(d.Itens);
        }
        finally
        {
            await PurgePorChavesAsync(cs, ChaveC);
        }
    }

    // =========================================================================
    // FIXTURE D — estrutura NF-e com modelo 57 (CT-e): quarentena MODELO_NAO_SUPORTADO (taxonomia A3)
    // =========================================================================
    [Fact]
    public async Task FixtureD_Modelo57_FicaEmQuarentena_ModeloNaoSuportado()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await PurgePorChavesAsync(cs, ChaveD);
        try
        {
            var texto = File.ReadAllText(CaminhoFixture("b1-d-ct57.xml"));

            var repo = new DocumentosXmlRepository(cs);
            var res = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(texto) { NomeArquivo = "b1-d-ct57.xml" });
            Assert.Equal(1, res.EmQuarentena);
            var id = res.Documentos[0].DocumentoId!.Value;

            var resumo = (await repo.ListarDocumentosAsync(TenantSantaCasa)).FirstOrDefault(r => r.Id == id);
            Assert.NotNull(resumo);
            Assert.True(resumo!.Quarentena);
            Assert.Equal("MODELO_NAO_SUPORTADO", resumo.MotivoQuarentena);
            Assert.Equal(ChaveD, resumo.ChaveAcesso);

            var d = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, id);
            Assert.NotNull(d);
            Assert.True(d!.Quarentena);
            Assert.Equal("MODELO_NAO_SUPORTADO", d.MotivoQuarentena);
            Assert.Empty(d.Itens);
        }
        finally
        {
            await PurgePorChavesAsync(cs, ChaveD);
        }
    }

    // =========================================================================
    // Duplicidade idempotente: mesmos bytes repetidos OU texto sem BOM -> mesmo id
    // =========================================================================
    [Fact]
    public async Task Duplicata_MesmoConteudo_E_BitIdempotente()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await PurgePorChavesAsync(cs, ChaveA);
        try
        {
            var bytesArquivo = File.ReadAllBytes(CaminhoFixture("b1-a-nfe55.xml"));
            var texto = File.ReadAllText(CaminhoFixture("b1-a-nfe55.xml")); // sem BOM

            var repo = new DocumentosXmlRepository(cs);
            var r1 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(texto) { NomeArquivo = "b1-a-nfe55.xml", XmlBytes = bytesArquivo });
            var id1 = r1.Documentos[0].DocumentoId!.Value;

            // Reimportação com os MESMOS bytes originais -> duplicado idempotente reportado no retorno.
            var r2 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(texto) { NomeArquivo = "b1-a-nfe55.xml", XmlBytes = bytesArquivo });
            Assert.True(r2.Documentos[0].DuplicadoIdempotente);
            Assert.Equal(1, r2.DuplicadosIgnorados);
            var id2 = r2.Documentos[0].DocumentoId!.Value;

            // Reimportação como texto (API JSON clássica, sem BOM): mesmo conteúdo lógico.
            var r3 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(texto) { NomeArquivo = "b1-a-nfe55.xml" });
            var id3 = r3.Documentos[0].DocumentoId!.Value;

            Assert.Equal(id1, id2);
            Assert.Equal(id1, id3);

            // Nenhuma linha duplicada: apenas 1 documento para a chave.
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var total = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = @chave",
                new { chave = ChaveA });
            Assert.Equal(1, total);
        }
        finally
        {
            await PurgePorChavesAsync(cs, ChaveA);
        }
    }

    // =========================================================================
    // Conteúdo divergente com mesma chave de acesso -> falha da unidade com mensagem de erro
    // =========================================================================
    [Fact]
    public async Task Divergente_MesmaChave_OutroConteudo_FalhaComMensagemDeErro()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        await PurgePorChavesAsync(cs, ChaveA);
        try
        {
            var bytesArquivo = File.ReadAllBytes(CaminhoFixture("b1-a-nfe55.xml"));
            var texto = File.ReadAllText(CaminhoFixture("b1-a-nfe55.xml"));

            var repo = new DocumentosXmlRepository(cs);
            await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(texto) { NomeArquivo = "b1-a-nfe55.xml", XmlBytes = bytesArquivo });

            // Mesma chave, conteúdo diferente (nNF alterado).
            var divergente = texto.Replace("<nNF>101</nNF>", "<nNF>199</nNF>");
            Assert.NotEqual(texto, divergente);

            // A3: falha é POR UNIDADE — o loop não quebra; a unidade divergente vira falha com mensagem.
            var r2 = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(divergente) { NomeArquivo = "b1-a-divergente.xml", XmlBytes = System.Text.Encoding.UTF8.GetBytes(divergente) });

            Assert.Equal(1, r2.Falhas);
            Assert.Equal(0, r2.Importados);
            Assert.Equal(0, r2.DuplicadosIgnorados);
            Assert.NotNull(r2.Documentos[0].MensagemErro);
            Assert.Contains("divergente", r2.Documentos[0].MensagemErro!, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await PurgePorChavesAsync(cs, ChaveA);
        }
    }
}
