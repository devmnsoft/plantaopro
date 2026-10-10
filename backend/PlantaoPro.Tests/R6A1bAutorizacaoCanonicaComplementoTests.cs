using System.Net;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Tests.Infrastructure;
using PlantaoPro.Web.Services.Security;
using ApiUser = PlantaoPro.Api.ICurrentUserService;
using WebUser = PlantaoPro.Web.Services.Security.ICurrentUserService;
using WebPermissionService = PlantaoPro.Web.Services.Security.PermissionService;
using WebModuleAccessService = PlantaoPro.Web.Services.Security.ModuleAccessService;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R6-BlocoA item 1 (complemento R6-A1b) — comportamento de indisponibilidade por operação:
///
/// O guard dispara a verificação LIVE para QUALQUER página de módulo não-core (antes só
/// quando o módulo já estava nas claims do login). Consequências cobertas aqui:
///   1. live FALHOU + módulo NUNCA contratado no login → motivo honesto
///      VERIFICACAO_INDISPONIVEL (nunca "não contratado" sem poder consultar o contrato);
///   2. live FALHOU + módulo contratado no login → janela documentada degrada aos claims
///      (mesma fonte canônica emitida no login) e a página segue liberada;
///   3. live OK + conjunto sem o módulo → MODULO_NAO_CONTRATADO com prova (contraste).
/// A separação final de mensagens do spec: não contratado × permissão insuficiente ×
/// verificação temporariamente indisponível × cliente bloqueado (este último já coberto).
/// </summary>
[Collection("web-bff-live")]
public sealed class R6A1bVerificacaoIndisponivelWebTests
{
    private readonly PlantaoProWebFactoryLive _factory;

    public R6A1bVerificacaoIndisponivelWebTests(PlantaoProWebFactoryLive factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private static string SignIn(string modules, string permissions) =>
        WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_CLIENTE"), ("modules", modules), ("permissions", permissions));

    private void StubLiveDown() => _factory.ApiStub.Respond(req =>
        req.RequestUri?.AbsolutePath == "/api/auth/effective-modules"
            ? StubApiHandler.Json(HttpStatusCode.InternalServerError,
                "{\"success\":false,\"message\":\"erro interno\",\"data\":null,\"errors\":null,\"statusCode\":500,\"timestamp\":\"2026-10-10T00:00:00Z\"}")
            : StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));

