using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/contexto")]
[Authorize]
public sealed class ContextoController : ControllerBase
{
    private readonly IContextoService _contexto;
    public ContextoController(IContextoService contexto) => _contexto = contexto;

    [HttpGet("atual")]
    public IActionResult Atual() => Ok(ApiResponse<ContextoAtualDto>.Ok(_contexto.Atual(User)));

    [HttpGet("tenants-disponiveis")]
    public async Task<IActionResult> TenantsDisponiveis(CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<TenantDisponivelDto>>.Ok(await _contexto.TenantsDisponiveisAsync(User,ct)));

    [HttpGet("recentes")]
    public async Task<IActionResult> Recentes(CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<ContextoTrocaDto>>.Ok(await _contexto.RecentesAsync(User,ct)));

    [HttpPost("selecionar")]
    [Authorize(Policy = "CanSwitchTenant")]
    public async Task<IActionResult> Selecionar([FromBody] SelecionarContextoRequest request,CancellationToken ct)
    {
        if (request.TenantId == Guid.Empty) return BadRequest(ApiResponse<object>.Fail("Tenant inválido."));
        return Ok(ApiResponse<ContextSelectionDto>.Ok(await _contexto.SelecionarAsync(User,request,ct), "Contexto selecionado."));
    }

    [HttpPost("retornar-global")]
    [Authorize(Policy = "GlobalAccess")]
    public async Task<IActionResult> RetornarGlobal(CancellationToken ct) { await _contexto.RetornarGlobalAsync(User,ct); return Ok(ApiResponse<object>.Ok(new { contextMode="GLOBAL" }, "Contexto global restaurado.")); }

    [HttpGet("historico")]
    public async Task<IActionResult> Historico(CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<ContextoTrocaDto>>.Ok(await _contexto.HistoricoAsync(User,ct)));
}

public sealed record SelecionarContextoRequest(Guid TenantId, string? Motivo);
