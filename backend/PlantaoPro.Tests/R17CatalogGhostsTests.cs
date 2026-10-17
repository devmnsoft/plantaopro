using System.Net;
using PlantaoPro.Tests.Infrastructure;
using PlantaoPro.Web.Services.Security;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R17-B1 (topo da fila r16z, herdado de r13a0 §1.3/D4): três códigos do dicionário do guard
/// eram fantasmas do catalogo — ausentes de modulos_sistema, das camadas comerciais e de TODAS
/// as camadas de grants, enquanto modulos_efetivos() faz inner join com o catalogo nas quatro
/// ramas (evidencia r17a0 F1/F2): nenhum usuario nao-global jamais recebia claim de modulo para
/// esses codigos e caia em MODULO_NAO_CONTRATADO para sempre. Decisoes versionadas (r17a0):
/// D1 CentralEscala -> remapeio para o dono canonico do dominio de cobertura, ESCALAS (mesmo
/// padrao R13-D1/R5-E13; suite completa + grants dormentes nos perfis admin); D2 MINHA_AGENDA
/// -> modulo comum/core em codigo (workspace proprio do medico exposto no Meu Dia a todo papel
/// MEDICO; audiencia segue restrita por [Authorize(Roles=MEDICO)]; sem DDL/grants, paridade
/// Web+Api); D3 MEDICO_AREA -> contratado-somente por desenho (sem gêmeo, paginas placeholder
/// Fase2, landing protegido pelo fallback resiliente do contrato wpa1). Sem DDL;
/// access_catalog_version permanece v2149.
/// </summary>
public sealed class R17CatalogGhostsFonteTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));
    private static int Occurrences(string text, string token) =>
        text.Split(new[] { token }, StringSplitOptions.None).Length - 1;

    [Fact]
    public void Guard_ResolveCentralEscalaParaDonoCanonicoDoDominioDeCobertura_EPreservaAsDemaisEntradas()
    {
        // Fonte canonica da decisao D1: o dicionario em si (nao texto).
        Assert.True(SaasRouteGuardFilter.TryResolveModule("CentralEscala", out var escala));
        Assert.Equal("ESCALAS", escala);

        // D2/D3: as entradas permanecem (o fix foi nos SETS comuns e na documentacao, nao no mapa).
        Assert.True(SaasRouteGuardFilter.TryResolveModule("MinhaAgenda", out var minhaAgenda));
        Assert.Equal("MINHA_AGENDA", minhaAgenda);
        Assert.True(SaasRouteGuardFilter.TryResolveModule("MedicoArea", out var medicoArea));
        Assert.Equal("MEDICO_AREA", medicoArea);
    }

    [Fact]
    public void GuardFonte_CodigoFantasmaCentralEscalaExtintoDoDicionario()
    {
        var guard = Read("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs");

        Assert.Contains("[\"CentralEscala\"] = \"ESCALAS\",", guard);
        // Anti-regressao do formato R13 (BILLING): o literal do codigo morto nao pode voltar.
        Assert.DoesNotContain("\"CENTRAL_ESCALA\"", guard);
    }

    [Fact]
    public void Sidebar_CoberturaSegueOModuloCanonicoESemElementoMortoNaSecaoOperacional()
    {
        var sidebar = Read("backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml");

        Assert.DoesNotContain("Can(\"CENTRAL_ESCALA\")", sidebar);
        // O link de Cobertura continua apontando para o controller (agora porteador do gate ESCALAS).
        Assert.Contains("asp-controller=\"CentralEscala\"", sidebar);
        // Array canOperate: o elemento morto foi extinto; ESCALAS ja o cobre.
        Assert.DoesNotContain("\"CENTRAL_ESCALA\", ", sidebar);
    }

    [Fact]
    public void SetsComunsECore_WebEApi_IncluemMinhaAgendaComComentarioDeCasa()
    {
        var web = Read("backend/PlantaoPro.Web/Services/Security/AccessServices.cs");
        var apiAccess = Read("backend/PlantaoPro.Api/SecurityAccessServices.cs");
        var apiAdmin = Read("backend/PlantaoPro.Api/SecurityAdministrationServices.cs");

        // Web: CommonModules (PermissionService) E CoreModules (ModuleAccessService) — os dois
        // gates do guard (permissao + contratacao) precisam reconhecer o modulo comum.
        Assert.Equal(2, Occurrences(web, "\"MINHA_AGENDA\""));
        Assert.Contains("R17 (r17a0 D2)", web);

        // Paridade Api (os mesmos sets vivem nos servicos de seguranca da API).
        Assert.Contains("\"MINHA_AGENDA\"", apiAccess);
        Assert.Contains("\"MINHA_AGENDA\"", apiAdmin);
    }

    [Fact]
    public void GuardFonte_EntradasMedicoAreaEMinhaAgendaDocumentamASuasDecisoesD2ED3()
    {
        var guard = Read("backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs");

        Assert.Contains("r17a0 D3", guard);
        Assert.Contains("r17a0 D2", guard);
    }
}

