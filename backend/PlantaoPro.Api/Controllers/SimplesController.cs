using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// R83 (bloque B4): Simples Nacional. Leitura da tabela oficial vigente (api/simples/tabela)
/// e previa do DAS campo a campo (api/simples/calcular-das), com vetores da fonte unica.
/// Leitura/calculo sem efeito colateral — qualquer usuario autenticado pode consultar/simular.
/// </summary>
[ApiController]
[Route("api/simples")]
[Authorize]
public sealed class SimplesController : ControllerBase
{
    private readonly ISimplesNacionalService simples;

    public SimplesController(ISimplesNacionalService simples) => this.simples = simples;

    [HttpGet("tabela")]
    public async Task<IActionResult> Tabela([FromQuery] int? ano, CancellationToken ct)
    {
        try { return Ok(await simples.ObterTabelaAsync(ano, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("calcular-das")]
    public async Task<IActionResult> CalcularDas([FromBody] PreviaDasCommand comando, CancellationToken ct)
    {
        try { return Ok(await simples.GerarPreviaDasAsync(comando, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }
}
