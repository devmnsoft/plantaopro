using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/relatorios"), Authorize(Policy = "Adm360.Ver")]
public sealed class Adm360RelatoriosController : ControllerBase
{
    private readonly IAdm360RelatoriosRepository repository;
    private readonly ICurrentUserService current;

    public Adm360RelatoriosController(
        IAdm360RelatoriosRepository repository,
        ICurrentUserService current)
    {
        this.repository = repository;
        this.current = current;
    }

    private Guid Tenant =>
        current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado.");

    [HttpGet("vales-pendentes")]
    public async Task<IActionResult> ValesPendentes(CancellationToken ct)
    {
        var result = await repository.ValesPendentesAsync(Tenant, ct);
        return Ok(result);
    }

    [HttpGet("custodia-externa")]
    public async Task<IActionResult> CustodiaExterna(CancellationToken ct)
    {
        var result = await repository.CustodiaExternaAsync(Tenant, ct);
        return Ok(result);
    }

    [HttpGet("reconciliacao")]
    public async Task<IActionResult> Reconciliacao(CancellationToken ct)
    {
        var result = await repository.ReconciliacaoAsync(Tenant, ct);
        return Ok(result);
    }

    [HttpGet("rastreabilidade")]
    public async Task<IActionResult> Rastreabilidade([FromQuery] string? busca, CancellationToken ct)
    {
        var result = await repository.RastreabilidadeAsync(Tenant, busca, ct);
        return Ok(result);
    }
}
