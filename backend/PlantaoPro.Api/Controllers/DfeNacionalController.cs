using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// R85 (bloco B4): DF-e nacional. GETs expõem a configuração institucional da fonte única
/// (catálogo das 27 UFs com cUF e documentos apoiados, layouts oficiais vigentes). POSTs geram
/// e validam a chave de acesso de 44 dígitos com dígito verificador módulo 11. Configuração é
/// global (sem tenant): exige apenas sessão autenticada; parâmetros fora da faixa ou modelo não
/// apoiado na UF retornam o resultado com os erros individuais (200), só falha de contrato vira
/// 400 com a mensagem direta.
/// </summary>
[ApiController]
[Route("api/dfe-nacional")]
[Authorize]
public sealed class DfeNacionalController : ControllerBase
{
    private readonly IDfeNacionalService dfe;

    public DfeNacionalController(IDfeNacionalService dfe)
    {
        this.dfe = dfe;
    }

    [HttpGet("ufs")]
    public async Task<IActionResult> Ufs(CancellationToken ct)
    {
        try
        {
            return Ok(await dfe.ListarUfsAsync(ct));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("layouts")]
    public async Task<IActionResult> Layouts(CancellationToken ct)
    {
        try
        {
            return Ok(await dfe.ListarLayoutsAsync(ct));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("chave-acesso/gerar")]
    public async Task<IActionResult> GerarChave([FromBody] GerarChaveCommand comando, CancellationToken ct)
    {
        try
        {
            return Ok(await dfe.GerarChaveAsync(comando, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("chave-acesso/validar")]
    public async Task<IActionResult> ValidarChave([FromBody] ValidarChaveCommand comando, CancellationToken ct)
    {
        try
        {
            return Ok(await dfe.ValidarChaveAsync(comando?.Chave ?? string.Empty, ct));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }
}
