using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/impersonacao")]
[Authorize(Policy = "CanImpersonateTenant")]
public sealed class ImpersonacaoController : ControllerBase
{
    [HttpGet("usuarios-disponiveis")]
    public IActionResult UsuariosDisponiveis([FromQuery] Guid tenantId) => Ok(ApiResponse<object[]>.Ok(Array.Empty<object>()));

    [HttpPost("iniciar")]
    public IActionResult Iniciar([FromBody] IniciarImpersonacaoRequest request)
    {
        if (request.TenantId == Guid.Empty || request.UsuarioAlvoId == Guid.Empty || string.IsNullOrWhiteSpace(request.Motivo) || string.IsNullOrWhiteSpace(request.TicketReferencia))
            return BadRequest(ApiResponse<object>.Fail("Tenant, usuário alvo, motivo e ticket são obrigatórios."));
        var minutos = Math.Clamp(request.DuracaoMinutos ?? 30, 1, 30);
        return Ok(ApiResponse<object>.Ok(new { impersonation = true, request.UsuarioAlvoId, request.TenantId, expiraEm = DateTime.UtcNow.AddMinutes(minutos) }, "Impersonação iniciada."));
    }

    [HttpPost("encerrar")]
    public IActionResult Encerrar([FromBody] EncerrarImpersonacaoRequest request) => Ok(ApiResponse<object>.Ok(new { request.ImpersonacaoSessaoId, status = "ENCERRADA" }, "Impersonação encerrada."));

    [HttpGet("atual")]
    public IActionResult Atual() => Ok(ApiResponse<object>.Ok(new { impersonation = false }));

    [HttpGet("historico")]
    public IActionResult Historico() => Ok(ApiResponse<object[]>.Ok(Array.Empty<object>()));
}

public sealed record IniciarImpersonacaoRequest(Guid TenantId, Guid UsuarioAlvoId, string Motivo, string TicketReferencia, int? DuracaoMinutos);
public sealed record EncerrarImpersonacaoRequest(Guid? ImpersonacaoSessaoId);
