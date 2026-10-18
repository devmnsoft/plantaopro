using System.Net;
using PlantaoPro.Tests.Infrastructure;
using PlantaoPro.Web.Services.Security;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R18-B1 (topo da fila r17z §5): r17a0 mediu 3 controllers órfãos do dicionário do guard (F7);
/// a auditoria ao vivo do r18a0 ampliou o alcance real para 9 — incluindo infraestrutura do login
/// (Minha Central = botão HOME do sidebar + rota de fuga das telas de erro; centro de notificações;
/// Ajuda = ícone do topbar + botão "Acionar suporte" da própria tela AccessDenied) e a administração
/// global SaaS (SaasDashboard/Modulos, fluxo de contratos) — todos reproduzidos em
/// CATALOGO_NAO_CONFIGURADO para SV tenant-admin E superadmin (r18a0 F1/F2). Decisões versionadas:
/// D1 workspace comum (MINHA_CENTRAL/NOTIFICACOES entram nos sets + AJUDA só precisava da entrada);
/// D2 SAAS para SaasDashboard/Modulos (global-admin passa pelo bypass; tenant-admin recebe
/// MODULO_NAO_CONTRATADO honesto); D3 donos canônicos do domínio operacional medidos no catálogo
/// (FECHAMENTO/COBERTURA/OPERACAO360 — sem DDL, fantasma FECHAMENTOS do sidebar corrigido, override
/// de escrita Fechamentos/Acao→CONFIRMAR); D4 COORDENACAO pin contratado-somente (desenho R17-D3).
/// </summary>
public sealed class R18GuardOrphansFonteTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));
    private static int Occurrences(string text, string token) =>
        text.Split(new[] { token }, StringSplitOptions.None).Length - 1;

    [Fact]
    public void Guard_TodosONoveOrfaosResolvemParaOsDonosMedidosNoCatalogo()
    {
        // Fonte canonica da decisao: o dicionario em si (nao texto). Antes do fix qualquer um
        // destes resolvia CATALOGO_NAO_CONFIGURADO ate para o global admin (reproducao ao vivo r18a0).
        Assert.True(SaasRouteGuardFilter.TryResolveModule("MinhaCentral", out var minhaCentral));
        Assert.Equal("MINHA_CENTRAL", minhaCentral);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("Notificacoes", out var notificacoes));
        Assert.Equal("NOTIFICACOES", notificacoes);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("Ajuda", out var ajuda));
        Assert.Equal("AJUDA", ajuda);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("SaasDashboard", out var saasDashboard));
        Assert.Equal("SAAS", saasDashboard);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("Modulos", out var modulos));
        Assert.Equal("SAAS", modulos);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("Fechamentos", out var fechamentos));
        Assert.Equal("FECHAMENTO", fechamentos);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("Cobertura", out var cobertura));
        Assert.Equal("COBERTURA", cobertura);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("CommandCenter", out var commandCenter));
        Assert.Equal("OPERACAO360", commandCenter);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("Coordenacao", out var coordenacao));
        Assert.Equal("COORDENACAO", coordenacao);
    }

    [Fact]
    public void GuardFonte_OOverrideDaEscritaDeFechamentoExisteEFantasmaDoPluralExtintoDoSidebar()
    {
        var guard = Read("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs");
        var sidebar = Read("backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml");

        // POST /fechamentos/{id}/acao concentra as escritas (aprovar/devolver/rejeitar/gerar
        // financeiro): sem o override o mapeamento generico resolveria tudo como VER (padrao R5-A2).
        Assert.Contains("[\"Fechamentos/Acao\"] = \"CONFIRMAR\",", guard);

        // Anti-regressao no formato CENTRAL_ESCALA/BILLING (R17/R13): o literal do codigo morto
        // (plural, ausente de modulos_sistema) nao pode voltar como entrada ativa.
        Assert.DoesNotContain("\"FECHAMENTOS\"", guard);
        Assert.DoesNotContain("\"FECHAMENTOS\"", sidebar);
        Assert.Contains("Can(\"FECHAMENTO\")", sidebar);
        Assert.Contains("\"FECHAMENTO\"", sidebar); // array canOperate
    }

    [Fact]
    public void SetsComunsECore_WebEApi_IncluemNotificacoesEMinhaCentralComComentarioDeCasa()
    {
        var web = Read("backend/PlantaoPro.Web/Services/Security/AccessServices.cs");
        var apiAccess = Read("backend/PlantaoPro.Api/SecurityAccessServices.cs");
        var apiAdmin = Read("backend/PlantaoPro.Api/SecurityAdministrationServices.cs");

        // Web: CommonModules (PermissionService) E CoreModules (ModuleAccessService) — os dois
        // gates do guard (permissao + contratacao) precisam reconhecer o modulo comum.
        Assert.Equal(2, Occurrences(web, "\"NOTIFICACOES\""));
        Assert.Equal(2, Occurrences(web, "\"MINHA_CENTRAL\""));
        Assert.Contains("R18 (r18a0 D1)", web);

        // Paridade Api (os mesmos sets vivem nos servicos de seguranca da API — padrao R17-D2).
        Assert.Contains("\"NOTIFICACOES\"", apiAccess);
        Assert.Contains("\"MINHA_CENTRAL\"", apiAccess);
        Assert.Contains("\"NOTIFICACOES\"", apiAdmin);
        Assert.Contains("\"MINHA_CENTRAL\"", apiAdmin);
    }

    [Fact]
    public void CentralEscala_RotaNuaRestauradaPorAttributeRouteNoEstiloDaCasa()
    {
        var controller = Read("backend/PlantaoPro.Web/Controllers/CentralEscalaController.cs");

        Assert.Contains("[Route(\"CentralEscala\")]", controller);
        Assert.Contains("[HttpGet(\"\")]", controller); // rota nua
        Assert.Contains("[HttpGet(\"Index\")]", controller); // URL legado preservada
        Assert.Contains("IndexLegacy", controller);
    }

    [Fact]
    public void GuardFonte_ComentariosDocumentamASDecisoessDeCasa()
    {
        var guard = Read("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs");

        Assert.Contains("R18 (r18a0 D1)", guard);
        Assert.Contains("R18 (r18a0 D2)", guard);
        Assert.Contains("R18 (r18a0 D3)", guard);
        Assert.Contains("R18 (r18a0 D4)", guard);
    }
}

