using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.Financeiro)]
[Route("api/comercial")]
[Tags("Comercial SaaS")]
public sealed class ComercialController : ControllerBase
{
    private readonly ComercialService service;
    private readonly ILogger<ComercialController> logger;

    public ComercialController(ComercialService service, ILogger<ComercialController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet("leads")]
    public async Task<IActionResult> Leads() { try { var response = await service.LeadsAsync(); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao listar leads"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar leads.", 500)); } }

    [HttpPost("leads")]
    public async Task<IActionResult> CriarLead([FromBody] ComercialLeadRequest request) { try { var response = await service.CriarLeadAsync(User, request, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao criar lead"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar lead.", 500)); } }

    [HttpPut("leads/{id:guid}")]
    public async Task<IActionResult> AtualizarLead(Guid id, [FromBody] ComercialLeadRequest request) { try { var response = await service.AtualizarLeadAsync(User, id, request, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao atualizar lead {LeadId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar lead.", 500)); } }

    [HttpGet("oportunidades")]
    public async Task<IActionResult> Oportunidades() { try { var response = await service.OportunidadesAsync(); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao listar oportunidades"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar oportunidades.", 500)); } }

    [HttpPost("oportunidades")]
    public async Task<IActionResult> CriarOportunidade([FromBody] ComercialOportunidadeRequest request) { try { var response = await service.CriarOportunidadeAsync(User, request, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao criar oportunidade"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar oportunidade.", 500)); } }

    [HttpPut("oportunidades/{id:guid}")]
    public async Task<IActionResult> AtualizarOportunidade(Guid id, [FromBody] ComercialOportunidadeRequest request) { try { var response = await service.AtualizarOportunidadeAsync(User, id, request, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao atualizar oportunidade {OportunidadeId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar oportunidade.", 500)); } }

    [HttpPost("oportunidades/{id:guid}/ganhar")]
    public async Task<IActionResult> Ganhar(Guid id) { try { var response = await service.GanharOportunidadeAsync(User, id, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao ganhar oportunidade {OportunidadeId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível ganhar oportunidade.", 500)); } }

    [HttpPost("oportunidades/{id:guid}/perder")]
    public async Task<IActionResult> Perder(Guid id, [FromBody] PerderOportunidadeRequest request) { try { var response = await service.PerderOportunidadeAsync(User, id, request, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao perder oportunidade {OportunidadeId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível perder oportunidade.", 500)); } }

    [HttpGet("propostas")]
    public async Task<IActionResult> Propostas() { try { var response = await service.PropostasAsync(); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao listar propostas"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar propostas.", 500)); } }

    [HttpPost("propostas")]
    public async Task<IActionResult> CriarProposta([FromBody] ComercialPropostaRequest request) { try { var response = await service.CriarPropostaAsync(User, request, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao criar proposta"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar proposta.", 500)); } }

    [HttpPost("propostas/{id:guid}/enviar")]
    public async Task<IActionResult> EnviarProposta(Guid id) { try { var response = await service.EnviarPropostaAsync(User, id, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao enviar proposta {PropostaId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível enviar proposta.", 500)); } }

    [HttpPost("propostas/{id:guid}/aprovar")]
    public async Task<IActionResult> AprovarProposta(Guid id) { try { var response = await service.AprovarPropostaAsync(User, id, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao aprovar proposta {PropostaId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível aprovar proposta.", 500)); } }

    [HttpPost("propostas/{id:guid}/recusar")]
    public async Task<IActionResult> RecusarProposta(Guid id) { try { var response = await service.RecusarPropostaAsync(User, id, HttpContext); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao recusar proposta {PropostaId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível recusar proposta.", 500)); } }

    [HttpGet("funil")]
    public async Task<IActionResult> Funil() { try { var response = await service.FunilAsync(); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao carregar funil comercial"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar funil comercial.", 500)); } }

    [HttpGet("previsao-receita")]
    public async Task<IActionResult> PrevisaoReceita() { try { var response = await service.PrevisaoReceitaAsync(); return StatusCode(response.StatusCode, response); } catch (Exception ex) { logger.LogError(ex, "Erro ao carregar previsão de receita"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar previsão de receita.", 500)); } }

    [HttpPost("sugerir-plano")]
    public IActionResult SugerirPlano([FromBody] SugerirPlanoRequest request)
    {
        var response = service.SugerirPlano(request);
        return StatusCode(response.StatusCode, response);
    }
}
