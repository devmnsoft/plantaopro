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
/// R71C (pedido do usuário, fora do plano dos 50 sprints): Mistral (api.mistral.ai)
/// entra como provider dedicado na camada canônica de assistentes — provedor com
/// PLANO GRATUITO na La Plateforme (chave gratuita, sem cartão de crédito), wire
/// format OpenAI-compatível e o MESMO contrato dos demais:
///   - chave do tenant tem prioridade sobre a global do servidor;
///   - modelo padrão mistral-small-2603 (changelog oficial da Mistral) + governança
///     de compatibilidade (rejeita catálogo de outros provedores e famílias
///     aposentadas; famílias vigentes mistral-/ministral-/codestral- avisa sem
///     bloquear quando o nome não é de família documentada);
///   - classificação canônica de falhas e auditoria em ai_usos idênticas às dos
///     outros provedores; nenhum segredo sai da camada.
/// Evidências: mock HTTP (wire format/endpoint/autenticação/corpo) + gateway ponta a
/// ponta sobre o banco real de teste (salvar, executar tarefa, teste de conexão).
/// </summary>
[Collection("ia-camada")]
public sealed class R71CProvedorMistralContratoTests : IAsyncLifetime
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
            Assert.Fail($"PostgreSQL obrigatório para os testes do provedor Mistral não está acessível " +
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
    public void Contrato_ProvedorMistralRegistradoNoServidorENaUi()
    {
        Assert.Equal("mistral", AiProviderCodes.Mistral);
        Assert.Contains(AiProviderCodes.Mistral, AiProviderCodes.All);

        var program = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "backend", "PlantaoPro.Api", "Program.cs"), System.Text.Encoding.UTF8);
        Assert.Contains("AddHttpClient(\"AiMistral\"", program);
        Assert.Contains("https://api.mistral.ai/v1/", program);
        Assert.Contains("AddSingleton<IAiProviderAdapter, MistralAdapter>", program);

        var view = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "backend", "PlantaoPro.Web", "Views", "AssistenteIa", "Index.cshtml"), System.Text.Encoding.UTF8);
        // IA da casa continua liderando a lista; o Mistral entra como mais uma opção
        // (principal e reserva) na tela de configuração.
        Assert.Contains("string[] provedores = new[] { \"drael\", \"groq\", \"gemini\", \"deepseek\", \"mistral\" };", view);
        Assert.Contains("\"mistral\" => \"Mistral\"", view);

        // Contrato de banco: a migration v2361 estende os checks com 'mistral' e está
        // amarrada ao manifest + scripts completos (mesma regra das rodadas anteriores).
        var mig = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "database", "migrations", "2026_10_v2361_r71c_ai_provedor_mistral.sql"), System.Text.Encoding.UTF8);
        Assert.Contains("CHECK (provedor IN ('groq', 'gemini', 'deepseek', 'drael', 'mistral'))", mig);
        Assert.Contains("ai_config_fallback_provedor_check", mig);
        Assert.Contains("ai_precos_modelos_provedor_check", mig);
        var manifest = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "database", "migration-manifest.json"), System.Text.Encoding.UTF8);
        Assert.Contains("2026_10_v2361_r71c_ai_provedor_mistral", manifest);
        var completo = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,
            "database", "scrpt_completo.sql"), System.Text.Encoding.UTF8);
        Assert.Contains("-- SOURCE: database/migrations/2026_10_v2361_r71c_ai_provedor_mistral.sql",
            completo.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task WireFormat_OpenAiCompativel_EndpointChaveEClassificacao()
    {
        var (fSemChave, _) = MontarStub(mistralKey: null);
        var semChave = new MistralAdapter(fSemChave, CfgBase(null));
        Assert.Equal("mistral", semChave.Provider);
        Assert.False(semChave.HasGlobalKey);

        var (fChave, stub) = MontarStub(mistralKey: "msl-teste-global");
        var comChave = new MistralAdapter(fChave, CfgBase("msl-teste-global"));
        Assert.True(comChave.HasGlobalKey);
        Assert.Equal("msl-teste-global", comChave.GlobalKey);

        var r = await comChave.CompleteAsync(
            new AiCompletionRequest("mistral-small-2603", "Responda direto.", "Pergunta do teste.", 128, 0.2),
            "msl-do-tenant", CancellationToken.None);

        // Endpoint OpenAI-compatível na base v1; a chave viaja SÓ no Authorization (tenant primeiro).
        Assert.Equal(new Uri("http://mistral.test/v1/chat/completions"), stub.Ultimo!.RequestUri);
        Assert.Equal("Bearer msl-do-tenant", stub.Ultimo.Headers.Authorization?.ToString());
        var corpo = stub.UltimoCorpo!;
        Assert.Contains("\"model\":\"mistral-small-2603\"", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("msl-do-tenant", corpo);
        Assert.Equal("RESPOSTA_TESTE", r.Text);
        Assert.Equal("mistral-small-2603", r.Model);
        Assert.Equal(11, r.TokensIn);
        Assert.Equal(7, r.TokensOut);

        // 429 = limitação do provedor (classe canônica PROVEDOR_LIMITADO) — adapter próprio no stub 429.
        var (f429, _) = MontarStub(mistralKey: "k", status: 429, body: "{}");
        var a429 = new MistralAdapter(f429, CfgBase("k"));
        var exLim = await Assert.ThrowsAsync<AiProviderException>(() => a429.CompleteAsync(
            new AiCompletionRequest("mistral-small-2603", "s", "u", 16, 0), "k", CancellationToken.None));
        Assert.Equal(AiErrorKinds.ProvedorLimitado, exLim.ErrorKind);

        // Corpo sem texto útil = RESPOSTA_INVALIDA — adapter próprio no stub de corpo inválido.
        var (fInv, _) = MontarStub(mistralKey: "k", status: 200, body: "{\"choices\":[]}");
        var aInv = new MistralAdapter(fInv, CfgBase("k"));
        var exInv = await Assert.ThrowsAsync<AiProviderException>(() => aInv.CompleteAsync(
            new AiCompletionRequest("mistral-small-2603", "s", "u", 16, 0), "k", CancellationToken.None));
        Assert.Equal(AiErrorKinds.RespostaInvalida, exInv.ErrorKind);
    }

    [Fact]
    public void Governancia_ModeloBloqueanteEAvisoNaoBloqueante()
    {
        // Famílias vigentes aceitas sem bloqueio.
        Assert.Null(AiModeloCompatibilidade.Validar("mistral", "mistral-small-2603"));
        Assert.Null(AiModeloCompatibilidade.Validar("mistral", "ministral-3-8b-25-12"));
        Assert.Null(AiModeloCompatibilidade.Validar("mistral", "codestral-25-08"));
        // Terceiro partido servido pela Mistral: passa do bloqueio e cai no aviso.
        Assert.Null(AiModeloCompatibilidade.Validar("mistral", "zai-glm-5-3"));

        // Catálogo de outros provedores nunca chega à chamada HTTP.
        Assert.NotNull(AiModeloCompatibilidade.Validar("mistral", "gemini-flash-latest"));
        Assert.NotNull(AiModeloCompatibilidade.Validar("mistral", "deepseek-flash"));
        Assert.NotNull(AiModeloCompatibilidade.Validar("mistral", "drael-v1"));
        Assert.NotNull(AiModeloCompatibilidade.Validar("mistral", "llama-3-3-70b-versatile"));
        Assert.NotNull(AiModeloCompatibilidade.Validar("mistral", "gpt-oss-120b"));
        var exOutro = AiModeloCompatibilidade.Validar("mistral", "gemini-flash-latest");
        Assert.Contains("outro provedor", exOutro);

        // Famílias aposentadas do catálogo (docs oficiais de 2026-10) são rejeitadas.
        Assert.NotNull(AiModeloCompatibilidade.Validar("mistral", "magistral-medium-2509"));
        Assert.NotNull(AiModeloCompatibilidade.Validar("mistral", "devstral-2512"));
        Assert.Contains("aposentado", AiModeloCompatibilidade.Validar("mistral", "magistral-medium-2509"));

        // Aviso não bloqueia: nome fora das famílias documentadas só chama atenção.
        Assert.Null(AiModeloCompatibilidade.Aviso("mistral", "mistral-small-2603"));
        Assert.Null(AiModeloCompatibilidade.Aviso("mistral", "ministral-3-14b-25-12"));
        Assert.Null(AiModeloCompatibilidade.Aviso("mistral", "codestral-25-08"));
        Assert.NotNull(AiModeloCompatibilidade.Aviso("mistral", "zai-glm-5-3"));
    }

    [Fact]
    public async Task Gateway_SalvarExecutarETestarConexaoComProvedorMistral()
    {
        var current = new FakeCurrent
        {
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Roles = new[] { "ADMINISTRADOR_CLIENTE" }
        };
        _tenants.Add(current.TenantId!.Value);

        var cfg = CfgBase(mistralKey: "msl-teste-global");
        var repo = new AiConfigRepository(cfg);
        var mistral = MontarStub(mistralKey: null);
        var factory = mistral.Factory;
        var gateway = new AiGateway(
            new IAiProviderAdapter[] { new MistralAdapter(factory, cfg) },
            repo, new AiSecretProtector(cfg), current, cfg, NullLogger<AiGateway>.Instance);

        var view = await gateway.SalvarConfiguracaoAsync(AiTaskCodes.MeuDiaResumo,
            new AiConfigUpdate(true, "mistral", "mistral-small-2603", null, null, 4000, 1200, 30, 100, null),
            CancellationToken.None);
        Assert.Equal("mistral", view.Provedor);
        Assert.True(view.Habilitada);
        Assert.Equal("mistral-small-2603", view.ModeloSugerido);

        var outcome = await gateway.ExecutarTarefaAsync(AiTaskCodes.MeuDiaResumo,
            new AiTaskExecution(current.TenantId!.Value, current.UserId!.Value, "TESTE", null,
                "Resuma as pendências do meu dia.", new[] { "linha um" }), CancellationToken.None);
        Assert.True(outcome.Success, outcome.Mensagem);
        Assert.Contains("RESPOSTA_TESTE", outcome.Texto!);
        Assert.Equal("mistral", outcome.Provedor);
        Assert.Equal("mistral-small-2603", outcome.Modelo);
        Assert.False(outcome.FallbackUsado);

        // Auditoria: uso gravado em ai_usos com o provedor mistral.
        long usos = 0;
        await using (var cn = new NpgsqlConnection(Cs))
        {
            await cn.OpenAsync();
            await using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM plantaopro.ai_usos WHERE tenant_id = @t AND provedor = 'mistral' AND status = 'SUCESSO';", cn);
            cmd.Parameters.AddWithValue("@t", current.TenantId!.Value);
            usos = (long)(await cmd.ExecuteScalarAsync())!;
        }
        Assert.True(usos >= 1, "nenhum uso auditado para o provedor mistral");

        var conn = await gateway.TestarConexaoAsync("mistral", CancellationToken.None);
        Assert.True(conn.Success, conn.Mensagem);
        Assert.Contains("mistral", conn.Mensagem!);

        // Modelo de concorrente não é aceito no provedor mistral (governança bloqueante).
        var ex = await Assert.ThrowsAsync<AiConfigException>(() => gateway.SalvarConfiguracaoAsync(
            AiTaskCodes.CotacaoAnalise,
            new AiConfigUpdate(false, "mistral", "drael-v1", null, null, 4000, 1200, 30, 100, null),
            CancellationToken.None));
        Assert.Contains("outro provedor", ex.Message);
    }

    // ------------------------------------------------------------------
    // Harness mínimo (mesmo padrão dos testes de governança IA)
    // ------------------------------------------------------------------

    private static IConfiguration CfgBase(string? mistralKey)
    {
        var dados = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = Cs
        };
        if (mistralKey is not null) dados["Ai:Providers:Mistral:ApiKey"] = mistralKey;
        return new ConfigurationBuilder().AddInMemoryCollection(dados).Build();
    }

    private static (IHttpClientFactory Factory, StubHttp Stub) MontarStub(
        string? mistralKey, int status = 200, string? body = null)
    {
        var cfg = CfgBase(mistralKey);
        var stub = new StubHttp((req, corpo, ct) => Task.FromResult((status,
            body ?? OkMistral())));
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddHttpClient("AiMistral")
            .ConfigurePrimaryHttpMessageHandler(() => stub)
            .ConfigureHttpClient(c => { c.BaseAddress = new Uri("http://mistral.test/v1/"); c.Timeout = TimeSpan.FromSeconds(5); });
        var prov = sc.BuildServiceProvider();
        return (prov.GetRequiredService<IHttpClientFactory>(), stub);
    }

    private static string OkMistral() => JsonSerializer.Serialize(new
    {
        id = "gen-teste",
        @object = "chat.completion",
        created = 1,
        model = "mistral-small-2603",
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
