using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.Coordenacao + "," + RolesConstants.Financeiro)]
[Route("api/jornada-clientes")]
[Tags("Jornada do Cliente")]
public sealed class JornadaClientesController : ControllerBase
{
    private readonly JornadaClienteService service;
    private readonly ILogger<JornadaClientesController> logger;

    public JornadaClientesController(JornadaClienteService service, ILogger<JornadaClientesController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try { var response = await service.ListarAsync(User); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar jornadas"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar jornadas.", 500)); }
    }

    [HttpGet("{clienteId:guid}")]
    public async Task<IActionResult> Details(Guid clienteId)
    {
        try { var response = await service.ObterAsync(User, clienteId); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao detalhar jornada {ClienteId}", clienteId); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível detalhar jornada.", 500)); }
    }

    [HttpPost("{clienteId:guid}/avancar")]
    public async Task<IActionResult> Avancar(Guid clienteId, [FromBody] MudarEtapaJornadaRequest request)
    {
        try { var response = await service.AvancarAsync(User, clienteId, request, HttpContext); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao avançar jornada {ClienteId}", clienteId); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível avançar jornada.", 500)); }
    }

    [HttpPost("{clienteId:guid}/retroceder")]
    public async Task<IActionResult> Retroceder(Guid clienteId, [FromBody] MudarEtapaJornadaRequest request)
    {
        try { var response = await service.RetrocederAsync(User, clienteId, request, HttpContext); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao retroceder jornada {ClienteId}", clienteId); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível retroceder jornada.", 500)); }
    }

    [HttpPost("{clienteId:guid}/eventos")]
    public async Task<IActionResult> Eventos(Guid clienteId, [FromBody] CriarEventoJornadaRequest request)
    {
        try { var response = await service.CriarEventoAsync(User, clienteId, request, HttpContext); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao criar evento jornada {ClienteId}", clienteId); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar evento.", 500)); }
    }

    [HttpPost("{clienteId:guid}/tarefas")]
    public async Task<IActionResult> Tarefas(Guid clienteId, [FromBody] CriarTarefaJornadaRequest request)
    {
        try { var response = await service.CriarTarefaAsync(User, clienteId, request, HttpContext); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao criar tarefa jornada {ClienteId}", clienteId); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar tarefa.", 500)); }
    }

    [HttpPost("tarefas/{id:guid}/concluir")]
    public async Task<IActionResult> ConcluirTarefa(Guid id)
    {
        try { var response = await service.ConcluirTarefaAsync(User, id, HttpContext); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao concluir tarefa {TarefaId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível concluir tarefa.", 500)); }
    }

    [HttpGet("funil")]
    public async Task<IActionResult> Funil()
    {
        try { var response = await service.FunilAsync(User); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar funil jornada"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar funil.", 500)); }
    }

    [HttpGet("tarefas")]
    public async Task<IActionResult> Tarefas()
    {
        try { var response = await service.TarefasAsync(User); return StatusCode(response.StatusCode, response); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar tarefas jornada"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar tarefas.", 500)); }
    }
}
