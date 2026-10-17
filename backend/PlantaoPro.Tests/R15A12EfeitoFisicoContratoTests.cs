using System.Net;
using System.Text;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

// ============================================================================
// R15b1 (topo da fila R15, P1b herdada R11→R14): baixa/devolução dirigida por destino.
// Antes, o RESOLVER inverte todo o bloqueio e o destino do encerrar era texto livre sem
// efeito físico: encerrar com "descarte" devolvia tudo a LIBERADO. Agora o efeito é
// canônico (DEVOLUCAO|BAIXA) e aplicado no ENCERRAR: DEVOLUCAO espelha a liberação
// histórica; BAIXA consome fisicamente (perna única BAIXA_OCORRENCIA) — a única exceção
// autorizada à soma física nula do ciclo. Sem DDL: o efeito é inferível dos movimentos
// (origem_tipo='OCORRENCIA').
// ============================================================================

/// <summary>Source-scan: strings canônicas do novo idioma (padrão da casa, como na R14).</summary>
public sealed class R15A12EfeitoFisicoScanFonteTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void WriterDeOcorrencias_BaixaComPernaUnicaTipoCanonicalEEspelhoCompartilhado()
    {
        var src = Read("backend/PlantaoPro.Infrastructure/Administrativo360/OcorrenciasRepository.cs");

        Assert.Contains("'BAIXA_OCORRENCIA'", src);
        Assert.Contains(":BXA:", src); // chave idempotência da perna única de consumo
        Assert.Contains("LerBlocosRegistradosAsync", src); // espelho compartilhado liberação/baixa
    }

    [Fact]
    public void ContratoEVista_ProgramamOEfeitoFisicoDoEncerrar()
    {
        var contratos = Read("backend/PlantaoPro.Application/Administrativo360/Contracts.cs");
        var view = Read("backend/PlantaoPro.Web/Views/Administrativo360/Ocorrencias.cshtml");
        var controller = Read("backend/PlantaoPro.Web/Controllers/Administrativo360Controller.ComprasRecebimentos.cs");

        Assert.Contains("string? EfeitoFisico = null", contratos);
        Assert.Contains("name=\"efeitoFisico\"", view);
        Assert.Contains("value=\"DEVOLUCAO\"", view);
        Assert.Contains("value=\"BAIXA\"", view);
        Assert.Contains("EfeitoFisico =", controller);
    }
}

/// <summary>
/// web-bff (stub determinístico, padrão R12/R14): a página renderiza o seletor de efeito
/// físico e o POST do encerrar programa EfeitoFisico no payload enviado à API. Colecao
/// web-bff: live-check desligado, decisão pelos claims da sessão (determinístico).
/// </summary>
[Collection("web-bff")]
public sealed class R15A12EfeitoFisicoWebBffTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public R15A12EfeitoFisicoWebBffTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private static string SigninAdminCliente() =>
        WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_CLIENTE"), ("modules", "ADM360"), ("permissions", "ADM360.*"), ("claim", "tenant_id=d3f6584c-2c64-4e5a-9ea9-4e1428647502"));

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string token, string url, Dictionary<string, string> campos)
    {
        var todos = new Dictionary<string, string>(campos) { ["__RequestVerificationToken"] = token };
        using var form = new FormUrlEncodedContent(todos);
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        req.Headers.TryAddWithoutValidation("Referer", "http://localhost/Administrativo360/Ocorrencias");
        return await client.SendAsync(req);
    }

    [Fact]
    public async Task PaginaOcorrencias_RenderizaOSeletorDeEfeitoFisicoComAsDuasOpcoes()
    {
        // O seletor vive no colapso da linha RESOLVIDA: a lista precisa trazer uma ocorrencia
        // nesse estado para o form de encerrar existir na pagina.
        _factory.ApiStub.Respond(req =>
        {
            if (req.RequestUri!.AbsolutePath.EndsWith("/ocorrencias"))
                return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":[{\"id\":\"4d96cc88-1111-1111-1111-111111111111\",\"produto\":\"Xilotox Sirope 20ml\",\"lote\":\"L-2026-001\",\"local\":\"Depo A\",\"tipo\":\"QUEBRA\",\"descricao\":\"Amostra com lacre rompido\",\"quantidade\":2,\"situacao\":\"RESOLVIDA\",\"responsavelId\":null,\"prazo\":null,\"destino\":null,\"justificativaEncerramento\":null,\"versao\":3,\"criadoEm\":\"2026-10-17T08:00:00Z\"}]}");
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"success\":true,\"data\":[]}");
        });

        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SigninAdminCliente());
        var resp = await client.GetAsync("/Administrativo360/Ocorrencias");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("name=\"efeitoFisico\"", html);
        Assert.Contains("value=\"DEVOLUCAO\" selected", html); // default mantém o comportamento histórico
        Assert.Contains("value=\"BAIXA\"", html);
    }

    [Fact]
    public async Task EncerrarOcorrencia_ProgramaOEfeitoFisicoNoPayloadParaA_Api()
    {
        var corpo = new StringBuilder();
        _factory.ApiStub.Respond(req =>
        {
            if (req.Content != null)
                corpo.AppendLine(req.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            if (req.RequestUri!.AbsolutePath.EndsWith("/encerrar"))
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            return StubApiHandler.Json(HttpStatusCode.OK, "{\"ok\":true}");
        });

        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAdminCliente());
        var resp = await PostFormAsync(client, token, "Administrativo360/EncerrarOcorrencia", new Dictionary<string, string>
        {
            ["id"] = "4d96cc88-1111-1111-1111-111111111111",
            ["versao"] = "3",
            ["destino"] = "Descarte assistido",
            ["justificativa"] = "Quebra confirmada no descarte",
            ["efeitoFisico"] = "BAIXA"
        });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode); // 302 -> Ocorrencias (sucesso)
        Assert.Contains("Ocorrencias", resp.Headers.Location?.OriginalString ?? string.Empty);
        Assert.Contains("POST http://api.test.local/api/administrativo360/ocorrencias/4d96cc88-1111-1111-1111-111111111111/encerrar", _factory.ApiStub.Requests);

        var enviado = corpo.ToString();
        Assert.Contains("EfeitoFisico", enviado, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"BAIXA\"", enviado);
    }
}
