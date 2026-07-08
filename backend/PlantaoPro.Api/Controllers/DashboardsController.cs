using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboards")]
public sealed class DashboardsController : ControllerBase
{
    private readonly DashboardPremiumService service;
    public DashboardsController(DashboardPremiumService service) { this.service = service; }
    [HttpGet("admin-global")] public Task<ActionResult<ApiResponse<DashboardPremiumDto>>> AdminGlobal() => Responder("admin-global");
    [HttpGet("admin-cliente")] public Task<ActionResult<ApiResponse<DashboardPremiumDto>>> AdminCliente() => Responder("admin-cliente");
    [HttpGet("coordenacao")] public Task<ActionResult<ApiResponse<DashboardPremiumDto>>> Coordenacao() => Responder("coordenacao");
    [HttpGet("medico")] public Task<ActionResult<ApiResponse<DashboardPremiumDto>>> Medico() => Responder("medico");
    [HttpGet("financeiro")] public Task<ActionResult<ApiResponse<DashboardPremiumDto>>> Financeiro() => Responder("financeiro");
    [HttpGet("saude360")] public Task<ActionResult<ApiResponse<DashboardPremiumDto>>> Saude360() => Responder("saude360");
    private async Task<ActionResult<ApiResponse<DashboardPremiumDto>>> Responder(string perfil) => Ok(ApiResponse<DashboardPremiumDto>.Ok(await service.ObterAsync(perfil, null), "Dashboard real carregado."));
}
