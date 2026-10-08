using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// B4: decisão B2B das solicitações de plano (upgrade/downgrade/cancelamento).
/// Somente MNSOFT global; transições idempotentes (só SOLICITADO /
/// CANCELAMENTO_SOLICITADO decidem; repetição devolve o estado atual).
/// </summary>
[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
[Route("api/admin-saas/solicitacoes-planos")]
[Tags("Administração MNSOFT - solicitações de plano")]
public sealed class SolicitacoesPlanosController : ControllerBase
{
    private readonly SolicitacoesPlanosService _service;
    public SolicitacoesPlanosController(SolicitacoesPlanosService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? tipo, [FromQuery] string? status, CancellationToken ct)
    {
        var r = await _service.ListarAsync(tipo, status, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost("upgrade/{id:guid}/aprovar")]
    public async Task<IActionResult> AprovarUpgrade(Guid id, [FromBody] JustificativaRequest? request, CancellationToken ct)
        => await Decidir("UPGRADE", id, true, request?.Justificativa, ct);

    [HttpPost("upgrade/{id:guid}/recusar")]
    public async Task<IActionResult> RecusarUpgrade(Guid id, [FromBody] JustificativaRequest? request, CancellationToken ct)
        => await Decidir("UPGRADE", id, false, request?.Justificativa, ct);

    [HttpPost("downgrade/{id:guid}/aprovar")]
    public async Task<IActionResult> AprovarDowngrade(Guid id, [FromBody] JustificativaRequest? request, CancellationToken ct)
        => await Decidir("DOWNGRADE", id, true, request?.Justificativa, ct);

    [HttpPost("downgrade/{id:guid}/recusar")]
    public async Task<IActionResult> RecusarDowngrade(Guid id, [FromBody] JustificativaRequest? request, CancellationToken ct)
        => await Decidir("DOWNGRADE", id, false, request?.Justificativa, ct);

    [HttpPost("cancelamento/{id:guid}/aprovar")]
    public async Task<IActionResult> AprovarCancelamento(Guid id, [FromBody] JustificativaRequest? request, CancellationToken ct)
        => await Decidir("CANCELAMENTO", id, true, request?.Justificativa, ct);

    [HttpPost("cancelamento/{id:guid}/recusar")]
    public async Task<IActionResult> RecusarCancelamento(Guid id, [FromBody] JustificativaRequest? request, CancellationToken ct)
        => await Decidir("CANCELAMENTO", id, false, request?.Justificativa, ct);

    private async Task<IActionResult> Decidir(string tipo, Guid id, bool aprovar, string? justificativa, CancellationToken ct)
    {
        var r = await _service.DecidirAsync(tipo, id, aprovar, justificativa, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return StatusCode(r.StatusCode, r);
    }
}
