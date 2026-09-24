using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/cirurgias"), Authorize(Policy = "Adm360.Cirurgias")]
public sealed class Adm360CirurgiasController : ControllerBase
{
    private readonly ICirurgiaRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360CirurgiasController> logger;

    public Adm360CirurgiasController(
        ICirurgiaRepository repository,
        ICurrentUserService current,
        ILogger<Adm360CirurgiasController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] string? situacao,
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarAsync(tenant, busca, situacao, inicio, fim, ct);
        return Ok(lista);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var cirurgia = await repository.ObterPorIdAsync(tenant, id, ct);
        if (cirurgia is null) return NotFound(new { error = "Cirurgia não encontrada." });
        return Ok(cirurgia);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarCirurgiaCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.CriarAsync(tenant, user, command, ct);
        logger.LogInformation("Cirurgia {CirurgiaId} criada com sucesso para tenant {TenantId}.", id, tenant);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarCirurgiaCommand command, CancellationToken ct)
    {
        if (id != command.CirurgiaId)
            return BadRequest(new { error = "Identificador da rota difere do corpo da requisição." });

        var (tenant, user) = Context();
        await repository.AtualizarAsync(tenant, user, command, ct);
        logger.LogInformation("Cirurgia {CirurgiaId} atualizada para tenant {TenantId}.", id, tenant);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarCirurgiaCommand command, CancellationToken ct)
    {
        if (id != command.CirurgiaId)
            return BadRequest(new { error = "Identificador da rota difere do corpo da requisição." });

        var (tenant, user) = Context();
        await repository.CancelarAsync(tenant, user, id, command.Motivo, ct);
        logger.LogInformation("Cirurgia {CirurgiaId} cancelada para tenant {TenantId}.", id, tenant);
        return NoContent();
    }
}
