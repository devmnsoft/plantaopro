using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/vales"), Authorize(Policy = "Adm360.Cirurgias")]
public sealed class Adm360ValesController : ControllerBase
{
    private readonly IValeConsignacaoRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360ValesController> logger;

    public Adm360ValesController(
        IValeConsignacaoRepository repository,
        ICurrentUserService current,
        ILogger<Adm360ValesController> logger)
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
        var vale = await repository.ObterPorIdAsync(tenant, id, ct);
        if (vale is null) return NotFound(new { error = "Vale de consignação não encontrado." });
        return Ok(vale);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarValeCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.CriarAsync(tenant, user, command, ct);
        logger.LogInformation("Vale de consignação {ValeId} criado para tenant {TenantId}.", id, tenant);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpPost("{id:guid}/separar-item")]
    public async Task<IActionResult> SepararItem(Guid id, [FromBody] SepararItemValeCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.SepararItemAsync(tenant, user, id, command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/concluir-separacao")]
    public async Task<IActionResult> ConcluirSeparacao(Guid id, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.ConcluirSeparacaoAsync(tenant, user, id, ct);
        logger.LogInformation("Separação do vale {ValeId} concluída para tenant {TenantId}.", id, tenant);
        return NoContent();
    }

    [HttpPost("{id:guid}/expedir")]
    public async Task<IActionResult> Expedir(Guid id, [FromBody] ExpedirValeCommand command, CancellationToken ct)
    {
        if (id != command.ValeId)
            return BadRequest(new { error = "Identificador da rota difere do corpo da requisição." });

        var (tenant, user) = Context();
        await repository.ExpedirAsync(tenant, user, command, ct);
        logger.LogInformation("Vale {ValeId} expedido com sucesso para tenant {TenantId}.", id, tenant);
        return NoContent();
    }

    [HttpPost("{id:guid}/consumo")]
    public async Task<IActionResult> RegistrarConsumo(Guid id, [FromBody] RegistrarConsumoValeCommand command, CancellationToken ct)
    {
        if (id != command.ValeId)
            return BadRequest(new { error = "Identificador da rota difere do corpo da requisição." });

        var (tenant, user) = Context();
        await repository.RegistrarConsumoAsync(tenant, user, command, ct);
        logger.LogInformation("Consumo registrado para item {ItemId} do vale {ValeId}.", command.ValeItemId, id);
        return NoContent();
    }

    [HttpPost("{id:guid}/retorno")]
    public async Task<IActionResult> RegistrarRetorno(Guid id, [FromBody] RegistrarRetornoValeCommand command, CancellationToken ct)
    {
        if (id != command.ValeId)
            return BadRequest(new { error = "Identificador da rota difere do corpo da requisição." });

        var (tenant, user) = Context();
        await repository.RegistrarRetornoAsync(tenant, user, command, ct);
        logger.LogInformation("Retorno registrado para item {ItemId} do vale {ValeId}.", command.ValeItemId, id);
        return NoContent();
    }

    [HttpPost("{id:guid}/perda")]
    public async Task<IActionResult> RegistrarPerda(Guid id, [FromBody] RegistrarPerdaValeCommand command, CancellationToken ct)
    {
        if (id != command.ValeId)
            return BadRequest(new { error = "Identificador da rota difere do corpo da requisição." });

        var (tenant, user) = Context();
        await repository.RegistrarPerdaAsync(tenant, user, command, ct);
        logger.LogInformation("Perda/avaria registrada para item {ItemId} do vale {ValeId}.", command.ValeItemId, id);
        return NoContent();
    }

    [HttpPost("{id:guid}/reconciliar")]
    public async Task<IActionResult> Reconciliar(Guid id, [FromBody] ReconciliarValeCommand command, CancellationToken ct)
    {
        if (id != command.ValeId)
            return BadRequest(new { error = "Identificador da rota difere do corpo da requisição." });

        var (tenant, user) = Context();
        await repository.ReconciliarAsync(tenant, user, command, ct);
        logger.LogInformation("Vale {ValeId} reconciliado com sucesso para tenant {TenantId}.", id, tenant);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarValeCommand command, CancellationToken ct)
    {
        if (id != command.ValeId)
            return BadRequest(new { error = "Identificador da rota difere do corpo da requisição." });

        var (tenant, user) = Context();
        await repository.CancelarAsync(tenant, user, id, command.Motivo, ct);
        logger.LogInformation("Vale {ValeId} cancelado para tenant {TenantId}.", id, tenant);
        return NoContent();
    }
}
