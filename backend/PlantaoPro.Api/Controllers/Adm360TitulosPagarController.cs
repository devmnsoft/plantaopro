using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/titulos-pagar")]
public sealed class Adm360TitulosPagarController : ControllerBase
{
    private readonly IContasPagarRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360TitulosPagarController> logger;

    public Adm360TitulosPagarController(
        IContasPagarRepository repository,
        ICurrentUserService current,
        ILogger<Adm360TitulosPagarController> logger)
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
        [FromQuery] Guid? fornecedorId,
        [FromQuery] string? centroCusto,
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarAsync(tenant, busca, situacao, fornecedorId, centroCusto, inicio, fim, ct);
        return Ok(lista);
    }

    [HttpGet("{id:guid}"), Authorize(Policy = "Adm360.Financeiro")]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var titulo = await repository.ObterPorIdAsync(tenant, id, ct);
        if (titulo is null) return NotFound(new { error = "Título a pagar não encontrado." });
        return Ok(titulo);
    }

    [HttpPost("despesa-manual"), Authorize(Policy = "Adm360.CriarDespesa")]
    public async Task<IActionResult> CriarDespesa([FromBody] CriarDespesaManualCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.CriarDespesaManualAsync(tenant, user, command, ct);
        logger.LogInformation("Despesa manual {TituloId} criada para o fornecedor {FornecedorId}.", id, command.FornecedorId);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }

    [HttpPost("{id:guid}/aprovar"), Authorize(Policy = "Adm360.AprovarDespesa")]
    public async Task<IActionResult> Aprovar(Guid id, [FromBody] AprovarTituloPagarCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        if (id != command.TituloId)
            command = command with { TituloId = id };

        await repository.AprovarAsync(tenant, user, command, ct);
        logger.LogInformation("Título a pagar {TituloId} aprovado.", id);
        return Ok(new { success = true });
    }

    [HttpPost("pagar"), Authorize(Policy = "Adm360.Pagar")]
    public async Task<IActionResult> Pagar([FromBody] PagarTituloCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var pagamentoId = await repository.PagarAsync(tenant, user, command, ct);
        logger.LogInformation("Pagamento manual {PagamentoId} registrado para o título {TituloId}.", pagamentoId, command.TituloId);
        return Ok(new { id = pagamentoId });
    }

    [HttpPost("estornar"), Authorize(Policy = "Adm360.Estornar")]
    public async Task<IActionResult> Estornar([FromBody] EstornarPagamentoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var estornoId = await repository.EstornarPagamentoAsync(tenant, user, command, ct);
        logger.LogInformation("Estorno de pagamento {EstornoId} registrado para o pagamento {PagamentoId}.", estornoId, command.PagamentoId);
        return Ok(new { id = estornoId });
    }

    [HttpGet("comissoes-pendentes"), Authorize(Policy = "Adm360.Financeiro")]
    public async Task<IActionResult> ListarComissoesPendentes(
        [FromQuery] Guid? vendedorId,
        [FromQuery] DateOnly? inicio,
        [FromQuery] DateOnly? fim,
        CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarComissoesPendentesAsync(tenant, vendedorId, inicio, fim, ct);
        return Ok(lista);
    }

    [HttpPost("gerar-de-comissao"), Authorize(Policy = "Adm360.CriarDespesa")]
    public async Task<IActionResult> GerarDeComissao([FromBody] GerarTituloPagarDeComissaoCommand command, CancellationToken ct)
    {
        var (tenant, user) = Context();
        var id = await repository.GerarDeComissaoAsync(tenant, user, command, ct);
        logger.LogInformation("Título a pagar {TituloId} gerado a partir de comissões do vendedor {VendedorId}.", id, command.VendedorId);
        return CreatedAtAction(nameof(ObterPorId), new { id }, new { id });
    }
}
