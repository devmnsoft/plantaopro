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
    public async Task GET_500_repassa_mensagem_da_api_com_atribuicao()
    {
        // D-R24-1 (F-R14-D, decidido em r24a0): o 500 da chamada simples passa a seguir o mesmo
        // criterio dos demais ramos (repasse da mensagem da API), alinhado ao comportamento do
        // parser paginado aprovado ao vivo na r14b2. A assimetria historica (literal fixo) acaba.
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.InternalServerError,
            "{\"message\":\"excecao tecnica interna\"}"));

        var (_, error, status) = await ctrl.ReadApiResponse<ItemDto>(ctrl.CreateApiClient(), "api/instavel");

        Assert.Equal(HttpStatusCode.InternalServerError, status);
        Assert.Equal("excecao tecnica interna", error);
    }

    [Fact]
    public async Task GET_500_sem_mensagem_no_corpo_mantem_literal_de_instabilidade()
    {
        // Fallback honesto (D-R24-1): JSON sem nenhuma chave reconhecida (message/mensagem/
        // error/detail/title/errors) nao produz mensagem — o literal fixo da casa permanece.
        var (ctrl, _, _) = Sonda(_ => StubApiHandler.Json(HttpStatusCode.InternalServerError,
            "{\"code\":500}"));

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

    // Actions sem <form> estatico apontando para elas. Desde R21 (r21a0) o catalogo esta
    // dividido em dois sets versionados, cada um auto-verificado pelo fato permanente
    // whitelist_acoes_sem_form_estatico_e_auto_verificada — nome novo deve ser classificado
    // (JS-driven x fantasma) com evidencia file:line. A uniao segue consumida pelo scan principal.
    //
    // JS_DRIVEN — invocadas por construcao dinamica de URL na view; os parametros trafegam
    // pelo MESMO form HTML da acao estatica pareada (mesma classe de risco).
    private static readonly HashSet<string> AcoesJsDrivenSemFormEstatico = new()
    {
        // Cargos.cshtml L256: formImp.action = inativar ? '.../InativarCargo' : '.../AtivarCargo'
        "AtivarCargo",
        // Departamentos.cshtml L231: formImp.action = inativar ? '.../InativarDepartamento' : '.../AtivarDepartamento'
        "AtivarDepartamento",
        // Cotacoes/Detalhes.cshtml L572: painel IA POST via fetch (data-ai-url
        // /Administrativo360/Cotacoes/{id}/AnaliseIa, R20) — nao existe <form>.
        "AnaliseIa",
        // _AnexosBloco.cshtml (R72): remocao via fetch — literal da rota
        // ('/Administrativo360/Anexos/Remover') montado no proprio bloco JS do partial.
        "Remover"
    };

    // FANTASMA — zero pontos de chamada no Web (nem <form>, nem literal de URL) com a cadeia
    // BFF->API completa: gap funcional documentado (r21a0 F-R21-B). Vazio por design apos R22
    // (r22b1+b2 ligaram as 4 entradas da R21 nos detalhes/planos). Fantasma novo entra aqui
    // classificado (r21a0); enquanto cadastrado, o fato auto-verificador exige zero ocorrencias
    // case-sensitive em Views/** (botao ligado => falha e a saida daqui e obrigatoria).
    private static readonly HashSet<string> AcoesFantasmaSemChamador = new()
    {
    };

    private static readonly HashSet<string> AcoesSemFormEstatico =
        AcoesJsDrivenSemFormEstatico.Concat(AcoesFantasmaSemChamador).ToHashSet();

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
                var acaoAsp = System.Text.RegularExpressions.Regex.Match(formTag.Value, "asp-action\\s*=\\s*\"([A-Z][A-Za-z0-9_]*)\"").Groups[1].Value;
                // R20 (extensao F-R15-A): os Adm360*WebController* usam forms com URL estatica
                // (action="/Administrativo360/<Area>/<rota>") em vez de asp-action; casa o path
                // contra os templates de rota conhecidos ({param:tipo} aceita qualquer valor).
                var queryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var acao = string.IsNullOrEmpty(acaoAsp)
                    ? ResolverAcaoPorUrlEstatica(formTag.Value, acoes, queryKeys)
                    : acaoAsp;
                if (!acoes.TryGetValue(acao, out var entrada)) continue;
                mapeadas.Add(acao);
                pareadas++;
                parametrosVerificados += entrada.Escalares.Count;

                var fim = viewText.IndexOf("</form>", formTag.Index + formTag.Length, StringComparison.Ordinal);
                var corte = fim < 0 ? viewText.Length : fim;
                var slice = viewText[(formTag.Index + formTag.Length)..corte];
                var campos = System.Text.RegularExpressions.Regex.Matches(slice, "name\\s*=\\s*\"([A-Za-z0-9_.]+)\"")
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var rotas = System.Text.RegularExpressions.Regex.Matches(formTag.Value, "asp-route-([A-Za-z0-9_]+)")
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Parametros que entram por slot {param} da URL ja estao garantidos pelo match
                // de template acima (todos os segmentos presentes); o resto exige campo,
                // asp-route ou chave de query na URL do form.
                var slots = SlotsDeTemplate(entrada.Template ?? acao);

                foreach (var p in entrada.Escalares.Where(p => !campos.Contains(p) && !rotas.Contains(p) && !queryKeys.Contains(p) && !slots.Contains(p)))
                    violacoes.Add($"{viewFile}::{acao} => falta campo/rota '{p}'");
            }
        }

        // Antivacuidade: o scan precisa estar olhando a superficie real. Baseline r16 (12 files
        // Administrativo360Controller*): 54 pareamentos / 141 parametros. Extensao R20 aos 4
        // Adm360*WebController* (medido em r20b1): 79 / 203 / 76 acoes. r22b1 ligou as 3 acoes
        // fantasma diretas (Rejeitar/Cancelar orcamento, Cancelar vale) nas views de detalhe:
        // 82 / 209 / 76. r22b2 ligou OrcamentoCancelarReserva no plano de reserva (reservas
        // ativas agora expostas individualmente pela API): 83 / 212 / 76. Os pisos travam
        // regressao de scan (ex.: glob que volta a ignorar os *WebController* ou forms que
        // desaparecem dos modais).
        Assert.True(pareadas >= 75, $"Scan suspeito: apenas {pareadas} formacoes action×form (superficie R22b2: 83).");
        Assert.True(parametrosVerificados >= 190, $"Scan suspeito: apenas {parametrosVerificados} parametros (superficie R22b2: 212).");
        Assert.True(acoes.Count >= 70, $"Scan suspeito: apenas {acoes.Count} acoes com escalares implicitos (superficie R22b2: 76).");
        Assert.Empty(violacoes);

        var semForm = acoes.Keys.Except(mapeadas)
            .Except(AcoesSemFormEstatico)
            .OrderBy(x => x)
            .ToList();
        Assert.Empty(semForm);
    }

    /// <summary>R21 (r21a0 D1): torna o catalogo AcoesSemFormEstatico auto-verificavel.
    /// JS-driven deve manter ≥1 literal de URL dinamico em quotes na view; fantasma deve ter
    /// zero ocorrencias em qualquer .cshtml (botao ligado ⇒ reclasificar). Antivacuidade: toda
    /// entrada continua sendo acao HttpPost extraida pelo scan principal (sem row obsoleta).</summary>
    [Fact]
    public void whitelist_acoes_sem_form_estatico_e_auto_verificada()
    {
        var acoes = ExtrairAcoesPostComEscalaresImplicitos();
        var views = LerTodasAsViews().ToList();
        Assert.True(views.Count >= 200, $"Scan suspeito de views: apenas {views.Count} .cshtml em Views/** (superficie R21: 484).");

        var obsoletas = AcoesSemFormEstatico.Where(a => !acoes.ContainsKey(a)).OrderBy(x => x).ToList();
        Assert.True(obsoletas.Count == 0,
            "Entradas da whitelist que nao sao mais acoes HttpPost com escalares implicitos (rows obsoletas): "
            + string.Join(", ", obsoletas));

        // JS_DRIVEN: literal em quotes contendo /Administrativo360/[seg/]\<acao\> (a regex tolera
        // segmentos intermediarios — o data-ai-url carrega Cotacoes/{id}/AnaliseIa).
        foreach (var acao in AcoesJsDrivenSemFormEstatico.OrderBy(x => x))
        {
            var rx = new System.Text.RegularExpressions.Regex(
                "[\"'=]\\s*/Administrativo360(?:/[^\\s\"'<>;)]*)*/" + acao + "(?=[\"'\\s?;)]|$)");
            var hit = views.FirstOrDefault(v => rx.IsMatch(v.Texto));
            Assert.True(hit.NomeArquivo != null,
                $"{acao}: sem URL dinamica (/Administrativo360/[...]/" + acao + ") em nenhuma view — JS-driven morta ou virou estatica; reclasifique (r21a0 D1).");
        }
        Assert.True(AcoesJsDrivenSemFormEstatico.Count >= 3,
            $"Catalogo suspeito: apenas {AcoesJsDrivenSemFormEstatico.Count} classificadas JS-driven (baseline R21: 3).");

        // FANTASMA: zero ocorrencias (case-sensitive) do nome em qualquer .cshtml.
        foreach (var acao in AcoesFantasmaSemChamador.OrderBy(x => x))
        {
            var hits = views.Where(v => v.Texto.Contains(acao, StringComparison.Ordinal))
                .Select(v => v.NomeArquivo).ToList();
            Assert.True(hits.Count == 0,
                $"{acao}: fantasma com ponto de chamada (" + string.Join(", ", hits) + ") — ligacao feita; tire do set de fantasmas e deixe o scan principal parear (r21a0 D1).");
        }
        // R22b2: sem piso numerico para o set de fantasmas — vazio por design (as 4 entradas da
        // R21 foram ligadas em r22b1+b2); o loop por entrada acima cobre qualquer entrada nova.
    }

    /// <summary>Casa uma URL estatica de form (/Administrativo360/&lt;Area&gt;/&lt;path&gt;) com a acao
    /// cuja template de rota ({param:tipo} = wild-card) produz o mesmo path; devolve tambem as
    /// chaves de query embutidas na URL (?chave=...) como canal estatico de satisfacao.</summary>
    private static string ResolverAcaoPorUrlEstatica(string formTag, IReadOnlyDictionary<string, AcaoPost> acoes, HashSet<string> queryKeys)
    {
        var m = System.Text.RegularExpressions.Regex.Match(formTag, "action\\s*=\\s*\"/Administrativo360/[^/\"]+/([^\"]*)\"");
        if (!m.Success) return "__sem_acao__";
        var pathPart = m.Groups[1].Value;
        var qIdx = pathPart.IndexOf('?');
        if (qIdx >= 0)
        {
            foreach (var par in pathPart[(qIdx + 1)..].Split('&', System.StringSplitOptions.RemoveEmptyEntries))
            {
                var eq = par.IndexOf('=');
                if (eq > 0) queryKeys.Add(par[..eq]);
            }
            pathPart = pathPart[..qIdx];
        }
        var segs = pathPart.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
        foreach (var kv in acoes)
        {
            var tsegs = (kv.Value.Template ?? kv.Key)
                .Split('/', System.StringSplitOptions.RemoveEmptyEntries);
            if (tsegs.Length != segs.Length) continue;
            var ok = true;
            for (var i = 0; i < tsegs.Length; i++)
            {
                var ts = tsegs[i];
                if (ts.StartsWith('{') && ts.EndsWith('}')) continue;
                if (!string.Equals(ts, segs[i], StringComparison.OrdinalIgnoreCase)) { ok = false; break; }
            }
            if (ok) return kv.Key;
        }
        return "__sem_acao__";
    }

    private static HashSet<string> SlotsDeTemplate(string template)
    {
        return System.Text.RegularExpressions.Regex.Matches(template, "\\{(\\w+)")
            .Select(x => x.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record AcaoPost(List<string> Escalares, string? Template);

    private static Dictionary<string, AcaoPost> ExtrairAcoesPostComEscalaresImplicitos()
    {
        var controllersDir = Path.Combine(Root, "backend/PlantaoPro.Web/Controllers");
        var resultado = new Dictionary<string, AcaoPost>();
        // R20 (extensao F-R15-A): a template de rota ([HttpPost("...")]) e capturada junto —
        // em forms com URL estatica ela decide quais parametros entram pela URL (slots {param}).
        var padrao = new System.Text.RegularExpressions.Regex(
            "\\[HttpPost(?<tpl>(?:\\(\"[^\"]*\"\\))?)[^\\]]*\\]\\s*(?:\\[[^\\]]*\\]\\s*)*(?:public|protected)\\s+(?:async\\s+)?[\\w<>,\\[\\]?]+\\s+([A-Z]\\w+)\\s*\\(");

        // R20 (r20b1 / extensao F-R15-A): cobertura explicita dos Adm360*WebController* — os 4
        // files da familia (Cotacoes/DocumentosXml/Fiscal/Gestao) que ficavam fora do scan
        // original de r16b1, criado so para Administrativo360Controller*.cs.
        var arquivos = Directory.GetFiles(controllersDir, "Administrativo360Controller*.cs")
            .Concat(Directory.GetFiles(controllersDir, "Adm360*WebController*.cs"))
            .ToArray();

        foreach (var arquivo in arquivos)
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
                var template = m.Groups["tpl"].Success
                    ? System.Text.RegularExpressions.Regex.Match(m.Groups["tpl"].Value, "\"([^\"]*)\"").Groups[1].Value
                    : null;
                if (escalares.Count > 0) resultado[m.Groups[1].Value] = new AcaoPost(escalares, template);
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

    /// <summary>R21 (r21a0 D1): o mesmo scan em Views/** — um call-site de acao Adm360 pode
    /// morar em qualquer pagina (portal, modulos); a verificacao da whitelist cobre tudo.</summary>
    private static IEnumerable<(string NomeArquivo, string Texto)> LerTodasAsViews()
    {
        var viewsDir = Path.Combine(Root, "backend/PlantaoPro.Web/Views");
        foreach (var arquivo in Directory.GetFiles(viewsDir, "*.cshtml", SearchOption.AllDirectories))
            yield return (Path.GetRelativePath(viewsDir, arquivo).Replace('\\', '/'), File.ReadAllText(arquivo));
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
