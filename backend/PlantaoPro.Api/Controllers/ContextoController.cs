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
    public IActionResult TenantsDisponiveis() => Ok(ApiResponse<IEnumerable<TenantDisponivelDto>>.Ok(_contexto.TenantsDisponiveis(User)));

    [HttpGet("recentes")]
    public IActionResult Recentes() => Ok(ApiResponse<IEnumerable<ContextoTrocaDto>>.Ok(_contexto.Recentes(User)));

    [HttpPost("selecionar")]
    [Authorize(Policy = "CanSwitchTenant")]
    public IActionResult Selecionar([FromBody] SelecionarContextoRequest request)
    {
        if (request.TenantId == Guid.Empty) return BadRequest(ApiResponse<object>.Fail("Tenant inválido."));
        return Ok(ApiResponse<object>.Ok(_contexto.Selecionar(User, request), "Contexto selecionado."));
    }

    [HttpPost("retornar-global")]
    [Authorize(Policy = "GlobalAccess")]
    public IActionResult RetornarGlobal() => Ok(ApiResponse<object>.Ok(_contexto.RetornarGlobal(User), "Contexto global restaurado."));

    [HttpGet("historico")]
    public IActionResult Historico() => Ok(ApiResponse<IEnumerable<ContextoTrocaDto>>.Ok(_contexto.Historico(User)));
}

public sealed record SelecionarContextoRequest(Guid TenantId, string? Motivo);
