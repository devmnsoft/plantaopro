using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Api.Data;

namespace PlantaoPro.Api.Controllers;

/// <summary>
/// R84 (bloque B4): calendario fiscal/obrigacoes legais por tenant. GET retorna o calendario
/// do periodo com datas oficiais da fonte unica e o estado de reminder contra a data atual.
/// POSTs alteram o estado por tenant (ativa/desativa por periodo e marca de conclusao).
/// Sem organizacao na sessao retorna 401; validacao/parametros fora da faixa retornam 400
/// com a mensagem direta.
/// R100: as notificacoes sao pautadas contra este calendario — GET calendario pauta em melhor
/// esforço antes de responder, GET alertas devolve os SLAs do periodo e POST notificacoes/gerar
/// e o gatilho explicito (idempotente pela chave canonica da caixa).
/// </summary>
[ApiController]
[Route("api/calendario-fiscal")]
[Authorize]
public sealed class CalendarioFiscalController : ControllerBase
{
    private readonly ICalendarioFiscalService calendario;
    private readonly ICalendarioFiscalAlertasService alertas;
    private readonly ICurrentUserService currentUser;

    public CalendarioFiscalController(ICalendarioFiscalService calendario, ICalendarioFiscalAlertasService alertas, ICurrentUserService currentUser)
    {
        this.calendario = calendario;
        this.alertas = alertas;
        this.currentUser = currentUser;
    }

    private Guid Tenant() => currentUser.TenantId ?? throw new UnauthorizedAccessException("Selecione uma organizacao.");

    [HttpGet("calendario")]
    public async Task<IActionResult> Calendario([FromQuery] int mes, [FromQuery] int ano, CancellationToken ct)
    {
        try
        {
            // Pauta em melhor esforço (mesmo padrão da reavaliação lazy): a leitura nunca depende dela.
            try { await alertas.GerarAsync(Tenant(), mes, ano, DateOnly.FromDateTime(DateTime.Today), ct); } catch { }
            return Ok(await calendario.ObterCalendarioAsync(Tenant(), mes, ano, DateOnly.FromDateTime(DateTime.Today), ct));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { erro = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken ct)
    {
        try
        {
            return Ok(await alertas.GerarAsync(Tenant(), mes, ano, DateOnly.FromDateTime(DateTime.Today), ct));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { erro = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("notificacoes/gerar")]
    public async Task<IActionResult> GerarNotificacoes([FromQuery] int? mes, [FromQuery] int? ano, CancellationToken ct)
    {
        try
        {
            return Ok(await alertas.GerarAsync(Tenant(), mes, ano, DateOnly.FromDateTime(DateTime.Today), ct));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { erro = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("alterar-ativa")]
    public async Task<IActionResult> AlterarAtiva([FromBody] AlterarAtivaObrigacaoCommand comando, CancellationToken ct)
    {
        try
        {
            return Ok(await calendario.AlterarAtivaAsync(comando, Tenant(), DateOnly.FromDateTime(DateTime.Today), ct));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { erro = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }

    [HttpPost("alternar-conclusao")]
    public async Task<IActionResult> AlternarConclusao([FromBody] AlternarConclusaoObrigacaoCommand comando, CancellationToken ct)
    {
        try
        {
            var usuario = User.Identity?.Name ?? string.Empty;
            return Ok(await calendario.AlternarConclusaoAsync(comando, Tenant(), usuario, DateOnly.FromDateTime(DateTime.Today), ct));
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { erro = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { erro = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { erro = ex.Message }); }
    }
}
