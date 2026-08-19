using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Api.Controllers;

[ApiController, Route("api/meu-dia"), Authorize]
public sealed class MeuDiaController(IMeuDiaService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await service.ObterResumoAsync(ct));
    [HttpGet("indicadores")] public async Task<IActionResult> Indicators(CancellationToken ct) => Ok(await service.IndicadoresAsync(ct));
    [HttpGet("pendencias")] public async Task<IActionResult> Actions(CancellationToken ct) => Ok(await service.PendenciasAsync(ct));
    [HttpGet("agenda")] public async Task<IActionResult> Agenda(CancellationToken ct) => Ok(await service.AgendaAsync(ct));
    [HttpGet("alertas")] public IActionResult Alerts() => Ok(Array.Empty<object>());
    [HttpGet("acoes-rapidas")] public IActionResult QuickActions() => Ok(service.AcoesRapidas());

    [HttpPost("itens/{id:guid}/concluir"), HttpPost("itens/{id:guid}/reabrir")]
    public IActionResult UnsupportedMutation(Guid id) => StatusCode(StatusCodes.Status410Gone, new
    {
        error = "A ação deve ser concluída na entidade operacional de origem."
    });
}
