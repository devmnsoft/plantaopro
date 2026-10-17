using System.Collections;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
// .NET 10: a feature de sessao passou a ser ISessionFeature (Microsoft.AspNetCore.Http.Features).
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
// .NET 10: o provider de TempData saiu do namespace Microsoft.AspNetCore.Mvc para ViewFeatures.
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PlantaoPro.Tests.Infrastructure;
using PlantaoPro.Web.Controllers;
using PlantaoPro.Web.Models;
using Xunit;

namespace PlantaoPro.Tests;

// ============================================================================
// R16b1 (topo da fila R16, P2 F-R14-C): uniformizar a exposicao de erro dos parsers
// Web. O caminho 2xx do parser devolvia Error = TryParseMessage(content), i.e. a
// `message` do envelope (padrao "Sucesso" de ApiResponse.Ok) — qualquer tela que
// lesse .Error em 2xx anunciava sucesso como falha (reproduzido ao vivo na R14 no
// banner de Faturamento SaaS; superficie atingivel mapeada no r16a0 F3/F4: Minha
// Assinatura, Minha Agenda, Saude360, Central Atendimento, Convites Equipe,
// Onboarding, Conferencia Execucao). Fix em um ponto (D1/r16a0): semantica canonica
// Error != null <=> a chamada NAO deu certo; precedido pelo ReadApiPagedResponseAsync,
// que ja devolvia null em 2xx. A simetria dos catchs (D3) e a regra dos 500
// (F-R14-D, D4) ficam preservadas e cobertas aqui como contrato.
// ============================================================================

