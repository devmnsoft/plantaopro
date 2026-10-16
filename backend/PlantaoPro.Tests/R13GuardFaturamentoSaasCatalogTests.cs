using System.Net;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R13-B1 (achado r12b2, P3 no topo da fila R13): o guard cobrava o codigo fantasma "BILLING",
/// ausente de modulos_sistema — o modulo canonico do dominio de cobranca SaaS e FATURAMENTO_SAAS
/// ("Faturamento SaaS", ATIVO/A, ordem 200), registrado no seed canonico do repo e com suite
/// completa de 31 permissoes (incl. .VER) ja grantada (dormente) nos perfis admin de tenant.
/// Consequencia do defeito: qualquer usuario nao-global que passava na checagem de papeis de
/// FaturamentoSaasController/BillingController caia em MODULO_NAO_CONTRATADO para sempre (o
/// drill-down "Receita Vencida" do BI Executivo apontava para /FaturamentoSaas?status=VENCIDA);
/// somente o bypass global-admin liberava. Fix: remapear as duas entradas do guard para o modulo
/// canonico — mesma classe de defeito dos fixes R6-BlocoA item 1 (Caixa), R7-E1 (Usuario) e
/// R12-B1 (Bi/DashboardsPremium); sem DDL, sem novo modulo duplicado, access_catalog_version v2149.
/// </summary>
public sealed class R13GuardFaturamentoCatalogoContratoTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Guard_MapeiaBillingEFaturamentoSaasParaModuloCanonicoDoCatalogo_EExtintoOCodigoFantasma()
    {
        var guard = Read("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs");

        Assert.Contains("[\"Billing\"] = \"FATURAMENTO_SAAS\",", guard);
        Assert.Contains("[\"FaturamentoSaas\"] = \"FATURAMENTO_SAAS\",", guard);
        // Anti-fantasma: o codigo BILLING nao existe em modulos_sistema (evidence r13a0 F3) —
        // qualquer regressao da entrada ao codigo morto reviveria a denegacao permanente.
        Assert.DoesNotContain("\"BILLING\"", guard);
    }

    [Fact]
    public void ModuloFaturamentoSaas_RegistradoNoSeedCanonicalDoRepo_NaoDependeDeBanco()
    {
        var seed = Read("database/migrations/2026_plantao_pro_self_service_white_label.sql");

        // Fonte canonica: a linha do catalogo (codigo, nome, descricao, ordem 200).
        Assert.Contains("('FATURAMENTO_SAAS','Faturamento SaaS','Faturas SaaS',200)", seed);
    }
}

/// <summary>
/// R13-B1 (comportamento): o guard resolve Billing/FaturamentoSaas -> FATURAMENTO_SAAS com os
/// motivos canonicos de denegacao (MODULO_NAO_CONTRATADO / PERMISSAO_NEGADA) em vez do motivo
/// permanente de modulo inexistente no catalogo, e libera 200 para sessao contratada com permissao
/// de leitura — incluindo o filtro ?status=VENCIDA usado pelo drill-down do card "Receita Vencida"
/// do BI Executivo (r10b3). O shell /Billing (R5-B6) segue o mesmo gate e redireciona para a
/// operacao canonica quando liberado; global admin continua no bypass. Colecao web-bff:
/// live-check desligado, decisao pelos claims da sessao (deterministico).
/// </summary>
[Collection("web-bff")]
public sealed class R13GuardFaturamentoAcessoWebTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public R13GuardFaturamentoAcessoWebTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private static void StubFaturamentoVazio(PlantaoProWebFactory factory) => factory.ApiStub.Respond(req =>
    {
        var path = req.RequestUri!.AbsolutePath;
        if (path.EndsWith("/api/faturamento-saas/resumo"))
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{}}");
        if (path.EndsWith("/api/faturamento-saas/faturas"))
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"items\":[]}}");
        return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
    });

    [Fact]
    public async Task FaturamentoSaas_SemModuloContratado_NegaComModuloNaoContratado_EMostraModuloCanonico()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/FaturamentoSaas/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // O defeito era o motivo canonico apontando para o modulo fantasma: agora a denegacao e
        // honesta e nomeia o modulo real do catalogo (contratavel).
        Assert.Contains("module=FATURAMENTO_SAAS", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
    }

    [Fact]
    public async Task FaturamentoSaas_ComModuloSemPermissao_NegaComPermissaoNegada_NaoSucessoFalso()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,FATURAMENTO_SAAS"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/FaturamentoSaas/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // Modulo contratado (claim presente); falta a permissao de leitura do perfil.
        Assert.DoesNotContain("MODULO_NAO_CONTRATADO", location);
        Assert.Contains("module=FATURAMENTO_SAAS", location);
        Assert.Contains("reason=PERMISSAO_NEGADA", location);
    }

    [Fact]
    public async Task FaturamentoSaas_ComModuloEPermissao_RenderizaResumoEFaturasComEstadoVazioHonesto()
    {
        StubFaturamentoVazio(_factory);

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,FATURAMENTO_SAAS"), ("permissions", "FATURAMENTO_SAAS.*")));

        var resp = await client.GetAsync("/FaturamentoSaas/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Faturamento SaaS", html);
        // Cards do resumo presentes com valores zerados (nenhum numero demonstrativo inventado).
        Assert.Contains("Receita prevista", html);
        Assert.Contains("Receita recebida", html);
        Assert.Contains("Vencidas", html);
    }

    [Fact]
    public async Task FaturamentoSaas_FiltroStatusVencida_DoDrillDownBi_RefletidoNoFormulario()
    {
        StubFaturamentoVazio(_factory);

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,FATURAMENTO_SAAS"), ("permissions", "FATURAMENTO_SAAS.*")));

        var resp = await client.GetAsync("/FaturamentoSaas/Index?status=VENCIDA");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        // A rota exata do card "Receita Vencida" do BI (r10b3) chega com o filtro ja aplicado.
        Assert.Contains("value=\"VENCIDA\" selected", html);
    }

    [Fact]
    public async Task Billing_ShemallRedirecionamento_Liberado_RedirecionaParaOperacaoCanonicaDeFaturas()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,FATURAMENTO_SAAS"), ("permissions", "FATURAMENTO_SAAS.*")));

        var resp = await client.GetAsync("/Billing/Faturas");

        // O guard passa (modulo canonico + permissao) e a shell R5-B6 redireciona para a tela real.
        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/FaturamentoSaas/Index", location);
    }

    [Fact]
    public async Task Billing_ShemallRedirecionamento_SemModuloContratado_NegaPeloMesmoModuloCanonico()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "FINANCEIRO"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/Billing/Faturas");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        Assert.Contains("module=FATURAMENTO_SAAS", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
    }

    [Theory]
    [InlineData("/FaturamentoSaas/Index", true)]
    [InlineData("/Billing/Faturas", false)]
    public async Task GlobalAdmin_SemClaimsDeModuloOuPermissao_BypassPreservado(string url, bool renderizaDireto)
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_GLOBAL")));

        var resp = await client.GetAsync(url);

        if (renderizaDireto)
        {
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
        else
        {
            // Shell de redirecionamento: o bypass libera o guard e a acao redireciona.
            Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
            var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
            Assert.StartsWith("/FaturamentoSaas/Index", location);
        }
    }
}
