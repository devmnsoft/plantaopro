using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/operacao-inteligente")]
public sealed class OperacaoInteligenteController : ControllerBase
{
    private readonly OperacaoRecomendacaoService _service;
    private readonly ILogger<OperacaoInteligenteController> _logger;

    public OperacaoInteligenteController(OperacaoRecomendacaoService service, ILogger<OperacaoInteligenteController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("resumo")]
    public ActionResult<ApiResponse<OperacaoInteligenteResumoDto>> Resumo()
    {
        try
        {
            var perfil = User.Claims.FirstOrDefault(c => c.Type.EndsWith("role", StringComparison.OrdinalIgnoreCase))?.Value;
            var resumo = _service.GerarResumo(null, null, perfil);
            return Ok(ApiResponse<OperacaoInteligenteResumoDto>.Ok(resumo, "Resumo da operação inteligente carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar operação inteligente.");
            return StatusCode(500, ApiResponse<OperacaoInteligenteResumoDto>.Fail("Erro ao carregar operação inteligente.", 500));
        }
    }
}
