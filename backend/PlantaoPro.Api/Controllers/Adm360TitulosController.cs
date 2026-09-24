using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/titulos")]
public sealed class Adm360TitulosController : ControllerBase
{
    private readonly IContasReceberRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360TitulosController> logger;

    public Adm360TitulosController(
        IContasReceberRepository repository,
        ICurrentUserService current,
        ILogger<Adm360TitulosController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet, Authorize(Policy = "Adm360.Financeiro")]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busca,
        [FromQuery] string? situacao,
        [FromQuery] Guid? pagadorId,
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarAsync(tenant, busca, situacao, pagadorId, inicio, fim, ct);
        return Ok(lista);
    }

    [HttpGet("{id:guid}"), Authorize(Policy = "Adm360.Financeiro")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var titulo = await repository.ObterPorIdAsync(tenant, id, ct);
        if (titulo is null) return NotFound(new { error = "Título a receber não encontrado." });
        return Ok(titulo);
    }

    [HttpPost("receber"), Authorize(Policy = "Adm360.Receber")]
    public async Task<IActionResult> Receber([FromBody] ReceberTituloCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var baixaId = await repository.ReceberAsync(tenant, user, command, ct);
        logger.LogInformation("Baixa {BaixaId} registrada para o título {TituloId}.", baixaId, command.TituloId);
        return Ok(new { id = baixaId });
    }

    [HttpPost("estornar"), Authorize(Policy = "Adm360.Estornar")]
    public async Task<IActionResult> Estornar([FromBody] EstornarBaixaCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var estornoId = await repository.EstornarAsync(tenant, user, command, ct);
        logger.LogInformation("Estorno {EstornoId} registrado para a baixa {BaixaId}.", estornoId, command.BaixaId);
        return Ok(new { id = estornoId });
    }
}
