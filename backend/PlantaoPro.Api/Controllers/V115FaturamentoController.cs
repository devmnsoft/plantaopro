using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v115")]
public sealed class V115FaturamentoController : ControllerBase
{
    private readonly V115FaturamentoRegraService faturamento; private readonly V115RepasseMedicoService repasses; private readonly V115GlosaService glosas; private readonly ILogger<V115FaturamentoController> logger;
    public V115FaturamentoController(V115FaturamentoRegraService faturamento, V115RepasseMedicoService repasses, V115GlosaService glosas, ILogger<V115FaturamentoController> logger) { this.faturamento = faturamento; this.repasses = repasses; this.glosas = glosas; this.logger = logger; }
    [HttpGet("faturamento/regras")] public async Task<ActionResult<ApiResponse<IEnumerable<RegraFaturamentoDto>>>> Regras([FromQuery] string? tipo) => await Safe(() => faturamento.ListarRegrasAsync(tipo), "listar regras");
    [HttpPost("faturamento/regras")] public async Task<ActionResult<ApiResponse<V115RegraFaturamentoDto>>> CriarRegra([FromBody] V115RegraFaturamentoRequest request) => await Safe(() => faturamento.SalvarRegraAsync(request), "criar regra");
    [HttpPut("faturamento/regras/{id:guid}")] public async Task<ActionResult<ApiResponse<V115RegraFaturamentoDto>>> AtualizarRegra(Guid id, [FromBody] V115RegraFaturamentoRequest request) => await Safe(() => faturamento.SalvarRegraAsync(request, id), "atualizar regra");
    [HttpGet("faturamento/contas-receber")] public async Task<ActionResult<ApiResponse<IEnumerable<ContaReceberDto>>>> ContasReceber() => await Safe(() => faturamento.ContasReceberAsync(), "contas a receber");
    [HttpPost("faturamento/gerar-conta-consulta/{consultaId:guid}")] public async Task<ActionResult<ApiResponse<ContaReceberDto>>> GerarContaConsulta(Guid consultaId, [FromBody] V115GerarContaRequest? request) => await Safe(() => faturamento.GerarContaConsultaAsync(consultaId, request), "gerar conta consulta");
    [HttpPost("faturamento/gerar-conta-plantao/{plantaoId:guid}")] public async Task<ActionResult<ApiResponse<ContaReceberDto>>> GerarContaPlantao(Guid plantaoId, [FromBody] V115GerarContaRequest? request) => await Safe(() => faturamento.GerarContaPlantaoAsync(plantaoId, request), "gerar conta plantão");
    [HttpPost("faturamento/receber/{contaId:guid}")] public async Task<ActionResult<ApiResponse<RecebimentoResumoDto>>> Receber(Guid contaId, [FromBody] V115RecebimentoRequest request) => await Safe(() => faturamento.ReceberAsync(contaId, request), "receber conta");
    [HttpPost("faturamento/recebimentos/{recebimentoId:guid}/estornar")] public async Task<ActionResult<ApiResponse<RecebimentoResumoDto>>> Estornar(Guid recebimentoId, [FromBody] V115EstornoRequest request) => await Safe(() => faturamento.EstornarAsync(recebimentoId, request), "estornar recebimento");
    [HttpGet("repasses-medicos")] public async Task<ActionResult<ApiResponse<IEnumerable<RepasseMedicoDto>>>> Repasses() => await Safe(() => repasses.ListarAsync(), "repasses médicos");
    [HttpPost("repasses-medicos/gerar/{referenciaId:guid}")] public async Task<ActionResult<ApiResponse<object>>> GerarRepasse(Guid referenciaId, [FromBody] V115RepasseRequest request) => await Safe(() => repasses.GerarAsync(referenciaId, request), "gerar repasse");
    [HttpPost("repasses-medicos/{id:guid}/contestar")] public async Task<ActionResult<ApiResponse<object>>> Contestar(Guid id, [FromBody] V115ContestacaoRequest request) => await Safe(() => repasses.ContestarAsync(id, request), "contestar repasse");
    [HttpPost("repasses-medicos/{id:guid}/resolver")] public async Task<ActionResult<ApiResponse<object>>> ResolverRepasse(Guid id, [FromBody] V115ContestacaoRequest request) => await Safe(() => repasses.ResolverAsync(id, request), "resolver repasse");
    [HttpPost("repasses-medicos/{id:guid}/confirmar")] public async Task<ActionResult<ApiResponse<object>>> ConfirmarRepasse(Guid id) => await Safe(() => repasses.ConfirmarAsync(id), "confirmar repasse");
    [HttpGet("glosas")] public async Task<ActionResult<ApiResponse<IEnumerable<GlosaDto>>>> Glosas() => await Safe(() => glosas.ListarAsync(), "glosas");
    [HttpPost("glosas")] public async Task<ActionResult<ApiResponse<object>>> RegistrarGlosa([FromBody] V115GlosaRequest request) => await Safe(() => glosas.RegistrarAsync(request), "registrar glosa");
    [HttpPost("glosas/{id:guid}/resolver")] public async Task<ActionResult<ApiResponse<object>>> ResolverGlosa(Guid id, [FromBody] V115ContestacaoRequest request) => await Safe(() => glosas.ResolverAsync(id, request), "resolver glosa");
    [HttpGet("financeiro/alertas")] public async Task<ActionResult<ApiResponse<IEnumerable<AlertaFinanceiroDto>>>> Alertas() => await Safe(() => glosas.AlertasAsync(), "alertas financeiros");
    private async Task<ActionResult<ApiResponse<T>>> Safe<T>(Func<Task<ApiResponse<T>>> action, string nome) { try { return ToAction(await action()); } catch (Exception ex) { logger.LogError(ex, "Falha v1.15 em {Nome}", nome); return ToAction(ApiResponse<T>.Fail("Não foi possível concluir a ação v1.15 agora.", 500)); } }
    private static ActionResult<ApiResponse<T>> ToAction<T>(ApiResponse<T> response) => new ObjectResult(response) { StatusCode = response.StatusCode };
}
