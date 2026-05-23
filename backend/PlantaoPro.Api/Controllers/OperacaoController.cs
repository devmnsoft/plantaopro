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
    private readonly ILogger<OperacaoController> logger;

    public OperacaoController(OperacaoService operacaoService, ILogger<OperacaoController> logger)
    {
        this.operacaoService = operacaoService;
        this.logger = logger;
    }

    [HttpGet("resumo")]
    [ProducesResponseType(typeof(ApiResponse<OperacaoResumoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResumo()
    {
        try
        {
            var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
            var response = await operacaoService.GetResumoAsync(uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar resumo operacional");
            return StatusCode(500, ApiResponse<string>.Fail("Erro ao carregar resumo operacional.", 500));
        }
    }
}
