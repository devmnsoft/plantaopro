using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v114")]
public sealed class V114ProdutoController : ControllerBase
{
    private readonly V114ProdutoService service;
    private readonly ILogger<V114ProdutoController> logger;

    public V114ProdutoController(V114ProdutoService service, ILogger<V114ProdutoController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet("dashboard")] public async Task<ActionResult<ApiResponse<object>>> Dashboard() => await Safe(() => service.DashboardAsync(), "dashboard");
    [HttpGet("operacao/central")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Central() => await Safe(() => service.OperacaoCentralAsync(), "operacao central");
    [HttpGet("operacao/atividades")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Atividades() => await Safe(() => service.AtividadesAsync(), "atividades");
    [HttpGet("operacao/tarefas")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Tarefas([FromQuery] string? status) => await Safe(() => service.TarefasAsync(status), "tarefas");
    [HttpGet("operacao/outbox")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Outbox() => await Safe(() => service.OutboxAsync(), "outbox");
    [HttpGet("itens-faturaveis")] public async Task<ActionResult<ApiResponse<IEnumerable<ItemFaturavelDto>>>> Itens([FromQuery] string? q, [FromQuery] string? tipo, [FromQuery] string? status) => await Safe(() => service.ItensFaturaveisAsync(q, tipo, status), "itens faturáveis");
    [HttpPost("itens-faturaveis")] public async Task<ActionResult<ApiResponse<ItemFaturavelDto>>> CriarItem([FromBody] ItemFaturavelUpsertDto dto) => await Safe(() => service.CriarItemFaturavelAsync(dto), "criar item faturável");
    [HttpPut("itens-faturaveis/{id:guid}")] public async Task<ActionResult<ApiResponse<ItemFaturavelDto>>> AtualizarItem(Guid id, [FromBody] ItemFaturavelUpsertDto dto) => await Safe(() => service.AtualizarItemFaturavelAsync(id, dto), "atualizar item faturável");
    [HttpDelete("itens-faturaveis/{id:guid}")] public async Task<ActionResult<ApiResponse<object>>> InativarItem(Guid id) => await Safe(() => service.InativarItemFaturavelAsync(id), "inativar item faturável");
    [HttpGet("faturamento/contas-receber")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> ContasReceber() => await Safe(() => service.ContasReceberAsync(), "contas a receber");
    [HttpGet("faturamento/titulos")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Titulos() => await Safe(() => service.TitulosAsync(), "títulos");
    [HttpPost("faturamento/gerar-conta-atendimento/{atendimentoId:guid}")] public async Task<ActionResult<ApiResponse<object>>> GerarConta(Guid atendimentoId) => await Safe(() => service.GerarContaAtendimentoAsync(atendimentoId), "gerar conta atendimento");
    [HttpPost("faturamento/titulos/{id:guid}/demo-boleto")] public async Task<ActionResult<ApiResponse<object>>> DemoBoleto(Guid id) => await Safe(() => service.DemoBoletoAsync(id), "demo boleto");
    [HttpGet("faturamento/repasses-medicos")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Repasses() => await Safe(() => service.RepassesMedicosAsync(), "repasses médicos");
    [HttpGet("faturamento/glosas")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Glosas() => await Safe(() => service.GlosasAsync(), "glosas");
    [HttpGet("jornada/progresso")] public async Task<ActionResult<ApiResponse<object>>> Jornada() => await Safe(() => service.JornadaProgressoAsync(), "jornada");
    [HttpGet("jornada/proximas-acoes")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> ProximasAcoes() => await Safe(() => service.ProximasAcoesAsync(), "próximas ações");
    [HttpPost("jornada/acoes/{id:guid}/concluir")] public async Task<ActionResult<ApiResponse<object>>> ConcluirAcao(Guid id) => await Safe(() => service.ConcluirAcaoAsync(id), "concluir ação");
    [HttpGet("templates-operacionais")] public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Templates() => await Safe(() => service.TemplatesOperacionaisAsync(), "templates operacionais");
    [HttpPost("templates-operacionais/{id}/instalar")] public async Task<ActionResult<ApiResponse<object>>> InstalarTemplate(string id) => await Safe(() => service.InstalarTemplateAsync(id), "instalar template");
    [HttpGet("mobile/medico/dashboard")] public ActionResult<ApiResponse<object>> MobileMedicoDashboard() => ToAction(service.MobileMedicoResumo());

    private async Task<ActionResult<ApiResponse<T>>> Safe<T>(Func<Task<ApiResponse<T>>> action, string nome)
    {
        try { return ToAction(await action()); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha v1.14 em {Nome}", nome);
            return ToAction(ApiResponse<T>.Fail("Não foi possível concluir a ação v1.14 agora. Tente novamente.", 500));
        }
    }

    private static ActionResult<ApiResponse<T>> ToAction<T>(ApiResponse<T> response) => new ObjectResult(response) { StatusCode = response.StatusCode };
}
