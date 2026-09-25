using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/administrativo360/gestao")]
public sealed class Adm360GestaoController : ControllerBase
{
    private readonly IGestaoDashboardRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360GestaoController> logger;

    public Adm360GestaoController(
        IGestaoDashboardRepository repository,
        ICurrentUserService current,
        ILogger<Adm360GestaoController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet("dashboard")]
    [Authorize(Policy = "Adm360.Ver")]
    public async Task<IActionResult> ObterDashboard(
        [FromQuery] Guid? estabelecimentoId,
        [FromQuery] string? provedor,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] string? situacao,
        CancellationToken ct)
    {
        var (tenant, _) = Context();
        var filtro = new FiltroDashboardDto(
            EstabelecimentoId: estabelecimentoId,
            Provedor: provedor,
            DataInicio: dataInicio,
            DataFim: dataFim,
            Situacao: situacao);

        var dto = await repository.ObterDashboardAsync(tenant, filtro, ct);
        return Ok(dto);
    }

    [HttpGet("capacidades-ativas")]
    [Authorize(Policy = "Adm360.Ver")]
    public async Task<IActionResult> ListarCapacidadesAtivas(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarCapacidadesAtivasAsync(tenant, ct);
        return Ok(lista);
    }
}
