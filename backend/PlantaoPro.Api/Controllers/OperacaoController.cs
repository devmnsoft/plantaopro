using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/operacao")]
[Authorize]
public class OperacaoController : ControllerBase
{
    private readonly OperacaoService operacaoService;

    public OperacaoController(OperacaoService operacaoService)
    {
        this.operacaoService = operacaoService;
    }

    [HttpGet("resumo")]
    [ProducesResponseType(typeof(ApiResponse<OperacaoResumoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResumo()
    {
        var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        var response = await operacaoService.GetResumoAsync(uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        return StatusCode(response.StatusCode, response);
    }
}
