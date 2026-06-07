using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Route("api/jornada-clientes")]
[Tags("SaaS - Jornada do Cliente")]
public sealed class JornadaClientesController : ControllerBase
{
    private readonly JornadaClienteService service;
    private readonly ILogger<JornadaClientesController> logger;
    public JornadaClientesController(JornadaClienteService service, ILogger<JornadaClientesController> logger) { this.service = service; this.logger = logger; }

    [HttpGet]
    public async Task<IActionResult> Listar() { var r = await service.ListarAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("{clienteId:guid}")]
    public async Task<IActionResult> Detalhar(Guid clienteId) { var r = await service.DetalharAsync(clienteId); return StatusCode(r.StatusCode, r); }
    [HttpPost("{clienteId:guid}/avancar")]
    public async Task<IActionResult> Avancar(Guid clienteId, [FromBody] MudarEtapaJornadaRequest request) { try { var r = await service.MudarEtapaAsync(clienteId, request, true, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro avançar jornada"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível avançar etapa.", 500)); } }
    [HttpPost("{clienteId:guid}/retroceder")]
    public async Task<IActionResult> Retroceder(Guid clienteId, [FromBody] MudarEtapaJornadaRequest request) { try { var r = await service.MudarEtapaAsync(clienteId, request, false, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro retroceder jornada"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível retroceder etapa.", 500)); } }
    [HttpPost("{clienteId:guid}/eventos")]
    public async Task<IActionResult> Evento(Guid clienteId, [FromBody] CriarJornadaEventoRequest request)
    {
        try { var r = await service.CriarEventoAsync(clienteId, request, UserId()); return StatusCode(r.StatusCode, r); }
        catch (Exception ex) { logger.LogError(ex, "Erro criar evento jornada {ClienteId}", clienteId); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar evento.", 500)); }
    }
    [HttpPost("{clienteId:guid}/tarefas")]
    public async Task<IActionResult> Tarefa(Guid clienteId, [FromBody] CriarJornadaTarefaRequest request)
    {
        try { var r = await service.CriarTarefaAsync(clienteId, request); return StatusCode(r.StatusCode, r); }
        catch (Exception ex) { logger.LogError(ex, "Erro criar tarefa jornada {ClienteId}", clienteId); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar tarefa.", 500)); }
    }
    [HttpPost("tarefas/{id:guid}/concluir")]
    public async Task<IActionResult> Concluir(Guid id)
    {
        try { var r = await service.ConcluirTarefaAsync(id); return StatusCode(r.StatusCode, r); }
        catch (Exception ex) { logger.LogError(ex, "Erro concluir tarefa jornada {TarefaId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível concluir tarefa.", 500)); }
    }
    [HttpGet("funil")]
    public async Task<IActionResult> Funil() { var r = await service.FunilAsync(); return StatusCode(r.StatusCode, r); }

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UsuarioId"), out var id) ? id : null;
    private string? Perfil() => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