// Sem host e sem colecao: cada fato monta um StubApiHandler proprio — nada de estado
// compartilhado entre testes (o parser e exercitado direto, por instancia minima).
public sealed class R16WebParserErrorContratoTests
{
    [Fact]
    public async Task GET_2xx_envelope_sucesso_error_nulo_e_dados_deserializados()
    {
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.OK,
            "{\"success\":true,\"message\":\"Sucesso\",\"data\":{\"Nome\":\"Paciente X\"}}"));

        var (data, error, status) = await ctrl.ReadApiResponse<ItemDto>(ctrl.CreateApiClient(), "api/pacientes");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Null(error);
        Assert.NotNull(data);
        Assert.Equal("Paciente X", data.Nome);
    }

    [Fact]
    public async Task GET_2xx_mensagem_custom_significativa_tambem_eh_descartada_do_error()
    {
        // Nao so o literal "Sucesso": toda mensagem 2xx do envelope e chapa da API
        // (ex.: "Check-in registrado." no ProfessionalPortalService) — descarte uniforme.
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.OK,
            "{\"success\":true,\"message\":\"Check-in registrado.\",\"data\":{\"Nome\":\"Y\"}}"));

        var (data, error, status) = await ctrl.ReadApiResponse<ItemDto>(ctrl.CreateApiClient(), "api/presencas");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Null(error);
        Assert.Equal("Y", data!.Nome);
    }

    [Fact]
    public async Task GET_2xx_array_puro_error_nulo_regressao_caminho_adm360()
    {
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.OK,
            "[{\"Nome\":\"A\"},{\"Nome\":\"B\"}]"));

        var (data, error, status) = await ctrl.ReadApiResponse<IEnumerable<ItemDto>>(ctrl.CreateApiClient(), "api/lista");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Null(error);
        Assert.Equal(2, data!.Count());
    }

    [Fact]
    public async Task GET_2xx_objeto_puro_com_title_nao_vaza_para_error()
    {
        // Bonus do achado: TryParseMessage tambem olhava "title" — DTO sem envelope
        // com propriedade title deixava Error preenchido em 2xx antes do fix.
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.OK,
            "{\"id\":\"abc\",\"title\":\"Pacientes\"}"));

        var (data, error, status) = await ctrl.ReadApiResponse<JsonElement>(ctrl.CreateApiClient(), "api/titulo");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Null(error);
        Assert.Equal("Pacientes", data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GET_2xx_envelope_sem_sucesso_preserva_mensagem_do_envelope()
    {
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.OK,
            "{\"success\":false,\"message\":\"Cotação expirada.\",\"data\":null}"));

        var (data, error, status) = await ctrl.ReadApiResponse<ItemDto>(ctrl.CreateApiClient(), "api/cotacao");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Cotação expirada.", error);
        Assert.Null(data);
    }

    [Fact]
    public async Task GET_404_repassa_mensagem_da_api()
    {
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.NotFound,
            "{\"message\":\"Registro indisponível.\"}"));

        var (_, error, status) = await ctrl.ReadApiResponse<ItemDto>(ctrl.CreateApiClient(), "api/ausente");

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal("Registro indisponível.", error);
    }

    [Fact]
    public async Task GET_500_mantem_literal_fixo_e_descarta_mensagem_interna()
    {
        // D4/r16a0: BuildApiErrorMessage(500) segue descartando a apiMessage — a
        // intencao dos 500 e decisao separada (fila P3, F-R14-D, premissa refinada).
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.InternalServerError,
            "{\"message\":\"excecao tecnica interna\"}"));

        var (_, error, status) = await ctrl.ReadApiResponse<ItemDto>(ctrl.CreateApiClient(), "api/instavel");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal("Ocorreu uma instabilidade interna. Tente novamente em instantes.", error);
    }

    [Fact]
    public async Task POST_2xx_sucesso_error_nulo_escrita_passa_pelo_mesmo_parser()
    {
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.Created,
            "{\"success\":true,\"message\":\"Sucesso\",\"data\":{\"Nome\":\"Z\"}}"));

        var (data, error, status) = await ctrl.SendApiAsync<object, ItemDto>(ctrl.CreateApiClient(), HttpMethod.Post, "api/salvar", new { });

        Assert.Equal(HttpStatusCode.Created, status);
        Assert.Null(error);
        Assert.Equal("Z", data!.Nome);
    }

    [Fact]
    public async Task POST_falha_de_comunicacao_escreve_tempdata_e_retorna_erro_de_comunicacao()
    {
        // D3/r16a0: o catch do parser de escrita segue escrevendo TempData — existem
        // consumidores que descartam a tupla (var (x, _, status)) e dependem disso.
        var (ctrl, _, tempData) = Sonda(_ => throw new HttpRequestException("api fora do ar"));

        var (_, error, status) = await ctrl.SendApiAsync<object, ItemDto>(ctrl.CreateApiClient(), HttpMethod.Post, "api/salvar", new { });

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal("Falha de comunicação com a API.", error);
        Assert.Equal("Não foi possível concluir a operação agora. Tente novamente.", tempData.Peek("Error"));
    }

    [Fact]
    public async Task GET_falha_de_comunicacao_retorna_erro_sem_escrever_tempdata()
    {
        // Assimetria documentada (D3): o parser de leitura nao escreve TempData; o
        // consumidor decide a apresentacao pelo Error retornado.
        var (ctrl, _, tempData) = Sonda(_ => throw new HttpRequestException("api fora do ar"));

        var (_, error, status) = await ctrl.ReadApiResponse<ItemDto>(ctrl.CreateApiClient(), "api/lista");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal("Falha de comunicação com a API.", error);
        Assert.Null(tempData.Peek("Error"));
    }

    [Fact]
    public async Task RenderList_2xx_info_message_nula_e_lista_preenchida()
    {
        // D2/r16a0: apos D1 o Error em 2xx e sempre null; o helper compartilhado das
        // listas fica com InfoMessage literal null (antes repassava a mensagem do
        // envelope; nenhum layout de lista renderiza InfoMessage — campo morto).
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.OK,
            "{\"success\":true,\"message\":\"SENTINELA_R16_INFO\",\"data\":[{\"Nome\":\"Especialidade Alfa\"}]}"));

        var resultado = await ctrl.RenderList<ItemDto>("api/especialidades");
        var view = Assert.IsType<ViewResult>(resultado);
        var model = Assert.IsType<ListPageViewModel<ItemDto>>(view.Model);

        Assert.Null(model.InfoMessage);
        Assert.Null(model.ErrorMessage);
        var items = Assert.IsAssignableFrom<IEnumerable<ItemDto>>(model.Items);
        Assert.Single(items);
        Assert.Equal("Especialidade Alfa", items.First().Nome);
    }

    [Fact]
    public async Task GET_2xx_contas_financeiras_ativo_da_api_popula_vm_regressao_f9d()
    {
        // r16b2/F9d: a API serializa "ativo" (ContaFinanceiraDto.Ativo). O parametro
        // antigo do ViewModel ("Ativa") nunca mapeava — STJ case-insensitive nao ignora
        // grafia — e toda conta chegava inativa, zerando os 5 consumers .Where(c => c.Ativa):
        // Pagar Titulo, Receber Titulo, Fechar Caixa, Fluxo de Caixa e totais de Contas.
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.OK,
            "[{\"id\":\"550e8400-e29b-41d4-a716-446655440000\",\"nome\":\"Cofre Smoke\",\"tipo\":\"BANCO\"," +
            "\"banco\":null,\"agencia\":null,\"conta\":null,\"saldoInicial\":500.0," +
            "\"dataSaldoInicial\":\"2026-10-17\",\"saldoAtual\":500.0,\"ativo\":true}]"));

        var (data, error, status) = await ctrl.ReadApiResponse<List<ContaFinanceiraViewModel>>(ctrl.CreateApiClient(), "api/administrativo360/caixa/contas");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Null(error);
        var conta = Assert.Single(data!);
        Assert.True(conta.Ativo);
        Assert.Equal("Cofre Smoke", conta.Nome);
        Assert.Equal(500m, conta.SaldoAtual);
    }

    // ------------------------------------------------------------------
    // Harness: instancia minima de BaseWebController sem host — o parser e [NonAction]
    // publico e usa apenas Logger/User/TempData/HttpClientFactory. No .NET 10 o
    // HttpContext.Session lanca se a feature ausente (SessionFeature substituiu a
    // IHttpSessionFeature) e o TempData migrou para ITempDataDictionaryFactory — o
    // contexto de sonda provê os dois com implementacoes em memoria para afirmar a
    // assimetria D3 sem subir a aplicacao.
    // ------------------------------------------------------------------

    private static (SondaController Ctrl, HttpClient Cli, DicioTempData TempData) Sonda(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var stub = new StubApiHandler();
        stub.Respond(responder);
        var client = new HttpClient(stub) { BaseAddress = new Uri("http://api.test.local/") };
        var tempData = new DicioTempData();
        var services = new ServiceCollection()
            .AddSingleton<ITempDataDictionaryFactory>(new DicioTempDataFactory(tempData))
            .BuildServiceProvider();
        var http = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("jwt", "test-jwt") }))
        };
        // .NET 10: HttpContext.Session lanca se a ISessionFeature (feature da requisicao) ausente.
        http.Features.Set<ISessionFeature>(new SessaoFeatureSonda());
        var ctrl = new SondaController(client, http);
        return (ctrl, client, tempData);
    }

    private sealed record ItemDto(string Nome);

    private sealed class SondaController : BaseWebController
    {
        public SondaController(HttpClient client, HttpContext ctx)
            : base(new ClientFactoryFixo(client), NullLoggerFactory.Instance.CreateLogger<SondaController>())
        {
            ControllerContext = new ControllerContext { HttpContext = ctx };
        }
    }

    private sealed class ClientFactoryFixo : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public ClientFactoryFixo(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class DicioTempDataFactory : ITempDataDictionaryFactory
    {
        private readonly DicioTempData _dict;
        public DicioTempDataFactory(DicioTempData dict) => _dict = dict;
        public ITempDataDictionary GetTempData(HttpContext httpContext) => _dict;
    }

    // ISession minima em memoria (forma do .NET 10): so o que GetString/SetString tocam.
    private sealed class SessaoFeatureSonda : ISessionFeature
    {
        public ISession Session { get; set; } = new SessaoMemoria();
    }
    private sealed class SessaoMemoria : ISession
    {
        private readonly Dictionary<string, byte[]> _dados = new(StringComparer.Ordinal);
        public bool IsAvailable => true;
        public string Id => "r16-sonda";
        public IEnumerable<string> Keys => _dados.Keys;
        public Task LoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value)
        {
            if (_dados.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }
            value = null;
            return false;
        }
        public void Set(string key, byte[] value) => _dados[key] = value;
        public void Remove(string key) => _dados.Remove(key);
        public void Clear() => _dados.Clear();
    }

    private sealed class DicioTempData : ITempDataDictionary
    {
        public Dictionary<string, object?> Dados { get; } = new(StringComparer.Ordinal);
        public void Load() { }
        public void Save() { }
        public void Keep() { }
        public void Keep(string key) { }
        public object? Peek(string key) => Dados.TryGetValue(key, out var v) ? v : null;
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Dados.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Dados.GetEnumerator();
        public void Add(KeyValuePair<string, object?> item) => Dados.Add(item.Key, item.Value);
        public void Clear() => Dados.Clear();
        public bool Contains(KeyValuePair<string, object?> item) => Dados.ContainsKey(item.Key);
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => throw new NotSupportedException();
        public int Count => Dados.Count;
        public bool IsReadOnly => false;
        public bool Remove(KeyValuePair<string, object?> item) => Dados.Remove(item.Key);
        public bool ContainsKey(string key) => Dados.ContainsKey(key);
        public object? this[string key]
        {
            get => Dados[key];
            set => Dados[key] = value;
        }
        public ICollection<string> Keys => Dados.Keys;
        public ICollection<object?> Values => Dados.Values;
        public bool Remove(string key) => Dados.Remove(key);
        public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out object? value)
            => Dados.TryGetValue(key, out value);
        public void Add(string key, object? value) => Dados.Add(key, value);
    }
}

