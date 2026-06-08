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
        var response = clienteId.HasValue
            ? await service.ListarAlertasClienteAsync(clienteId.Value)
            : await service.ListarAlertasAbertosAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("clientes-risco")]
    public async Task<IActionResult> ClientesRisco([FromQuery] Guid? clienteId)
    {
        if (clienteId.HasValue)
        {
            var response = await service.CalcularSaudeClienteAsync(clienteId.Value);
            return StatusCode(response.StatusCode, response);
        }

        var lista = await service.ListarClientesRiscoAsync();
        return StatusCode(lista.StatusCode, lista);
    }

    [HttpGet("faturas-vencidas")]
    public async Task<IActionResult> FaturasVencidas()
    {
        var response = await service.ListarFaturasVencidasAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("oportunidades-upgrade")]
    public async Task<IActionResult> OportunidadesUpgrade([FromQuery] Guid? clienteId)
    {
        if (clienteId.HasValue)
        {
            var response = await service.GerarAcoesRecomendadasAsync(clienteId.Value);
            return StatusCode(response.StatusCode, response);
        }

        var lista = await service.ListarOportunidadesUpgradeAsync();
        return StatusCode(lista.StatusCode, lista);
    }

    [HttpGet("funil-comercial")]
    public async Task<IActionResult> FunilComercial()
    {
        var response = await service.GerarFunilComercialAsync();
        return StatusCode(response.StatusCode, response);
    }
}
