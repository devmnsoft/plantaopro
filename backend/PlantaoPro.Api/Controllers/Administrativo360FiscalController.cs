using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// Fiscal ADM360 (R5-A2): unico caminho de escrita das pre-notas e parametros.
/// Autorizacao POR ACAO (Adm360.Ver/Configurar/Criar/Editar/Reabrir/Confirmar/
/// Cancelar); auditor passa so nas leituras (VER). Erros de negocio viram 400/403/
/// 404 via Adm360BusinessExceptionFilter; ENVIANDO so com conector real (P1).
/// </summary>
[ApiController]
[Route("api/administrativo360/fiscal")]
public sealed class Administrativo360FiscalController : ControllerBase
{
    private readonly Administrativo360FiscalService service;
    private readonly ILogger<Administrativo360FiscalController> logger;

    public Administrativo360FiscalController(
        Administrativo360FiscalService service,
        ILogger<Administrativo360FiscalController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet("parametros")]
    [Authorize(Policy = "Adm360.Ver")]
    public async Task<IActionResult> ObterParametros(CancellationToken ct) =>
        Ok(await service.ObterParametrosAsync(ct));

    [HttpPost("parametros")]
    [Authorize(Policy = "Adm360.Configurar")]
    public async Task<IActionResult> SalvarParametros([FromBody] SalvarParametrosFiscaisCommand comando, CancellationToken ct) =>
        Ok(await service.SalvarParametrosAsync(comando, ct));

    [HttpGet("notas")]
    [Authorize(Policy = "Adm360.Ver")]
    public async Task<IActionResult> ListarNotas([FromQuery] string? situacao, CancellationToken ct) =>
        Ok(await service.ListarNotasAsync(situacao, ct));

    [HttpGet("notas/{id:guid}")]
    [Authorize(Policy = "Adm360.Ver")]
    public async Task<IActionResult> ObterNota(Guid id, CancellationToken ct) =>
        Ok(await service.ObterNotaAsync(id, ct));

    [HttpPost("notas")]
    [Authorize(Policy = "Adm360.Criar")]
    public async Task<IActionResult> CriarNota([FromBody] CriarNotaPreEmitidaCommand comando, CancellationToken ct)
    {
        var id = await service.CriarNotaAsync(comando, ct);
        logger.LogInformation("Pré-nota criada via API fiscal. Nota:{NotaId}", id);
        return Ok(new { id });
    }

    [HttpPost("notas/{id:guid}/marcar-pronta")]
    [Authorize(Policy = "Adm360.Editar")]
    public async Task<IActionResult> MarcarPronta(Guid id, CancellationToken ct) =>
        Ok(await service.MarcarProntaAsync(id, ct));

    [HttpPost("notas/{id:guid}/reabrir")]
    [Authorize(Policy = "Adm360.Reabrir")]
    public async Task<IActionResult> Reabrir(Guid id, CancellationToken ct) =>
        Ok(await service.ReabrirAsync(id, ct));

    [HttpPost("notas/{id:guid}/cancelar")]
    [Authorize(Policy = "Adm360.Cancelar")]
    public async Task<IActionResult> CancelarNota(Guid id, [FromBody] CancelarNotaPreEmitidaCommand comando, CancellationToken ct) =>
        Ok(await service.CancelarAsync(id, comando?.Motivo, ct));

    [HttpPost("notas/{id:guid}/emitir")]
    [Authorize(Policy = "Adm360.Confirmar")]
    public async Task<IActionResult> Emitir(Guid id, CancellationToken ct) =>
        Ok(await service.EmitirAsync(id, ct));
}
