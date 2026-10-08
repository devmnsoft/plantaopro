using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

// ============================================================================
// R5-B4 (Minha assinatura real): Index mapeia o DTO da API (sem estado vazio
// falso), Uso/Limites/Faturas leem a API, Upgrade/Downgrade/Cancelamento viram
// solicitacoes reais com estado honesto; AUDITOR barrado nos POSTs pelo guard.
// Catálogo público anônimo vem do banco (sem preços inventados).
// ============================================================================
[Collection("web-bff")]
public sealed class SaasComercialB4MinhaAssinaturaTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public SaasComercialB4MinhaAssinaturaTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private const string PlanoId = "5e27aa52-689d-487d-880d-36a8d36e5a7f";

    private static string SigninAdmin() =>
        WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_CLIENTE"), ("modules", "ASSINATURAS"), ("permissions", "ASSINATURAS.*"), ("claim", "tenant_id=d3f6584c-2c64-4e5a-9ea9-4e1428647502"));

    private static string SigninAuditor() =>
        WebBffTestKit.SigninQuery(("roles", "AUDITOR"), ("modules", "ASSINATURAS"), ("permissions", "ASSINATURAS.VER"), ("claim", "tenant_id=d3f6584c-2c64-4e5a-9ea9-4e1428647502"));

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string token, string url, Dictionary<string, string> campos)
    {
        var todos = new Dictionary<string, string>(campos) { ["__RequestVerificationToken"] = token };
        using var form = new FormUrlEncodedContent(todos);
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        req.Headers.TryAddWithoutValidation("Referer", "http://localhost/MinhaAssinatura");
        return await client.SendAsync(req);
    }

    private void ResponderComercial(StringBuilder? corpos = null, bool comPendente = false)
    {
        _factory.ApiStub.Respond(req =>
        {
            if (req.Content != null && corpos != null)
                corpos.AppendLine(req.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("/solicitar-upgrade", StringComparison.OrdinalIgnoreCase)
                || uri.Contains("/solicitar-downgrade", StringComparison.OrdinalIgnoreCase)
                || uri.Contains("/solicitar-cancelamento", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":\"ok\",\"message\":\"Solicitação registrada para avaliação comercial.\"}");
            if (uri.Contains("/minha-assinatura/solicitacoes", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, comPendente
                    ? "{\"success\":true,\"data\":[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"tipo\":\"UPGRADE\",\"planoDestinoNome\":\"B4 Plano B\",\"status\":\"SOLICITADO\",\"solicitadoEm\":\"2026-10-08T00:00:00Z\",\"mensagemEstado\":\"Aguardando avaliacao comercial. Nada mudou no seu plano.\"}]}"
                    : "{\"success\":true,\"data\":[]}");
            if (uri.Contains("/minha-assinatura/uso", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"planoNome\":\"B4 Plano A\",\"assinaturaStatus\":\"ATIVA\",\"dataFim\":\"2026-12-31T00:00:00Z\",\"dataTrialFim\":null,\"medicosUsados\":2,\"medicosLimite\":0,\"hospitaisUsados\":1,\"hospitaisLimite\":5,\"plantoesMesUsados\":10,\"plantoesMesLimite\":100,\"usuariosUsados\":3,\"usuariosLimite\":5,\"convitesMesUsados\":0,\"convitesMesLimite\":10,\"permiteMobile\":true,\"permiteBi\":false,\"permiteRelatoriosAvancados\":false,\"permiteIntegracoes\":false,\"permiteOperacaoAssistida\":false,\"permiteSuportePrioritario\":false}}");
            if (uri.Contains("/minha-assinatura/faturas", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":[]}");
            if (uri.Contains("/minha-assinatura", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"assinaturaId\":\"22222222-2222-2222-2222-222222222222\",\"clienteId\":\"33333333-3333-3333-3333-333333333333\",\"planoId\":\"44444444-4444-4444-4444-444444444444\",\"planoNome\":\"B4 Plano A\",\"status\":\"ATIVA\",\"valorContratado\":399.0,\"dataInicio\":\"2026-01-01T00:00:00Z\",\"dataFim\":\"2026-12-31T00:00:00Z\"}}");
            if (uri.Contains("/planos/publicos", StringComparison.OrdinalIgnoreCase) || uri.Contains("/public/planos", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":[{\"id\":\"" + PlanoId + "\",\"nome\":\"B4 Plano B\",\"slug\":\"b4-b\",\"descricao\":\"Plano B\",\"valorMensal\":899.0,\"limiteMedicos\":100,\"limiteHospitais\":10,\"limitePlantoesMes\":500,\"limiteUsuarios\":20,\"permiteMobile\":true,\"permiteBi\":false,\"permiteWhiteLabel\":false,\"destaque\":false,\"recursos\":[]}]}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":[]}");
        });
    }

    [Fact]
    public async Task Index_MapeiaPlanoReal_SemEstadoVazioFalso()
    {
        _factory.ApiStub.Reset();
        ResponderComercial();
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SigninAdmin());

        var resp = await client.GetAsync("MinhaAssinatura");
        var html = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("B4 Plano A", html);
        Assert.DoesNotContain("ainda não vinculada", html);
    }

    [Fact]
    public async Task Uso_LeApiReal_LimiteZeroEhIlimitado()
    {
        _factory.ApiStub.Reset();
        ResponderComercial();
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SigninAdmin());

        var resp = await client.GetAsync("MinhaAssinatura/Uso");
        var html = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("2 (ilimitado)", html);
        Assert.Contains("1/5", html);
        Assert.DoesNotContain("35/100", html);
    }

    [Fact]
    public async Task AUDITOR_PostSolicitarUpgrade_BarradoNoGuard_SemChamarApi()
    {
        _factory.ApiStub.Reset();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAuditor());

        var resp = await PostFormAsync(client, token, "MinhaAssinatura/SolicitarUpgrade",
            new Dictionary<string, string> { ["planoDestinoId"] = PlanoId, ["motivo"] = "quero mais" });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("AccessDenied", resp.Headers.Location?.OriginalString ?? string.Empty);
        Assert.Equal(0, _factory.ApiStub.RequestCount);
    }

    [Fact]
    public async Task Upgrade_Get_ListaPlanosReais_E_Pendentes()
    {
        _factory.ApiStub.Reset();
        ResponderComercial(comPendente: true);
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SigninAdmin());

        var resp = await client.GetAsync("MinhaAssinatura/Upgrade");
        var html = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("B4 Plano B", html);
        Assert.Contains("Aguardando avaliacao", html);
    }

    [Fact]
    public async Task Upgrade_Post_RegistraSolicitacaoReal()
    {
        _factory.ApiStub.Reset();
        var corpos = new StringBuilder();
        ResponderComercial(corpos);
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAdmin());

        var resp = await PostFormAsync(client, token, "MinhaAssinatura/SolicitarUpgrade",
            new Dictionary<string, string> { ["planoDestinoId"] = PlanoId, ["motivo"] = "crescimento" });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("POST http://api.test.local/api/minha-assinatura/solicitar-upgrade", _factory.ApiStub.Requests);
        Assert.Contains(PlanoId.ToLowerInvariant(), corpos.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cancelamento_Post_MotivoCurto_NaoChamaApi()
    {
        _factory.ApiStub.Reset();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAdmin());

        var resp = await PostFormAsync(client, token, "MinhaAssinatura/SolicitarCancelamento",
            new Dictionary<string, string> { ["motivo"] = "curto" });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("Cancelamento", resp.Headers.Location?.OriginalString ?? string.Empty);
        Assert.Equal(0, _factory.ApiStub.RequestCount);
    }

    [Fact]
    public async Task Cancelamento_Post_Valido_RegistraSolicitacao()
    {
        _factory.ApiStub.Reset();
        var corpos = new StringBuilder();
        ResponderComercial(corpos);
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAdmin());

        var resp = await PostFormAsync(client, token, "MinhaAssinatura/SolicitarCancelamento",
            new Dictionary<string, string> { ["motivo"] = "encerrando a operacao da unidade b4" });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("POST http://api.test.local/api/minha-assinatura/solicitar-cancelamento", _factory.ApiStub.Requests);
        Assert.Contains("encerrando a operacao", corpos.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Planos_Publicos_Anonimos_VemDoBanco()
    {
        _factory.ApiStub.Reset();
        ResponderComercial();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var resp = await client.GetAsync("planos");
        var html = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("B4 Plano B", html);
        Assert.Contains("GET http://api.test.local/api/public/planos", _factory.ApiStub.Requests);
    }
}
