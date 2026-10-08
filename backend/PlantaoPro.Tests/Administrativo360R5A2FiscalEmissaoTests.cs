using System.Net;
using System.Text;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

// ============================================================================
// R5-A2 (fiscal honesto): ENVIANDO reservado ao conector real + BFF fino.
// Parte 1 (unidade, sem DB): ValidarTransmissorDisponivel e catalogo vazio.
// Parte 2 (web-bff, stub deterministico): autorizacao POR ACAO no guard
// (AUDITOR barrado no POST antes da API; leitura liberada) e mapeamento honesto
// do Emitir/Cancelar via BFF (sem sucesso ficticio).
// ============================================================================

/// <summary>Transmissor falso para os testes de catalogo (nunca transmite de verdade).</summary>
internal sealed class FakeTransmissorFiscal : IFiscalTransmissor
{
    public string Provedor => "OPMENEXO";

    public Task<ResultadoEmissaoFiscal> TransmitirAsync(NotaPreEmitidaDetalhesDto nota, ParametrosFiscaisDto parametros, CancellationToken ct = default) =>
        Task.FromResult(new ResultadoEmissaoFiscal(true, NotaPreEmitidaSituacoes.Autorizada, "ok", new string('1', 44), DateTime.UtcNow));
}

// Mesma colecao das demais classes que escrevem status_transmissao='ENVIANDO':
// serializa entre si (aqui so ha regras puras, sem DB, mas a colecao ja existe).
[Collection("A360Transmissao")]
public sealed class Administrativo360R5A2FiscalTransmissaoTests
{
    [Fact]
    public void TransmissorRegistrado_NaoLanca()
    {
        var ex = Record.Exception(() => NotaPreEmitidaRegras.ValidarTransmissorDisponivel("OPMENEXO", true));
        Assert.Null(ex);
    }

    [Fact]
    public void TransmissorAusente_LancaBusinessExceptionComMensagemP1()
    {
        var ex = Assert.Throws<Administrativo360BusinessException>(
            () => NotaPreEmitidaRegras.ValidarTransmissorDisponivel("OPMENEXO", false));
        Assert.Contains("Emissão indisponível neste ambiente", ex.Message);
        Assert.Contains("OPMENEXO", ex.Message);
        Assert.Contains("P1", ex.Message);
    }

    [Fact]
    public void ProvedorNulo_LancaBusinessExceptionSemSucessoFalso()
    {
        var ex = Assert.Throws<Administrativo360BusinessException>(
            () => NotaPreEmitidaRegras.ValidarTransmissorDisponivel(null, false));
        Assert.Contains("não definido", ex.Message);
    }

    [Fact]
    public void CatalogoVazio_NaoTemTransmissor_EObterLancaKeyNotFound()
    {
        var catalogo = new FiscalTransmissorCatalogo();
        Assert.False(catalogo.TemPara("OPMENEXO"));
        Assert.False(catalogo.TemPara(null));
        Assert.Empty(catalogo.Provedores);
        Assert.Throws<KeyNotFoundException>(() => catalogo.Obter("OPMENEXO"));
    }

    [Fact]
    public void CatalogoComFake_ResolveCaseInsensitive()
    {
        var catalogo = new FiscalTransmissorCatalogo(new IFiscalTransmissor[] { new FakeTransmissorFiscal() });
        Assert.True(catalogo.TemPara("opmenexo"));
        Assert.True(catalogo.TemPara("OPMENEXO"));
        Assert.False(catalogo.TemPara("SEFAZ_DIRETO"));
        Assert.IsType<FakeTransmissorFiscal>(catalogo.Obter("OpMenexo"));
        Assert.Equal(new[] { "OPMENEXO" }, catalogo.Provedores);
    }
}

[Collection("web-bff")]
public sealed class Administrativo360R5A2FiscalBffTests : IDisposable
{
    private readonly PlantaoProWebFactory _factory;

    public Administrativo360R5A2FiscalBffTests(PlantaoProWebFactory factory) => _factory = factory;

    public void Dispose() => _factory.ApiStub.Reset();

    private static string SigninAuditor() =>
        WebBffTestKit.SigninQuery(("roles", "AUDITOR"), ("modules", "ADM360"), ("permissions", "ADM360.VER"), ("claim", "tenant_id=d3f6584c-2c64-4e5a-9ea9-4e1428647502"));

