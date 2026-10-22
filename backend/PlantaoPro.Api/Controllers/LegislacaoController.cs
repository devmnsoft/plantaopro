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
    private readonly ITributacaoPfService tributacao;
    private readonly IBateriaLegalService bateria;

    public LegislacaoController(IParametrosLegaisRepository repository, ITributacaoPfService tributacao, IBateriaLegalService bateria)
    {
        this.repository = repository;
        this.tributacao = tributacao;
        this.bateria = bateria;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? ano, CancellationToken ct) =>
        Ok(await repository.ListarAtivosAsync(ano, ct));

    // R90: bateria de contratos legais - todos os parametros ativos do ano agrupados
    // por tema (rota literal precede a parametrica {codigo}).
    [HttpGet("bateria")]
    public async Task<IActionResult> Bateria([FromQuery] int? ano, CancellationToken ct) =>
        Ok(await bateria.ObterBateriaAsync(ano, ct));

    [HttpGet("{codigo}")]
    public async Task<IActionResult> Obter(string codigo, [FromQuery] int? ano, CancellationToken ct)
    {
        var item = await repository.ObterPorCodigoAsync(codigo, ano, ct);
        return item is null
            ? NotFound(new { erro = "Parâmetro legal não encontrado para o filtro informado." })
            : Ok(item);
    }

    // R81: nucleo de tributacao PF com vetores oficiais da fonte unica.
    [HttpGet("tributacao-pf/tabela")]
    public async Task<IActionResult> TabelaTributacaoPf([FromQuery] int? ano, CancellationToken ct) =>
        Ok(await tributacao.ObterTabelaAsync(ano, ct));

    [HttpPost("tributacao-pf/calcular")]
    public async Task<IActionResult> CalcularTributacaoPf([FromBody] CalcularTributacaoPfCommand comando, CancellationToken ct)
    {
        try
        {
            return Ok(await tributacao.CalcularAsync(comando.Ano, comando.SalarioBruto, 0m, 0m, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }
}
