using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/vendas")]
public sealed class Adm360VendasController : ControllerBase
{
    private readonly IVendaRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360VendasController> logger;

    public Adm360VendasController(
        IVendaRepository repository,
        ICurrentUserService current,
        ILogger<Adm360VendasController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet, Authorize(Policy = "Adm360.Vendas")]
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

    [HttpGet("{id:guid}"), Authorize(Policy = "Adm360.Vendas")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var venda = await repository.ObterPorIdAsync(tenant, id, ct);
        if (venda is null) return NotFound(new { error = "Venda não encontrada." });
        return Ok(venda);
    }

    [HttpPost("confirmar"), Authorize(Policy = "Adm360.Vendas")]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarVendaCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.ConfirmarVendaAsync(tenant, user, command, ct);
        logger.LogInformation("Venda {VendaId} confirmada a partir da valorização {ValorizacaoId}.", id, command.ValorizacaoId);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpPost("{id:guid}/cancelar"), Authorize(Policy = "Adm360.Vendas")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] string motivo, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.CancelarVendaAsync(tenant, user, id, motivo, ct);
        logger.LogInformation("Venda {VendaId} cancelada pelo usuário {UsuarioId}.", id, user);
        return NoContent();
    }
}
