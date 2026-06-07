using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Route("api/saas-inteligencia")]
[Tags("SaaS - Inteligência")]
public sealed class SaasInteligenciaController : ControllerBase
{
    private readonly SaasIntelligenceService service;
    private readonly ILogger<SaasInteligenciaController> logger;

    public SaasInteligenciaController(SaasIntelligenceService service, ILogger<SaasInteligenciaController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet("clientes/{clienteId:guid}/saude")]
    public async Task<IActionResult> Saude(Guid clienteId)
    {
        var response = await service.CalcularSaudeClienteAsync(clienteId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("clientes/{clienteId:guid}/alertas")]
    public async Task<IActionResult> Alertas(Guid clienteId)
    {
        var response = await service.GerarAlertasClienteAsync(clienteId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("clientes/{clienteId:guid}/recomendacoes")]
    public async Task<IActionResult> Recomendacoes(Guid clienteId)
    {
        var response = await service.GerarAcoesRecomendadasAsync(clienteId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        var response = await service.GerarResumoSaasAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpPost("clientes/{clienteId:guid}/recalcular")]
    public async Task<IActionResult> Recalcular(Guid clienteId)
    {
        try
        {
            var response = await service.CalcularSaudeClienteAsync(clienteId);
            if (response.Success) await service.GerarAlertasClienteAsync(clienteId);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao recalcular inteligência SaaS cliente:{ClienteId}", clienteId);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível recalcular a inteligência do cliente.", 500));
        }
    }
}
