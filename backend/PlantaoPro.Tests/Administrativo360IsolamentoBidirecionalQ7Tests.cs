using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// GATE R4 (fecha o Q7 da auditoria da rodada 3) - isolamento bidirecional entre tenants
/// com o MESMO modulo (ADM360) contratado e ATIVO nos dois lados.
///
/// A auditoria identificou que os testes de isolamento existentes sempre comparavam um
/// tenant CONTRATADO contra um tenant SEM contrato, deixando sem prova o cenario em que
/// ambos os tenants tem ADM360 ativo - a configuracao em que vazamentos entre vizinhos
/// seriam mais provaveis (catalogo/configuracao global, consultas sem escopo de tenant).
///
/// Esta suíte roda os repositórios REAIS contra PostgreSQL com dados determinísticos:
///   - tenants 61a9e001/61a9e002 dedicados (nenhuma outra suíte os toca), cada um com
///     contrato ADM360 ATIVO (pré-condição verificada na semente);
///   - ESCRITA: parceiro/produto criado em A não aparece na listagem de B, e o simétrico;
///   - ESCRITA CRUZADA: alternar status por id pertencente ao outro tenant não encontra
///     nem altera (KeyNotFound + valor preservado);
///   - LEITURA: listagens e leitura por id não retornam registros do outro tenant;
///   - ARQUIVO: bytes/hash do XML de um documento só são acessíveis ao tenant dono;
///   - PROBE SQL BIDIRECIONAL: nenhuma linha com id do tenant X fica armazenada sob Y.
///
/// Semente idempotente (purge + insert numa única transação) com GUIDs fixos e marcadores
/// "Q7-" - mesmo padrão que protege Administrativo360FiltrosOpcionaisTests: reexecuções
/// paralelas nunca deixam estado residual que quebre a rodada seguinte.
/// </summary>
public sealed class Administrativo360IsolamentoBidirecionalQ7Tests
{
    private static readonly Guid TenantA = Guid.Parse("61a9e001-07a1-4000-8000-0000000007a1");
    private static readonly Guid TenantB = Guid.Parse("61a9e002-07b2-4000-8000-0000000007b2");
    private static readonly Guid EstabelecimentoA = Guid.Parse("61a9e101-10a1-4000-8000-0000000010a1");
    private static readonly Guid EstabelecimentoB = Guid.Parse("61a9e102-10b2-4000-8000-0000000010b2");
    private static readonly Guid ParceiroA = Guid.Parse("61a9e201-20a1-4000-8000-0000000020a1");
    private static readonly Guid ParceiroB = Guid.Parse("61a9e202-20b2-4000-8000-0000000020b2");
    private static readonly Guid ProdutoA = Guid.Parse("61a9e301-30a1-4000-8000-0000000030a1");
    private static readonly Guid ProdutoB = Guid.Parse("61a9e302-30b2-4000-8000-0000000030b2");
    private static readonly Guid DocumentoA = Guid.Parse("61a9e401-40a1-4000-8000-0000000040a1");
    private static readonly Guid DocumentoB = Guid.Parse("61a9e402-40b2-4000-8000-0000000040b2");

    private static readonly Task SementePronta = SementarAsync();

    private readonly CadastrosRepository _cadastros = new(TestDatabase.ConnectionString);
    private readonly DocumentosXmlRepository _documentos = new(TestDatabase.ConnectionString);

