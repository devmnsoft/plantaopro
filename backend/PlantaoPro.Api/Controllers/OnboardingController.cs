using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.IdentityModel.Tokens.Jwt;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/onboarding")]
[Authorize]
public sealed class OnboardingController : ControllerBase
{
    private readonly OnboardingService _onboardingService;
    private readonly OnboardingJornadaService _jornada;
    private readonly TenantContextService _tenantContext;
    private readonly IConfiguration _cfg;
    private readonly IAuditService _audit;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(OnboardingService onboardingService, OnboardingJornadaService jornada, TenantContextService tenantContext, IConfiguration cfg, IAuditService audit, ILogger<OnboardingController> logger)
    {
        _onboardingService = onboardingService;
        _jornada = jornada;
        _tenantContext = tenantContext;
        _cfg = cfg;
        _audit = audit;
        _logger = logger;
    }

    [HttpPost("cliente")]
    public async Task<IActionResult> CriarCliente([FromBody] CreateClienteOnboardingRequest request)
    {
        _logger.LogInformation("Iniciando requisição de onboarding para {RazaoSocial}", request.RazaoSocial);
        try
        {
            if (string.IsNullOrWhiteSpace(request.RazaoSocial) || string.IsNullOrWhiteSpace(request.Cnpj))
            {
                _logger.LogWarning("Validação falhou na requisição de onboarding.");
                return BadRequest(ApiResponse<string>.Fail("Informe Razão Social e CNPJ para continuar.", 400));
            }

            var usuarioIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
            {
                _logger.LogWarning("Token sem usuário válido para onboarding.");
                return Unauthorized(ApiResponse<string>.Fail("Sessão inválida. Faça login novamente.", 401));
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();
            var result = await _onboardingService.CriarClienteCompletoAsync(request, usuarioId, ip, ua);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar onboarding");
            return StatusCode(500, ApiResponse<string>.Fail("Erro interno ao processar onboarding.", 500));
        }
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromQuery] Guid clienteId)
    {
        try
        {
            var result = await _onboardingService.GetResumoAsync(clienteId);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter resumo de onboarding");
            return StatusCode(500, ApiResponse<string>.Fail("Erro interno ao obter resumo.", 500));
        }
    }

    // ---------------------------------------------------------------------
    // R5-C7/C8: jornada adaptada ao contrato + conclusao derivada de dados.
    // A avaliacao e lazy e roda no leitura (mesma governanca dos contratos B4/B6):
    // GET checklist reavalia contra os dados persistidos e persiste as transicoes.
    // Nenhum endpoint conclui etapa por clique quando existe criterio nao atendido.
    // ---------------------------------------------------------------------

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.StatusAsync(ctx.Data.TenantId.Value);
        return result.Data is null ? NotFound(ApiResponse<string>.Fail(result.Message, 404)) : Ok(result);
    }

    /// <summary>Inicia (ou garante) o onboarding do tenant a partir do catalogo canonicos.</summary>
    [HttpPost("iniciar")]
    public async Task<IActionResult> Iniciar()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.IniciarAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty);
        await AuditarAsync("INICIAR_ONBOARDING", null, result.Success);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("checklist")]
    public async Task<IActionResult> Checklist()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.ReavaliarAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("reavaliar")]
    public async Task<IActionResult> Reavaliar()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.ReavaliarAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty);
        await AuditarAsync("REAVALIAR_ONBOARDING", null, result.Success);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Re-sincroniza o checklist com os contratos efetivos de modulo (novo contrato / upgrade / downgrade).</summary>
    [HttpPost("sincronizar")]
    public async Task<IActionResult> Sincronizar()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.SincronizarAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty);
        await AuditarAsync("SINCRONIZAR_ONBOARDING", null, result.Success);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("checklist/{id:guid}/concluir")]
    public async Task<IActionResult> Concluir(Guid id)
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.ConcluirEtapaAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty, id);
        await AuditarAsync("CONCLUIR_ETAPA", id, result.Success);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Pular so vale para etapa OPCIONAL; justificativa e persistida (o antigo fake que so auditava sumiu).</summary>
    [HttpPost("checklist/{id:guid}/pular")]
    public async Task<IActionResult> Pular(Guid id, [FromBody] StatusRequest? request)
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.PularEtapaAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty, id, request?.Justificativa);
        await AuditarAsync("PULAR_ETAPA", id, result.Success);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("checklist/{id:guid}/restaurar")]
    public async Task<IActionResult> Restaurar(Guid id)
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.RestaurarEtapaAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty, id);
        await AuditarAsync("RESTAURAR_ETAPA", id, result.Success);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>Reinicio honesto: limpa conclusoes manuais/pulos, re-sincroniza e reavalia contra os dados.</summary>
    [HttpPost("reiniciar")]
    public async Task<IActionResult> Reiniciar()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.ReiniciarAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty);
        await AuditarAsync("REINICIAR_ONBOARDING", null, result.Success);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("proxima-acao")]
    public async Task<IActionResult> ProximaAcao()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        var result = await _jornada.ReavaliarAsync(ctx.Data.TenantId.Value, ctx.Data.ClienteId ?? Guid.Empty);
        if (!result.Success || result.Data is null) return StatusCode(result.StatusCode, result);
        var proxima = result.Data.FirstOrDefault(d => d.Status == "PENDENTE" || d.Status == "BLOQUEADO");
        return Ok(ApiResponse<OnboardingChecklistItemDto>.Ok(proxima ?? new OnboardingChecklistItemDto { Titulo = "Onboarding finalizado", Status = "FINALIZADO", LinkAcao = "/Home/Dashboard" }));
    }

    private async Task AuditarAsync(string acao, Guid? etapaId, bool sucesso)
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data?.ClienteId, "ONBOARDING", etapaId, acao,
            new { etapaId }, sucesso, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_CLIENTE");
    }
}
