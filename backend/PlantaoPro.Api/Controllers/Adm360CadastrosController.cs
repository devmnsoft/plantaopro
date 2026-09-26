using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/administrativo360/cadastros"), Authorize(Policy = "Adm360.Ver")]
public sealed class Adm360CadastrosController : ControllerBase
{
    private readonly ICadastrosRepository repository;
    private readonly ICurrentUserService current;

    public Adm360CadastrosController(ICadastrosRepository repository, ICurrentUserService current)
    {
        this.repository = repository;
        this.current = current;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet("lookups")]
    public async Task<IActionResult> ObterLookups(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var bundle = await repository.ObterLookupsAsync(tenant, ct);
        return Ok(bundle);
    }

    [HttpGet("parceiros")]
    public async Task<IActionResult> ListarParceiros([FromQuery] string? busca, [FromQuery] bool? fornecedor, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarParceirosAsync(tenant, busca, fornecedor, ct);
        return Ok(lista);
    }

    public sealed record SalvarParceiroRequest(Guid? Id, string Nome, string? Documento, bool Fornecedor, bool Ativo);

    [HttpPost("parceiros"), Authorize(Policy = "Adm360.MapearCadastros")]
    public async Task<IActionResult> SalvarParceiro([FromBody] SalvarParceiroRequest req, CancellationToken ct)
    {
        var (tenant, _) = Context();
        try
        {
            var id = await repository.SalvarParceiroAsync(tenant, req.Id, req.Nome, req.Documento, req.Fornecedor, req.Ativo, ct);
            return Ok(new { id });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("parceiros/{id:guid}/status"), Authorize(Policy = "Adm360.MapearCadastros")]
    public async Task<IActionResult> AlternarStatusParceiro(Guid id, [FromQuery] bool ativo, CancellationToken ct)
    {
        var (tenant, _) = Context();
        try
        {
            await repository.AlternarStatusParceiroAsync(tenant, id, ativo, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("produtos")]
    public async Task<IActionResult> ListarProdutos([FromQuery] string? busca, [FromQuery] bool apenasAtivos = false, CancellationToken ct = default)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarProdutosAsync(tenant, busca, apenasAtivos, ct);
        return Ok(lista);
    }

    public sealed record SalvarProdutoRequest(Guid? Id, string Sku, string Nome, string Unidade, string? CodigoBarras, bool ControlaLote, bool ExigeInspecao, decimal PrecoCusto, bool Ativo);

    [HttpPost("produtos"), Authorize(Policy = "Adm360.MapearProdutos")]
    public async Task<IActionResult> SalvarProduto([FromBody] SalvarProdutoRequest req, CancellationToken ct = default)
    {
        var (tenant, _) = Context();
        try
        {
            var id = await repository.SalvarProdutoAsync(tenant, req.Id, req.Sku, req.Nome, req.Unidade, req.CodigoBarras, req.ControlaLote, req.ExigeInspecao, req.PrecoCusto, req.Ativo, ct);
            return Ok(new { id });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("produtos/{id:guid}/status"), Authorize(Policy = "Adm360.MapearProdutos")]
    public async Task<IActionResult> AlternarStatusProduto(Guid id, [FromQuery] bool ativo, CancellationToken ct = default)
    {
        var (tenant, _) = Context();
        try
        {
            await repository.AlternarStatusProdutoAsync(tenant, id, ativo, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("locais")]
    public async Task<IActionResult> ListarLocais([FromQuery] string? tipo, [FromQuery] bool apenasAtivos = false, CancellationToken ct = default)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarLocaisAsync(tenant, tipo, apenasAtivos, ct);
        return Ok(lista);
    }

    public sealed record SalvarLocalRequest(Guid? Id, string Codigo, string Nome, string Tipo, bool Ativo);

    [HttpPost("locais"), Authorize(Policy = "Adm360.MapearCadastros")]
    public async Task<IActionResult> SalvarLocal([FromBody] SalvarLocalRequest req, CancellationToken ct)
    {
        var (tenant, _) = Context();
        try
        {
            var id = await repository.SalvarLocalAsync(tenant, req.Id, req.Codigo, req.Nome, req.Tipo, req.Ativo, ct);
            return Ok(new { id });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("lotes")]
    public async Task<IActionResult> ListarLotes([FromQuery] Guid? produtoId, CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarLotesAsync(tenant, produtoId, ct);
        return Ok(lista);
    }
}
