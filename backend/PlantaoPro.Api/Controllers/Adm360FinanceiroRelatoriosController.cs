using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/relatorios-financeiros"), Authorize(Policy = "Adm360.Financeiro")]
public sealed class Adm360FinanceiroRelatoriosController : ControllerBase
{
    private readonly IAdm360FinanceiroRelatoriosRepository repository;
    private readonly ICurrentUserService current;

    public Adm360FinanceiroRelatoriosController(
        IAdm360FinanceiroRelatoriosRepository repository,
        ICurrentUserService current)
    {
        this.repository = repository;
        this.current = current;
    }

    private Guid Tenant() =>
        current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado.");

    [HttpGet("vendas")]
    public async Task<IActionResult> Vendas(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        // s9 (rodada 9 r9f): filtro por CLIENTE comercial (renomeado de hospitalId); cross-tenant → 400.
        [FromQuery] Guid? clienteId,
        [FromQuery] Guid? vendedorId,
        CancellationToken ct)
    {
        var lista = await repository.RelatorioVendasAsync(Tenant(), inicio, fim, clienteId, vendedorId, ct);
        return Ok(lista);
    }

    [HttpGet("comissoes")]
    public async Task<IActionResult> Comissoes(
        [FromQuery] Guid? vendedorId,
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken ct)
    {
        var lista = await repository.RelatorioComissoesAsync(Tenant(), vendedorId, inicio, fim, ct);
        return Ok(lista);
    }

    [HttpGet("margem")]
    public async Task<IActionResult> Margem(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        [FromQuery] Guid? clienteId,
        CancellationToken ct)
    {
        var lista = await repository.RelatorioMargemAsync(Tenant(), inicio, fim, ct, clienteId);
        return Ok(lista);
    }

    // s10 (rodada 9 r9f): vencimentos por parcela — período sobre data_vencimento; vencido na data do relatório.
    [HttpGet("vencimentos")]
    public async Task<IActionResult> Vencimentos(
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        [FromQuery] Guid? clienteId,
        CancellationToken ct)
    {
        var lista = await repository.RelatorioVencimentosAsync(Tenant(), inicio, fim, clienteId, ct);
        return Ok(lista);
    }
}
