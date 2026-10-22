using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

// ============================================================================
// R88 (trabalhista / eSocial): superficie da previa trabalhista — catalogo
// oficial dos eventos eSocial S-1000 a S-1299 (leiaute S-1.3), registro em
// previa de eventos por colaborador, analise de horas/jornada semanal contra a
// carga contratual e o teto legal de 44h (CF/88 art. 7, XIII), estimativa de
// horas extras (CLT art. 59 §1º sobre salario/220h) e calculo do aviso previo
// proporcional (Lei 12.506/2011).
// ============================================================================

[ApiController]
[Route("api/esocial-trabalhista")]
[Authorize]
[Tags("eSocial Trabalhista")]
public sealed class EsocialTrabalhoController : ControllerBase
{
    private readonly EsocialTrabalhoService _service;
    private readonly ILogger<EsocialTrabalhoController> _logger;
    public EsocialTrabalhoController(EsocialTrabalhoService service, ILogger<EsocialTrabalhoController> logger) { _service = service; _logger = logger; }

    /// <summary>Catálogo oficial dos eventos eSocial da faixa S-1000 a S-1299 (leiaute S-1.3).</summary>
    [HttpGet("catalogo")]
    public async Task<IActionResult> Catalogo(CancellationToken ct)
    {
        var r = await _service.CatalogoAsync(TenantId(), ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Colaboradores com contexto trabalhista (contrato vigente, tipo/salário/carga, primeira admissão).</summary>
    [HttpGet("colaboradores")]
    public async Task<IActionResult> Colaboradores(CancellationToken ct)
    {
        var r = await _service.ColaboradoresAsync(TenantId(), ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Jornada semanal de um colaborador no mês (AAAA-MM): semanas, flags e estimativa de horas extras.</summary>
    [HttpGet("jornada")]
    public async Task<IActionResult> Jornada([FromQuery] Guid colaboradorId, [FromQuery] string mes, CancellationToken ct)
    {
        var r = await _service.JornadaAsync(TenantId(), colaboradorId, mes, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Declara/atualiza as horas de uma semana (semana normalizada à segunda-feira).</summary>
    [HttpPost("jornadas/declarar")]
    public async Task<IActionResult> DeclararJornada([FromBody] EsocialJornadaDeclararCommand cmd, CancellationToken ct)
    {
        var r = await _service.DeclararJornadaAsync(TenantId(), UserId(), cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Remove uma declaração semanal.</summary>
    [HttpPost("jornadas/excluir")]
    public async Task<IActionResult> ExcluirJornada([FromBody] EsocialExcluirCommand cmd, CancellationToken ct)
    {
        var r = await _service.ExcluirJornadaAsync(TenantId(), cmd.Id, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Aviso prévio proporcional ao tempo de serviço (base 30 dias + 3/ano até o teto de 90).</summary>
    [HttpPost("aviso-previo")]
    public async Task<IActionResult> AvisoPrevio([FromBody] EsocialAvisoPrevioCommand cmd, CancellationToken ct)
    {
        var r = await _service.AvisoPrevioAsync(TenantId(), cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Eventos registrados em prévia (filtro opcional por colaborador e período AAAA-MM).</summary>
    [HttpGet("eventos")]
    public async Task<IActionResult> Eventos([FromQuery] Guid? colaboradorId, [FromQuery] string? periodoApuracao, CancellationToken ct)
    {
        var r = await _service.EventosAsync(TenantId(), colaboradorId, periodoApuracao, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Registra um evento da faixa S-1000 a S-1299 em prévia (por colaborador ou institucional).</summary>
    [HttpPost("eventos/declarar")]
    public async Task<IActionResult> DeclararEvento([FromBody] EsocialEventoDeclararCommand cmd, CancellationToken ct)
    {
        var r = await _service.DeclararEventoAsync(TenantId(), UserId(), cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Remove um evento registrado em prévia.</summary>
    [HttpPost("eventos/excluir")]
    public async Task<IActionResult> ExcluirEvento([FromBody] EsocialExcluirCommand cmd, CancellationToken ct)
    {
        var r = await _service.ExcluirEventoAsync(TenantId(), cmd.Id, ct);
        return StatusCode(r.StatusCode, r);
    }

    private Guid TenantId() => Guid.TryParse(User.FindFirstValue("ClienteId") ?? User.FindFirstValue("cliente_id"), out var id) ? id : Guid.Empty;
    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UsuarioId"), out var id) ? id : null;
}
