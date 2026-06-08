using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Route("api/comercial")]
[Tags("SaaS - Comercial")]
public sealed class ComercialController : ControllerBase
{
    private readonly ComercialSaasService service;
    private readonly ILogger<ComercialController> logger;
    public ComercialController(ComercialSaasService service, ILogger<ComercialController> logger) { this.service = service; this.logger = logger; }

    [HttpGet("leads")]
    public async Task<IActionResult> Leads() { var r = await service.LeadsAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPost("leads")]
    public async Task<IActionResult> CriarLead([FromBody] ComercialLeadRequest request) { try { var r = await service.SalvarLeadAsync(null, request, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro criar lead"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar lead.", 500)); } }
    [HttpPut("leads/{id:guid}")]
    public async Task<IActionResult> EditarLead(Guid id, [FromBody] ComercialLeadRequest request) { try { var r = await service.SalvarLeadAsync(id, request, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro editar lead"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível editar lead.", 500)); } }
    [HttpGet("oportunidades")]
    public async Task<IActionResult> Oportunidades() { var r = await service.OportunidadesAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPost("oportunidades")]
    public async Task<IActionResult> CriarOportunidade([FromBody] ComercialOportunidadeRequest request) { try { var r = await service.SalvarOportunidadeAsync(null, request, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro criar oportunidade"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar oportunidade.", 500)); } }
    [HttpPut("oportunidades/{id:guid}")]
    public async Task<IActionResult> EditarOportunidade(Guid id, [FromBody] ComercialOportunidadeRequest request) { try { var r = await service.SalvarOportunidadeAsync(id, request, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro editar oportunidade"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível editar oportunidade.", 500)); } }
    [HttpPost("oportunidades/{id:guid}/ganhar")]
    public async Task<IActionResult> Ganhar(Guid id) { try { var r = await service.GanharOportunidadeAsync(id, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro ganhar oportunidade"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível ganhar oportunidade.", 500)); } }
    [HttpPost("oportunidades/{id:guid}/perder")]
    public async Task<IActionResult> Perder(Guid id, [FromBody] StatusComMotivoRequest request) { try { var r = await service.PerderOportunidadeAsync(id, request, UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro perder oportunidade"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível perder oportunidade.", 500)); } }
    [HttpGet("propostas")]
    public async Task<IActionResult> Propostas() { var r = await service.PropostasAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPost("propostas")]
    public async Task<IActionResult> CriarProposta([FromBody] ComercialPropostaRequest request) { try { var r = await service.CriarPropostaAsync(request, User.IsInRole(RolesConstants.AdministradorGlobal), UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); } catch (Exception ex) { logger.LogError(ex, "Erro proposta"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar proposta.", 500)); } }
    [HttpPost("propostas/{id:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid id) { var r = await service.AlterarStatusPropostaAsync(id, "ENVIADA", UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); }
    [HttpPost("propostas/{id:guid}/aprovar")]
    public async Task<IActionResult> Aprovar(Guid id) { var r = await service.AlterarStatusPropostaAsync(id, "APROVADA", UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); }
    [HttpPost("propostas/{id:guid}/recusar")]
    public async Task<IActionResult> Recusar(Guid id) { var r = await service.AlterarStatusPropostaAsync(id, "RECUSADA", UserId(), Ip(), Perfil()); return StatusCode(r.StatusCode, r); }
    [HttpGet("funil")]
    public async Task<IActionResult> Funil() { var r = await service.FunilAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("previsao-receita")]
    public async Task<IActionResult> PrevisaoReceita() { var r = await service.PrevisaoReceitaAsync(); return StatusCode(r.StatusCode, r); }
    [HttpPost("sugerir-plano")]
    public IActionResult SugerirPlano([FromBody] SugerirPlanoRequest request) { try { return Ok(ApiResponse<PlanoSugeridoDto>.Ok(service.SugerirPlano(request))); } catch (Exception ex) { logger.LogError(ex, "Erro sugerir plano comercial"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível sugerir plano.", 500)); } }

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UsuarioId"), out var id) ? id : null;
    private string? Perfil() => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
