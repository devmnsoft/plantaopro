using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Série WP-A1 (rodada 2): landing do médico e guarda de módulos em páginas.
/// - Médico sem MEDICO_AREA contratado deve cair no Meu Dia (módulo core) após o login,
///   nunca numa AccessDenied logo ao autenticar (item 11 do backlog da rodada 1).
/// - Páginas continuam com o contrato de redirect de página (302 + motivo), que é o
///   comportamento esperado para navegadores.
/// </summary>
[Collection("web-bff")]
public sealed class WpA1LandingMedicoTests : IDisposable
{
    internal const string PayloadTemplate = @"
        {
          ""success"": true,
          ""message"": """",
          ""data"": {
            ""token"": ""jwt-login-teste"",
            ""expiresAt"": ""2026-10-05T12:00:00Z"",
            ""usuarioId"": ""11111111-1111-1111-1111-111111111111"",
            ""nome"": ""Medico Teste"",
            ""email"": ""medico@clinica.teste"",
            ""roles"": [""MEDICO""],
            ""tenantId"": ""22222222-2222-2222-2222-222222222222"",
            ""tenantNome"": ""Clinica Teste"",
            ""modules"": __MODULES__,
            ""permissions"": [""MEU_DIA.*""]
          },
          ""errors"": null,
          ""statusCode"": 200,
          ""timestamp"": ""2026-10-04T12:00:00Z""
        }
        ";

    private readonly PlantaoProWebFactory _factory;

    public WpA1LandingMedicoTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    [Fact]
    public async Task Login_medico_sem_medico_area_contratado_landing_em_meu_dia()
    {
        var resp = await FazerLoginAsync("[\"MEU_DIA\",\"AGENDA\"]");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.Equal("/MeuDia", resp.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Login_medico_com_medico_area_contratado_landing_em_area_do_medico()
    {
        var resp = await FazerLoginAsync("[\"MEU_DIA\",\"AGENDA\",\"MEDICO_AREA\"]");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.Equal("/MedicoArea/Index", resp.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Pagina_modulo_nao_contratado_redireciona_para_access_denied_com_motivo()
    {
        _factory.ApiStub.Reset();
        var client = await WebBffTestKit.ClienteAutenticadoAsync(
            _factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/MedicoArea/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        Assert.Contains("reason=MODULO_NAO_CONTRATADO", location);
    }

    [Fact]
    public async Task Pagina_sem_permissao_da_acao_redireciona_para_access_denied_com_motivo()
    {
        _factory.ApiStub.Reset();
        var client = await WebBffTestKit.ClienteAutenticadoAsync(
            _factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO"), ("modules", "MEU_DIA,AGENDA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/Agenda");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        Assert.Contains("reason=PERMISSAO_NEGADA", location);
    }

    private async Task<HttpResponseMessage> FazerLoginAsync(string modulesJson)
    {
        _factory.ApiStub.Respond(req =>
            req.RequestUri?.AbsolutePath == "/api/auth/login"
                ? StubApiHandler.Json(HttpStatusCode.OK, PayloadTemplate.Replace("__MODULES__", modulesJson))
                : StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}"));

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Antiforgery no MESMO client: o token raspado da página e o cookie antiforgery
        // precisam pertencer ao mesmo contexto anônimo que fará o POST do login.
        var pagina = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, pagina.StatusCode);
        var html = await pagina.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Token antiforgery nao encontrado na pagina de login.");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "medico@clinica.teste",
            ["senha"] = "senha-valida-123"
        });
        form.Headers.TryAddWithoutValidation("RequestVerificationToken", match.Groups[1].Value);
        return await client.PostAsync("/Account/Login", form);
    }
}
