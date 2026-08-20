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
    public async Task<IActionResult> UsuariosDisponiveis([FromQuery] Guid tenantId,CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<object>>.Ok(await _impersonation.UsuariosAsync(tenantId,ct)));

    [HttpPost("iniciar")]
    public async Task<IActionResult> Iniciar([FromBody] IniciarImpersonacaoRequest request,CancellationToken ct)
    {
        if (request.TenantId == Guid.Empty || request.UsuarioAlvoId == Guid.Empty || string.IsNullOrWhiteSpace(request.Motivo) || string.IsNullOrWhiteSpace(request.TicketReferencia))
            return BadRequest(ApiResponse<object>.Fail("Tenant, usuário alvo, motivo e ticket são obrigatórios."));
        return Ok(ApiResponse<ImpersonationSessionDto>.Ok(await _impersonation.IniciarAsync(User,request,ct), "Impersonação iniciada."));
    }

    [HttpPost("encerrar")]
    public async Task<IActionResult> Encerrar([FromBody] EncerrarImpersonacaoRequest request,CancellationToken ct) { await _impersonation.EncerrarAsync(User,request,ct); return Ok(ApiResponse<object>.Ok(new { status="ENCERRADA" }, "Impersonação encerrada.")); }

    [HttpGet("atual")]
    public IActionResult Atual() => Ok(ApiResponse<object>.Ok(_impersonation.Atual(User)));

    [HttpGet("historico")]
    public async Task<IActionResult> Historico(CancellationToken ct) => Ok(ApiResponse<IReadOnlyList<object>>.Ok(await _impersonation.HistoricoAsync(User,ct)));
}

public sealed record IniciarImpersonacaoRequest(Guid TenantId, Guid UsuarioAlvoId, string Motivo, string TicketReferencia, int? DuracaoMinutos);
public sealed record EncerrarImpersonacaoRequest(Guid? ImpersonacaoSessaoId);
