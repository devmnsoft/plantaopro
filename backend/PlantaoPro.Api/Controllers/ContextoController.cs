using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using PlantaoPro.CrossCutting.Security;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/contexto")]
[Authorize]
public sealed class ContextoController : ControllerBase
{
    [HttpGet("atual")]
    public IActionResult Atual() => Ok(ApiResponse<object>.Ok(new
    {
        usuarioId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        tenantId = User.FindFirstValue("tenant_id"),
        clienteId = User.FindFirstValue("cliente_id"),
        accessScope = User.FindFirstValue("access_scope") ?? AccessScopes.Tenant,
        contextMode = User.FindFirstValue("context_mode") ?? AccessScopes.Tenant,
        primaryRole = User.FindFirstValue("primary_role") ?? User.FindFirstValue("role")
    }));

    [HttpGet("tenants-disponiveis")]
    public IActionResult TenantsDisponiveis() => Ok(ApiResponse<object[]>.Ok(Array.Empty<object>()));

    [HttpGet("recentes")]
    public IActionResult Recentes() => Ok(ApiResponse<object[]>.Ok(Array.Empty<object>()));

    [HttpPost("selecionar")]
    [Authorize(Policy = "CanSwitchTenant")]
    public IActionResult Selecionar([FromBody] SelecionarContextoRequest request)
    {
        if (request.TenantId == Guid.Empty) return BadRequest(ApiResponse<object>.Fail("Tenant inválido."));
        return Ok(ApiResponse<object>.Ok(new { tenantId = request.TenantId, contextMode = AccessScopes.Tenant }, "Contexto selecionado."));
    }

    [HttpPost("retornar-global")]
    [Authorize(Policy = "GlobalAccess")]
    public IActionResult RetornarGlobal() => Ok(ApiResponse<object>.Ok(new { contextMode = AccessScopes.Global }, "Contexto global restaurado."));

    [HttpGet("historico")]
    public IActionResult Historico() => Ok(ApiResponse<object[]>.Ok(Array.Empty<object>()));
}

public sealed record SelecionarContextoRequest(Guid TenantId, string? Motivo);
