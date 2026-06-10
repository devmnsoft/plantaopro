using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
public sealed class MedicosMeDisponibilidadeController : ControllerBase
{
    private readonly OperationalAutomationService service;
    public MedicosMeDisponibilidadeController(OperationalAutomationService service) { this.service = service; }
    private Guid Uid() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));

    [HttpGet("api/medicos/me/disponibilidade")]
    public async Task<IActionResult> Disponibilidade() { var r = await service.ListarDisponibilidadesAsync(Uid()); return StatusCode(r.StatusCode, r); }

    [HttpPost("api/medicos/me/disponibilidade")]
    public async Task<IActionResult> CriarDisponibilidade([FromBody] MedicoDisponibilidadeRequest request) { var r = await service.CriarDisponibilidadeAsync(Uid(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPut("api/medicos/me/disponibilidade/{id:guid}")]
    public async Task<IActionResult> AtualizarDisponibilidade(Guid id, [FromBody] MedicoDisponibilidadeRequest request) { var r = await service.AtualizarDisponibilidadeAsync(Uid(), id, request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpDelete("api/medicos/me/disponibilidade/{id:guid}")]
    public async Task<IActionResult> RemoverDisponibilidade(Guid id) { var r = await service.RemoverDisponibilidadeAsync(Uid(), id, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpGet("api/medicos/me/indisponibilidade")]
    public async Task<IActionResult> Indisponibilidade() { var r = await service.ListarIndisponibilidadesAsync(Uid()); return StatusCode(r.StatusCode, r); }

    [HttpPost("api/medicos/me/indisponibilidade")]
    public async Task<IActionResult> CriarIndisponibilidade([FromBody] MedicoIndisponibilidadeRequest request) { var r = await service.CriarIndisponibilidadeAsync(Uid(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpDelete("api/medicos/me/indisponibilidade/{id:guid}")]
    public async Task<IActionResult> RemoverIndisponibilidade(Guid id) { var r = await service.RemoverIndisponibilidadeAsync(Uid(), id, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpGet("api/medicos/me/preferencias")]
    public async Task<IActionResult> Preferencias() { var r = await service.ObterPreferenciasAsync(Uid()); return StatusCode(r.StatusCode, r); }

    [HttpPut("api/medicos/me/preferencias")]
    public async Task<IActionResult> AtualizarPreferencias([FromBody] MedicoPreferenciasRequest request) { var r = await service.AtualizarPreferenciasAsync(Uid(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.PlantoesGestao + "," + RolesConstants.EscalasGestao + "," + RolesConstants.Financeiro)]
[Route("api/coordenacao")]
public sealed class CoordenacaoDisponibilidadeController : ControllerBase
{
    private readonly OperationalAutomationService service;
    public CoordenacaoDisponibilidadeController(OperationalAutomationService service) { this.service = service; }

    [HttpGet("medicos-disponiveis")]
    public async Task<IActionResult> MedicosDisponiveis([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, [FromQuery] Guid? hospitalId, [FromQuery] Guid? especialidadeId)
    {
        var ctx = await service.ObterContextoUsuarioAsync(User);
        var r = await service.ListarMedicosDisponiveisAsync(ctx.ClienteId, dataInicio, dataFim, hospitalId, especialidadeId);
        return StatusCode(r.StatusCode, r);
    }
}

[ApiController]
[Authorize(Roles = RolesConstants.PlantoesGestao + "," + RolesConstants.EscalasGestao)]
[Route("api/central-escala")]
public sealed class CentralEscalaSugestoesFase4Controller : ControllerBase
{
    private readonly OperationalAutomationService service;
    public CentralEscalaSugestoesFase4Controller(OperationalAutomationService service) { this.service = service; }
    private Guid Uid() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));

    [HttpGet("plantoes/{plantaoId:guid}/sugestoes")]
    public async Task<IActionResult> Sugestoes(Guid plantaoId) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ObterSugestoesAsync(plantaoId, ctx.ClienteId); return StatusCode(r.StatusCode, r); }

    [HttpPost("plantoes/{plantaoId:guid}/gerar-sugestoes")]
    public async Task<IActionResult> Gerar(Guid plantaoId) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.GerarSugestaoAsync(plantaoId, ctx.UsuarioId, ctx.ClienteId, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("plantoes/{plantaoId:guid}/convidar-sugeridos")]
    public async Task<IActionResult> Convidar(Guid plantaoId) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ConvidarSugeridosAsync(plantaoId, Uid(), ctx.ClienteId, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("sugestoes/{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, [FromBody] SugestaoFeedbackRequest request) { var r = await service.RegistrarFeedbackSugestaoAsync(id, Uid(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/substituicoes")]
public sealed class SubstituicoesFase4Controller : ControllerBase
{
    private readonly OperationalAutomationService service;
    public SubstituicoesFase4Controller(OperationalAutomationService service) { this.service = service; }
    private Guid Uid() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));

    [HttpGet]
    public async Task<IActionResult> Listar() { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ListarSubstituicoesAsync(ctx.ClienteId); return StatusCode(r.StatusCode, r); }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ObterSubstituicaoAsync(id, ctx.ClienteId); return StatusCode(r.StatusCode, r); }

    [HttpPost("{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id, [FromBody] DecisaoSubstituicaoRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.MudarStatusSubstituicaoAsync(id, Uid(), ctx.ClienteId, "APROVAR", "APROVADA", request.Justificativa, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("{id:guid}/recusar")]
    public async Task<IActionResult> Recusar(Guid id, [FromBody] DecisaoSubstituicaoRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.MudarStatusSubstituicaoAsync(id, Uid(), ctx.ClienteId, "RECUSAR", "RECUSADA", request.Justificativa, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("{id:guid}/convidar-substituto")]
    public async Task<IActionResult> Convidar(Guid id, [FromBody] ConvidarSubstitutoRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ConvidarSubstitutoAsync(id, Uid(), ctx.ClienteId, request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("{id:guid}/confirmar-substituto")]
    public async Task<IActionResult> Confirmar(Guid id, [FromBody] ConfirmarSubstitutoRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ConfirmarSubstitutoAsync(id, Uid(), ctx.ClienteId, request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpGet("{id:guid}/historico")]
    public async Task<IActionResult> Historico(Guid id) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.HistoricoSubstituicaoAsync(id, ctx.ClienteId); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize(Roles = RolesConstants.Medico)]
public sealed class MedicosMeSubstituicoesController : ControllerBase
{
    private readonly OperationalAutomationService service;
    public MedicosMeSubstituicoesController(OperationalAutomationService service) { this.service = service; }
    private Guid Uid() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));

    [HttpPost("api/medicos/me/substituicoes")]
    public async Task<IActionResult> Solicitar([FromBody] SolicitarSubstituicaoRequest request) { var r = await service.SolicitarSubstituicaoAsync(Uid(), request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/pendencias")]
public sealed class PendenciasFase4Controller : ControllerBase
{
    private readonly OperationalAutomationService service;
    public PendenciasFase4Controller(OperationalAutomationService service) { this.service = service; }
    private Guid Uid() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));

    [HttpGet]
    public async Task<IActionResult> Listar() { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ListarPendenciasAsync(ctx.ClienteId); return StatusCode(r.StatusCode, r); }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo() { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ResumoPendenciasAsync(ctx.ClienteId, ctx.UsuarioId); return StatusCode(r.StatusCode, r); }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ListarPendenciasAsync(ctx.ClienteId); return StatusCode(r.StatusCode, ApiResponse<PendenciaOperacionalDto>.Ok((r.Data ?? Array.Empty<PendenciaOperacionalDto>()).FirstOrDefault(x => x.Id == id) ?? new PendenciaOperacionalDto(), "Pendência carregada.")); }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarPendenciaRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.CriarPendenciaAsync(Uid(), ctx.ClienteId, request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("{id:guid}/atribuir")]
    public async Task<IActionResult> Atribuir(Guid id, [FromBody] AtribuirPendenciaRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.AtualizarPendenciaAsync(id, Uid(), ctx.ClienteId, "ATRIBUIR", "ABERTA", request.Observacao ?? "Pendência atribuída.", request.ResponsavelUsuarioId, null, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("{id:guid}/resolver")]
    public async Task<IActionResult> Resolver(Guid id, [FromBody] ResolverPendenciaRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.AtualizarPendenciaAsync(id, Uid(), ctx.ClienteId, "RESOLVER", "RESOLVIDA", request.Observacao, null, null, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("{id:guid}/adiar")]
    public async Task<IActionResult> Adiar(Guid id, [FromBody] AdiarPendenciaRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.AtualizarPendenciaAsync(id, Uid(), ctx.ClienteId, "ADIAR", "ADIADA", request.Observacao, null, request.NovoPrazo, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }

    [HttpPost("recalcular")]
    public async Task<IActionResult> Recalcular() { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.RecalcularPendenciasAsync(Uid(), ctx.ClienteId, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString()); return StatusCode(r.StatusCode, r); }
}

[ApiController]
[Authorize]
[Route("api/relatorios")]
public sealed class RelatoriosExecutivosFase4Controller : ControllerBase
{
    private readonly OperationalAutomationService service;
    public RelatoriosExecutivosFase4Controller(OperationalAutomationService service) { this.service = service; }
    private Guid Uid() => Guid.Parse(User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Usuário inválido"));

    [HttpGet("operacional")]
    public async Task<IActionResult> Operacional([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.RelatorioAsync("OPERACIONAL", ctx.ClienteId, inicio, fim); return StatusCode(r.StatusCode, r); }
    [HttpGet("financeiro")]
    public async Task<IActionResult> Financeiro([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.RelatorioAsync("FINANCEIRO", ctx.ClienteId, inicio, fim); return StatusCode(r.StatusCode, r); }
    [HttpGet("saas")]
    public async Task<IActionResult> Saas([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim) { var r = await service.RelatorioAsync("SAAS", null, inicio, fim); return StatusCode(r.StatusCode, r); }

    [HttpGet("exportar-csv")]
    public async Task<IActionResult> Exportar([FromQuery] string tipo = "operacional", [FromQuery] DateTime? inicio = null, [FromQuery] DateTime? fim = null) { var ctx = await service.ObterContextoUsuarioAsync(User); var bytes = await service.ExportarCsvAsync(tipo, ctx.ClienteId, ctx.UsuarioId, inicio, fim); return File(bytes, "text/csv; charset=utf-8", "relatorio-" + tipo + ".csv"); }

    [HttpPost("filtros-salvar")]
    public async Task<IActionResult> SalvarFiltro([FromBody] SalvarFiltroRelatorioRequest request) { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.SalvarFiltroAsync(Uid(), ctx.ClienteId, request); return StatusCode(r.StatusCode, r); }

    [HttpGet("filtros-salvos")]
    public async Task<IActionResult> Filtros() { var ctx = await service.ObterContextoUsuarioAsync(User); var r = await service.ListarFiltrosAsync(Uid(), ctx.ClienteId); return StatusCode(r.StatusCode, r); }
}
