using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/orcamentos"), Authorize(Policy = "Adm360.Cirurgias")]
public sealed class Orcamentos360Controller : ControllerBase
{
    private readonly IOrcamentoCirurgicoRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Orcamentos360Controller> logger;

    public Orcamentos360Controller(
        IOrcamentoCirurgicoRepository repository,
        ICurrentUserService current,
        ILogger<Orcamentos360Controller> logger)
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
        var orc = await repository.ObterPorIdAsync(tenant, id, ct);
        if (orc is null) return NotFound("Orçamento não encontrado.");
        return Ok(orc);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarOrcamentoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.CriarAsync(tenant, user, command, ct);
        logger.LogInformation("Orçamento cirúrgico {OrcamentoId} criado pelo usuário {UsuarioId} no tenant {TenantId}.", id, user, tenant);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarOrcamentoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        if (id != command.OrcamentoId) return BadRequest("ID da rota diverge do comando.");
        await repository.AtualizarAsync(tenant, user, command, ct);
        logger.LogInformation("Orçamento cirúrgico {OrcamentoId} atualizado/revisado pelo usuário {UsuarioId} no tenant {TenantId}.", id, user, tenant);
        return NoContent();
    }

    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var (tenant, user) = Context();
        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey.Trim();
        await repository.AprovarAsync(tenant, user, id, key, ct);
        logger.LogInformation("Orçamento cirúrgico {OrcamentoId} aprovado com chave {Key}.", id, key);
        return NoContent();
    }

    [HttpPost("{id:guid}/rejeitar")]
    public async Task<IActionResult> Rejeitar(Guid id, [FromForm] string motivo, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.RejeitarAsync(tenant, user, id, motivo, ct);
        logger.LogInformation("Orçamento cirúrgico {OrcamentoId} rejeitado. Motivo: {Motivo}.", id, motivo);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromForm] string motivo, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.CancelarAsync(tenant, user, id, motivo, ct);
        logger.LogInformation("Orçamento cirúrgico {OrcamentoId} cancelado. Motivo: {Motivo}.", id, motivo);
        return NoContent();
    }

    [HttpGet("{id:guid}/revisoes")]
    public async Task<IActionResult> Revisoes(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var revisoes = await repository.ObterRevisoesAsync(tenant, id, ct);
        return Ok(revisoes);
    }

    [HttpGet("{id:guid}/reserva-planejamento")]
    public async Task<IActionResult> PlanejamentoReserva(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var plano = await repository.ObterPlanejamentoReservaAsync(tenant, id, ct);
        if (plano is null) return NotFound("Orçamento não encontrado ou ainda não aprovado.");
        return Ok(plano);
    }

    [HttpPost("{id:guid}/reservas")]
    public async Task<IActionResult> ReservarItem(Guid id, [FromBody] ReservarCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        await repository.ReservarItemAsync(tenant, user, id, command, ct);
        logger.LogInformation("Material reservado para o orçamento cirúrgico {OrcamentoId}. Produto: {ProdutoId}, Lote: {LoteId}, Quantidade: {Quantidade}.", id, command.ProdutoId, command.LoteId, command.Quantidade);
        return NoContent();
    }

    [HttpDelete("{id:guid}/reservas/{reservaId:guid}")]
    public async Task<IActionResult> CancelarReserva(Guid id, Guid reservaId, [FromQuery] string? motivo, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var mot = string.IsNullOrWhiteSpace(motivo) ? "Cancelamento solicitado pelo usuário" : motivo.Trim();
        await repository.CancelarReservaAsync(tenant, user, id, reservaId, mot, ct);
        logger.LogInformation("Reserva {ReservaId} do orçamento cirúrgico {OrcamentoId} cancelada.", reservaId, id);
        return NoContent();
    }
}
