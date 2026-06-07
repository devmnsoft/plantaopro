using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.Medico)]
[Route("api/medico-area")]
public class MedicoAreaController : ControllerBase
{
    private readonly MedicoAreaService service;

    public MedicoAreaController(MedicoAreaService service)
    {
        this.service = service;
    }

    private Guid? Uid()
    {
        var uidClaim = User.FindFirst("uid")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("user_id")?.Value;

        if (Guid.TryParse(uidClaim, out var id))
            return id;

        return null;
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        var uid = Uid();
        if (uid is null)
            return Unauthorized();

        var response = await service.ResumoAsync(uid.Value);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("plantoes-disponiveis")]
    public async Task<IActionResult> PlantoesDisponiveis([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var uid = Uid();
        if (uid is null)
            return Unauthorized();

        var response = await service.PlantoesDisponiveisAsync(uid.Value, page, pageSize);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("minhas-escalas")]
    public async Task<IActionResult> MinhasEscalas([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var uid = Uid();
        if (uid is null)
            return Unauthorized();

        var response = await service.MinhasEscalasAsync(uid.Value, page, pageSize);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("meus-pagamentos")]
    public async Task<IActionResult> MeusPagamentos([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var uid = Uid();
        if (uid is null)
            return Unauthorized();

        var response = await service.MeusPagamentosAsync(uid.Value, page, pageSize);
        return StatusCode(response.StatusCode, response);
    }
}