/// <summary>
/// R17-B1 (comportamento, estilo R12/R13): colecao web-bff com live-check desligado (decisao
/// deterministica pelos claims da sessao). Contratos: (i) medico SEM modulo contratado ou
/// permissao renderiza MinhaAgenda em 200 — comum por desenho D2, audiencia restrita ao papel
/// MEDICO pela authorization attribute; (ii) MEDICO_AREA segue negado honestamente para quem nao
/// contratou (D3 — o fallback wpa1 de landing permanece o caminho do dia a dia); (iii)
/// CentralEscala nega NOMEANDO o modulo real do catalogo (ESCALAS, nao o fantasma extinto) e
/// libera 200 para sessao com ESCALAS + leitura; (iv) bypass global-admin preservado.
/// </summary>
[Collection("web-bff")]
public sealed class R17CatalogGhostsAcessoWebTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public R17CatalogGhostsAcessoWebTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private static void StubRotasMedicoECobertura(PlantaoProWebFactory factory) => factory.ApiStub.Respond(req =>
    {
        var path = req.RequestUri!.AbsolutePath;
        if (path == "/api/medico-area/meu-dia")
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":null}");
        if (path.StartsWith("/api/medico-area/meus-pagamentos", StringComparison.Ordinal))
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":{\"items\":[],\"total\":0,\"page\":1,\"pageSize\":20}}");
        if (path == "/api/central-escala/resumo")
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":null}");
        return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":null}");
    });

    [Fact]
    public async Task MinhaAgenda_MedicoSemModuloContratadoOuPermissao_Renderiza200_ComumPorDesenho()
    {
        StubRotasMedicoECobertura(_factory);

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO")));

        var resp = await client.GetAsync("/MinhaAgenda/Index");

        // Antes do fix: 302 AccessDenied reason=MODULO_NAO_CONTRATADO (os cards do Meu Dia eram
        // links mortos para todo medico). Agora o workspace proprio do medico e comum.
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task MinhaAgenda_Medico_PaginaDePagamentosTambemLiberadaPeloMesmoModuloComum()
    {
        StubRotasMedicoECobertura(_factory);

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO")));

        var resp = await client.GetAsync("/MinhaAgenda/MeusPagamentos");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Meus demonstrativos", html);
    }

    [Fact]
    public async Task MinhaAgenda_NaoMedico_GateDePapelContinuaASegundaLinhaDeDefesa()
    {
        StubRotasMedicoECobertura(_factory);

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/MinhaAgenda/Index");

        // O conjunto comum nao alarga a audiencia: [Authorize(Roles=MEDICO)] precede o guard e a
        // denegacao de papel vai para o AccessDeniedPath configurado (Program.cs).
        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        // O AccessDeniedPath do cookie handler emite URL absoluta (diferente do guard, relativo).
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString().Replace("http://localhost", "");
        Assert.StartsWith("/Account/AccessDenied", location);
    }

    [Fact]
    public async Task MedicoArea_MedicoSemContrato_NegaHonestamente_MantemADecisaoD3()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/MedicoArea/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // Contratado-somente por desenho: a denegacao e honesta e nomeia o modulo real.
        Assert.Contains("module=MEDICO_AREA", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
    }

    [Fact]
    public async Task CentralEscala_SemContrato_NegaNomeandoOModuloCanonicoDoCatalogoENaoOFantasma()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/CentralEscala/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // O defeito era a denegacao permanente nomeando o codigo ausente do catalogo.
        Assert.Contains("module=ESCALAS", location);
        Assert.DoesNotContain("CENTRAL_ESCALA", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
    }

    [Fact]
    public async Task CentralEscala_ModuloContratadoSemPermissaoDeLeitura_PermissoaNegadaNaoSucessoFalso()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,ESCALAS"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/CentralEscala/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        Assert.DoesNotContain("MODULO_NAO_CONTRATADO", location);
        Assert.Contains("module=ESCALAS", location);
        Assert.Contains("reason=PERMISSAO_NEGADA", location);
    }

    [Fact]
    public async Task CentralEscala_ModuloEPermissaoDeLeitura_Renderiza200()
    {
        StubRotasMedicoECobertura(_factory);

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "MEU_DIA,ESCALAS"), ("permissions", "ESCALAS.*")));

        var resp = await client.GetAsync("/CentralEscala/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task GlobalAdmin_BypassPreservado_CentralEscalaRenderiza200SemClaimsDeModulo()
    {
        // MinhaAgenda nao entra aqui de proposito: [Authorize(Roles=MEDICO)] precede qualquer
        // bypass — o gate de papel segue autoridade de audiencia ate para o global admin.
        StubRotasMedicoECobertura(_factory);

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_GLOBAL")));

        var resp = await client.GetAsync("/CentralEscala/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
