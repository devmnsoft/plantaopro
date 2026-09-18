using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Api.Controllers;
[ApiController, Authorize(Roles = RolesConstants.Operacao), Route("api/manager-command-center")]
public sealed class ManagerCommandCenterController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromServices] ManagerCommandCenterService service, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? status, CancellationToken ct)
    {
        var isGlobal = User.IsInRole(RolesConstants.AdministradorGlobal) || User.IsInRole("ADMINISTRADOR_GLOBAL") || User.IsInRole("Admin");
        var raw = User.FindFirst("tenant_id")?.Value ?? User.FindFirst("cliente_id")?.Value;

        if (isGlobal && (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out _)))
        {
            var globalResponse = await service.GetGlobalAsync(ct);
            return StatusCode(globalResponse.StatusCode, globalResponse);
        }

        if (!Guid.TryParse(raw, out var tenantId)) return Forbid();
        var response = await service.GetAsync(tenantId, from ?? DateOnly.FromDateTime(DateTime.UtcNow), to ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), status, ct);
        return StatusCode(response.StatusCode, response);
    }
}
