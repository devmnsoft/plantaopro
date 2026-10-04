using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Série WP-A1 (rodada 2): contrato JSON do BFF. Os consumidores de API (/bff/*) sempre
/// recebem status HTTP adequado + envelope JSON — nunca redirect silencioso para HTML,
/// página de erro HTML, corpo 3xx repassado ou chamada à API com cliente bloqueado.
/// Suíte serializada em coleção própria (um único host Web + um único stub de API).
/// </summary>
[Collection("web-bff")]
public sealed class WpA1BffContratoJsonTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public WpA1BffContratoJsonTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    [Theory]
    [InlineData("/bff/operacao/status")]
    [InlineData("/bff/notificacoes")]
    public async Task BFF_sem_sessao_responde_401_json_sem_html(string url)
    {
        var (resp, body) = await GetAnonAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        WebBffTestKit.AssertJsonContent(resp);
        Assert.Contains("SESSAO_EXPIRADA", body);
        Assert.DoesNotContain("<", body);
    }

    [Fact]
    public async Task BFF_sessao_valida_encaminha_para_api_com_bearer_e_retorna_json()
    {
        string? bearer = null;
        _factory.ApiStub.Respond(req =>
        {
            bearer = req.Headers.Authorization?.Parameter;
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"origem\":\"api\",\"ok\":true}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory);
        var resp = await client.GetAsync("/bff/operacao/status");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        WebBffTestKit.AssertJsonContent(resp);
        Assert.Contains("\"origem\":\"api\"", body);
        Assert.Contains("GET http://api.test.local/api/status", _factory.ApiStub.Requests);
        Assert.Equal("test-jwt", bearer);
    }

    [Fact]
    public async Task BFF_sessao_valida_sem_jwt_retorna_401_json_sem_chamar_api()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, WebBffTestKit.SigninQuery(("semJwt", "true")));
        var resp = await client.GetAsync("/bff/operacao/status");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        WebBffTestKit.AssertJsonContent(resp);
        Assert.Contains("SESSAO_EXPIRADA", body);
        Assert.Equal(0, _factory.ApiStub.RequestCount);
    }

    [Fact]
    public async Task BFF_token_revogado_api_responde_401_e_bff_mantem_json()
    {
        _factory.ApiStub.Respond(_ => StubApiHandler.Json(HttpStatusCode.Unauthorized, "{\"status\":401,\"message\":\"Sessão revogada pelo servidor\"}"));
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory);

        var resp = await client.GetAsync("/bff/operacao/usuario");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        WebBffTestKit.AssertJsonContent(resp);
        Assert.Contains("revogada", body);
    }

    [Fact]
    public async Task BFF_api_redireciona_para_login_bff_converte_em_401_json()
    {
        _factory.ApiStub.Respond(_ => StubApiHandler.Redirect(new Uri("http://api.test.local/Account/Login")));
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory);

        var resp = await client.GetAsync("/bff/operacao/status");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        WebBffTestKit.AssertJsonContent(resp);
        Assert.Contains("SESSAO_EXPIRADA", body);
        Assert.DoesNotContain("<", body);
    }

    [Fact]
    public async Task BFF_api_redireciona_para_outro_destino_bff_converte_em_502_json()
    {
        _factory.ApiStub.Respond(_ => StubApiHandler.Redirect(new Uri("http://api.test.local/home")));
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory);

        var resp = await client.GetAsync("/bff/operacao/status");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadGateway, resp.StatusCode);
        WebBffTestKit.AssertJsonContent(resp);
        Assert.Contains("SERVICO_INDISPONIVEL", body);
    }

    [Fact]
    public async Task BFF_api_responde_html_em_erro_bff_converte_em_json()
    {
        _factory.ApiStub.Respond(_ => StubApiHandler.Html(HttpStatusCode.InternalServerError, "<html><body>erro interno</body></html>"));
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory);

        var resp = await client.GetAsync("/bff/operacao/status");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
        WebBffTestKit.AssertJsonContent(resp);
        Assert.Contains("ERRO_SERVICO", body);
        Assert.DoesNotContain("<html", body);
    }

    [Fact]
    public async Task BFF_cliente_bloqueado_retorna_403_json_sem_chamar_api()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, WebBffTestKit.SigninQuery(("clienteBloqueado", "true")));

        var resp = await client.GetAsync("/bff/operacao/status");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        WebBffTestKit.AssertJsonContent(resp);
        Assert.Contains("CLIENTE_BLOQUEADO", body);
        Assert.Equal(0, _factory.ApiStub.RequestCount);
    }

    [Fact]
    public async Task BFF_post_sem_antiforgery_retorna_400_json_sem_chamar_api()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory);

        var resp = await client.PostAsync("/bff/operacao/saude", new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal(0, _factory.ApiStub.RequestCount);
    }

    [Fact]
    public async Task BFF_post_com_antiforgery_valido_encaminha_post_para_api()
    {
        _factory.ApiStub.Respond(req => StubApiHandler.Json(HttpStatusCode.OK, "{\"metodo\":\"" + req.Method + "\",\"ok\":true}"));
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory);

        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/operacao/saude")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("RequestVerificationToken", token);

        var resp = await client.SendAsync(request);
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("\"metodo\":\"POST\"", body);
        Assert.Contains("POST http://api.test.local/api/saude", _factory.ApiStub.Requests);
    }

    private async Task<(HttpResponseMessage Resp, string Body)> GetAnonAsync(string url)
    {
        _factory.ApiStub.Reset();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync(url);
        var body = await resp.Content.ReadAsStringAsync();
        return (resp, body);
    }
}

[CollectionDefinition("web-bff")]
public sealed class WebBffCollectionDefinition : ICollectionFixture<PlantaoProWebFactory>
{
}

/// <summary>
/// Helpers de sessão/antiforgery para os testes de integração Web da rodada 2.
/// </summary>
internal static class WebBffTestKit
{
    public static string SigninQuery(params (string Key, string? Value)[] pairs)
    {
        var query = new StringBuilder("?");
        foreach (var (key, value) in pairs)
        {
            if (value is null) continue;
            query.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value)).Append('&');
        }
        return query.ToString().TrimEnd('&');
    }

    /// <summary>Cria um client com sessão autenticada via /__test/signin (claims controlados).</summary>
    public static async Task<HttpClient> ClienteAutenticadoAsync(PlantaoProWebFactory factory, string query = "")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("__test/signin" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    /// <summary>
    /// Client autenticado + token antiforgery emitido no contexto JÁ autenticado (uid do
    /// usuário de teste). A antiforgery do ASP.NET Core associa o token ao uid da sessão,
    /// portanto o scraping anônimo da página de login falha ao validar em POST autenticado.
    /// O cookie antiforgery já fica guardado no próprio client pelo /__test/signin.
    /// </summary>
    public static async Task<(HttpClient Client, string Token)> ClienteComAntiforgeryAsync(PlantaoProWebFactory factory, string query = "")
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("__test/signin" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(body, "\"antiforgery\"\\s*:\\s*\"([^\"]+)\"");
        Assert.True(match.Success, "Token antiforgery nao retornado pelo signin de teste.");
        return (client, match.Groups[1].Value);
    }

    /// <summary>Extrai o token antiforgery real da página de login (mesmo fluxo de produção).</summary>
    public static async Task<string> AntiforgeryTokenAsync(PlantaoProWebFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var resp = await client.GetAsync("/Account/Login");
        var html = await resp.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Token antiforgery nao encontrado na pagina de login.");
        return match.Groups[1].Value;
    }

    public static void AssertJsonContent(HttpResponseMessage resp)
    {
        var mediaType = resp.Content?.Headers.ContentType?.MediaType ?? string.Empty;
        Assert.StartsWith("application/json", mediaType);
    }
}