/// <summary>
/// R18-B1 (comportamento, estilo R17): colecao web-bff com live-check desligado (decisao
/// deterministica pelos claims da sessao). Contratos: (i) Ajuda/Notificacoes/MinhaCentral renderizam
/// 200 para medico SEM nenhum modulo/permissao — infraestrutura do login comum por desenho D1
/// (antes do fix: CATALOGO_NAO_CONFIGURADO, ou seja, link morto para TODO papel autenticado);
/// (ii) Coordenacao/Fechamentos/CommandCenter/SaasDashboard/Modulos negam NOMEANDO o modulo real do
/// catalogo com MODULO_NAO_CONTRATADO — nunca mais CATALOGO_NAO_CONFIGURADO (a razao que indicava
/// bug de configuracao em vez de estado canônico de contrato); (iii) bypass global-admin preservado.
/// </summary>
[Collection("web-bff")]
public sealed class R18GuardOrphansAcessoWebTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public R18GuardOrphansAcessoWebTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private async Task<HttpClient> MedicoSemNenhumClaimAsync() =>
        await WebBffTestKit.ClienteAutenticadoAsync(_factory, WebBffTestKit.SigninQuery(("roles", "MEDICO")));

    private async Task<HttpClient> AdminTenantSemContratosOperacionaisAsync() =>
        await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

    [Fact]
    public async Task Ajuda_MedicoSemModulosNemPermissoes_Renderiza200_InfraestruturaDoLogin()
    {
        var client = await MedicoSemNenhumClaimAsync();

        var resp = await client.GetAsync("/Ajuda/Index");

        // Antes do fix: 302 AccessDenied reason=CATALOGO_NAO_CONFIGURADO — o icone do topbar e o
        // botao "Acionar suporte" da propria tela de recusa eram links mortos. AJUDA ja constava nos
        // sets comuns (r18a0 F4): faltava apenas a entrada no dicionario.
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Notificacoes_MedicoSemModulosNemPermissoes_Renderiza200()
    {
        var client = await MedicoSemNenhumClaimAsync();

        var resp = await client.GetAsync("/Notificacoes/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Central de Notificações", html);
    }

    [Fact]
    public async Task MinhaCentral_MedicoSemModulosContratados_Renderiza200_BotaoHomeDoAppERotaDeFuga()
    {
        _factory.ApiStub.Respond(req =>
            StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":null}"));

        var client = await MedicoSemNenhumClaimAsync();

        var resp = await client.GetAsync("/MinhaCentral");

        // Antes do fix: 302 AccessDenied reason=CATALOGO_NAO_CONFIGURADO — inclusive como destino
        // dos botoes "Ir para Minha Central" das telas NotFound/Error (rota de fuga universal).
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Coordenacao_SemContrato_NegaHonestamenteNomeandoOModuloENaoOCatalogoMalConfigurado()
    {
        var client = await AdminTenantSemContratosOperacionaisAsync();

        var resp = await client.GetAsync("/Coordenacao/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // Pin contratado-somente (D4): a denegacao nomeia o modulo e usa a razao canonica.
        Assert.Contains("module=COORDENACAO", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
        Assert.DoesNotContain("CATALOGO_NAO_CONFIGURADO", location);
    }

    [Fact]
    public async Task Fechamentos_SemContrato_NegaNomeandoOModuloCanonicoSingular()
    {
        var client = await AdminTenantSemContratosOperacionaisAsync();

        var resp = await client.GetAsync("/fechamentos");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        Assert.Contains("module=FECHAMENTO", location);
        Assert.DoesNotContain("FECHAMENTOS", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
    }

    [Fact]
    public async Task CommandCenter_SemContrato_NomeiaOperacao360UnicoModuloOperacionalComSuite()
    {
        var client = await AdminTenantSemContratosOperacionaisAsync();

        var resp = await client.GetAsync("/CommandCenter");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        Assert.Contains("module=OPERACAO360", location);
        Assert.DoesNotContain("CATALOGO_NAO_CONFIGURADO", location);
    }

    [Fact]
    public async Task SaasDashboardEModulos_AdminTenantSemContratoSaaS_NegaHonestamenteOMesmoModulo()
    {
        var client = await AdminTenantSemContratosOperacionaisAsync();

        var respDashboard = await client.GetAsync("/SaasDashboard/Index");
        Assert.Equal(HttpStatusCode.Found, respDashboard.StatusCode);
        var locationDashboard = Assert.IsType<Uri>(respDashboard.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", locationDashboard);
        Assert.Contains("module=SAAS", locationDashboard);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", locationDashboard);

        var respModulos = await client.GetAsync("/Modulos/Index");
        Assert.Equal(HttpStatusCode.Found, respModulos.StatusCode);
        var locationModulos = Assert.IsType<Uri>(respModulos.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", locationModulos);
        Assert.Contains("module=SAAS", locationModulos);
    }

    [Fact]
    public async Task GlobalAdmin_BypassPreservado_AjudaRenderiza200SemClaimsDeModulo()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_GLOBAL")));

        var resp = await client.GetAsync("/Ajuda/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
