using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/impersonacao")]
[Authorize(Policy = "CanImpersonateTenant")]
public sealed class ImpersonacaoController : ControllerBase
{
    private readonly IImpersonationService _impersonation;
    public ImpersonacaoController(IImpersonationService impersonation) => _impersonation = impersonation;

    [HttpGet("usuarios-disponiveis")]
    public IActionResult UsuariosDisponiveis([FromQuery] Guid tenantId) => Ok(ApiResponse<object[]>.Ok(Array.Empty<object>()));

    [HttpPost("iniciar")]
    public IActionResult Iniciar([FromBody] IniciarImpersonacaoRequest request)
    {
        if (request.TenantId == Guid.Empty || request.UsuarioAlvoId == Guid.Empty || string.IsNullOrWhiteSpace(request.Motivo) || string.IsNullOrWhiteSpace(request.TicketReferencia))
            return BadRequest(ApiResponse<object>.Fail("Tenant, usuário alvo, motivo e ticket são obrigatórios."));
        return Ok(ApiResponse<object>.Ok(_impersonation.Iniciar(User, request), "Impersonação iniciada."));
    }

    [HttpPost("encerrar")]
    public IActionResult Encerrar([FromBody] EncerrarImpersonacaoRequest request) => Ok(ApiResponse<object>.Ok(_impersonation.Encerrar(User, request), "Impersonação encerrada."));

    [HttpGet("atual")]
    public IActionResult Atual() => Ok(ApiResponse<object>.Ok(_impersonation.Atual(User)));

    [HttpGet("historico")]
    public IActionResult Historico() => Ok(ApiResponse<IEnumerable<object>>.Ok(_impersonation.Historico(User)));
}

public sealed record IniciarImpersonacaoRequest(Guid TenantId, Guid UsuarioAlvoId, string Motivo, string TicketReferencia, int? DuracaoMinutos);
public sealed record EncerrarImpersonacaoRequest(Guid? ImpersonacaoSessaoId);
