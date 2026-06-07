using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Route("api/saas-dashboard")]
[Tags("SaaS - Dashboard")]
public sealed class SaasDashboardController : ControllerBase
{
    private readonly SaasIntelligenceService service;

    public SaasDashboardController(SaasIntelligenceService service)
    {
        this.service = service;
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        var response = await service.GerarResumoSaasAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas([FromQuery] Guid? clienteId)
    {
        if (!clienteId.HasValue) return BadRequest(ApiResponse<string>.Fail("Informe o cliente para listar alertas.", 400));
        var response = await service.ListarAlertasClienteAsync(clienteId.Value);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("clientes-risco")]
    public async Task<IActionResult> ClientesRisco([FromQuery] Guid clienteId)
    {
        var response = await service.CalcularSaudeClienteAsync(clienteId);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("faturas-vencidas")]
    public IActionResult FaturasVencidas()
    {
        return Ok(ApiResponse<string>.Ok("Use GET /api/faturamento-saas/inadimplencia para a lista paginada de faturas vencidas."));
    }

    [HttpGet("oportunidades-upgrade")]
    public async Task<IActionResult> OportunidadesUpgrade([FromQuery] Guid clienteId)
    {
        var response = await service.GerarAcoesRecomendadasAsync(clienteId);
        return StatusCode(response.StatusCode, response);
    }
}
