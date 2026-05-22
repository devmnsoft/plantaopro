using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.IdentityModel.Tokens.Jwt;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public sealed class OnboardingController : ControllerBase
{
    private readonly OnboardingService _onboardingService;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(OnboardingService onboardingService, ILogger<OnboardingController> logger)
    {
        _onboardingService = onboardingService;
        _logger = logger;
    }

    [HttpPost("cliente")]
    public async Task<IActionResult> CriarCliente([FromBody] CreateClienteOnboardingRequest request)
    {
        _logger.LogInformation("Iniciando requisição de onboarding para {RazaoSocial}", request.RazaoSocial);
        try
        {
            if (string.IsNullOrWhiteSpace(request.RazaoSocial) || string.IsNullOrWhiteSpace(request.Cnpj))
            {
                _logger.LogWarning("Validação falhou na requisição de onboarding.");
                return BadRequest(ApiResponse<string>.Fail("Informe Razão Social e CNPJ para continuar.", 400));
            }

            var usuarioIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
            {
                _logger.LogWarning("Token sem usuário válido para onboarding.");
                return Unauthorized(ApiResponse<string>.Fail("Sessão inválida. Faça login novamente.", 401));
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();
            var result = await _onboardingService.CriarClienteCompletoAsync(request, usuarioId, ip, ua);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar onboarding");
            return StatusCode(500, ApiResponse<string>.Fail("Erro interno ao processar onboarding.", 500));
        }
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromQuery] Guid clienteId)
    {
        try
        {
            var result = await _onboardingService.GetResumoAsync(clienteId);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter resumo de onboarding");
            return StatusCode(500, ApiResponse<string>.Fail("Erro interno ao obter resumo.", 500));
        }
    }
}
