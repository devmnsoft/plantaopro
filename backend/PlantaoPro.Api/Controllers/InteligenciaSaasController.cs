using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.Financeiro)]
[Route("api/inteligencia")]
[Tags("Inteligência SaaS")]
public sealed class InteligenciaSaasController : ControllerBase
{
    private readonly SaasIntelligenceService intelligence;
    private readonly ComercialService comercial;
    private readonly ILogger<InteligenciaSaasController> logger;

    public InteligenciaSaasController(SaasIntelligenceService intelligence, ComercialService comercial, ILogger<InteligenciaSaasController> logger)
    {
        this.intelligence = intelligence;
        this.comercial = comercial;
        this.logger = logger;
    }

    [HttpGet("saas/resumo")]
    public async Task<IActionResult> Resumo() { var response = await intelligence.GerarResumoSaasAsync(); return StatusCode(response.StatusCode, response); }

    [HttpGet("clientes/{clienteId:guid}/saude")]
    public async Task<IActionResult> Saude(Guid clienteId) { var response = await intelligence.CalcularSaudeClienteAsync(clienteId); return StatusCode(response.StatusCode, response); }

    [HttpGet("clientes/{clienteId:guid}/alertas")]
    public async Task<IActionResult> Alertas(Guid clienteId) { var response = await intelligence.GerarAlertasClienteAsync(clienteId); return StatusCode(response.StatusCode, response); }

    [HttpGet("clientes/{clienteId:guid}/proximas-acoes")]
    public async Task<IActionResult> ProximasAcoes(Guid clienteId) { var response = await intelligence.GerarAcoesRecomendadasAsync(clienteId); return StatusCode(response.StatusCode, response); }

    [HttpPost("sugerir-plano")]
    public IActionResult SugerirPlano([FromBody] SugerirPlanoRequest request) { var response = comercial.SugerirPlano(request); return StatusCode(response.StatusCode, response); }

    [HttpPost("recalcular")]
    public async Task<IActionResult> Recalcular([FromQuery] Guid? clienteId)
    {
        try
        {
            if (clienteId.HasValue)
            {
                var response = await intelligence.CalcularSaudeClienteAsync(clienteId.Value);
                return StatusCode(response.StatusCode, response);
            }

            var resumo = await intelligence.GerarResumoSaasAsync();
            return StatusCode(resumo.StatusCode, resumo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao recalcular inteligência SaaS");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível recalcular inteligência SaaS.", 500));
        }
    }
}
