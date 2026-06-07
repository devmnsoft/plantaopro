using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Route("api/inteligencia")]
[Tags("SaaS - Inteligência")]
public sealed class InteligenciaSaasController : ControllerBase
{
    private readonly SaasIntelligenceService intelligence;
    private readonly ComercialSaasService comercial;
    private readonly ILogger<InteligenciaSaasController> logger;
    public InteligenciaSaasController(SaasIntelligenceService intelligence, ComercialSaasService comercial, ILogger<InteligenciaSaasController> logger) { this.intelligence = intelligence; this.comercial = comercial; this.logger = logger; }
    [HttpGet("saas/resumo")]
    public async Task<IActionResult> Resumo() { var r = await intelligence.GerarResumoSaasAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("clientes/{clienteId:guid}/saude")]
    public async Task<IActionResult> Saude(Guid clienteId) { var r = await intelligence.CalcularSaudeClienteAsync(clienteId); return StatusCode(r.StatusCode, r); }
    [HttpGet("clientes/{clienteId:guid}/alertas")]
    public async Task<IActionResult> Alertas(Guid clienteId) { var r = await intelligence.GerarAlertasClienteAsync(clienteId); return StatusCode(r.StatusCode, r); }
    [HttpGet("clientes/{clienteId:guid}/proximas-acoes")]
    public async Task<IActionResult> ProximasAcoes(Guid clienteId) { var r = await intelligence.GerarAcoesRecomendadasAsync(clienteId); return StatusCode(r.StatusCode, r); }
    [HttpPost("sugerir-plano")]
    public IActionResult SugerirPlano([FromBody] SugerirPlanoRequest request) { var dto = comercial.SugerirPlano(request); return Ok(ApiResponse<PlanoSugeridoDto>.Ok(dto)); }
    [HttpPost("recalcular")]
    public async Task<IActionResult> Recalcular([FromQuery] Guid? clienteId)
    {
        try
        {
            if (clienteId.HasValue)
            {
                var r = await intelligence.CalcularSaudeClienteAsync(clienteId.Value);
                if (r.Success) await intelligence.GerarAlertasClienteAsync(clienteId.Value);
                return StatusCode(r.StatusCode, r);
            }
            var resumo = await intelligence.GerarResumoSaasAsync();
            return StatusCode(resumo.StatusCode, resumo);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro recalcular inteligência SaaS"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível recalcular inteligência.", 500)); }
    }
}
