using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize, Route("api/portal-cliente/modulos")]
[Tags("Portal do Cliente - módulos")]
public sealed class ClientModulesController : ControllerBase
{
    private readonly ModuleContractingService service;
    public ClientModulesController(ModuleContractingService service) => this.service = service;

    [HttpGet("catalogo")]
    public async Task<IActionResult> Catalog(CancellationToken ct) => Ok(await service.CatalogAsync(ct));

    [HttpPost("revisao")]
    [Authorize(Roles = RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor)]
    public async Task<IActionResult> Review([FromBody] ContractReviewRequest request, CancellationToken ct) => await Result(() => service.ReviewAsync(request, ct));

    [HttpPost("solicitacoes")]
    [Authorize(Roles = RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor)]
    public async Task<IActionResult> Confirm([FromBody] ConfirmContractRequest request, CancellationToken ct) => await Result(() => service.ConfirmAsync(request, ct));

    [HttpGet("solicitacoes")]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct) => Ok(await service.ListAsync(status, null, ct));

    [HttpPost("solicitacoes/{id:guid}/cancelar")]
    [Authorize(Roles = RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] ContractDecisionRequest request, CancellationToken ct) => await Result(async () => { await service.CancelAsync(id, request.Justificativa, ct); return new { id, status = "CANCELADA" }; });

    private async Task<IActionResult> Result<T>(Func<Task<T>> action) { try { return Ok(await action()); } catch (ConditionsChangedException ex) { return Conflict(new { code="CONDITIONS_CHANGED", message=ex.Message }); } catch (ArgumentException ex) { return BadRequest(new { code="VALIDATION_ERROR", message=ex.Message }); } catch (InvalidOperationException ex) { return Conflict(new { code="INVALID_STATE", message=ex.Message }); } catch (UnauthorizedAccessException) { return Forbid(); } }
}

[ApiController, Authorize(Roles = RolesConstants.AdministradorGlobal), Route("api/admin-saas/solicitacoes-modulos")]
[Tags("Administração MNSOFT - contratação de módulos")]
public sealed class AdminModuleRequestsController : ControllerBase
{
    private readonly ModuleContractingService service;
    public AdminModuleRequestsController(ModuleContractingService service) => this.service = service;
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] Guid? tenantId, CancellationToken ct) => Ok(await service.ListAsync(status, tenantId, ct));
    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Approve(Guid id,[FromBody] ContractDecisionRequest request,CancellationToken ct)=>await Decide(id,true,request,ct);
    [HttpPost("{id:guid}/recusar")]
    public async Task<IActionResult> Reject(Guid id,[FromBody] ContractDecisionRequest request,CancellationToken ct)=>await Decide(id,false,request,ct);
    private async Task<IActionResult> Decide(Guid id,bool approve,ContractDecisionRequest request,CancellationToken ct){try{return Ok(await service.DecideAsync(id,approve,request,ct));}catch(ConditionsChangedException ex){return Conflict(new{code="CONDITIONS_CHANGED",message=ex.Message});}catch(ArgumentException ex){return BadRequest(new{code="VALIDATION_ERROR",message=ex.Message});}catch(KeyNotFoundException){return NotFound();}}
}
