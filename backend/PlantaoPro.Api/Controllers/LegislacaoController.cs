using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// R80 (bloque B4 - legislacao brasileira): consulta da fonte unica de parametros
/// legais (v2363_parametros_legais). Leitura para qualquer usuario autenticado:
/// referencia juridica por pais/ano, nao dado do tenant. Blocos seguintes
/// (R81 tributacao PF, R83 Simples, R84 calendario fiscal) leem daqui.
/// </summary>
[ApiController]
[Route("api/legislacao/parametros")]
[Authorize]
public sealed class LegislacaoController : ControllerBase
{
    private readonly IParametrosLegaisRepository repository;

    public LegislacaoController(IParametrosLegaisRepository repository) => this.repository = repository;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? ano, CancellationToken ct) =>
        Ok(await repository.ListarAtivosAsync(ano, ct));

    [HttpGet("{codigo}")]
    public async Task<IActionResult> Obter(string codigo, [FromQuery] int? ano, CancellationToken ct)
    {
        var item = await repository.ObterPorCodigoAsync(codigo, ano, ct);
        return item is null
            ? NotFound(new { erro = "Parâmetro legal não encontrado para o filtro informado." })
            : Ok(item);
    }
}
