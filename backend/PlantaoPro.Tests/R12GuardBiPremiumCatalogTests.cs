using System.Net;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R12-B1 (achado r11b2, P2 no topo da fila R12): BiController (BI Executivo) e
/// DashboardsPremiumController (dashboards executivos por perfil) estavam orfos do catalogo
/// SaasRouteGuardFilter.ControllerModules — qualquer usuario autenticado (incl. global admin)
/// caia em CATALOGO_NAO_CONFIGURADO ANTES de qualquer checagem de modulo/contratacao/permissao,
/// bloqueando integralmente a homologacao dos 7 drill-downs da R10 (27-L208). Fix: entradas
/// canonicas ["Bi"]="BI" e ["DashboardsPremium"]="DASHBOARD" — modulos ja existentes em
/// modulos_sistema com suite completa de acoes (incluindo .VER); DASHBOARD estava livre no
/// guard (CLINICA_DASHBOARD e dominio distinto, dono ClinicaDashboard). Todas as acoes dos dois
/// controllers resolvem para VER (telas somente leitura) — sem overrides de acao. Exposicao em
/// menu segue como decisao de produto (fila R12); ate la as rotas sao acessiveis por URL direta
/// quando contratadas. Mesmo padrao dos fixes R6-BlocoA item 1 (Caixa) e R7-E1 (Usuario).
/// </summary>
public sealed class R12GuardBiPremiumCatalogTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Guard_MapeiaBiEPremiumParaModulosCanonicosDoCatalogo()
    {
        var guard = Read("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs");

        Assert.Contains("[\"Bi\"] = \"BI\",", guard);
        Assert.Contains("[\"DashboardsPremium\"] = \"DASHBOARD\",", guard);
    }

    [Fact]
    public void TelasAlvo_ViewsExistentes_NaoHaRotaParaViewAusente()
    {
        Assert.True(File.Exists(Path.Combine(Root, "backend/PlantaoPro.Web/Views/Bi/Index.cshtml")));
        Assert.True(File.Exists(Path.Combine(Root, "backend/PlantaoPro.Web/Views/DashboardsPremium/Perfil.cshtml")));
    }
}

/// <summary>
/// R12-B1 (comportamento): o guard resolve Bi->BI e DashboardsPremium->DASHBOARD com os
/// motivos canonicos de denegacao (MODULO_NAO_CONTRATADO / PERMISSAO_NEGADA) em vez do
/// defeito CATALOGO_NAO_CONFIGURADO, e libera 200 para sessao contratada com permissao de
/// leitura e para global admin (bypass preservado). Colecao web-bff: live-check desligado,
/// decisao pelos claims da sessao (deterministico).
/// </summary>
[Collection("web-bff")]
public sealed class R12GuardBiPremiumAcessoWebTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public R12GuardBiPremiumAcessoWebTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    [Fact]
    public async Task Bi_SemModuloContratado_NegaComModuloNaoContratado_NaoCatalogoInexistente()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/Bi/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // O defeito do r11b2 era CATALOGO_NAO_CONFIGURADO (modulo nao resolvido no catalogo).
        // Agora o guard resolve BI e nega pelo motivo canonico de contrato.
        Assert.DoesNotContain("CATALOGO_NAO_CONFIGURADO", location);
        Assert.Contains("module=BI", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
    }

    [Fact]
    public async Task Bi_ComModuloSemPermissao_NegaComPermissaoNegada_NaoSucessoFalso()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,BI"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/Bi/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // O modulo esta contratado (claim presente); o que falta e a permissao de leitura do perfil.
        Assert.DoesNotContain("MODULO_NAO_CONTRATADO", location);
        Assert.Contains("module=BI", location);
        Assert.Contains("reason=PERMISSAO_NEGADA", location);
    }

    [Fact]
    public async Task Bi_ComModuloEPermissao_RenderizaPaginaBiExecutivo()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,BI"), ("permissions", "BI.*")));

        var resp = await client.GetAsync("/Bi/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("BI Executivo", html);
    }

    [Fact]
    public async Task DashboardsPremium_SemModuloContratado_NegaComModuloNaoContratado_NaoCatalogoInexistente()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/DashboardsPremium/AdministradorCliente");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // Mesmo defeito, segunda tela: agora o motivo e de contrato (DASHBOARD), nao de catalogo.
        Assert.DoesNotContain("CATALOGO_NAO_CONFIGURADO", location);
        Assert.Contains("module=DASHBOARD", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
    }

    [Fact]
    public async Task DashboardsPremium_ComModuloEPermissao_RenderizaPerfilAdministradorCliente()
    {
        _factory.ApiStub.Respond(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("/api/dashboards/admin-cliente"))
                return StubApiHandler.Json(HttpStatusCode.OK,
                    "{\"success\":true,\"data\":{\"kpis\":[],\"alertas\":[],\"proximosPassos\":[],\"atalhos\":[],\"series\":[],\"statusPlanoModulo\":\"Plano PRO - modulos validados\"}}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,DASHBOARD"), ("permissions", "DASHBOARD.*")));

        var resp = await client.GetAsync("/DashboardsPremium/AdministradorCliente");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Dashboard Administrador Cliente", html);
        // KPIs vazios -> estado vazio honesto (nenhum numero demonstrativo exibido).
        Assert.Contains("Sem indicadores reais", html);
    }

    [Theory]
    [InlineData("/Bi/Index")]
    [InlineData("/DashboardsPremium/AdministradorCliente")]
    public async Task GlobalAdmin_SemClaimsDeModuloOuPermissao_AceitaTela(string url)
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_GLOBAL")));

        var resp = await client.GetAsync(url);

        // Bypass canonico preservado: no defeito original do r11b2 o global admin caia em
        // CATALOGO_NAO_CONFIGURADO antes de qualquer checagem; agora nunca.
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
