using System.Net;
using System.Text;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// WS-A2 (item 2 - gate financeiro): integridade monetaria/cultural ponta a ponta no BFF.
/// O valor digitado por um humano (pt-BR) deve chegar a API operacional como NUMERO sem
/// ambiguidade: "12,50" persiste 12.50 (nunca 1250), "1.234,56" persiste 1234.56, zero e
/// limites passam, e separador ambiguo nao chega ao sistema. A API e um stub deterministico;
/// as afirmacoes valem sobre o payload JSON (contrato BFF->API) e o status HTTP.
/// </summary>
[Collection("web-bff")]
public sealed class Administrativo360GateMonetarioTests
{
    private readonly PlantaoProWebFactory _factory;

    public Administrativo360GateMonetarioTests(PlantaoProWebFactory factory) => _factory = factory;

    private static string SigninQuery() =>
        WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_CLIENTE"), ("modules", "ADM360"), ("permissions", "ADM360.*"));

    /// <summary>Stub que registra os corpos recebidos e responde OK genérico.</summary>
    private StringBuilder InstalarStubObrigaOk()
    {
        var corpos = new StringBuilder();
        _factory.ApiStub.Respond(req =>
        {
            if (req.Content != null)
                corpos.AppendLine(req.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"id\":\"4d96cc88-1111-1111-1111-111111111111\"}}");
        });
        return corpos;
    }

    private async Task<HttpResponseMessage> PostarProdutoAsync(HttpClient client, string antiforgery, Dictionary<string, string> campos)
    {
        var todos = new Dictionary<string, string>(campos);
        todos["__RequestVerificationToken"] = antiforgery;
        using var form = new FormUrlEncodedContent(todos);
        using var req = new HttpRequestMessage(HttpMethod.Post, "Administrativo360/SalvarProduto") { Content = form };
        req.Headers.TryAddWithoutValidation("Referer", "https://web.test.local/Administrativo360/Produtos");
        return await client.SendAsync(req);
    }

    private static Dictionary<string, string> CamposProduto(string precoCusto, string? id = null)
    {
        // sku fixa: a API e um stub sem restricao de unicidade, e o valor nao pode
        // conter digitos para nao falsificar a assercao "1250 nao aparece no payload".
        var campos = new Dictionary<string, string>
        {
            ["sku"] = "GATE-A2",
            ["nome"] = "Produto do Gate Monetario",
            ["unidade"] = "UN",
            ["precoCusto"] = precoCusto,
            ["controlaLote"] = "true",
            ["exigeInspecao"] = "true",
            ["ativo"] = "true"
        };
        if (id != null) campos["id"] = id;
        return campos;
    }

    [Fact]
    public async Task Inclusao_DozesVirgulacinquenta_ChegaAoSistemaComo12ponto50()
    {
        _factory.ApiStub.Reset();
        var corpos = InstalarStubObrigaOk();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninQuery());

        var resp = await PostarProdutoAsync(client, token, CamposProduto("12,50"));

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("/Administrativo360/Produtos", resp.Headers.Location?.OriginalString);
        Assert.Contains("POST http://api.test.local/api/administrativo360/cadastros/produtos", _factory.ApiStub.Requests);

        var corpo = corpos.ToString();
        Assert.Contains("\"precoCusto\":12.5", corpo, StringComparison.OrdinalIgnoreCase);
        // o valor mutado do bug M2.5 nao pode aparecer em nenhum lugar do payload
        Assert.DoesNotContain("1250", corpo);
    }

    [Fact]
    public async Task Edicao_MesmoFluxo_IdenticoContrato()
    {
        _factory.ApiStub.Reset();
        var corpos = InstalarStubObrigaOk();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninQuery());
        var idProduto = Guid.NewGuid().ToString();

        var resp = await PostarProdutoAsync(client, token, CamposProduto("12,50", idProduto));

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        var corpo = corpos.ToString();
        Assert.Contains("\"precoCusto\":12.5", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(idProduto.ToLowerInvariant(), corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1250", corpo);
    }

    [Fact]
    public async Task MilduzentosTrezentosEQuarentaESeisCentavos_FormulacaoCompleta()
    {
        _factory.ApiStub.Reset();
        var corpos = InstalarStubObrigaOk();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninQuery());

        var resp = await PostarProdutoAsync(client, token, CamposProduto("1.234,56"));

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("\"precoCusto\":1234.56", corpos.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Zero_PassaComoZero()
    {
        _factory.ApiStub.Reset();
        var corpos = InstalarStubObrigaOk();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninQuery());

        var resp = await PostarProdutoAsync(client, token, CamposProduto("0"));

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("\"precoCusto\":0", corpos.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ambiguo_PontoMilDuzentosETrezentos_NaoChegaAoSistema()
    {
        _factory.ApiStub.Reset();
        InstalarStubObrigaOk();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninQuery());

        var resp = await PostarProdutoAsync(client, token, CamposProduto("1.234"));

        // binding falhou ANTES da acao (ModelStateInvalidoFiltro): PRG de volta a pagina de
        // origem com mensagem humana em TempData, e NENHUMA chamada a API foi feita.
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("/Administrativo360/Produtos", resp.Headers.Location?.OriginalString);
        Assert.Empty(_factory.ApiStub.Requests);
    }

    [Fact]
    public async Task Limite_ExcedeNumeric184_NaoChegaAoSistema()
    {
        _factory.ApiStub.Reset();
        InstalarStubObrigaOk();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninQuery());

        var resp = await PostarProdutoAsync(client, token, CamposProduto("1.000.000.000.000"));

        // mesmo contrato do caso ambiguo: nada chega ao sistema
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("/Administrativo360/Produtos", resp.Headers.Location?.OriginalString);
        Assert.Empty(_factory.ApiStub.Requests);
    }

    [Fact]
    public async Task ExportacaoCsv_Financeira_ApresentaPtBrSemAlterarValor()
    {
        _factory.ApiStub.Reset();
        _factory.ApiStub.Respond(req =>
        {
            Assert.StartsWith("api/administrativo360/relatorios-financeiros/vendas", req.RequestUri!.AbsolutePath.TrimStart('/'));
            return StubApiHandler.Json(HttpStatusCode.OK,
                "{\"success\":true,\"data\":[{\"numero\":\"V-1\",\"data\":\"2026-05-05\",\"hospital\":\"Hospital Demo\",\"pagador\":\"Pagador\",\"vendedor\":\"Vendedor\",\"totalBruto\":1234.56,\"desconto\":1.25,\"totalLiquido\":1233.31,\"totalCusto\":1000,\"comissaoPrevista\":60,\"situacao\":\"BAIXADA\"}]}");
        });
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SigninQuery());

        var resp = await client.GetAsync("Administrativo360/ExportarVendasCsv?inicio=2026-01-01&fim=2026-12-31");
        var csv = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("1.234,56", csv);   // apresentacao pt-BR (N2)
        Assert.DoesNotContain("1234.56", csv);
    }
}
