using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Ai;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R62 (fila r53b1 §7 — "coloque a sua IA"): Drael (drael.sh) entra como provider
/// dedicado na camada canônica de assistentes, com o MESMO contrato dos demais:
///   - wire format OpenAI-compatível na base https://drael.sh/v1/ (POST chat/completions,
///     Bearer dk-…; a chave do tenant tem prioridade sobre a global do servidor);
///   - modelo vigente drael-v1 como padrão do provedor + governança de compatibilidade
///     (prefixo 'drael-' bloqueante; catálogo documentado avisa sem bloquear);
///   - classificação canônica de falhas e auditoria em ai_usos idênticas às dos outros
///     provedores; nenhum segredo sai da camada (logs/mensagens sem chave, corpo ou URL).
/// Evidências: mock HTTP (wire format/endpoint/autenticação/corpo) + gateway ponta a
/// ponta sobre o banco real de teste (salvar, executar tarefa, teste de conexão).
/// </summary>
[Collection("ia-camada")]
public sealed class R62ProvedorDraelContratoTests : IAsyncLifetime
{
    private static readonly string Cs = TestDatabase.ConnectionString;
    private readonly HashSet<Guid> _tenants = new();

    public async Task InitializeAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(Cs);
            await cn.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para os testes do provedor Drael não está acessível " +
                        $"(fonte: {TestDatabase.Origem}): {ex.Message}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_tenants.Count == 0) return;
        try
        {
            await using var cn = new NpgsqlConnection(Cs);
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand(@"
                DELETE FROM plantaopro.ai_chamadas_ativas WHERE tenant_id = ANY(@ids);
                DELETE FROM plantaopro.ai_usos WHERE tenant_id = ANY(@ids);
                DELETE FROM plantaopro.ai_config WHERE tenant_id = ANY(@ids);", cn);
            cmd.Parameters.AddWithValue("@ids", _tenants.ToArray());
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort: o banco já foi verificado no InitializeAsync.
        }
    }

