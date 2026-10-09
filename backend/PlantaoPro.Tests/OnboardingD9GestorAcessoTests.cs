using System.Net;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R5-D9 (homologacao da jornada operacional): a pagina /Onboarding e fluxo core de todo
/// tenant com assinatura canonica vigente - ONBOARDING nao e linha de modulos_sistema, entao
/// o gestor nunca teria o claim "ONBOARDING" e ficava preso em MODULO_NAO_CONTRATADO mesmo
/// em cliente perfeitamente contratado. ONBOARDING entrou nos modulos administrativos do
/// tenant (mesma familia de ASSINATURAS/CLIENTE_PORTAL): gestor passa; demais perfis seguem
/// negados por permissao (motivo PERMISSAO_NEGADA, nunca sucesso falso).
/// </summary>
[Collection("web-bff")]
public sealed class OnboardingD9GestorAcessoTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public OnboardingD9GestorAcessoTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    [Fact]
    public async Task Gestor_sem_claim_onboarding_acessa_checklist_materializado()
    {
        // Sessao espelhando o login real do gestor B5: catalog v2149, claims de modulo
        // apenas dos modulos contratados (PLANTOES) - sem "ONBOARDING" entre eles.
        _factory.ApiStub.Respond(req =>
        {
            var path = req.RequestUri!.AbsolutePath;
            if (path.EndsWith("api/onboarding/status"))
                return StubApiHandler.Json(HttpStatusCode.OK,
                    "{\"success\":true,\"data\":{\"tenantId\":\"22222222-2222-2222-2222-222222222222\",\"clienteId\":\"33333333-3333-3333-3333-333333333333\",\"status\":\"EM_ANDAMENTO\",\"progresso\":25,\"proximaAcao\":\"Convide o primeiro usuario\"}}");
            if (path.EndsWith("api/onboarding/checklist"))
                return StubApiHandler.Json(HttpStatusCode.OK,
                    "{\"success\":true,\"data\":[" +
                    "{\"id\":\"aaaaaaaa-0000-0000-0000-000000000001\",\"codigo\":\"ONB_EMPRESA_DADOS\",\"titulo\":\"Dados da empresa\",\"moduloCodigo\":\"\",\"status\":\"CONCLUIDA\",\"ordem\":1,\"obrigatorio\":true,\"atendida\":true}," +
                    "{\"id\":\"aaaaaaaa-0000-0000-0000-000000000002\",\"codigo\":\"ONB_PL_PRIMEIRO_PLANTAO\",\"titulo\":\"Publicar o primeiro plantao\",\"moduloCodigo\":\"PLANTOES\",\"moduloNome\":\"Plantoes\",\"status\":\"PENDENTE\",\"ordem\":5,\"obrigatorio\":true,\"atendida\":false}" +
                    "]}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR"), ("modules", "PLANTOES")));

        var resp = await client.GetAsync("/Onboarding/Index");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Dados da empresa", html);
        Assert.Contains("Publicar o primeiro plantao", html);
    }

    [Fact]
    public async Task Perfil_comum_sem_permissao_e_negado_por_permissao_nao_por_contrato()
    {
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory,
            WebBffTestKit.SigninQuery(("roles", "MEDICO"), ("modules", "MEU_DIA"), ("permissions", "MEU_DIA.*")));

        var resp = await client.GetAsync("/Onboarding/Index");

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        var location = Assert.IsType<Uri>(resp.Headers.Location).ToString();
        Assert.StartsWith("/Account/AccessDenied", location);
        // O modulo esta habilitado para o tenant (core); o que falta e permissao do perfil.
        Assert.DoesNotContain("MODULO_NAO_CONTRATADO", location);
        Assert.Contains("reason=PERMISSAO_NEGADA", location);
    }
}
