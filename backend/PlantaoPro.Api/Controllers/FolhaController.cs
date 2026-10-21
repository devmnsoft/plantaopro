using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Folha;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// R82 (bloque B4): folha de pagamento PF em linguagem leiga. Calculos de referencia
/// com parametros legais da fonte unica (meses com 15+ dias contam cheios, HE +50%,
/// deducoes, FGTS, multa de 40%). Leitura/cálculo sem efeito colateral — qualquer
/// usuario autenticado pode simular.
/// </summary>
[ApiController]
[Route("api/folha")]
[Authorize]
public sealed class FolhaController : ControllerBase
{
    private readonly IFolhaPfService folha;

    public FolhaController(IFolhaPfService folha) => this.folha = folha;

    [HttpPost("mes")]
    public async Task<IActionResult> CalcularMes([FromBody] CalcularMesCommand comando, CancellationToken ct)
    {
        try { return Ok(await folha.CalcularMesAsync(comando, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("decimo-terceiro")]
    public async Task<IActionResult> CalcularDecimoTerceiro([FromBody] CalcularDecimoTerceiroCommand comando, CancellationToken ct)
    {
        try { return Ok(await folha.CalcularDecimoTerceiroAsync(comando, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("ferias")]
    public async Task<IActionResult> CalcularFerias([FromBody] CalcularFeriasCommand comando, CancellationToken ct)
    {
        try { return Ok(await folha.CalcularFeriasAsync(comando, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("rescisao")]
    public async Task<IActionResult> CalcularRescisao([FromBody] CalcularRescisaoCommand comando, CancellationToken ct)
    {
        try { return Ok(await folha.CalcularRescisaoAsync(comando, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }
}