    [Fact(DisplayName = "Q7 escrita: parceiro criado em A não aparece no B (e o simétrico)")]
    public async Task Escrita_Parceiro_CriacaoBidirecional_NaoVaza()
    {
        await SementePronta;

        var criadoEmA = await _cadastros.SalvarParceiroAsync(
            TenantA, null, "Q7-CRIADO PARCEIRO EM A", "77777777000301", true, true, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, criadoEmA);

        var listaA = await _cadastros.ListarParceirosAsync(TenantA, "Q7-CRIADO PARCEIRO EM A", null, CancellationToken.None);
        var dono = Assert.Single(listaA);
        Assert.Equal(criadoEmA, dono.Id);

        var listaB = await _cadastros.ListarParceirosAsync(TenantB, "Q7-CRIADO PARCEIRO EM A", null, CancellationToken.None);
        Assert.DoesNotContain(listaB, p => p.Id == criadoEmA);

        // Sentido simétrico: criação em B também fica invisível para A.
        var criadoEmB = await _cadastros.SalvarParceiroAsync(
            TenantB, null, "Q7-CRIADO PARCEIRO EM B", "66666666000402", true, true, CancellationToken.None);

        var listaAApos = await _cadastros.ListarParceirosAsync(TenantA, "Q7-CRIADO PARCEIRO EM B", null, CancellationToken.None);
        Assert.DoesNotContain(listaAApos, p => p.Id == criadoEmB);

        var listaBApos = await _cadastros.ListarParceirosAsync(TenantB, "Q7-CRIADO PARCEIRO EM B", null, CancellationToken.None);
        Assert.Contains(listaBApos, p => p.Id == criadoEmB);
    }

    [Fact(DisplayName = "Q7 escrita: produto criado em A não aparece no B (e o simétrico)")]
    public async Task Escrita_Produto_CriacaoBidirecional_NaoVaza()
    {
        await SementePronta;

        await _cadastros.SalvarProdutoAsync(
            TenantA, null, "Q7-SKU-CRIADO-A", "Q7 Produto Criado Em A", "UN", null, false, false, 5.00m, true, CancellationToken.None);

        var produtosNoA = await _cadastros.ListarProdutosAsync(TenantA, "Q7-SKU-CRIADO-A", false, CancellationToken.None);
        Assert.Single(produtosNoA);

        var produtosNoB = await _cadastros.ListarProdutosAsync(TenantB, "Q7-SKU-CRIADO-A", false, CancellationToken.None);
        Assert.Empty(produtosNoB);

        // Sentido simétrico.
        await _cadastros.SalvarProdutoAsync(
            TenantB, null, "Q7-SKU-CRIADO-B", "Q7 Produto Criado Em B", "UN", null, false, false, 7.00m, true, CancellationToken.None);

        var produtosNoBApos = await _cadastros.ListarProdutosAsync(TenantB, "Q7-SKU-CRIADO-B", false, CancellationToken.None);
        Assert.Single(produtosNoBApos);

        var produtosNoAApos = await _cadastros.ListarProdutosAsync(TenantA, "Q7-SKU-CRIADO-B", false, CancellationToken.None);
        Assert.Empty(produtosNoAApos);
    }

    [Fact(DisplayName = "Q7 escrita cruzada: status de registro do outro tenant não é encontrado nem alterado")]
    public async Task EscritaCruzada_OutroTenant_KeyNotFoundENaoAltera()
    {
        await SementePronta;

        // B tenta inativar o parceiro DOIS DIGITOS pertencente a A: não encontra.
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _cadastros.AlternarStatusParceiroAsync(TenantB, ParceiroA, false, CancellationToken.None));

        var listaA = await _cadastros.ListarParceirosAsync(TenantA, "Fornecedor Q7-A", null, CancellationToken.None);
        var parceiroA = Assert.Single(listaA);
        Assert.True(parceiroA.Ativo, "O parceiro de A foi alterado por uma escrita do tenant B.");

