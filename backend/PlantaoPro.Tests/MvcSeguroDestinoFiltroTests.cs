using System.Net;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R4-A3 (MVC seguro): o destino de retorno do ModelStateInvalidoFiltro nao pode confiar no
/// Referer cru (open redirect) e o tipo de resposta deve distinguir form MVC (PRG com
/// destino local validado), BFF/API (JSON 400 com envelope canonico) e GET (mesma URL,
/// sem efeito no banco). Unidade: helper DestinoLocalValidado. Integracao: os tres ramos
/// contra o host real em ambiente Testing, com stub deterministico de API.
/// </summary>
public sealed class MvcSeguroDestinoUnitTests
{
    private static HttpRequest Pedido(string scheme = "https", string host = "localhost", int porta = 443, string? referer = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = new HostString(host, porta);
        ctx.Request.Path = "/pagina";
        if (referer is not null) ctx.Request.Headers["Referer"] = referer;
        return ctx.Request;
    }

    [Fact]
    public void SemReferer_RetornaNulo()
        => Assert.Null(MvcSeguroDestinoFixture.Destino(Pedido()));

    [Theory]
    [InlineData("pagina/outra")]
    [InlineData("//outro.com/x")]
    public void RefererRelativoOuInvalido_RetornaNulo(string referer)
        => Assert.Null(MvcSeguroDestinoFixture.Destino(Pedido(referer: referer)));

    [Fact]
    public void RefererExterno_RetornaNulo()
        => Assert.Null(MvcSeguroDestinoFixture.Destino(Pedido(referer: "https://outro.invalid/form")));

    [Fact]
    public void PortaDiferente_RetornaNulo()
        => Assert.Null(MvcSeguroDestinoFixture.Destino(Pedido(referer: "https://localhost:9999/form")));

    [Fact]
    public void SchemeDiferente_RetornaNulo()
        => Assert.Null(MvcSeguroDestinoFixture.Destino(Pedido(scheme: "https", referer: "http://localhost/form")));

    [Fact]
    public void HostEmMaiusculas_MesmaOrigem_Aceita()
        => Assert.Equal("/form", MvcSeguroDestinoFixture.Destino(Pedido(referer: "HTTPS://LOCALHOST/form")));

    [Fact]
    public void RefererLocal_RetornaPathQueryPreservado()
        => Assert.Equal(
            "/__test/mvcfiltro/get?valor=12%2C5",
            MvcSeguroDestinoFixture.Destino(Pedido(referer: "https://localhost/__test/mvcfiltro/get?valor=12%2C5")));
}

internal static class MvcSeguroDestinoFixture
{
    public static string? Destino(HttpRequest request)
        => PlantaoPro.Web.Services.Mvc.ModelStateInvalidoFiltro.DestinoLocalValidado(request);
}

[Collection("web-bff")]
public sealed class MvcSeguroDestinoIntegracaoTests
{
    private readonly PlantaoProWebFactory _factory;

    public MvcSeguroDestinoIntegracaoTests(PlantaoProWebFactory factory) => _factory = factory;

    private static string SigninQuery() =>
        WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_CLIENTE"), ("modules", "ADM360"), ("permissions", "ADM360.*"));

    private async Task<(HttpClient Client, string Token)> AuthAsync()
        => await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninQuery());

    private static HttpRequestMessage FormPost(string rota, string valor, string token, string? referer = null)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["valor"] = valor,
            ["__RequestVerificationToken"] = token
        });
        var req = new HttpRequestMessage(HttpMethod.Post, rota) { Content = form };
        if (referer is not null) req.Headers.TryAddWithoutValidation("Referer", referer);
        return req;
    }

    [Fact]
    public async Task PostDecimalAmbiguo_SemReferer_400DiretoSemChamadaDeApi()
    {
        _factory.ApiStub.Reset();
        var (client, token) = await AuthAsync();

        using var resp = await client.SendAsync(FormPost("__test/mvcfiltro/post", "1.234", token));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Null(resp.Headers.Location);
        Assert.Empty(_factory.ApiStub.Requests); // sem efeito no sistema (stub da API nunca chamado)
    }

    [Fact]
    public async Task PostDecimalAmbiguo_RefererExterno_400ENaoOpenRedirect()
    {
        _factory.ApiStub.Reset();
        var (client, token) = await AuthAsync();

        using var resp = await client.SendAsync(FormPost("__test/mvcfiltro/post", "1.234", token, referer: "https://externo.invalid/form"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Null(resp.Headers.Location);
        Assert.Empty(_factory.ApiStub.Requests);
    }

    [Fact]
    public async Task PostDecimalAmbiguo_RefererLocal_Valido_RedirectParaOrigem()
    {
        _factory.ApiStub.Reset();
        var (client, token) = await AuthAsync();

        using var resp = await client.SendAsync(FormPost("__test/mvcfiltro/post", "1.234", token, referer: "http://localhost/Administrativo360/Produtos"));

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal("/Administrativo360/Produtos", resp.Headers.Location?.OriginalString);
        Assert.Empty(_factory.ApiStub.Requests);
    }

    [Fact]
    public async Task GetDecimalAmbiguo_RedirectParaPropriaUrlComQueryPreservada()
    {
        _factory.ApiStub.Reset();
        var (client, _) = await AuthAsync();

        using var resp = await client.GetAsync("__test/mvcfiltro/get?valor=1.234");

        // GET invalido nao executa acao nem toca o banco; reenvia a mesma URL (filtros preservados).
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Equal("/__test/mvcfiltro/get?valor=1.234", resp.Headers.Location?.OriginalString);
        Assert.Empty(_factory.ApiStub.Requests);
    }

    [Fact]
    public async Task GetValorValido_PassaPeloFiltroEExecutaAcao()
    {
        _factory.ApiStub.Reset();
        var (client, _) = await AuthAsync();

        using var resp = await client.GetAsync("__test/mvcfiltro/get?valor=12%2C50");
        var corpo = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("\"ok\":true", corpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostBffDecimalAmbiguo_Json400DadosInvalidosSemRedirectHtml()
    {
        _factory.ApiStub.Reset();
        var (client, token) = await AuthAsync();

        using var resp = await client.SendAsync(FormPost("bff/mvcfiltro", "1.234", token));
        var corpo = await resp.Content.ReadAsStringAsync();

        // consumidor de API recebe JSON canonico, nunca redirect HTML nem problem+json
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.StartsWith("application/json", resp.Content.Headers.ContentType?.MediaType);
        Assert.Null(resp.Headers.Location);
        Assert.Contains("\"reason\":\"DADOS_INVALIDOS\"", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"status\":400", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_factory.ApiStub.Requests);
    }
}
