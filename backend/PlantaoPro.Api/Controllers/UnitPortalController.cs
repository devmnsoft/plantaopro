using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Api.Controllers;

[ApiController,Authorize(Roles="Hospital,Gestor,Administrador,ADMINISTRADOR_CLIENTE"),Route("api/unit-portal")]
public sealed class UnitPortalController : ControllerBase
{
    private bool Context(out Guid tenant,out Guid user){return Guid.TryParse(User.FindFirst("tenant_id")?.Value??User.FindFirst("cliente_id")?.Value,out tenant)&Guid.TryParse(User.FindFirst("sub")?.Value??User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out user);}
    [HttpGet("dashboard/{unitId:guid}")]
    public async Task<IActionResult> Dashboard(Guid unitId,[FromServices]UnitDashboardService service,CancellationToken ct){if(!Context(out var tenant,out _))return Forbid();var result=await service.GetAsync(tenant,unitId,ct);return StatusCode(result.StatusCode,result);}
    [HttpPost("requests")]
    public async Task<IActionResult> Request([FromBody]CreateShiftRequestDto input,[FromServices]ShiftRequestService service,CancellationToken ct){if(!Context(out var tenant,out var user))return Forbid();var result=await service.CreateAsync(tenant,user,input,ct);return StatusCode(result.StatusCode,result);}
    [HttpPost("requests/{requestId:guid}/approve"),Authorize(Roles="Gestor,Administrador,ADMINISTRADOR_CLIENTE")]
    public async Task<IActionResult> Approve(Guid requestId,[FromQuery]bool convert,[FromServices]ShiftRequestApprovalService service,CancellationToken ct){if(!Context(out var tenant,out var user))return Forbid();var result=await service.DecideAsync(tenant,user,requestId,true,null,convert,ct);return StatusCode(result.StatusCode,result);}
    [HttpPost("requests/{requestId:guid}/reject"),Authorize(Roles="Gestor,Administrador,ADMINISTRADOR_CLIENTE")]
    public async Task<IActionResult> Reject(Guid requestId,[FromBody]ShiftRequestDecisionDto decision,[FromServices]ShiftRequestApprovalService service,CancellationToken ct){if(!Context(out var tenant,out var user))return Forbid();var result=await service.DecideAsync(tenant,user,requestId,false,decision.Reason,false,ct);return StatusCode(result.StatusCode,result);}
}