        // Simétrico: A tenta inativar o produto pertencente a B.
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _cadastros.AlternarStatusProdutoAsync(TenantA, ProdutoB, false, CancellationToken.None));

        var listaB = await _cadastros.ListarProdutosAsync(TenantB, "Q7-SKU-B", false, CancellationToken.None);
        var produtoB = Assert.Single(listaB);
        Assert.True(produtoB.Ativo, "O produto de B foi alterado por uma escrita do tenant A.");
    }

    [Fact(DisplayName = "Q7 leitura: listagens de cadastros só retornam registros do próprio tenant")]
    public async Task Leitura_Listagens_SomenteRegistroProprio()
    {
        await SementePronta;

        // Cada tenant enxerga exatamente o seu registro de seed, pelo nome.
        var parceirosA = await _cadastros.ListarParceirosAsync(TenantA, "Fornecedor Q7", null, CancellationToken.None);
        Assert.Single(parceirosA);
        Assert.Equal(ParceiroA, parceirosA[0].Id);

        var parceirosB = await _cadastros.ListarParceirosAsync(TenantB, "Fornecedor Q7", null, CancellationToken.None);
        Assert.Single(parceirosB);
        Assert.Equal(ParceiroB, parceirosB[0].Id);

        var produtosA = await _cadastros.ListarProdutosAsync(TenantA, "Q7-SKU", false, CancellationToken.None);
        Assert.Single(produtosA);
        Assert.Equal(ProdutoA, produtosA[0].Id);

        var produtosB = await _cadastros.ListarProdutosAsync(TenantB, "Q7-SKU", false, CancellationToken.None);
        Assert.Single(produtosB);
        Assert.Equal(ProdutoB, produtosB[0].Id);
    }

    [Fact(DisplayName = "Q7 leitura: documento por id de outro tenant retorna nulo")]
    public async Task Leitura_DocumentoPorId_Cruzado_Nulo()
    {
        await SementePronta;

        Assert.Null(await _documentos.ObterDocumentoPorIdAsync(TenantA, DocumentoB, CancellationToken.None));
        Assert.Null(await _documentos.ObterDocumentoPorIdAsync(TenantB, DocumentoA, CancellationToken.None));

        var proprioA = await _documentos.ObterDocumentoPorIdAsync(TenantA, DocumentoA, CancellationToken.None);
        Assert.NotNull(proprioA);
        Assert.Equal(ChaveAcesso(1), proprioA!.ChaveAcesso);

        var proprioB = await _documentos.ObterDocumentoPorIdAsync(TenantB, DocumentoB, CancellationToken.None);
        Assert.NotNull(proprioB);
        Assert.Equal(ChaveAcesso(2), proprioB!.ChaveAcesso);
    }

    [Fact(DisplayName = "Q7 leitura: listagem de documentos só contém a chave do próprio tenant")]
    public async Task Leitura_ListagemDocumentos_SomenteDoTenantProprio()
    {
        await SementePronta;

        var docsA = await _documentos.ListarDocumentosAsync(TenantA);
        Assert.Single(docsA);
        Assert.Equal(ChaveAcesso(1), docsA[0].ChaveAcesso);

        var docsB = await _documentos.ListarDocumentosAsync(TenantB);
        Assert.Single(docsB);
        Assert.Equal(ChaveAcesso(2), docsB[0].ChaveAcesso);
    }

    [Fact(DisplayName = "Q7 arquivo: bytes/hash do XML só acessível ao tenant dono")]
    public async Task Arquivo_BytesXml_Cruzados_Nulos()
    {
        await SementePronta;

        var bytesA = await _documentos.ObterXmlBytesAsync(TenantA, DocumentoA, CancellationToken.None);
        Assert.NotNull(bytesA);
        var tupA = bytesA!.Value;
        Assert.Equal(DocumentoHash(TenantA, 1), tupA.Hash);
        Assert.NotEmpty(tupA.Bytes);

        var bytesB = await _documentos.ObterXmlBytesAsync(TenantB, DocumentoB, CancellationToken.None);
        Assert.NotNull(bytesB);
        var tupB = bytesB!.Value;
        Assert.Equal(DocumentoHash(TenantB, 2), tupB.Hash);
        Assert.NotEmpty(tupB.Bytes);

        // As quatro leituras cruzadas (id do outro tenant) devem falhar silenciosamente.
        Assert.Null(await _documentos.ObterXmlBytesAsync(TenantA, DocumentoB, CancellationToken.None));
        Assert.Null(await _documentos.ObterXmlBytesAsync(TenantB, DocumentoA, CancellationToken.None));
    }

    [Fact(DisplayName = "Q7 probe SQL bidirecional: nenhum id do tenant X armazena-se sob o tenant Y")]
    public async Task ProbeSql_Bidirecional_NenhumaLinhaSobTenantErrado()
    {
        await SementePronta;

        (string Tabela, Guid Id, Guid TenantCorreto)[] alvos =
        {
            ("adm360_parceiros", ParceiroA, TenantA),
            ("adm360_parceiros", ParceiroB, TenantB),
            ("adm360_produtos", ProdutoA, TenantA),
            ("adm360_produtos", ProdutoB, TenantB),
            ("adm360_documentos_recebidos", DocumentoA, TenantA),
            ("adm360_documentos_recebidos", DocumentoB, TenantB),
            ("adm360_estabelecimentos", EstabelecimentoA, TenantA),
            ("adm360_estabelecimentos", EstabelecimentoB, TenantB),
        };

        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        foreach (var alvo in alvos)
        {
            var vazados = await cn.ExecuteScalarAsync<int>(
                $@"SELECT count(*)::int FROM plantaopro.{alvo.Tabela}
                   WHERE id = @id AND tenant_id <> @tenantCorreto",
                new { id = alvo.Id, tenantCorreto = alvo.TenantCorreto });
            Assert.True(vazados == 0,
                $"{alvo.Tabela}: {vazados} linha(s) com id {alvo.Id} fora do tenant {alvo.TenantCorreto}.");
        }
    }

    /// <summary>
    /// Chave fiscal sintética de 44 dígitos com o modelo "55" nas posições 16-17
    /// (determinística por tag; única por tenant graças ao índice (tenant_id, chave_acesso)).
    /// </summary>
    private static string ChaveAcesso(int tag) =>
        ("3526100" + tag.ToString("D2") + "000000" + "55").PadRight(44, '0');

    private static (string Xml, byte[] Bytes, string Hash) DocumentoXml(Guid tenant, int tag)
    {
        var xml = $"<nfeProc><NFe><infNFe>PLANTAOPRO-Q7-{tenant:N}-{tag}</infNFe></NFe></nfeProc>";
        var bytes = Encoding.UTF8.GetBytes(xml);
        return (xml, bytes, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    private static string DocumentoHash(Guid tenant, int tag) => DocumentoXml(tenant, tag).Hash;

    /// <summary>
    /// Semente idempotente: purge dos registros determinísticos (e dos criados em execuções
    /// anteriores pelos marcadores "Q7-CRIADO%"/"Q7-SKU-CRIADO%") + insert da estrutura,
    /// tudo numa única transação. No fim, valida a pré-condição central do cenário:
    /// os DOIS tenants precisam ter contrato ADM360 ATIVO habilitado.
    /// </summary>
    private static async Task SementarAsync()
    {
        var (xmlA, bytesA, hashA) = DocumentoXml(TenantA, 1);
        var (xmlB, bytesB, hashB) = DocumentoXml(TenantB, 2);

        try
        {
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();

            await cn.ExecuteAsync(@"
                DELETE FROM plantaopro.adm360_documentos_recebidos WHERE id IN (@docA, @docB);
                DELETE FROM plantaopro.adm360_produtos
                WHERE id IN (@prodA, @prodB) OR (tenant_id IN (@tA, @tB) AND sku LIKE 'Q7-SKU-CRIADO%');
                DELETE FROM plantaopro.adm360_parceiros
                WHERE id IN (@parA, @parB) OR (tenant_id IN (@tA, @tB) AND nome LIKE 'Q7-CRIADO%');
                DELETE FROM plantaopro.adm360_estabelecimentos WHERE id IN (@estabA, @estabB);
                DELETE FROM plantaopro.tenant_modulos
                WHERE tenant_id IN (@tA, @tB) AND upper(codigo) = 'ADM360';
                DELETE FROM plantaopro.tenants WHERE id IN (@tA, @tB);",
                new
                {
                    docA = DocumentoA, docB = DocumentoB,
                    prodA = ProdutoA, prodB = ProdutoB,
                    parA = ParceiroA, parB = ParceiroB,
                    estabA = EstabelecimentoA, estabB = EstabelecimentoB,
                    tA = TenantA, tB = TenantB
                }, tx);

            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.tenants (id, tenant_id, codigo, nome, status)
                VALUES
                    (@tA, @tA, 'q7-tenant-a', 'Tenant Q7 A', 'ATIVO'),
                    (@tB, @tB, 'q7-tenant-b', 'Tenant Q7 B', 'ATIVO');

                -- Coração do cenário: o MESMO módulo (ADM360) contratado como ATIVO nos dois tenants.
                INSERT INTO plantaopro.tenant_modulos
                    (id, tenant_id, modulo_id, codigo, codigo_modulo, habilitado, status, origem, reg_status, reg_date)
                SELECT gen_random_uuid(), tn.tenant_id, m.id, m.codigo, m.codigo, true, 'ATIVO', 'DEMO_MIGRATION', 'A', now()
                FROM unnest(ARRAY[@tA, @tB]::uuid[]) AS tn(tenant_id)
                CROSS JOIN LATERAL (
                    SELECT id, codigo FROM plantaopro.modulos_sistema
                    WHERE upper(codigo) = 'ADM360' AND reg_status = 'A' LIMIT 1
                ) m;

                INSERT INTO plantaopro.adm360_estabelecimentos (id, tenant_id, cnpj, razao_social, ambiente)
                VALUES
                    (@estabA, @tA, '11111111000191', 'Estabelecimento Q7 A', 'HOMOLOGACAO'),
                    (@estabB, @tB, '22222222000292', 'Estabelecimento Q7 B', 'HOMOLOGACAO');

                INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, ativo)
                VALUES
                    (@parA, @tA, 'Fornecedor Q7-A', '99999999000101', true, true),
                    (@parB, @tB, 'Fornecedor Q7-B', '88888888000202', true, true);

                INSERT INTO plantaopro.adm360_produtos (id, tenant_id, sku, nome, unidade, preco_custo, controla_lote)
                VALUES
                    (@prodA, @tA, 'Q7-SKU-A', 'Produto Q7-A', 'UN', 10.00, false),
                    (@prodB, @tB, 'Q7-SKU-B', 'Produto Q7-B', 'UN', 20.00, false);

                INSERT INTO plantaopro.adm360_documentos_recebidos (
                    id, tenant_id, estabelecimento_id, chave_acesso, numero, serie, modelo, data_emissao,
                    emitente_cnpj, emitente_nome, destinatario_cnpj, destinatario_nome,
                    valor_total, valor_produtos, tipo_documento, status_manifestacao, status_conferencia,
                    xml_conteudo, xml_hash, origem, xml_bytes
                )
                VALUES
                    (@docA, @tA, @estabA, @chaveA, '700001', '1', '55', '2026-09-01',
                     '11222333000181', 'Emitente Q7 A', '46389044000130', 'Destinatario Q7 A',
                     100.00, 100.00, 'NFE_COMPLETA', 'SEM_MANIFESTACAO', 'PENDENTE',
                     @xmlA, @hashA, 'DADOS_DE_TESTE', @bytesA),
                    (@docB, @tB, @estabB, @chaveB, '800002', '1', '55', '2026-09-02',
                     '11222333000181', 'Emitente Q7 B', '46389044000130', 'Destinatario Q7 B',
                     100.00, 100.00, 'NFE_COMPLETA', 'SEM_MANIFESTACAO', 'PENDENTE',
                     @xmlB, @hashB, 'DADOS_DE_TESTE', @bytesB);",
                new
                {
                    chaveA = ChaveAcesso(1), chaveB = ChaveAcesso(2),
                    xmlA, bytesA, hashA,
                    xmlB, bytesB, hashB,
                    estabA = EstabelecimentoA, estabB = EstabelecimentoB,
                    parA = ParceiroA, parB = ParceiroB,
                    prodA = ProdutoA, prodB = ProdutoB,
                    docA = DocumentoA, docB = DocumentoB,
                    tA = TenantA, tB = TenantB
                }, tx);

            await tx.CommitAsync();

            // Pré-condição do cenário: sem os dois contratos ativos, o teste não mede o Q7.
            var contratos = await cn.ExecuteScalarAsync<int>(@"
                SELECT count(*)::int FROM plantaopro.tenant_modulos
                WHERE tenant_id IN (@tA, @tB) AND upper(codigo) = 'ADM360'
                  AND status = 'ATIVO' AND habilitado = true AND reg_status = 'A';",
                new { tA = TenantA, tB = TenantB });
            if (contratos != 2)
                throw new InvalidOperationException(
                    $"Q7 exige contrato ADM360 ATIVO nos dois tenants; encontrados {contratos}/2.");
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL inacessível na suíte de isolamento Q7: {ex.Message}");
        }
    }
}
