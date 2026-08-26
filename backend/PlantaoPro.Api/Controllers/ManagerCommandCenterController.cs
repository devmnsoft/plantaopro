using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Api.Controllers;
[ApiController, Authorize(Roles="Admin,Gestor,Administrador"), Route("api/manager-command-center")]
public sealed class ManagerCommandCenterController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromServices] ManagerCommandCenterService service,[FromQuery] DateOnly? from,[FromQuery] DateOnly? to,[FromQuery] string? status,CancellationToken ct)
    {
        var raw=User.FindFirst("tenant_id")?.Value??User.FindFirst("cliente_id")?.Value;
        if(!Guid.TryParse(raw,out var tenantId)) return Forbid();
        var response=await service.GetAsync(tenantId,from??DateOnly.FromDateTime(DateTime.UtcNow),to??DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),status,ct);
        return StatusCode(response.StatusCode,response);
    }
}
