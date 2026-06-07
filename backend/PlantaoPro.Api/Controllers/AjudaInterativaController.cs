using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ajuda")]
[Tags("Ajuda Interativa")]
public sealed class AjudaInterativaController : ControllerBase
{
    private readonly AjudaInterativaService service;
    private readonly ILogger<AjudaInterativaController> logger;

    public AjudaInterativaController(AjudaInterativaService service, ILogger<AjudaInterativaController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet("topicos")]
    public async Task<IActionResult> Topicos([FromQuery] string? perfil) { try { var response = await service.TopicosAsync(perfil); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ajuda tópicos"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar tópicos.", 500)); } }

    [HttpGet("artigos")]
    public async Task<IActionResult> Artigos([FromQuery] string? perfil) { try { var response = await service.ArtigosAsync(perfil); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ajuda artigos"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar artigos.", 500)); } }

    [HttpGet("artigos/{id:guid}")]
    public async Task<IActionResult> Artigo(Guid id) { try { var response = await service.ArtigoAsync(id); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ajuda artigo {ArtigoId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar artigo.", 500)); } }

    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string termo) { try { var response = await service.BuscarAsync(termo); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro busca ajuda"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível buscar ajuda.", 500)); } }

    [HttpPost("artigos/{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, [FromBody] AjudaFeedbackRequest request) { try { var response = await service.FeedbackAsync(User, id, request, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro feedback ajuda {ArtigoId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar feedback.", 500)); } }

    [HttpGet("checklists/perfil/{perfil}")]
    public async Task<IActionResult> Checklists(string perfil) { try { var response = await service.ChecklistsAsync(perfil); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro checklist ajuda {Perfil}", perfil); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar checklist.", 500)); } }
}
