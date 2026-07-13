using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v116")]
public sealed class V116ConsolidacaoController : ControllerBase
{
    private readonly V116ConvenioService convenios; private readonly V116LoteFaturamentoService lotes; private readonly V116CaixaService caixa; private readonly V116TimelineService timeline; private readonly V116NotificacaoOperacionalService notificacoes; private readonly V116RelatorioExecutivoService relatorios; private readonly ILogger<V116ConsolidacaoController> logger;
    public V116ConsolidacaoController(V116ConvenioService convenios, V116LoteFaturamentoService lotes, V116CaixaService caixa, V116TimelineService timeline, V116NotificacaoOperacionalService notificacoes, V116RelatorioExecutivoService relatorios, ILogger<V116ConsolidacaoController> logger) { this.convenios = convenios; this.lotes = lotes; this.caixa = caixa; this.timeline = timeline; this.notificacoes = notificacoes; this.relatorios = relatorios; this.logger = logger; }
    [HttpGet("convenios/autorizacoes")] public Task<ActionResult<ApiResponse<IEnumerable<object>>>> Autorizacoes() => Safe(() => convenios.ListarAutorizacoesAsync(), "listar autorizações");
    [HttpPost("convenios/autorizacoes")] public Task<ActionResult<ApiResponse<object>>> CriarAutorizacao(V116OperacaoRequest r) => Safe(() => convenios.CriarAutorizacaoAsync(r), "criar autorização");
    [HttpPost("convenios/autorizacoes/{id:guid}/aprovar")] public Task<ActionResult<ApiResponse<object>>> Aprovar(Guid id, V116OperacaoRequest? r) => Safe(() => convenios.AprovarAsync(id, r), "aprovar autorização");
    [HttpPost("convenios/autorizacoes/{id:guid}/negar")] public Task<ActionResult<ApiResponse<object>>> Negar(Guid id, V116OperacaoRequest? r) => Safe(() => convenios.NegarAsync(id, r), "negar autorização");
    [HttpPost("convenios/autorizacoes/{id:guid}/cancelar")] public Task<ActionResult<ApiResponse<object>>> Cancelar(Guid id, V116OperacaoRequest? r) => Safe(() => convenios.CancelarAsync(id, r), "cancelar autorização");
    [HttpGet("convenios/guias")] public Task<ActionResult<ApiResponse<IEnumerable<object>>>> Guias() => Safe(() => convenios.ListarGuiasAsync(), "listar guias");
    [HttpPost("convenios/guias")] public Task<ActionResult<ApiResponse<object>>> CriarGuia(V116OperacaoRequest r) => Safe(() => convenios.GerarGuiaAsync(r), "gerar guia");
    [HttpPost("convenios/guias/{id:guid}/vincular-conta")] public Task<ActionResult<ApiResponse<object>>> VincularConta(Guid id, V116OperacaoRequest r) => Safe(() => convenios.VincularContaAsync(id, r), "vincular guia");
    [HttpGet("faturamento/lotes")] public Task<ActionResult<ApiResponse<IEnumerable<object>>>> Lotes() => Safe(() => lotes.ListarAsync(), "listar lotes");
    [HttpPost("faturamento/lotes")] public Task<ActionResult<ApiResponse<object>>> CriarLote(V116OperacaoRequest r) => Safe(() => lotes.CriarAsync(r), "criar lote");
    [HttpGet("faturamento/lotes/{id:guid}")] public Task<ActionResult<ApiResponse<object>>> Lote(Guid id) => Safe(() => lotes.ObterAsync(id), "obter lote");
    [HttpPost("faturamento/lotes/{id:guid}/itens")] public Task<ActionResult<ApiResponse<object>>> AddItem(Guid id, V116OperacaoRequest r) => Safe(() => lotes.AdicionarItemAsync(id, r), "adicionar item");
    [HttpDelete("faturamento/lotes/{id:guid}/itens/{itemId:guid}")] public Task<ActionResult<ApiResponse<object>>> DelItem(Guid id, Guid itemId) => Safe(() => lotes.RemoverItemAsync(id, itemId), "remover item");
    [HttpPost("faturamento/lotes/{id:guid}/fechar")] public Task<ActionResult<ApiResponse<object>>> FecharLote(Guid id, V116OperacaoRequest? r) => Safe(() => lotes.FecharAsync(id, r), "fechar lote");
    [HttpPost("faturamento/lotes/{id:guid}/reabrir")] public Task<ActionResult<ApiResponse<object>>> ReabrirLote(Guid id, V116OperacaoRequest? r) => Safe(() => lotes.ReabrirAsync(id, r), "reabrir lote");
    [HttpPost("faturamento/lotes/{id:guid}/marcar-enviado-demo")] public Task<ActionResult<ApiResponse<object>>> Enviado(Guid id) => Safe(() => lotes.EnviadoDemoAsync(id), "enviar demo");
    [HttpPost("faturamento/lotes/{id:guid}/retorno-demo")] public Task<ActionResult<ApiResponse<object>>> Retorno(Guid id, V116OperacaoRequest? r) => Safe(() => lotes.RetornoDemoAsync(id, r), "retorno demo");
    [HttpGet("caixa/status")] public Task<ActionResult<ApiResponse<object>>> CaixaStatus() => Safe(() => caixa.StatusAsync(), "status caixa");
    [HttpPost("caixa/abrir")] public Task<ActionResult<ApiResponse<object>>> Abrir(V116OperacaoRequest r) => Safe(() => caixa.AbrirAsync(r), "abrir caixa");
    [HttpPost("caixa/fechar")] public Task<ActionResult<ApiResponse<object>>> Fechar(V116OperacaoRequest r) => Safe(() => caixa.FecharAsync(r), "fechar caixa");
    [HttpGet("caixa/movimentos")] public Task<ActionResult<ApiResponse<IEnumerable<object>>>> Movimentos() => Safe(() => caixa.MovimentosAsync(), "movimentos");
    [HttpPost("caixa/receber/{contaId:guid}")] public Task<ActionResult<ApiResponse<object>>> Receber(Guid contaId, V116OperacaoRequest r) => Safe(() => caixa.ReceberAsync(contaId, r), "receber");
    [HttpPost("caixa/estornar/{movimentoId:guid}")] public Task<ActionResult<ApiResponse<object>>> Estornar(Guid movimentoId, V116OperacaoRequest r) => Safe(() => caixa.EstornarAsync(movimentoId, r), "estornar");
    [HttpPost("caixa/conciliar/{movimentoId:guid}")] public Task<ActionResult<ApiResponse<object>>> Conciliar(Guid movimentoId, V116OperacaoRequest r) => Safe(() => caixa.ConciliarAsync(movimentoId, r), "conciliar");
    [HttpGet("timelines/{entidade}/{id:guid}")] public Task<ActionResult<ApiResponse<IEnumerable<object>>>> Timelines(string entidade, Guid id) => Safe(() => timeline.ListarAsync(entidade, id), "timeline");
    [HttpPost("timelines/{entidade}/{id:guid}/comentario")] public Task<ActionResult<ApiResponse<object>>> Comentario(string entidade, Guid id, V116OperacaoRequest r) => Safe(() => timeline.ComentarAsync(entidade, id, r), "comentário");
    [HttpGet("historico-acoes/{entidade}/{id:guid}")] public Task<ActionResult<ApiResponse<IEnumerable<object>>>> Historico(string entidade, Guid id) => Safe(() => timeline.HistoricoAsync(entidade, id), "histórico");
    [HttpGet("notificacoes-operacionais")] public Task<ActionResult<ApiResponse<IEnumerable<object>>>> Notificacoes() => Safe(() => notificacoes.ListarAsync(), "notificações");
    [HttpPost("notificacoes-operacionais/{id:guid}/lida")] public Task<ActionResult<ApiResponse<object>>> Lida(Guid id) => Safe(() => notificacoes.LidaAsync(id), "lida");
    [HttpPost("notificacoes-operacionais/lidas")] public Task<ActionResult<ApiResponse<object>>> Lidas() => Safe(() => notificacoes.TodasLidasAsync(), "lidas");
    [HttpPost("notificacoes-operacionais/reprocessar")] public Task<ActionResult<ApiResponse<object>>> Reprocessar() => Safe(() => notificacoes.ReprocessarAsync(), "reprocessar");
    [HttpGet("relatorios/{nome}")] public Task<ActionResult<ApiResponse<IEnumerable<object>>>> Relatorio(string nome) => Safe(() => relatorios.GerarAsync(nome), "relatório");
    private async Task<ActionResult<ApiResponse<T>>> Safe<T>(Func<Task<ApiResponse<T>>> action, string nome) { try { return ToAction(await action()); } catch (Exception ex) { logger.LogError(ex, "Falha v1.16 em {Nome}", nome); return ToAction(ApiResponse<T>.Fail("Não foi possível concluir a ação v1.16 agora.", 500)); } }
    private static ActionResult<ApiResponse<T>> ToAction<T>(ApiResponse<T> response) => new ObjectResult(response) { StatusCode = response.StatusCode };
}
