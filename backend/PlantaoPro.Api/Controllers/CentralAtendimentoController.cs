using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Policy = "CentralAtendimento.Ver")]
[Route("api/central-atendimento")]
public sealed class CentralAtendimentoController : ControllerBase
{
    private readonly ICentralAtendimentoService service;

    public CentralAtendimentoController(ICentralAtendimentoService service)
    {
        this.service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] CentralAtendimentoFiltro filtro)
    {
        var response = await service.ListarAsync(filtro);
        return StatusCode(response.StatusCode, response);
    }
}
