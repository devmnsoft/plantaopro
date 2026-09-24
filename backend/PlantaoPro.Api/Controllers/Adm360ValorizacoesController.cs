using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/valorizacoes")]
public sealed class Adm360ValorizacoesController : ControllerBase
{
    private readonly IValorizacaoRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360ValorizacoesController> logger;

    public Adm360ValorizacoesController(
        IValorizacaoRepository repository,
        ICurrentUserService current,
        ILogger<Adm360ValorizacoesController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet("pendentes"), Authorize(Policy = "Adm360.Valorizar")]
    public async Task<IActionResult> ListarPendentes(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarValesPendentesAsync(tenant, ct);
        return Ok(lista);
    }

    [HttpGet("previa/{valeId:guid}"), Authorize(Policy = "Adm360.Valorizar")]
    public async Task<IActionResult> ObterPrevia(Guid valeId, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var previa = await repository.ObterPreviaAsync(tenant, valeId, ct);
        if (previa is null) return NotFound(new { error = "Vale não encontrado para valorização." });
        return Ok(previa);
    }

    [HttpPost, Authorize(Policy = "Adm360.Valorizar")]
    public async Task<IActionResult> Valorizar([FromBody] ValorizarValeCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.ValorizarAsync(tenant, user, command, ct);
        logger.LogInformation("Vale {ValeId} valorizado com ID {ValorizacaoId} para tenant {TenantId}.", command.ValeId, id, tenant);
        return Ok(new { id });
    }
}
