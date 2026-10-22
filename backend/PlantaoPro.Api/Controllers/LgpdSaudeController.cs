using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

// ============================================================================
// R87 - Superficie de LGPD + saude: consentimento por escopo controlado
// (PRONTUARIO/COAM, dados sensiveis com consentimento especifico e destacado),
// direitos do titular (LISTAR/EXPORTAR/APAGAR com trilha auditavel) e retencao
// minima legal do prontuario (Lei 13.787/2018, art. 6: 20 anos do ultimo registro).
// ============================================================================

[ApiController]
[Route("api/lgpd-saude")]
[Authorize]
[Tags("LGPD + Saúde")]
public sealed class LgpdSaudeController : ControllerBase
{
    private readonly LgpdSaudeService _service;
    private readonly ILogger<LgpdSaudeController> _logger;
    public LgpdSaudeController(LgpdSaudeService service, ILogger<LgpdSaudeController> logger) { _service = service; _logger = logger; }

    /// <summary>Consentimentos registrados do paciente por escopo (mais recente primeiro).</summary>
    [HttpGet("consentimentos/{pacienteId:guid}")]
    public async Task<IActionResult> Consentimentos(Guid pacienteId, CancellationToken ct)
    {
        var r = await _service.ConsentimentosAsync(TenantId(), pacienteId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Registra/revoconsentimento de escopo (PRONTUARIO/COAM) com trilha auditável.</summary>
    [HttpPost("consentimentos")]
    public async Task<IActionResult> RegistrarConsentimento([FromBody] LgpdConsentimentoCommand cmd, CancellationToken ct)
    {
        var r = await _service.RegistrarConsentimentoAsync(TenantId(), UserId(), Ip(), cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Envelope de dados do paciente (leitura simples, sem trilha).</summary>
    [HttpGet("dados/{pacienteId:guid}")]
    public async Task<IActionResult> Dados(Guid pacienteId, CancellationToken ct)
    {
        var r = await _service.ListarDadosAsync(TenantId(), pacienteId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Exerce direito do titular (LISTAR/EXPORTAR/APAGAR) deixando trilha; APAGAR respeita a retenção mínima legal.</summary>
    [HttpPost("direitos/exercer")]
    public async Task<IActionResult> ExercerDireito([FromBody] ExercerDiretoCommand cmd, CancellationToken ct)
    {
        var r = await _service.ExercerDireitoAsync(TenantId(), UserId(), Ip(), cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Preview da retenção mínima legal do prontuário (último registro, data apta e fonte citável).</summary>
    [HttpGet("retencao/{pacienteId:guid}")]
    public async Task<IActionResult> Retencao(Guid pacienteId, CancellationToken ct)
    {
        var r = await _service.RetencaoAsync(TenantId(), pacienteId, ct);
        return StatusCode(r.StatusCode, r);
    }

    private Guid TenantId() => Guid.TryParse(User.FindFirstValue("ClienteId") ?? User.FindFirstValue("cliente_id"), out var id) ? id : Guid.Empty;
    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UsuarioId"), out var id) ? id : null;
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
