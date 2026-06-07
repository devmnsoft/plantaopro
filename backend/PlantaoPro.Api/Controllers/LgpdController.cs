using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lgpd")]
[Tags("LGPD")]
public sealed class LgpdController : ControllerBase
{
    private readonly LgpdService service;
    private readonly ILogger<LgpdController> logger;

    public LgpdController(LgpdService service, ILogger<LgpdController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet("politica-atual")]
    [AllowAnonymous]
    public async Task<IActionResult> PoliticaAtual()
    {
        try
        {
            var response = await service.PoliticaAtualAsync();
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar política LGPD atual");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar a política de privacidade.", 500));
        }
    }

    [HttpGet("consentimentos")]
    public async Task<IActionResult> Consentimentos()
    {
        try
        {
            var response = await service.ConsentimentosAsync(User);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar consentimentos LGPD");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar consentimentos.", 500));
        }
    }

    [HttpPost("consentimentos/registrar")]
    public async Task<IActionResult> RegistrarConsentimento([FromBody] RegistrarConsentimentoRequest request)
    {
        try
        {
            var response = await service.RegistrarConsentimentoAsync(User, request, HttpContext);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao registrar consentimento LGPD");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar consentimento.", 500));
        }
    }

    [HttpGet("solicitacoes")]
    public async Task<IActionResult> Solicitacoes()
    {
        try
        {
            var response = await service.SolicitacoesAsync(User);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar solicitações LGPD");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar solicitações LGPD.", 500));
        }
    }

    [HttpPost("solicitacoes")]
    public async Task<IActionResult> CriarSolicitacao([FromBody] CriarSolicitacaoLgpdRequest request)
    {
        try
        {
            var response = await service.CriarSolicitacaoAsync(User, request, HttpContext);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar solicitação LGPD");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar solicitação LGPD.", 500));
        }
    }

    [HttpPost("solicitacoes/{id:guid}/responder")]
    public async Task<IActionResult> ResponderSolicitacao(Guid id, [FromBody] ResponderSolicitacaoLgpdRequest request)
    {
        try
        {
            var response = await service.ResponderSolicitacaoAsync(User, id, request, HttpContext);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao responder solicitação LGPD {SolicitacaoId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível responder solicitação LGPD.", 500));
        }
    }

    [HttpGet("exportar-meus-dados")]
    public async Task<IActionResult> ExportarMeusDados()
    {
        try
        {
            var response = await service.ExportarMeusDadosAsync(User, HttpContext);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao exportar dados LGPD");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível exportar seus dados.", 500));
        }
    }

    [HttpPost("anonimizar/{usuarioId:guid}")]
    public async Task<IActionResult> Anonimizar(Guid usuarioId)
    {
        try
        {
            var response = await service.AnonimizarAsync(User, usuarioId, HttpContext);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao anonimizar usuário {UsuarioId}", usuarioId);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível processar anonimização.", 500));
        }
    }

    [HttpGet("eventos")]
    public async Task<IActionResult> Eventos()
    {
        try
        {
            var response = await service.EventosAsync(User);
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar eventos LGPD");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar eventos LGPD.", 500));
        }
    }

    [HttpGet("retencao")]
    public async Task<IActionResult> Retencao()
    {
        try
        {
            var response = await service.RetencaoAsync();
            return StatusCode(response.StatusCode, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar retenção LGPD");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar regras de retenção.", 500));
        }
    }
}