    [Fact]
    public void Contrato_ProvedorDraelRegistradoNoServidorENaUi()
    {
        Assert.Equal("drael", AiProviderCodes.Drael);
        Assert.Contains(AiProviderCodes.Drael, AiProviderCodes.All);

        var program = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "backend", "PlantaoPro.Api", "Program.cs"), System.Text.Encoding.UTF8);
        Assert.Contains("AddHttpClient(\"AiDrael\"", program);
        Assert.Contains("https://drael.sh/v1/", program);
        Assert.Contains("AddSingleton<IAiProviderAdapter, DraelAdapter>", program);

        var view = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "backend", "PlantaoPro.Web", "Views", "AssistenteIa", "Index.cshtml"), System.Text.Encoding.UTF8);
        // IA da casa lidera a lista de provedores (principal e reserva) na tela de configuração.
        Assert.Contains("string[] provedores = new[] { \"drael\", \"groq\", \"gemini\", \"deepseek\" };", view);
        Assert.Contains("Drael — IA da casa", view);

        // Contrato de banco: a migration v2356 estende os checks com 'drael' e está
        // amarrada ao manifest + scripts completos (mesma regra das rodadas anteriores).
        var mig = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "database", "migrations", "2026_10_v2356_r62_ai_provedor_drael.sql"), System.Text.Encoding.UTF8);
        Assert.Contains("CHECK (provedor IN ('groq', 'gemini', 'deepseek', 'drael'))", mig);
        Assert.Contains("ai_config_fallback_provedor_check", mig);
        Assert.Contains("ai_precos_modelos_provedor_check", mig);
        var manifest = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "database", "migration-manifest.json"), System.Text.Encoding.UTF8);
        Assert.Contains("2026_10_v2356_r62_ai_provedor_drael", manifest);
        var completo = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "database", "scrpt_completo.sql"), System.Text.Encoding.UTF8);
        Assert.Contains("-- SOURCE: database/migrations/2026_10_v2356_r62_ai_provedor_drael.sql",
            completo.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task WireFormat_OpenAiCompativel_EndpointChaveEClassificacao()
    {
        var (fSemChave, _) = MontarStub(draelKey: null);
        var semChave = new DraelAdapter(fSemChave, CfgBase(null));
        Assert.Equal("drael", semChave.Provider);
        Assert.False(semChave.HasGlobalKey);

        var (fChave, stub) = MontarStub(draelKey: "dk-teste-global");
        var comChave = new DraelAdapter(fChave, CfgBase("dk-teste-global"));
        Assert.True(comChave.HasGlobalKey);
        Assert.Equal("dk-teste-global", comChave.GlobalKey);

        var r = await comChave.CompleteAsync(
            new AiCompletionRequest("drael-v1", "Responda direto.", "Pergunta do teste.", 128, 0.2),
            "dk-do-tenant", CancellationToken.None);

        // Endpoint OpenAI-compatível na base v1; a chave viaja SÓ no Authorization (tenant primeiro).
        Assert.Equal(new Uri("http://drael.test/v1/chat/completions"), stub.Ultimo!.RequestUri);
        Assert.Equal("Bearer dk-do-tenant", stub.Ultimo.Headers.Authorization?.ToString());
        var corpo = stub.UltimoCorpo!;
        Assert.Contains("\"model\":\"drael-v1\"", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dk-do-tenant", corpo);
        Assert.Equal("RESPOSTA_TESTE", r.Text);
        Assert.Equal("drael-v1", r.Model);
        Assert.Equal(11, r.TokensIn);
        Assert.Equal(7, r.TokensOut);

        // 429 = limitação do provedor (classe canônica PROVEDOR_LIMITADO) — adapter próprio no stub 429.
        var (f429, _) = MontarStub(draelKey: "k", status: 429, body: "{}");
        var a429 = new DraelAdapter(f429, CfgBase("k"));
        var exLim = await Assert.ThrowsAsync<AiProviderException>(() => a429.CompleteAsync(
            new AiCompletionRequest("drael-v1", "s", "u", 16, 0), "k", CancellationToken.None));
        Assert.Equal(AiErrorKinds.ProvedorLimitado, exLim.ErrorKind);

        // Corpo sem texto útil = RESPOSTA_INVALIDA — adapter próprio no stub de corpo inválido.
        var (fInv, _) = MontarStub(draelKey: "k", status: 200, body: "{\"choices\":[]}");
        var aInv = new DraelAdapter(fInv, CfgBase("k"));
        var exInv = await Assert.ThrowsAsync<AiProviderException>(() => aInv.CompleteAsync(
            new AiCompletionRequest("drael-v1", "s", "u", 16, 0), "k", CancellationToken.None));
        Assert.Equal(AiErrorKinds.RespostaInvalida, exInv.ErrorKind);
    }

    [Fact]
    public void Governancia_ModeloBloqueanteEAvisoNaoBloqueante()
    {
        Assert.Null(AiModeloCompatibilidade.Validar("drael", "drael-v1"));
        Assert.NotNull(AiModeloCompatibilidade.Validar("drael", "deepseek-flash"));
        Assert.NotNull(AiModeloCompatibilidade.Validar("drael", "gemini-flash-latest"));
        Assert.NotNull(AiModeloCompatibilidade.Validar("drael", "llama-3-8b-instruct"));

        Assert.Null(AiModeloCompatibilidade.Aviso("drael", "drael-v1"));
        Assert.NotNull(AiModeloCompatibilidade.Aviso("drael", "drael-x9"));
    }

    [Fact]
    public async Task Gateway_SalvarExecutarETestarConexaoComProvedorDrael()
    {
        var current = new FakeCurrent
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Roles = new[] { "ADMINISTRADOR_CLIENTE" }
        };
        _tenants.Add(current.TenantId!.Value);

        var cfg = CfgBase(draelKey: "dk-teste-global");
        var repo = new AiConfigRepository(cfg);
        var drael = MontarStub(draelKey: null);
        var factory = drael.Factory;
        var gateway = new AiGateway(
            new IAiProviderAdapter[] { new DraelAdapter(factory, cfg) },
            repo, new AiSecretProtector(cfg), current, cfg, NullLogger<AiGateway>.Instance);

        var view = await gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            new AiConfigUpdate(true, "drael", "drael-v1", null, null, 4000, 1200, 30, 100, null),
            CancellationToken.None);
        Assert.Equal("drael", view.Provedor);
        Assert.True(view.Habilitada);
        Assert.Equal("drael-v1", view.ModeloSugerido);

        var outcome = await gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo,
            new AiTaskExecution(current.TenantId!.Value, current.UserId!.Value, "TESTE", null,
                "Resuma as pendências do meu dia.", new[] { "linha um" }), CancellationToken.None);
        Assert.True(outcome.Success, outcome.Mensagem);
        Assert.Contains("RESPOSTA_TESTE", outcome.Texto!);
        Assert.Equal("drael", outcome.Provedor);
        Assert.Equal("drael-v1", outcome.Modelo);
        Assert.False(outcome.FallbackUsado);

        // Auditoria: uso gravado em ai_usos com o provedor drael.
        long usos = 0;
        await using (var cn = new NpgsqlConnection(Cs))
        {
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM plantaopro.ai_usos WHERE tenant_id = @t AND provedor = 'drael' AND status = 'SUCESSO';", cn);
            cmd.Parameters.AddWithValue("@t", current.TenantId!.Value);
            usos = (long)(await cmd.ExecuteScalarAsync())!;
        }
        Assert.True(usos >= 1, "nenhum uso auditado para o provedor drael");

        var conn = await gateway.TestarConexaoAsync("drael", CancellationToken.None);
        Assert.True(conn.Success, conn.Mensagem);
        Assert.Contains("drael", conn.Mensagem!);

        // Modelo de concorrente não é aceito no provedor drael (governança bloqueante).
        var ex = await Assert.ThrowsAsync<AiConfigException>(() => gateway.SalvarConfiguracaoAsync(
            AiTaskCodes.CotacaoAnalise,
            new AiConfigUpdate(false, "drael", "deepseek-flash", null, null, 4000, 1200, 30, 100, null),
            CancellationToken.None));
        Assert.Contains("prefixo 'drael-'", ex.Message);
    }

    // ------------------------------------------------------------------
    // Harness mínimo (mesmo padrão dos testes de governança IA)
    // ------------------------------------------------------------------

    private static IConfiguration CfgBase(string? draelKey)
    {
        var dados = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = Cs
        };
        if (draelKey is not null) dados["Ai:Providers:Drael:ApiKey"] = draelKey;
        return new ConfigurationBuilder().AddInMemoryCollection(dados).Build();
    }

    private static (IHttpClientFactory Factory, StubHttp Stub) MontarStub(
        string? draelKey, int status = 200, string? body = null)
    {
        var cfg = CfgBase(draelKey);
        var stub = new StubHttp((req, corpo, ct) => Task.FromResult((status,
            body ?? OkDrael())));
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddHttpClient("AiDrael")
            .ConfigurePrimaryHttpMessageHandler(() => stub)
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://drael.test/v1/"); c.Timeout = TimeSpan.FromSeconds(5); });
        var prov = sc.BuildServiceProvider();
        return (prov.GetRequiredService<IHttpClientFactory>(), stub);
    }

    private static string OkDrael() => JsonSerializer.Serialize(new
    {
        id = "gen-teste",
        @object = "chat.completion",
        created = 1,
        model = "drael-v1",
        choices = new[] { new { index = 0, message = new { role = "assistant", content = "RESPOSTA_TESTE" }, finish_reason = "stop" } },
        usage = new { prompt_tokens = 11, completion_tokens = 7, total_tokens = 18 }
    });

    private delegate Task<(int Status, string Body)> StubResponder(HttpRequestMessage request, string corpo, CancellationToken ct);

    private class StubHttp : HttpMessageHandler
    {
        private readonly StubResponder _responder;
        public StubHttp(StubResponder responder) { _responder = responder; }
        public HttpRequestMessage? Ultimo { get; private set; }
        public string? UltimoCorpo { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Ultimo = request;
            UltimoCorpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
            var (status, body) = await _responder(request, UltimoCorpo, ct);
            return new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FakeCurrent : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public Guid? TenantId { get; set; }
        public bool Global { get; set; }
        public Guid? ClienteId => TenantId;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => Global;
        public bool IsTenantAdmin() => false;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => Roles.Contains(role);
    }
}
