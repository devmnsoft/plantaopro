using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;


[ApiController, Authorize, Route("api/consultas")]
public sealed class ConsultasWorkspaceController : ControllerBase
{
    private readonly IConsultaApplicationService service;
    public ConsultasWorkspaceController(IConsultaApplicationService service) => this.service = service;
    [HttpGet("fila-medica"), Authorize(Policy = "Consulta.Visualizar")] public async Task<IActionResult> Fila([FromQuery] Guid? unidadeId, [FromQuery] int pagina = 1, [FromQuery] int tamanho = 25, CancellationToken ct = default) => Responder(await service.FilaAsync(unidadeId, pagina, tamanho, ct));
    [HttpGet("{id:guid}/workspace"), Authorize(Policy = "Consulta.VerDadosSensiveis")] public async Task<IActionResult> Workspace(Guid id, CancellationToken ct) => Responder(await service.WorkspaceAsync(id, ct));
    [HttpPost("{id:guid}/iniciar"), Authorize(Policy = "Consulta.Iniciar")] public async Task<IActionResult> Iniciar(Guid id, IniciarConsultaRequest request, CancellationToken ct) => Responder(await service.IniciarAsync(id, request, ct));
    [HttpPut("{id:guid}/rascunho"), Authorize(Policy = "Consulta.Editar")] public async Task<IActionResult> Rascunho(Guid id, SalvarConsultaRascunhoRequest request, CancellationToken ct) => Responder(await service.SalvarAsync(id, request, ct));
    [HttpGet("{id:guid}/pendencias-finalizacao"), Authorize(Policy = "Consulta.Finalizar")] public async Task<IActionResult> Pendencias(Guid id, CancellationToken ct) => Responder(await service.PendenciasAsync(id, ct));
    [HttpPost("{id:guid}/finalizar"), Authorize(Policy = "Consulta.Finalizar")] public async Task<IActionResult> Finalizar(Guid id, FinalizarConsultaRequest request, CancellationToken ct) => Responder(await service.FinalizarAsync(id, request, ct));
    private ObjectResult Responder<T>(ApiResponse<T> response) => StatusCode(response.StatusCode, response);
}
