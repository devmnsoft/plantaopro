using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
namespace PlantaoPro.Api.Controllers;
[ApiController,Route("api/conferencia-execucao"),Authorize(Roles=RolesConstants.EscalasGestao+","+RolesConstants.Administrador+","+RolesConstants.AdministradorCliente)]
public sealed class ExecutionConferenceController:ControllerBase
{
 private readonly ExecutionConferenceService service;public ExecutionConferenceController(ExecutionConferenceService service){this.service=service;}
 [HttpGet]public async Task<IActionResult> List([FromQuery]ExecutionConferenceFilter filter,CancellationToken ct){var r=await service.ListAsync(filter,ct);return StatusCode(r.StatusCode,r);}
 [HttpPost("correcoes/{id:guid}/decidir")]public async Task<IActionResult> Decide(Guid id,[FromBody]DecideExecutionCorrectionRequest request,CancellationToken ct){var r=await service.DecideAsync(id,request,ct);return StatusCode(r.StatusCode,r);}
}