    private void StubLiveOk(string dataJson) => _factory.ApiStub.Respond(req =>
        req.RequestUri?.AbsolutePath == "/api/auth/effective-modules"
            ? StubApiHandler.Json(HttpStatusCode.OK,
                $"{{\"success\":true,\"message\":\"\",\"data\":[{dataJson}],\"errors\":null,\"statusCode\":200,\"timestamp\":\"2026-10-10T00:00:00Z\"}}")
            : StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));

    [Fact]
    public async Task LiveDown_ModuloNuncaContratadoNoLogin_MotivoVerificacaoIndisponivelComRetornoHonesto()
    {
        _factory.ApiStub.Reset();
        StubLiveDown();

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SignIn("MEU_DIA", "MEU_DIA.*"));
        using var resp = await client.GetAsync("__test/mvcfiltro/get?valor=1%2C50");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var loc = resp.Headers.Location?.ToString() ?? string.Empty;
        Assert.StartsWith("/Account/AccessDenied", loc);
        Assert.Contains("reason=VERIFICACAO_INDISPONIVEL", loc);
        Assert.DoesNotContain("MODULO_NAO_CONTRATADO", loc);
        // "Tentar novamente" aponta para a página que o usuário tentou abrir (guard passa retorno).
        Assert.Contains("retorno=%2FMvcSeguroTest%2FGet", loc);
        // De fato tentou consultar a fonte canônica antes de concluir "indisponível".
        Assert.Contains(_factory.ApiStub.Requests, r => r.EndsWith("/api/auth/effective-modules", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task LiveDown_ClaimDoModuloNoLogin_JanelaDocumentadaDegradaAosClaimsELiberaPagina()
    {
        _factory.ApiStub.Reset();
        StubLiveDown();

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SignIn("MEU_DIA,ADM360", "ADM360.*"));
        using var resp = await client.GetAsync("__test/mvcfiltro/get?valor=12%2C50");
        var corpo = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("\"ok\":true", corpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LiveOk_ConjuntoVazio_SemClaimNoLogin_MotivoModuloNaoContratadoComProva()
    {
        _factory.ApiStub.Reset();
        StubLiveOk(""); // live respondeu: conjunto efetivo sem o módulo (prova de revogação/ausência)

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SignIn("MEU_DIA", "MEU_DIA.*"));
        using var resp = await client.GetAsync("__test/mvcfiltro/get?valor=1%2C50");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var loc = resp.Headers.Location?.ToString() ?? string.Empty;
        Assert.StartsWith("/Account/AccessDenied", loc);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", loc);
        Assert.DoesNotContain("VERIFICACAO_INDISPONIVEL", loc);
    }
}

/// <summary>
/// R6-A1b — suspensão/revogação DURANTE sessão ativa testada por chamada DIRETA à API
/// (spec do bloco A item 1). O Saude360ModuleFilter reavalia a função canônica
/// modulo_efetivo() por request contra o banco: com o contrato ativo a chamada passa;
/// desativando a linha do contrato (habilitado=false) a MESMA sessão — mesmo usuário,
/// mesmas claims congeladas no token — é negada no próximo request, sem re-login e sem
/// refresh de token. Comprova que a autorização não vive apenas nas claims antigas.
/// </summary>
public sealed class R6A1bRevogacaoSessaoAtivaApiTests
{
    private const string MsgModulo = "Módulo Saúde 360 não contratado para este cliente.";

    private sealed class FakeApiUser : ApiUser
    {
        public FakeApiUser(Guid? tenantId = null) => TenantId = tenantId;

        public Guid? UserId => Guid.Empty;
        public Guid? TenantId { get; }
        public Guid? ClienteId => TenantId;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => false;
        public bool IsTenantAdmin() => true;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => false;
    }

    private static IConfiguration BuildCfg(string cs) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = cs })
        .Build();

    private static AuthorizationFilterContext BuildCtx()
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("sub", Guid.NewGuid().ToString()),
        }, "TestAuth"));
        return new AuthorizationFilterContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());
    }

    private static async Task<(Guid ModuloId, Guid AssinaturaId)> SeedModuloS360Async(string cs, Guid tenant, bool habilitado, string status)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        var moduloId = Guid.NewGuid();
        var assinaturaId = Guid.NewGuid();
        // Código único por teste (ux por código); o gate lê tm.codigo_modulo (SAUDE360).
        await cn.ExecuteAsync(@"insert into plantaopro.modulos_sistema(id, codigo, nome, status, reg_status, reg_date)
values(@id, @codigo, @nome, 'ATIVO', 'A', now())", new { id = moduloId, codigo = "S360REV_" + tenant.ToString("N").Substring(0, 8), nome = "Saúde 360 (revogação " + status + ")" });
        await cn.ExecuteAsync(@"insert into plantaopro.tenant_modulos(id, tenant_id, cliente_id, modulo_id, codigo_modulo, codigo, nome, status, habilitado, origem, reg_status, reg_date)
values(@id, @tenant, @tenant, @moduloId, 'SAUDE360', 'SAUDE360', 'Saúde 360', @status, @habilitado, 'TESTE_R6A1B', 'A', now())",
            new { id = assinaturaId, tenant, moduloId, status, habilitado });
        return (moduloId, assinaturaId);
    }

    [Fact]
    public async Task FiltroApi_SuspensaoDuranteSessaoAtiva_NegaSemReloginEmChamadaDireta()
    {
        var cs = TestDatabase.ConnectionString;
        var tenant = Guid.NewGuid();
        var (moduloId, assinaturaId) = await SeedModuloS360Async(cs, tenant, habilitado: true, status: "ATIVO");

        try
        {
            var cfg = BuildCfg(cs);
            var filter = new Saude360ModuleFilter(new FakeApiUser(tenant), cfg);

            // 1) Contrato vigente: a chamada direta à API passa (autorização confirmada no banco).
            var ctx1 = BuildCtx();
            await filter.OnAuthorizationAsync(ctx1);
            Assert.Null(ctx1.Result);

            // 2) Suspensão DO MEIO DA SESSÃO: desativa-se a linha do contrato no banco.
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                var linhas = await cn.ExecuteAsync("update plantaopro.tenant_modulos set habilitado=false where id=@id", new { id = assinaturaId });
                Assert.Equal(1, linhas);
            }

            // 3) MESMA sessão (mesmo usuário, claims congeladas): o próximo request direto
            //    reavalia a função canônica e nega com a mensagem exata de contrato.
            var ctx2 = BuildCtx();
            await filter.OnAuthorizationAsync(ctx2);
            var obj = Assert.IsType<ObjectResult>(ctx2.Result);
            Assert.Equal(403, obj.StatusCode);
            var json = System.Text.Json.JsonSerializer.SerializeToElement(obj.Value!);
            Assert.False(json.GetProperty("success").GetBoolean());
            Assert.Equal(MsgModulo, json.GetProperty("message").GetString());
        }
        finally
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            await cn.ExecuteAsync("delete from plantaopro.tenant_modulos where id=@id", new { id = assinaturaId });
            await cn.ExecuteAsync("delete from plantaopro.modulos_sistema where id=@id", new { id = moduloId });
        }
    }
}

