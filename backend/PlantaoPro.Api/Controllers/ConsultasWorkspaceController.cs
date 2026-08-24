using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Clinical;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize, Route("api/consultas")]
public sealed class ConsultasWorkspaceController : ControllerBase
{
    private readonly IConsultaApplicationService service;
    public ConsultasWorkspaceController(IConsultaApplicationService service) { this.service = service; }
    private IActionResult Result<T>(ApiResponse<T> result) => StatusCode(result.StatusCode, result);
    [HttpGet("fila-medica"), Authorize(Policy = "Consulta.Iniciar")]
    public async Task<IActionResult> Fila([FromQuery] Guid? unidadeId, [FromQuery] Guid? medicoId, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 30, CancellationToken ct = default) => Result(await service.FilaAsync(unidadeId, medicoId, pagina, tamanho, ct));
    [HttpPost("{id:guid}/iniciar"), Authorize(Policy = "Consulta.Iniciar")]
    public async Task<IActionResult> Iniciar(Guid id, IniciarConsultaRequest request, CancellationToken ct) => Result(await service.IniciarAsync(id, request, ct));
    [HttpGet("{id:guid}/workspace"), Authorize(Policy = "Consulta.VerDadosSensiveis")]
    public async Task<IActionResult> Workspace(Guid id, CancellationToken ct) => Result(await service.WorkspaceAsync(id, ct));
    [HttpPut("{id:guid}/rascunho"), Authorize(Policy = "Consulta.Editar")]
    public async Task<IActionResult> Rascunho(Guid id, SalvarConsultaRascunhoRequest request, CancellationToken ct) => Result(await service.SalvarRascunhoAsync(id, request, ct));
    [HttpGet("{id:guid}/pendencias-finalizacao"), Authorize(Policy = "Consulta.Finalizar")]
    public async Task<IActionResult> Pendencias(Guid id, CancellationToken ct) => Result(await service.PendenciasAsync(id, ct));
    [HttpPost("{id:guid}/finalizar"), Authorize(Policy = "Consulta.Finalizar")]
    public async Task<IActionResult> Finalizar(Guid id, FinalizarConsultaRequest request, CancellationToken ct) => Result(await service.FinalizarAsync(id, request, ct));
    [HttpGet("{id:guid}/cids"), Authorize(Policy = "Consulta.VerDadosSensiveis")]
    public async Task<IActionResult> Cids(Guid id, CancellationToken ct) => Result(await service.ListarCidsAsync(id, ct));
    [HttpPost("{id:guid}/cids"), Authorize(Policy = "CID.Vincular")]
    public async Task<IActionResult> AdicionarCid(Guid id, AdicionarConsultaCidRequest request, CancellationToken ct) => Result(await service.AdicionarCidAsync(id, request, ct));
    [HttpDelete("{id:guid}/cids/{consultaCidId:guid}"), Authorize(Policy = "CID.Remover")]
    public async Task<IActionResult> RemoverCid(Guid id, Guid consultaCidId, CancellationToken ct) => Result(await service.RemoverCidAsync(id, consultaCidId, ct));
    [HttpGet("{id:guid}/adendos"), Authorize(Policy = "Consulta.VerDadosSensiveis")]
    public async Task<IActionResult> Adendos(Guid id, CancellationToken ct) => Result(await service.ListarAdendosAsync(id, ct));
    [HttpPost("{id:guid}/adendos"), Authorize(Policy = "Consulta.Adendo")]
    public async Task<IActionResult> CriarAdendo(Guid id, CriarConsultaAdendoRequest request, CancellationToken ct) => Result(await service.CriarAdendoAsync(id, request, ct));
}