// ============================================================================
// R16b1 (adesao F-R15-A do item 1 da fila): contrato permanente de binding. Na R15 o
// typo `efetivoFisico` x param `efeitoFisico` causou falha silenciosa de 302 porque
// parametros escalar NAO-NULAVEIS sem source attribute ganham [Required] implicito
// (NRT) e o ModelStateInvalidoFiltro redireciona antes da acao — e a view nao renderiza
// TempData. Este source-scan trava a invariante: todo parametro escalar nao-nulavel
// sem source explicito de uma acao [HttpPost] da familia Administrativo360 deve existir
// como campo (name="...") ou rota (asp-route-...) DO FORM que aponta para ela
// (asp-action). Baseline r16a0/r16b1: 0 violacoes em 54 formacoes action×form
// (141 parametros) apos os 4 renames de campo encontrados pela varredura inicial
// (valorPago, contaFinanceiraId, fechamentoId, id do form PagarTitulo).
// ============================================================================

public sealed class R16ScanFormBindingFonteTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static readonly string ScalarRegex = @"^(string|Guid|int|long|double|float|decimal|bool|DateOnly|DateTime)$";

    // Actions invocadas por construcao dinamica de URL em JS (formImp.action =
    // '/Administrativo360/AtivarCargo' nos modais Inativar* — mesmo form, mesmos
    // parametros) ou ainda sem ponto de chamada estatico (candidatos ao catalogo de
    // fantasmas, itens 2/8 da fila R17). Nomes novos aqui falham o teste de proposito.
    private static readonly HashSet<string> AcoesSemFormEstatico = new()
    {
        "OrcamentoRejeitar", "OrcamentoCancelar", "OrcamentoCancelarReserva",
        "ValeCancelar", "AtivarDepartamento", "AtivarCargo"
    };

    [Fact]
    public void acoes_post_adm360_parametros_escalares_implicitos_existem_no_form_que_as_aponta()
    {
        var acoes = ExtrairAcoesPostComEscalaresImplicitos();
        Assert.NotEmpty(acoes);

        var violacoes = new List<string>();
        var pareadas = 0;
        var parametrosVerificados = 0;
        var mapeadas = new HashSet<string>();

        foreach (var (viewFile, viewText) in LerViews())
        {
            foreach (System.Text.RegularExpressions.Match formTag in System.Text.RegularExpressions.Regex.Matches(viewText, "<form\\b[^>]*>"))
            {
                var acao = System.Text.RegularExpressions.Regex.Match(formTag.Value, "asp-action\\s*=\\s*\"([A-Z][A-Za-z0-9_]*)\"").Groups[1].Value;
                if (!acoes.TryGetValue(acao, out var escalares)) continue;
                mapeadas.Add(acao);
                pareadas++;
                parametrosVerificados += escalares.Count;

                var fim = viewText.IndexOf("</form>", formTag.Index + formTag.Length, StringComparison.Ordinal);
                var corte = fim < 0 ? viewText.Length : fim;
                var slice = viewText[(formTag.Index + formTag.Length)..corte];
                var campos = System.Text.RegularExpressions.Regex.Matches(slice, "name\\s*=\\s*\"([A-Za-z0-9_.]+)\"")
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var rotas = System.Text.RegularExpressions.Regex.Matches(formTag.Value, "asp-route-([A-Za-z0-9_]+)")
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var p in escalares.Where(p => !campos.Contains(p) && !rotas.Contains(p)))
                    violacoes.Add($"{viewFile}::{acao} => falta campo/rota '{p}'");
            }
        }

        // Antivacuidade: o scan precisa estar olhando a superfice real (baseline 54 pares / 141 parametros).
        Assert.True(pareadas >= 40, $"Scan suspeito: apenas {pareadas} formacoes action×form (baseline 54).");
        Assert.True(parametrosVerificados >= 120, $"Scan suspeito: apenas {parametrosVerificados} parametros (baseline 141).");
        Assert.Empty(violacoes);

        var semForm = acoes.Keys.Except(mapeadas)
            .Except(AcoesSemFormEstatico)
            .OrderBy(x => x)
            .ToList();
        Assert.Empty(semForm);
    }

    private static Dictionary<string, List<string>> ExtrairAcoesPostComEscalaresImplicitos()
    {
        var controllersDir = Path.Combine(Root, "backend/PlantaoPro.Web/Controllers");
        var resultado = new Dictionary<string, List<string>>();
        var padrao = new System.Text.RegularExpressions.Regex(
            "\\[HttpPost[^\\]]*\\]\\s*(?:\\[[^\\]]*\\]\\s*)*(?:public|protected)\\s+(?:async\\s+)?[\\w<>,\\[\\]?]+\\s+([A-Z]\\w+)\\s*\\(");

        foreach (var arquivo in Directory.GetFiles(controllersDir, "Administrativo360Controller*.cs"))
        {
            var texto = File.ReadAllText(arquivo);
            foreach (System.Text.RegularExpressions.Match m in padrao.Matches(texto))
            {
                var abre = m.Index + m.Length - 1;
                var prof = 0; var fecha = -1;
                for (var i = abre; i < texto.Length; i++)
                {
                    if (texto[i] == '(') prof++;
                    else if (texto[i] == ')') { prof--; if (prof == 0) { fecha = i; break; } }
                }
                if (fecha < 0) continue;

                var escalares = new List<string>();
                foreach (var bruto in DividirTopoLevel(texto[(abre + 1)..fecha]))
                {
                    var p = bruto.Trim();
                    if (p.Length == 0 || p.Contains("CancellationToken")) continue;

                    var rest = p;
                    while (rest.StartsWith('['))
                    {
                        var d = 0; var end = -1;
                        for (var i = 0; i < rest.Length; i++)
                        {
                            if (rest[i] == '[') d++;
                            else if (rest[i] == ']') { d--; if (d == 0) { end = i; break; } }
                        }
                        if (end < 0) break;
                        rest = rest[(end + 1)..];
                    }
                    rest = rest.Trim();

                    // Com default (= ...) o binder tem fallback; complexos bindam por propriedade.
                    if (rest.Contains('=')) continue;
                    var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length != 2) continue;

                    var tipo = tokens[0];
                    var ehArray = tipo.EndsWith("[]", StringComparison.Ordinal);
                    var baseType = ehArray ? tipo[..^2] : tipo;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(baseType, ScalarRegex)) continue;

                    escalares.Add(tokens[1]);
                }
                if (escalares.Count > 0) resultado[m.Groups[1].Value] = escalares;
            }
        }
        return resultado;
    }

    private static IEnumerable<(string NomeArquivo, string Texto)> LerViews()
    {
        var viewsDir = Path.Combine(Root, "backend/PlantaoPro.Web/Views/Administrativo360");
        foreach (var arquivo in Directory.GetFiles(viewsDir, "*.cshtml", SearchOption.AllDirectories))
            yield return (Path.GetFileName(arquivo), File.ReadAllText(arquivo));
    }

    private static IEnumerable<string> DividirTopoLevel(string s)
    {
        var d = 0; var cur = new System.Text.StringBuilder();
        foreach (var ch in s)
        {
            if (ch is '(' or '[' or '{') d++;
            else if (ch is ')' or ']' or '}') d--;
            if (ch == ',' && d == 0) { yield return cur.ToString(); cur.Clear(); }
            else cur.Append(ch);
        }
        if (cur.Length > 0) yield return cur.ToString();
    }
}
