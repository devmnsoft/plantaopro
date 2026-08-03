using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Operation360.WorkItems;

namespace PlantaoPro.Api.Controllers;
[ApiController, Authorize, Route("api/work-items")]
public sealed class WorkItemsController : ControllerBase
{
    private readonly IWorkItemService service; public WorkItemsController(IWorkItemService service) => this.service = service;
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.ListAsync(ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) { var item = await service.GetAsync(id, ct); return item == null ? NotFound() : Ok(item); }
    [HttpGet("{id:guid}/historico")] public async Task<IActionResult> History(Guid id, CancellationToken ct) => Ok(await service.HistoryAsync(id, ct));
    [HttpPost("{id:guid}/mover")] public Task<IActionResult> Move(Guid id, [FromBody] WorkItemMoveRequest request, CancellationToken ct) => Respond(id == request.ItemId ? service.MoveAsync(request, ct) : throw new ArgumentException("O item da rota difere do conteúdo."));
    [HttpPost("{id:guid}/assumir")] public Task<IActionResult> Take(Guid id, [FromBody] WorkItemVersionRequest request, CancellationToken ct) => Respond(service.AssignAsync(id, Guid.Empty, request, ct));
    [HttpPost("{id:guid}/encaminhar")] public Task<IActionResult> Forward(Guid id, [FromBody] WorkItemForwardRequest request, CancellationToken ct) => Respond(service.AssignAsync(id, request.ResponsibleId, new(request.Version, request.IdempotencyKey), ct));
    [HttpPost("{id:guid}/comentar")] public Task<IActionResult> Comment(Guid id, [FromBody] WorkItemCommentRequest request, CancellationToken ct) => Respond(service.CommentAsync(id, request, ct));
    [HttpPost("{id:guid}/adiar")] public Task<IActionResult> Postpone(Guid id, [FromBody] WorkItemPostponeRequest request, CancellationToken ct) => Respond(service.PostponeAsync(id, request, ct));
    [HttpPost("{id:guid}/concluir")] public Task<IActionResult> Complete(Guid id, [FromBody] WorkItemVersionRequest r, CancellationToken ct) => Respond(service.MoveAsync(new(id, WorkItemStatus.EmAndamento, WorkItemStatus.Concluido, 0, r.Version, r.IdempotencyKey), ct));
    [HttpPost("{id:guid}/reabrir")] public Task<IActionResult> Reopen(Guid id, [FromBody] WorkItemVersionRequest r, CancellationToken ct) => Respond(service.MoveAsync(new(id, WorkItemStatus.Concluido, WorkItemStatus.Entrada, 0, r.Version, r.IdempotencyKey), ct));
    private async Task<IActionResult> Respond(Task<WorkItemMutationResult> task) { try { var r = await task; if (!r.Found) return NotFound(); if (r.Conflict) return Conflict(r.Item); return Ok(r.Item); } catch (ArgumentException ex) { return BadRequest(new { title = ex.Message }); } }
}
