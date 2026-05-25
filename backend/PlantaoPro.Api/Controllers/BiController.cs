using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/bi")]
[Authorize]
public class BiController : ControllerBase
{
    private readonly BiService biService;

    public BiController(BiService biService)
    {
        this.biService = biService;
    }

    [HttpGet("resumo-executivo")]
    [ProducesResponseType(typeof(ApiResponse<BiResumoExecutivoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResumoExecutivo()
    {
        var response = await biService.GetResumoExecutivoAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("plantao-cobertura")]
    public IActionResult PlantaoCobertura() => Ok(ApiResponse<string>.Ok("Em evolução"));
    [HttpGet("financeiro")]
    public IActionResult Financeiro() => Ok(ApiResponse<string>.Ok("Em evolução"));
    [HttpGet("medicos")]
    public IActionResult Medicos() => Ok(ApiResponse<string>.Ok("Em evolução"));
    [HttpGet("clientes")]
    public IActionResult Clientes() => Ok(ApiResponse<string>.Ok("Em evolução"));
    [HttpGet("sla")]
    public IActionResult Sla() => Ok(ApiResponse<string>.Ok("Em evolução"));
    [HttpGet("tendencias")]
    public IActionResult Tendencias() => Ok(ApiResponse<string>.Ok("Em evolução"));
}
