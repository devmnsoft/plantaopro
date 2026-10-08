using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

// ============================================================================
// R5-B5 (provisionamento e convites no Web): Cadastro.Confirmar chama o
// finalizar real (sem sucesso sem persistência); convites com token único;
// AUDITOR barrado nos POSTs pelo guard.
// ============================================================================
[Collection("web-bff")]
public sealed class ProvisionamentoB5WebTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public ProvisionamentoB5WebTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private const string Token = "b5token0123456789abcdef0123456789abcdef0123456789abcdef0123456789";

    private static string SigninAdmin() =>
        WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_CLIENTE"), ("modules", "USUARIOS"), ("permissions", "USUARIOS.*"), ("claim", "tenant_id=d3f6584c-2c64-4e5a-9ea9-4e1428647502"));

    private static string SigninAuditor() =>
        WebBffTestKit.SigninQuery(("roles", "AUDITOR"), ("modules", "USUARIOS"), ("permissions", "USUARIOS.VER"), ("claim", "tenant_id=d3f6584c-2c64-4e5a-9ea9-4e1428647502"));

    private static Dictionary<string, string> CadastroCampos() => new()
    {
        ["NomeFantasia"] = "B5 Web LTDA",
        ["RazaoSocial"] = "B5 Web LTDA",
        ["Cnpj"] = "12345678000195",
        ["Cidade"] = "Olinda",
        ["Uf"] = "PE",
        ["EmailCorporativo"] = "b5web@example.test",
        ["PlanoId"] = "11111111-1111-1111-1111-111111111111",
        ["AceiteTermos"] = "true",
        ["AceitePrivacidade"] = "true",
        ["ResponsavelNome"] = "B5 Web Admin",
        ["ResponsavelEmail"] = "b5web-admin@example.test",
        ["Senha"] = "B5senha123!"
    };

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string token, string url, Dictionary<string, string> campos, string referer)
    {
        var todos = new Dictionary<string, string>(campos) { ["__RequestVerificationToken"] = token };
        using var form = new FormUrlEncodedContent(todos);
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        req.Headers.TryAddWithoutValidation("Referer", referer);
        return await client.SendAsync(req);
    }

    private static async Task<(HttpClient Client, string Token)> ClienteAnonimoComAntiforgeryAsync(PlantaoProWebFactory factory, string pagina)
    {
        // Antiforgery vincula token ao cookie: scraping e POST precisam do MESMO client.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync(pagina);
        var html = await resp.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Token antiforgery nao encontrado na página " + pagina + ".");
        return (client, match.Groups[1].Value);
    }

    [Fact]
    public async Task Cadastro_Confirmar_ChamaFinalizarReal_E_RedirecionaSucesso()
    {
        _factory.ApiStub.Reset();
        var corpos = new StringBuilder();
        _factory.ApiStub.Respond(req =>
        {
            if (req.Content != null)
                corpos.AppendLine(req.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"solicitacaoId\":\"11111111-1111-1111-1111-111111111111\",\"tenantId\":\"22222222-2222-2222-2222-222222222222\",\"clienteId\":\"33333333-3333-3333-3333-333333333333\",\"assinaturaId\":\"44444444-4444-4444-4444-444444444444\",\"usuarioAdminId\":\"55555555-5555-5555-5555-555555555555\",\"loginUrl\":\"/Account/Login\",\"onboardingUrl\":\"/Onboarding\"}}");
        });
        var (client, token) = await ClienteAnonimoComAntiforgeryAsync(_factory, "cadastro/confirmacao");

        var resp = await PostFormAsync(client, token, "cadastro/confirmacao", CadastroCampos(), "http://localhost/cadastro/confirmacao");

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("sucesso", (resp.Headers.Location?.OriginalString ?? string.Empty).ToLowerInvariant());
        Assert.Contains("POST http://api.test.local/api/public/cadastro/finalizar", _factory.ApiStub.Requests);
        Assert.Contains("12345678000195", corpos.ToString());
    }

    [Fact]
    public async Task Cadastro_Confirmar_Api400_ReexibeComMotivoReal()
    {
        _factory.ApiStub.Reset();
        _factory.ApiStub.Respond(_ => StubApiHandler.Json(HttpStatusCode.BadRequest, "{\"success\":false,\"message\":\"CNPJ já cadastrado.\"}"));
        var (client, token) = await ClienteAnonimoComAntiforgeryAsync(_factory, "cadastro/confirmacao");

        var resp = await PostFormAsync(client, token, "cadastro/confirmacao", CadastroCampos(), "http://localhost/cadastro/confirmacao");
        var html = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("cadastrado", html);
    }

    [Fact]
    public async Task Convites_Index_Lista_E_CriarMostraTokenUmaVez()
    {
        _factory.ApiStub.Reset();
        _factory.ApiStub.Respond(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("/equipe/convites") && req.Method == HttpMethod.Post)
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"id\":\"66666666-6666-6666-6666-666666666666\",\"email\":\"novo@exemplo.test\",\"perfilIds\":[],\"perfilNomes\":[\"B5\"],\"expiraEm\":\"2026-10-15T00:00:00Z\",\"estado\":\"PENDENTE\",\"criadoEm\":\"2026-10-08T00:00:00Z\",\"token\":\"" + Token + "\"}}");
            if (uri.Contains("perfis-atribuiveis"))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":[]}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":[{\"id\":\"66666666-6666-6666-6666-666666666666\",\"email\":\"novo@exemplo.test\",\"perfilNomes\":[\"B5\"],\"expiraEm\":\"2026-10-15T00:00:00Z\",\"estado\":\"PENDENTE\",\"criadoEm\":\"2026-10-08T00:00:00Z\"}]}");
        });
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAdmin());

        var criar = await PostFormAsync(client, token, "ConvitesEquipe/Criar",
            new Dictionary<string, string> { ["email"] = "novo@exemplo.test", ["perfilIds"] = "11111111-1111-1111-1111-111111111111", ["diasValidade"] = "7" }, "http://localhost/ConvitesEquipe");
        Assert.Equal(HttpStatusCode.Redirect, criar.StatusCode);
        Assert.Contains("POST http://api.test.local/api/equipe/convites", _factory.ApiStub.Requests);

        var index = await client.GetAsync("ConvitesEquipe");
        var html = await index.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
        Assert.Contains("/convite/aceitar/" + Token, html);
        Assert.Contains("novo@exemplo.test", html);
    }

    [Fact]
    public async Task AUDITOR_PostCriarConvite_BarradoNoGuard_SemChamarApi()
    {
        _factory.ApiStub.Reset();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAuditor());

        var resp = await PostFormAsync(client, token, "ConvitesEquipe/Criar",
            new Dictionary<string, string> { ["email"] = "x@exemplo.test" }, "http://localhost/ConvitesEquipe");

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("AccessDenied", resp.Headers.Location?.OriginalString ?? string.Empty);
        Assert.Equal(0, _factory.ApiStub.RequestCount);
    }

    [Fact]
    public async Task ConvitePublico_AceitarGet_MostraFormulario_QuandoValido()
    {
        _factory.ApiStub.Reset();
        _factory.ApiStub.Respond(_ => StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"email\":\"novo@exemplo.test\",\"tenantNome\":\"B5 Tenant\",\"expiraEm\":\"2026-10-15T00:00:00Z\",\"valido\":true,\"motivo\":\"\"}}"));
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var resp = await client.GetAsync("convite/aceitar/abc123");
        var html = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("novo@exemplo.test", html);
        Assert.Contains("Criar minha conta", html);
    }

    [Fact]
    public async Task ConvitePublico_AceitarPost_Registra_E_RedirecionaLogin()
    {
        _factory.ApiStub.Reset();
        var corpos = new StringBuilder();
        _factory.ApiStub.Respond(req =>
        {
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (req.Content != null && uri.Contains("/aceitar"))
                corpos.AppendLine(req.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            if (uri.Contains("/aceitar") && req.Method == HttpMethod.Post)
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":\"77777777-7777-7777-7777-777777777777\"}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"email\":\"novo@exemplo.test\",\"tenantNome\":\"B5 Tenant\",\"expiraEm\":\"2026-10-15T00:00:00Z\",\"valido\":true,\"motivo\":\"\"}}");
        });
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var get = await client.GetAsync("convite/aceitar/abc123");
        var html = await get.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Antiforgery da página pública não encontrado.");

        var resp = await PostFormAsync(client, match.Groups[1].Value, "convite/aceitar/abc123",
            new Dictionary<string, string> { ["nome"] = "Novo Membro", ["senha"] = "B5senha123!" }, "http://localhost/convite/aceitar/abc123");

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("Login", resp.Headers.Location?.OriginalString ?? string.Empty);
        Assert.Contains("POST http://api.test.local/api/public/convites/abc123/aceitar", _factory.ApiStub.Requests);
    }
}
