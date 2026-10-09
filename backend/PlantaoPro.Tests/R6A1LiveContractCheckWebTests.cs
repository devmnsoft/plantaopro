using System.Net;
using Microsoft.AspNetCore.Hosting;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Fábrica Web com o check de contratação efetiva LIVE ATIVADO (Access:LiveEffectiveModuleCheck=true).
/// As demais coleções web-bff o mantêm desligado para preservar contratos determinísticos de guarda
/// que não modelam o endpoint canônico effective-modules.
/// </summary>
public sealed class PlantaoProWebFactoryLive : PlantaoProWebFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        // Ativa o check de contratação efetiva LIVE nesta fábrica (a base o desliga).
        builder.UseSetting("Access:LiveEffectiveModuleCheck", "true");
    }
}

[CollectionDefinition("web-bff-live")]
public sealed class WebBffLiveCollectionDefinition : ICollectionFixture<PlantaoProWebFactoryLive>
{
}

/// <summary>
/// R6-BlocoA item 1 — prova end-to-end (guard → resolver → api/auth/effective-modules) da
/// verificação LIVE da contratação efetiva no Web. O usuário ASSINA com ADM360 nas claims do login
/// (contratado no login); o endpoint canônico responde se o módulo AINDA é efetivo:
///   - revogado desde o login (data vazia)  → guard nega com MODULO_NAO_CONTRATADO (sem re-login);
///   - ainda contratado (data=["ADM360"])→ página liberada.
/// Comprova a exigência do spec de não depender somente de claims antigos para comandos críticos.
/// </summary>
[Collection("web-bff-live")]
public sealed class R6A1LiveContractCheckWebTests
{
    private readonly PlantaoProWebFactory _factory;

    public R6A1LiveContractCheckWebTests(PlantaoProWebFactoryLive factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private static string SignIn(string modules, string permissions) =>
        WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_CLIENTE"), ("modules", modules), ("permissions", permissions));

    private void StubEffectiveModules(string dataJson) => _factory.ApiStub.Respond(req =>
        req.RequestUri?.AbsolutePath == "/api/auth/effective-modules"
            ? StubApiHandler.Json(HttpStatusCode.OK,
                $"{{\"success\":true,\"message\":\"\",\"data\":[{dataJson}],\"errors\":null,\"statusCode\":200,\"timestamp\":\"2026-10-09T00:00:00Z\"}}")
            : StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));

    [Fact]
    public async Task RevogadoDesdeLogin_GuardNegaSemReloginComModuloNaoContratado()
    {
        _factory.ApiStub.Reset();
        StubEffectiveModules(""); // data vazia — módulo saiu do conjunto efetivo após o login

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SignIn("MEU_DIA,ADM360", "ADM360.*"));
        using var resp = await client.GetAsync("__test/mvcfiltro/get?valor=12%2C50");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var loc = resp.Headers.Location?.ToString() ?? string.Empty;
        Assert.StartsWith("/Account/AccessDenied", loc);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", loc);
        // E de fato consultou a fonte canônica (não caiu só nos claims do login).
        Assert.Contains(_factory.ApiStub.Requests, r => r.EndsWith("/api/auth/effective-modules", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AindaContratado_EndpointConfirma_ModuloLiberado()
    {
        _factory.ApiStub.Reset();
        StubEffectiveModules("\"ADM360\""); // data=["ADM360"] — ainda efetivo

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SignIn("MEU_DIA,ADM360", "ADM360.*"));
        using var resp = await client.GetAsync("__test/mvcfiltro/get?valor=12%2C50");
        var corpo = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("\"ok\":true", corpo, StringComparison.OrdinalIgnoreCase);
    }
}
