using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// B5: convites de equipe (tenant) + aceite público por token.
/// Criar/listar/revogar: administradores do cliente. Validar/aceitar:
/// anônimo (o token é a credencial; expira e só vale uma vez).
/// </summary>
[ApiController]
[Route("api/equipe/convites")]
[Authorize(Roles = "ADMINISTRADOR,ADMINISTRADOR_CLIENTE,DIRETOR")]
[Tags("Equipe - convites")]
public sealed class ConvitesEquipeController : ControllerBase
{
    private readonly ConvitesEquipeService _service;
    public ConvitesEquipeController(ConvitesEquipeService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarConviteEquipeRequest request, CancellationToken ct)
    {
        var r = await _service.CriarAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var r = await _service.ListarAsync(ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost("{id:guid}/revogar")]
    public async Task<IActionResult> Revogar(Guid id, CancellationToken ct)
    {
        var r = await _service.RevogarAsync(id, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return StatusCode(r.StatusCode, r);
    }
}

/// <summary>B5: ponta pública do convite (validação + aceite por token).</summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/convites")]
[Tags("Convite público")]
public sealed class ConvitePublicoController : ControllerBase
{
    private readonly ConvitesEquipeService _service;
    public ConvitePublicoController(ConvitesEquipeService service) => _service = service;

    [HttpGet("{token}")]
    public async Task<IActionResult> Validar(string token, CancellationToken ct)
    {
        var r = await _service.ValidarAsync(token, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost("{token}/aceitar")]
    public async Task<IActionResult> Aceitar(string token, [FromBody] AceitarConviteRequest request, CancellationToken ct)
    {
        var r = await _service.AceitarAsync(token, request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return StatusCode(r.StatusCode, r);
    }
}

/// <summary>B5: provisionamento manual B2B sobre o mesmo núcleo do self-service.</summary>
[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
[Route("api/admin-saas/provisionar-cliente")]
[Tags("Administração MNSOFT - provisionamento")]
public sealed class ProvisionamentoController : ControllerBase
{
    private readonly SelfServiceSaasService _service;
    public ProvisionamentoController(SelfServiceSaasService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Provisionar([FromBody] ProvisionarClienteRequest request, CancellationToken ct)
    {
        var r = await _service.ProvisionarManualAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString());
        return StatusCode(r.StatusCode, r);
    }
}
