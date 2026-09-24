using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/caixa"), Authorize(Policy = "Adm360.Financeiro")]
public sealed class Adm360CaixaController : ControllerBase
{
    private readonly ICaixaRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360CaixaController> logger;

    public Adm360CaixaController(
        ICaixaRepository repository,
        ICurrentUserService current,
        ILogger<Adm360CaixaController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet("contas")]
    public async Task<IActionResult> ListarContas(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarContasAsync(tenant, ct);
        return Ok(lista);
    }

    [HttpGet("contas/{id:guid}")]
    public async Task<IActionResult> ObterConta(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var conta = await repository.ObterContaPorIdAsync(tenant, id, ct);
        if (conta is null) return NotFound(new { error = "Conta financeira não encontrada." });
        return Ok(conta);
    }

    [HttpPost("contas")]
    public async Task<IActionResult> CriarConta([FromBody] CriarContaFinanceiraCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.CriarContaAsync(tenant, user, command, ct);
        logger.LogInformation("Conta financeira {ContaId} criada para tenant {TenantId}.", id, tenant);
        return CreatedAtAction(nameof(ObterConta), new { id }, new { id });
    }

    [HttpGet("contas/{id:guid}/extrato")]
    public async Task<IActionResult> Extrato(
        Guid id, [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var extrato = await repository.ExtratoContaAsync(tenant, id, inicio, fim, ct);
        return Ok(extrato);
    }

    [HttpGet("fluxo")]
    public async Task<IActionResult> Fluxo(
        [FromQuery] DateOnly inicio, [FromQuery] DateOnly fim, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var fluxo = await repository.FluxoCaixaAsync(tenant, inicio, fim, ct);
        return Ok(fluxo);
    }
}
