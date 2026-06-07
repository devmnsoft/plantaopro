using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ajuda")]
[Tags("Ajuda Interativa")]
public sealed class AjudaApiController : ControllerBase
{
    private readonly AjudaInterativaService service;
    private readonly ILogger<AjudaApiController> logger;
    public AjudaApiController(AjudaInterativaService service, ILogger<AjudaApiController> logger) { this.service = service; this.logger = logger; }
    [HttpGet("topicos")]
    public async Task<IActionResult> Topicos([FromQuery] string? perfil) { var r = await service.TopicosAsync(perfil); return StatusCode(r.StatusCode, r); }
    [HttpGet("artigos")]
    public async Task<IActionResult> Artigos([FromQuery] string? perfil) { var r = await service.ArtigosAsync(perfil); return StatusCode(r.StatusCode, r); }
    [HttpGet("artigos/{id:guid}")]
    public async Task<IActionResult> Artigo(Guid id) { var r = await service.ArtigoAsync(id); return StatusCode(r.StatusCode, r); }
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string? termo) { var r = await service.BuscarAsync(termo); return StatusCode(r.StatusCode, r); }
    [HttpPost("artigos/{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, [FromBody] AjudaFeedbackRequest request)
    {
        try
        {
            var r = await service.FeedbackAsync(id, request, UserId());
            return StatusCode(r.StatusCode, r);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao registrar feedback de ajuda {ArtigoId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar feedback.", 500));
        }
    }
    [HttpGet("checklists/perfil/{perfil}")]
    public async Task<IActionResult> Checklist(string perfil) { var r = await service.ChecklistPerfilAsync(perfil); return StatusCode(r.StatusCode, r); }
    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UsuarioId"), out var id) ? id : null;
}