/// <summary>
/// R6-A1b — remoção dos fallbacks por papel pré-v2149 (adendo D.3 #9): a decisão é ÚNICA
/// e baseada nos mesmos claims que o login canônico emite (módulo efetivo + permissão por
/// ação). Sessões sem access_catalog_version (apenas possíveis fora dos emissores atuais,
/// pois login Data.cs, SessionClaimsBuilder e TestSigninController sempre a carregam)
/// deixam de cair em conjuntos papel→módulo hardcoded: decidem por claims, e papel sem
/// claims não autoriza módulo nem bypassa BI_AVANCADO.
/// </summary>
public sealed class R6A1bDecisaoUnicaPorClaimsTests
{
    private sealed class FakeWebUser : WebUser
    {
        private readonly bool _tenantAdmin;

        public FakeWebUser(ClaimsPrincipal user, bool tenantAdmin = false)
        {
            User = user;
            _tenantAdmin = tenantAdmin;
        }

        public ClaimsPrincipal User { get; }
        public Guid? UserId => Guid.Empty;
        public Guid? TenantId => Guid.NewGuid();
        public Guid? ClienteId => TenantId;
        public string UserName => "teste";
        public IReadOnlyCollection<string> Roles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        public bool IsAuthenticated() => User.Identity?.IsAuthenticated ?? false;
        public bool IsGlobalAdmin() => false;
        public bool IsTenantAdmin() => _tenantAdmin;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => Roles().Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SessaoSemCatalogo_PoremComClaimsDeModuloEPermissao_DecidePorClaims()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("module", "PACIENTES"),
            new Claim("permissions", "PACIENTES.VER"),
        }, "TestAuth"));

        var perm = new WebPermissionService(new FakeWebUser(user));
        var mods = new WebModuleAccessService(perm, new FakeWebUser(user), new HttpContextAccessor());

        Assert.True(perm.HasPermission("Pacientes", "VER"),
            "claim de módulo + permissão emitidas no login devem autorizar independentemente do catálogo da sessão.");
        Assert.True(mods.IsModuleEnabled("Pacientes"),
            "sem contexto http a decisão degrada aos claims (mesma fonte canônica emitida no login).");
    }

    [Fact]
    public void PapelSemClaims_NaoAutorizaModuloClinico_NemBypassPorAusenciaDeCatalogo()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "TRIAGEM"),
        }, "TestAuth"));

        var perm = new WebPermissionService(new FakeWebUser(user));
        var mods = new WebModuleAccessService(perm, new FakeWebUser(user), new HttpContextAccessor());

        Assert.False(mods.IsModuleEnabled("SAUDE360_TRIAGEM"),
            "papel isolado não autoriza módulo: os conjuntos papel->módulo pré-v2149 foram removidos.");
        Assert.False(perm.HasPermission("SAUDE360_TRIAGEM", "VER"),
            "permissão exige módulo efetivo (contratação) + grant por ação (perfil) — distinção canônica do spec.");
        Assert.False(mods.IsModuleEnabled("BI_AVANCADO"),
            "o antigo bypass pré-v2149 ('tudo liberado menos BI_AVANCADO') não existe mais.");
    }
}
