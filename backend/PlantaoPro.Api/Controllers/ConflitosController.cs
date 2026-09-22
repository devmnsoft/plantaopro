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
    private readonly TenantGuardService _tenantGuardService;
    private readonly UsuarioContextService _usuarioContextService;
    private readonly ILogger<ConflitosController> _logger;

    public ConflitosController(ConflitoHorarioService conflitoHorarioService, TenantGuardService tenantGuardService, UsuarioContextService usuarioContextService, ILogger<ConflitosController> logger)
    {
        _conflitoHorarioService = conflitoHorarioService;
        _tenantGuardService = tenantGuardService;
        _usuarioContextService = usuarioContextService;
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

            var acessoNegado = await ValidarAcessoMedicoAsync(request.MedicoId);
            if (acessoNegado is not null) return acessoNegado;

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

            var acessoNegado = await ValidarAcessoMedicoAsync(medicoId);
            if (acessoNegado is not null) return acessoNegado;

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

    private async Task<IActionResult?> ValidarAcessoMedicoAsync(Guid medicoId)
    {
        var usuarioId = _usuarioContextService.GetUsuarioId();
        if (medicoId == Guid.Empty || !usuarioId.HasValue || !await _tenantGuardService.PodeAcessarMedicoAsync(usuarioId.Value, medicoId))
        {
            await _tenantGuardService.RegistrarAcessoNegadoAsync(AuditoriaConstants.Entidades.Medico, medicoId == Guid.Empty ? null : medicoId, AuditoriaConstants.Acoes.BloqueioTenant, "Consulta de conflito fora do tenant autorizado.");
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ConflitoHorarioResultadoDto>.Fail("Você não possui acesso ao médico informado.", 403));
        }

        return null;
    }
}
