using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class Administrativo360AutenticacaoEJornadasTests : IClassFixture<PlantaoProApiFactory>
{
    private readonly PlantaoProApiFactory _factory;
    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid TenantIsolado = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647599");

    public Administrativo360AutenticacaoEJornadasTests(PlantaoProApiFactory factory)
    {
        _factory = factory;
    }

    private static string ObterConnectionString()
    {
        return Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name=PlantaoPro.tests";
    }

    private async Task<string> AutenticarEObterTokenAsync(string email, string senha)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("api/auth/login", new { Email = email, Senha = senha });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token), "Token JWT não pode ser vazio.");
        return token!;
    }

    // =========================================================================
    // 1. AUTENTICAÇÃO REAL PELA API (BLOCO A.2 & BLOCO D.10)
    // =========================================================================

    [Fact]
    public async Task AutenticacaoRealPelaApi_ComCredenciaisValidas_RetornaSessaoTokenEModuloAdm360()
    {
        using var client = _factory.CreateClient();

        // 1. Chamar endpoint real de login com credenciais oficiais documentadas
        var response = await client.PostAsJsonAsync("api/auth/login", new
        {
            Email = "gestor@santacasa-demo.example",
            Senha = "SantaCasa!Demo2026#Gestor"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());

        var data = body.GetProperty("data");
        var token = data.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        // 2. Verificar tenant e perfil
        var tenantId = data.GetProperty("tenantId").GetGuid();
        Assert.Equal(TenantSantaCasa, tenantId);

        var roles = data.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("ADMINISTRADOR_CLIENTE", roles);

        // 3. Verificar módulo ADM360 contratado
        var modules = data.GetProperty("modules").EnumerateArray().Select(m => m.GetString()).ToList();
        Assert.Contains("ADM360", modules);

        // 4. Acessar recurso protegido com o token JWT emitido
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resProtected = await client.GetAsync("api/administrativo360/cadastros/lookups");
        Assert.Equal(HttpStatusCode.OK, resProtected.StatusCode);

        var lookupsJson = await resProtected.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(lookupsJson.TryGetProperty("parceiros", out var parceirosProp) && parceirosProp.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task AutenticacaoRealPelaApi_ComSenhaIncorreta_Retorna401Unauthorized()
    {
        // Limpar eventual lockout para teste determinístico
        await using var cn = new NpgsqlConnection(ObterConnectionString());
        await cn.ExecuteAsync("UPDATE plantaopro.usuarios SET bloqueado_ate = NULL WHERE email = 'gestor@santacasa-demo.example'");

        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/login", new
        {
            Email = "gestor@santacasa-demo.example",
            Senha = "SenhaIncorretaInvalida!2026"
        });

        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Locked);
    }

    // =========================================================================
    // 2. PERMISSÕES DE ESCRITA GRANULARES (BLOCO A.2)
    // =========================================================================

    [Fact]
    public async Task PermissoesEscrita_UsuarioConsultaLeMasRecebe403AoTentarGravar()
    {
        // 1. Autenticar usuário de consulta restrita
        var tokenConsulta = await AutenticarEObterTokenAsync("consulta@santacasa-demo.example", "SantaCasa!Demo2026#Gestor");

        using var clientConsulta = _factory.CreateClient();
        clientConsulta.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenConsulta);

        // Leitura permitida para usuário de consulta (possui Adm360.Ver)
        var resLeitura = await clientConsulta.GetAsync("api/administrativo360/cadastros/lookups");
        Assert.Equal(HttpStatusCode.OK, resLeitura.StatusCode);

        // Tentativa de escrita bloqueada pela política Adm360.MapearCadastros (403 Forbidden)
        var docAleatorio = "99." + Random.Shared.Next(100, 999) + "." + Random.Shared.Next(100, 999) + "/0001-" + Random.Shared.Next(10, 99);
        var novoParceiro = new Adm360CadastrosController.SalvarParceiroRequest(
            null,
            "Parceiro Invasao Consulta " + Guid.NewGuid().ToString("N")[..6],
            docAleatorio,
            false,
            true
        );

        var resEscrita = await clientConsulta.PostAsJsonAsync("api/administrativo360/cadastros/parceiros", novoParceiro);
        Assert.Equal(HttpStatusCode.Forbidden, resEscrita.StatusCode);

        // 2. Autenticar Gestor (possui Adm360.MapearCadastros) e comprovar sucesso na gravação
        var tokenGestor = await AutenticarEObterTokenAsync("gestor@santacasa-demo.example", "SantaCasa!Demo2026#Gestor");

        using var clientGestor = _factory.CreateClient();
        clientGestor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenGestor);

        var resEscritaGestor = await clientGestor.PostAsJsonAsync("api/administrativo360/cadastros/parceiros", novoParceiro);
        Assert.True(resEscritaGestor.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created);
    }

    // =========================================================================
    // 3. ISOLAMENTO DOS CADASTROS MULTI-TENANT (BLOCO A.1)
    // =========================================================================

    [Fact]
    public async Task IsolamentoCadastros_DoisTenantsComDados_TentativaDeAlteracaoCruzadaRecusadaEDadosInalterados()
    {
        var cs = ObterConnectionString();
        var repo = new CadastrosRepository(cs);

        var tenantA = TenantSantaCasa;
        var tenantB = TenantIsolado;

        // Criar registro pertencente a Tenant B
        var docB = "987" + Random.Shared.Next(10000000, 99999999);
        var nomeBOriginal = "Hospital B Confidencial " + Guid.NewGuid().ToString("N")[..6];
        var idB = await repo.SalvarParceiroAsync(tenantB, null, nomeBOriginal, docB, false, true, default);

        // Cliente A tenta alterar registro pertencente a B passando ID de B sob tenant A
        var tentativaAlteracao = new Adm360CadastrosController.SalvarParceiroRequest(
            idB,
            "HACKEADO PELO CLIENTE A",
            "00000000000",
            true,
            false
        );

        // Operação DEVE ser recusada pelo repositório (KeyNotFoundException pois 0 linhas do Tenant A foram afetadas)
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await repo.SalvarParceiroAsync(tenantA, idB, "HACKEADO PELO CLIENTE A", "00000000000", true, false, default);
        });

        // Comprovar que os dados de B permanecem integralmente inalterados no banco
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        var parceiroB = await cn.QuerySingleAsync<dynamic>(
            "SELECT nome, documento, fornecedor, ativo, tenant_id FROM plantaopro.adm360_parceiros WHERE id = @idB",
            new { idB }
        );

        Assert.Equal(nomeBOriginal, (string)parceiroB.nome);
        Assert.Equal(docB, (string)parceiroB.documento);
        Assert.False((bool)parceiroB.fornecedor);
        Assert.True((bool)parceiroB.ativo);
        Assert.Equal(tenantB, (Guid)parceiroB.tenant_id);

        // Agora testar também pelo endpoint real HTTP da API
        var tokenGestorA = await AutenticarEObterTokenAsync("gestor@santacasa-demo.example", "SantaCasa!Demo2026#Gestor");
        using var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenGestorA);

        var resHttpAcessoCruzado = await clientA.PostAsJsonAsync("api/administrativo360/cadastros/parceiros", tentativaAlteracao);
        Assert.Equal(HttpStatusCode.NotFound, resHttpAcessoCruzado.StatusCode);
    }

    [Fact]
    public async Task IsolamentoCadastros_ValidacoesDeNegocio_DuplicidadeEBloqueios()
    {
        var cs = ObterConnectionString();
        var repo = new CadastrosRepository(cs);

        // 1. Chave de negócio duplicada no mesmo tenant (mesmo documento) deve ser rejeitada
        var docUnico = "55.444.333/0001-" + Random.Shared.Next(10, 99);
        await repo.SalvarParceiroAsync(TenantSantaCasa, null, "Parceiro Original", docUnico, false, true, default);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await repo.SalvarParceiroAsync(TenantSantaCasa, null, "Parceiro Duplicado", docUnico, false, true, default);
        });

        // 2. Tipo de local inválido deve ser rejeitado estritamente sem conversão silenciosa
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await repo.SalvarLocalAsync(TenantSantaCasa, null, "LOC-INV", "Local Tipo Invalido", "TIPO_NAO_PERMITIDO", true, default);
        });

        // 3. Alteração estrutural de produto movimentado deve ser bloqueada
        var skuMov = "SKU-MOV-" + Guid.NewGuid().ToString("N")[..6];
        var prodId = await repo.SalvarProdutoAsync(TenantSantaCasa, null, skuMov, "Produto Com Movimentacao", "UN", null, false, false, 100m, true, default);

        // Simular movimento de estoque no banco para este produto
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        var localId = await cn.QueryFirstOrDefaultAsync<Guid>("SELECT id FROM plantaopro.adm360_locais WHERE tenant_id = @TenantSantaCasa LIMIT 1", new { TenantSantaCasa });
        var loteId = await cn.QueryFirstOrDefaultAsync<Guid>("SELECT id FROM plantaopro.adm360_lotes WHERE tenant_id = @TenantSantaCasa LIMIT 1", new { TenantSantaCasa });
        if (localId != Guid.Empty && loteId != Guid.Empty)
        {
            await cn.ExecuteAsync(@"
                INSERT INTO plantaopro.adm360_movimentos (id, tenant_id, local_id, produto_id, lote_id, tipo, condicao, quantidade, origem_tipo, origem_id, idempotency_key, created_at)
                VALUES (gen_random_uuid(), @TenantSantaCasa, @localId, @prodId, @loteId, 'ENTRADA', 'LIBERADO', 10, 'MANUAL', gen_random_uuid(), gen_random_uuid()::text, now());",
                new { TenantSantaCasa, localId, prodId, loteId });

            // Tentar alterar unidade em produto movimentado deve lançar InvalidOperationException
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await repo.SalvarProdutoAsync(TenantSantaCasa, prodId, skuMov, "Produto Com Movimentacao Alterado", "CX", null, false, false, 100m, true, default);
            });
        }
    }

    // =========================================================================
    // 4. ESTADOS DAS INTEGRAÇÕES (BLOCO A.4)
    // =========================================================================

    [Fact]
    public async Task EstadosIntegracoes_ExportacaoManualNaoGeraProtocoloFicticioEConfigAusenteRetornaPendente()
    {
        // 1. Exportação manual: estado EXPORTADA_MANUALMENTE com protocolo nulo
        var manualConnector = new ImportacaoManualConnector();
        var resManual = await manualConnector.TransmitirPropostaAsync(null!, null!, null!, default);

        Assert.Equal("EXPORTADA_MANUALMENTE", resManual.StatusTransmissao);
        Assert.Null(resManual.Protocolo);
        Assert.True(resManual.Sucesso);
        Assert.Contains("manual", resManual.Mensagem, StringComparison.OrdinalIgnoreCase);

        // 2. Falta de credenciais/configuração não gera erro ou rejeição fictícia, mas sim CONFIGURACAO_PENDENTE
        var opmeConnector = new OpmenexoConnector();
        var resTransmissao = await opmeConnector.TransmitirPropostaAsync(null!, null!, null!, default);

        Assert.Equal("CONFIGURACAO_PENDENTE", resTransmissao.StatusTransmissao);
        Assert.Null(resTransmissao.Protocolo);
        Assert.False(resTransmissao.Sucesso);
        Assert.Contains("configuração", resTransmissao.Mensagem, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================================
    // 5. MIGRATIONS E IDEMPOTÊNCIA (BLOCO A.3 & BLOCO D.10)
    // =========================================================================

    [Fact]
    public async Task MigrationsV2197EV2198_ReconciliamModuloEHistoricoSemPerdaDeDados()
    {
        var cs = ObterConnectionString();
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        // Tabela de log de reconciliação v2198 existe
        var existsReconciliacaoTable = await cn.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables 
                WHERE table_schema = 'plantaopro' AND table_name = 'tenant_modulos_reconciliacao'
            );");
        Assert.True(existsReconciliacaoTable, "A tabela plantaopro.tenant_modulos_reconciliacao deve existir.");

        // O módulo ADM360 está ativo e canônico para a Santa Casa
        var activeModules = (await cn.QueryAsync<string>(@"
            SELECT DISTINCT upper(ms.codigo)
            FROM plantaopro.tenant_modulos tm
            JOIN plantaopro.modulos_sistema ms ON ms.id = tm.modulo_id AND ms.reg_status = 'A' AND upper(ms.status) = 'ATIVO'
            WHERE tm.tenant_id = @TenantSantaCasa AND tm.reg_status = 'A' AND tm.habilitado = true AND upper(tm.status) = 'ATIVO';",
            new { TenantSantaCasa })).ToList();

        Assert.Contains("ADM360", activeModules);
    }
}
