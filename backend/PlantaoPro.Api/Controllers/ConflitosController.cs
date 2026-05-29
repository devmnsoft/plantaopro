using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conflitos")]
public sealed class ConflitosController : ControllerBase
{
    private readonly ConflitoHorarioService _conflitoHorarioService;
    private readonly ILogger<ConflitosController> _logger;

    public ConflitosController(ConflitoHorarioService conflitoHorarioService, ILogger<ConflitosController> logger)
    {
        _conflitoHorarioService = conflitoHorarioService;
        _logger = logger;
    }

    [HttpPost("verificar")]
    [ProducesResponseType(typeof(ApiResponse<ConflitoHorarioResultadoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Verificar([FromBody] VerificarConflitoRequest request)
    {
        try
        {
            if (request.DataFim <= request.DataInicio)
            {
                return BadRequest(ApiResponse<ConflitoHorarioResultadoDto>.Fail("Data fim deve ser maior que data início."));
            }

            var resultado = await _conflitoHorarioService.VerificarAsync(request.MedicoId, request.DataInicio, request.DataFim, request.EscalaIgnoradaId);
            return Ok(ApiResponse<ConflitoHorarioResultadoDto>.Ok(resultado));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar conflito de horário para médico {MedicoId}", request.MedicoId);
            return StatusCode(500, ApiResponse<ConflitoHorarioResultadoDto>.Fail("Não foi possível verificar conflitos no momento.", 500));
        }
    }

    [HttpGet("medico/{medicoId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ConflitoHorarioResultadoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PorMedico(Guid medicoId, [FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
    {
        try
        {
            var inicio = dataInicio ?? DateTime.UtcNow.Date;
            var fim = dataFim ?? inicio.AddDays(30);
            if (fim <= inicio)
            {
                return BadRequest(ApiResponse<ConflitoHorarioResultadoDto>.Fail("Período inválido para consulta de conflitos."));
            }

            var resultado = await _conflitoHorarioService.VerificarAsync(medicoId, inicio, fim);
            return Ok(ApiResponse<ConflitoHorarioResultadoDto>.Ok(resultado));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar conflitos por médico {MedicoId}", medicoId);
            return StatusCode(500, ApiResponse<ConflitoHorarioResultadoDto>.Fail("Não foi possível listar conflitos do médico.", 500));
        }
    }

    [HttpGet("plantao/{plantaoId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConflitoHorarioDetalheDto>>), StatusCodes.Status200OK)]
    public IActionResult PorPlantao(Guid plantaoId)
    {
        return Ok(ApiResponse<IEnumerable<ConflitoHorarioDetalheDto>>.Ok(Array.Empty<ConflitoHorarioDetalheDto>(), "Informe médico e período em /api/conflitos/verificar para validação precisa do plantão."));
    }
}
