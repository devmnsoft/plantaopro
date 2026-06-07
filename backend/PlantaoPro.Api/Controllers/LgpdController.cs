using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lgpd")]
[Tags("LGPD")]
public sealed class LgpdController : ControllerBase
{
    private readonly LgpdService service;
    private readonly ILogger<LgpdController> logger;
    public LgpdController(LgpdService service, ILogger<LgpdController> logger) { this.service = service; this.logger = logger; }

    [HttpGet("politica-atual")]
    public async Task<IActionResult> PoliticaAtual() { var r = await service.PoliticaAtualAsync(); return StatusCode(r.StatusCode, r); }

    [HttpGet("consentimentos")]
    public async Task<IActionResult> Consentimentos() { var r = await service.ConsentimentosAsync(UserId()); return StatusCode(r.StatusCode, r); }

    [HttpPost("consentimentos/registrar")]
    public async Task<IActionResult> RegistrarConsentimento([FromBody] RegistrarConsentimentoRequest request)
    {
        try { var r = await service.RegistrarConsentimentoAsync(UserId(), request, Ip(), Perfil()); return StatusCode(r.StatusCode, r); }
        catch (Exception ex) { logger.LogError(ex, "Erro controller LGPD consentimento"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar consentimento.", 500)); }
    }

    [HttpGet("solicitacoes")]
    public async Task<IActionResult> Solicitacoes() { var r = await service.SolicitacoesAsync(UserId(), ClienteId(), IsAdmin()); return StatusCode(r.StatusCode, r); }

    [HttpPost("solicitacoes")]
    public async Task<IActionResult> CriarSolicitacao([FromBody] CriarSolicitacaoLgpdRequest request)
    {
        try { var r = await service.CriarSolicitacaoAsync(UserId(), ClienteId(), request, Ip(), Perfil()); return StatusCode(r.StatusCode, r); }
        catch (Exception ex) { logger.LogError(ex, "Erro controller LGPD solicitação"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar solicitação.", 500)); }
    }

    [HttpPost("solicitacoes/{id:guid}/responder")]
    [Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
    public async Task<IActionResult> Responder(Guid id, [FromBody] ResponderSolicitacaoLgpdRequest request)
    {
        try { var r = await service.ResponderSolicitacaoAsync(id, request, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); }
        catch (Exception ex) { logger.LogError(ex, "Erro controller responder LGPD"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível responder solicitação.", 500)); }
    }

    [HttpGet("exportar-meus-dados")]
    public async Task<IActionResult> ExportarMeusDados() { var r = await service.ExportarMeusDadosAsync(UserId(), ClienteId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); }

    [HttpPost("anonimizar/{usuarioId:guid}")]
    [Authorize(Roles = RolesConstants.AdministradorGlobal)]
    public async Task<IActionResult> Anonimizar(Guid usuarioId)
    {
        try { var r = await service.AnonimizarAsync(usuarioId, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); }
        catch (Exception ex) { logger.LogError(ex, "Erro controller anonimizar LGPD"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível anonimizar usuário.", 500)); }
    }

    [HttpGet("eventos")]
    public async Task<IActionResult> Eventos() { var r = await service.EventosAsync(UserId(), IsAdmin()); return StatusCode(r.StatusCode, r); }

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UsuarioId"), out var id) ? id : null;
    private Guid? ClienteId() => Guid.TryParse(User.FindFirstValue("ClienteId") ?? User.FindFirstValue("cliente_id"), out var id) ? id : null;
    private string? Perfil() => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private bool IsAdmin() => User.IsInRole(RolesConstants.AdministradorGlobal) || User.IsInRole(RolesConstants.Administrador);
}