    private static string SigninAdminCliente() =>
        WebBffTestKit.SigninQuery(("roles", "ADMINISTRADOR_CLIENTE"), ("modules", "ADM360"), ("permissions", "ADM360.*"), ("claim", "tenant_id=d3f6584c-2c64-4e5a-9ea9-4e1428647502"));

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string token, string url, Dictionary<string, string> campos)
    {
        var todos = new Dictionary<string, string>(campos) { ["__RequestVerificationToken"] = token };
        using var form = new FormUrlEncodedContent(todos);
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        req.Headers.TryAddWithoutValidation("Referer", "http://localhost/Administrativo360/Fiscal/Notas");
        return await client.SendAsync(req);
    }

    private static string Envelope(object data) =>
        "{\"success\":true,\"data\":" + data + "}";

    private void ResponderFiscalPadrao(StringBuilder? corpos = null)
    {
        var notaId = "4d96cc88-1111-1111-1111-111111111111";
        _factory.ApiStub.Respond(req =>
        {
            if (req.Content != null && corpos != null)
                corpos.AppendLine(req.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var uri = req.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("/emitir", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, Envelope("{\"avancou\":false,\"situacaoAtual\":\"PRONTA_PARA_EMISSAO\",\"mensagem\":\"Emissao indisponivel neste ambiente: conector 'OPMENEXO' nao integrado (P1).\"}"));
            if (uri.Contains("/parametros", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, Envelope("{\"parametros\":{\"id\":\"" + notaId + "\",\"tenantId\":\"" + notaId + "\",\"uf\":\"SP\",\"municipio\":\"Sao Paulo\",\"regimeFiscal\":\"SIMPLES_NACIONAL\",\"operacaoFiscal\":\"VENDA\",\"cfops\":{\"VENDA\":\"5101\"},\"responsavelId\":null,\"responsavelNome\":\"Gestor\",\"ambiente\":\"HOMOLOGACAO\",\"provedor\":\"OPMENEXO\",\"certificadoReferencia\":\"nfe-demo\",\"status\":\"CONFIGURADO\",\"observacao\":null,\"criadoEm\":\"2026-10-07T00:00:00Z\",\"atualizadoEm\":null},\"credencialDisponivelNoAmbiente\":true,\"transmissorRegistradoNoAmbiente\":false}"));
            if (uri.Contains("/notas/", StringComparison.OrdinalIgnoreCase))
                return StubApiHandler.Json(HttpStatusCode.OK, Envelope("{\"resumo\":{\"id\":\"" + notaId + "\",\"numero\":\"NPE-00000001\",\"origemTipo\":\"MANUAL\",\"origemId\":null,\"situacao\":\"PRONTA_PARA_EMISSAO\",\"destinatarioNome\":\"Cliente Teste\",\"destinatarioDocumento\":null,\"valorTotal\":100.0,\"chaveAcessoExterna\":null,\"emitidaEm\":null,\"criadoEm\":\"2026-10-07T00:00:00Z\"},\"itens\":[],\"rejeicaoMensagem\":null,\"canceladaEm\":null,\"motivoCancelamento\":null}"));
            return StubApiHandler.Json(HttpStatusCode.OK, Envelope("[]"));
        });
    }

    [Fact]
    public async Task AUDITOR_post_emitir_barrado_no_guard_sem_chamar_api()
    {
        _factory.ApiStub.Reset();
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAuditor());

        var resp = await PostFormAsync(client, token, "Administrativo360/Fiscal/Emitir",
            new Dictionary<string, string> { ["id"] = Guid.NewGuid().ToString() });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("AccessDenied", resp.Headers.Location?.OriginalString ?? string.Empty);
        Assert.Equal(0, _factory.ApiStub.RequestCount);
    }

    [Fact]
    public async Task AUDITOR_get_notas_liberado_e_consulta_api()
    {
        _factory.ApiStub.Reset();
        ResponderFiscalPadrao();
        var client = await WebBffTestKit.ClienteAutenticadoAsync(_factory, SigninAuditor());

        var resp = await client.GetAsync("Administrativo360/Fiscal/Notas");
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Contains("GET http://api.test.local/api/administrativo360/fiscal/notas", _factory.ApiStub.Requests);
        Assert.DoesNotContain("AccessDenied", body);
    }

    [Fact]
    public async Task ADMIN_post_emitir_sem_conector_mostra_motivo_honesto_sem_envio()
    {
        _factory.ApiStub.Reset();
        ResponderFiscalPadrao();
        var notaId = "4d96cc88-1111-1111-1111-111111111111";
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAdminCliente());

        var resp = await PostFormAsync(client, token, "Administrativo360/Fiscal/Emitir",
            new Dictionary<string, string> { ["id"] = notaId });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("Detalhes", resp.Headers.Location?.OriginalString ?? string.Empty);
        Assert.Contains("POST http://api.test.local/api/administrativo360/fiscal/notas/" + notaId + "/emitir", _factory.ApiStub.Requests);

        var detalhes = await client.GetAsync("Administrativo360/Fiscal/Detalhes/" + notaId);
        var html = await detalhes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, detalhes.StatusCode);
        Assert.Contains("nao integrado", html);
        Assert.Contains("INDISPON", html);
        Assert.DoesNotContain("Pré-nota transmitida", html);
    }

    [Fact]
    public async Task ADMIN_post_cancelar_envia_motivo_e_confirma_cancelamento()
    {
        _factory.ApiStub.Reset();
        var corpos = new StringBuilder();
        ResponderFiscalPadrao(corpos);
        var notaId = "4d96cc88-1111-1111-1111-111111111111";
        var (client, token) = await WebBffTestKit.ClienteComAntiforgeryAsync(_factory, SigninAdminCliente());

        var resp = await PostFormAsync(client, token, "Administrativo360/Fiscal/CancelarNota",
            new Dictionary<string, string> { ["id"] = notaId, ["motivoCancelamento"] = "Teste de cancelamento interno r5a2" });

        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("POST http://api.test.local/api/administrativo360/fiscal/notas/" + notaId + "/cancelar", _factory.ApiStub.Requests);
        Assert.Contains("Teste de cancelamento interno r5a2", corpos.ToString(), StringComparison.OrdinalIgnoreCase);

        var detalhes = await client.GetAsync("Administrativo360/Fiscal/Detalhes/" + notaId);
        var html = await detalhes.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, detalhes.StatusCode);
        Assert.Contains("nota cancelada", html);
    }
}
