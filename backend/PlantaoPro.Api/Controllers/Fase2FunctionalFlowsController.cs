using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/fase2/fluxos")]
[Tags("Fase 2 - Fluxos funcionais SaaS")]
public sealed class Fase2FunctionalFlowsController : ControllerBase
{
    private readonly ILogger<Fase2FunctionalFlowsController> logger;

    public Fase2FunctionalFlowsController(ILogger<Fase2FunctionalFlowsController> logger)
    {
        this.logger = logger;
    }

    [HttpGet("{area}")]
    public IActionResult Details(string area)
    {
        try
        {
            logger.LogWarning("Endpoint demonstrativo Fase 2 desativado. Área solicitada: {Area}", area);
            return StatusCode(StatusCodes.Status410Gone,
                ApiResponse<string>.Fail("Este endpoint demonstrativo foi desativado. Use a API do módulo operacional correspondente.", StatusCodes.Status410Gone));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar fluxo fase 2 {Area}", area);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar o fluxo funcional.", 500));
        }
    }

    [HttpPost("acao")]
    public IActionResult RegisterAction([FromBody] Fase2ActionRequest request)
    {
        try
        {
            logger.LogWarning("Comando demonstrativo Fase 2 rejeitado. Ação solicitada: {Action}", request.Action);
            return StatusCode(StatusCodes.Status410Gone,
                ApiResponse<string>.Fail("Ações devem ser executadas no endpoint da entidade real. Este comando demonstrativo foi desativado.", StatusCodes.Status410Gone));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao registrar ação funcional fase 2 {Action}", request.Action);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar a ação.", 500));
        }
    }

}
