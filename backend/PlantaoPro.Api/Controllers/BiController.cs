using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/bi")]
[Authorize]
public class BiController : ControllerBase
{
    private readonly BiService biService;
    private readonly AssinaturaGuardService assinaturaGuard;
    private readonly UsuarioContextService usuarioContext;
    private readonly IAuditService audit;

    public BiController(BiService biService, AssinaturaGuardService assinaturaGuard, UsuarioContextService usuarioContext, IAuditService audit)
    {
        this.biService = biService;
        this.assinaturaGuard = assinaturaGuard;
        this.usuarioContext = usuarioContext;
        this.audit = audit;
    }

    [HttpGet("resumo-executivo")]
    [ProducesResponseType(typeof(ApiResponse<BiResumoExecutivoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResumoExecutivo()
    {
        var bloqueio = await ValidarPlanoBiAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);

        var response = await biService.GetResumoExecutivoAsync();
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("plantao-cobertura")]
    public Task<IActionResult> PlantaoCobertura() => ResponderBiEmEvolucaoAsync("plantao-cobertura");
    [HttpGet("financeiro")]
    public Task<IActionResult> Financeiro() => ResponderBiEmEvolucaoAsync("financeiro");
    [HttpGet("medicos")]
    public Task<IActionResult> Medicos() => ResponderBiEmEvolucaoAsync("medicos");
    [HttpGet("clientes")]
    public Task<IActionResult> Clientes() => ResponderBiEmEvolucaoAsync("clientes");
    [HttpGet("sla")]
    public Task<IActionResult> Sla() => ResponderBiEmEvolucaoAsync("sla");
    [HttpGet("tendencias")]
    public Task<IActionResult> Tendencias() => ResponderBiEmEvolucaoAsync("tendencias");

    private async Task<IActionResult> ResponderBiEmEvolucaoAsync(string painel)
    {
        var bloqueio = await ValidarPlanoBiAsync();
        if (bloqueio is not null) return StatusCode(bloqueio.StatusCode, bloqueio);

        return Ok(ApiResponse<string>.Ok("Painel de BI " + painel + " em evolução."));
    }

    private async Task<ApiResponse<bool>?> ValidarPlanoBiAsync()
    {
        var clienteId = usuarioContext.GetClienteId();
        if (!clienteId.HasValue)
        {
            return ApiResponse<bool>.Fail("Cliente do usuário não identificado para acesso ao BI.", 403);
        }

        var permissao = await assinaturaGuard.PodeUsarBIAsync(clienteId.Value);
        if (!permissao.Success)
        {
            await audit.RegistrarAsync(usuarioContext.GetUsuarioId(), clienteId, AuditoriaConstants.Entidades.Relatorio, null, AuditoriaConstants.Acoes.AcessoNegado, new { motivo = permissao.Message }, false, HttpContext.Connection.RemoteIpAddress?.ToString(), string.Join(",", usuarioContext.GetRoles()));
            return permissao;
        }

        return null;
    }
}
