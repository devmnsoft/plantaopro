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
    private readonly TenantContextService _tenantContext;
    private readonly IConfiguration _cfg;
    private readonly IAuditService _audit;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(OnboardingService onboardingService, TenantContextService tenantContext, IConfiguration cfg, IAuditService audit, ILogger<OnboardingController> logger)
    {
        _onboardingService = onboardingService;
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

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var dto = await cn.QueryFirstOrDefaultAsync<OnboardingStatusDto>(@"select tenant_id as ""TenantId"", cliente_id as ""ClienteId"", coalesce(status,'') as ""Status"", progresso as ""Progresso"", coalesce(proxima_acao,'') as ""ProximaAcao"" from plantaopro.tenant_onboarding where tenant_id=@tenantId and reg_status='A' order by reg_date desc limit 1", new { tenantId = ctx.Data.TenantId.Value });
        return dto is null ? NotFound(ApiResponse<string>.Fail("Onboarding não encontrado.", 404)) : Ok(ApiResponse<OnboardingStatusDto>.Ok(dto));
    }

    [HttpGet("checklist")]
    public async Task<IActionResult> Checklist()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<OnboardingChecklistItemDto>(@"select c.id as ""Id"", coalesce(c.codigo,'') as ""Codigo"", coalesce(c.titulo,'') as ""Titulo"", coalesce(c.descricao,'') as ""Descricao"", c.ordem as ""Ordem"", c.obrigatorio as ""Obrigatorio"", c.concluido as ""Concluido"", coalesce(c.link_acao,'') as ""LinkAcao"", coalesce(c.status,'') as ""Status"" from plantaopro.tenant_onboarding_checklist c where c.tenant_id=@tenantId and c.reg_status='A' order by c.ordem limit 50", new { tenantId = ctx.Data.TenantId.Value });
        return Ok(ApiResponse<IEnumerable<OnboardingChecklistItemDto>>.Ok(rows));
    }

    [HttpPost("checklist/{id:guid}/concluir")]
    public async Task<IActionResult> Concluir(Guid id)
    {
        try
        {
            var ctx = await _tenantContext.ObterAtualAsync();
            if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            var rows = await cn.ExecuteAsync("update plantaopro.tenant_onboarding_checklist set concluido=true, concluido_em=now(), status='CONCLUIDO', reg_update=now() where id=@id and tenant_id=@tenantId and reg_status='A'", new { id, tenantId = ctx.Data.TenantId.Value }, tx);
            if (rows == 0)
            {
                await tx.RollbackAsync();
                return NotFound(ApiResponse<string>.Fail("Etapa não encontrada.", 404));
            }
            await AtualizarProgressoAsync(cn, tx, ctx.Data.TenantId.Value);
            await tx.CommitAsync();
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data.ClienteId, "ONBOARDING", id, "CONCLUIR_ETAPA", new { etapaId = id }, true, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_CLIENTE");
            return Ok(ApiResponse<string>.Ok("ok", "Etapa concluída."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao concluir etapa de onboarding");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível concluir a etapa.", 500));
        }
    }

    [HttpPost("pular-etapa")]
    public async Task<IActionResult> PularEtapa([FromBody] StatusRequest request)
    {
        await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), null, "ONBOARDING", null, "PULAR_ETAPA", new { motivo = string.IsNullOrWhiteSpace(request.Justificativa) ? "Não informado" : "informado" }, true, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_CLIENTE");
        return Ok(ApiResponse<string>.Ok("ok", "Etapa sinalizada como pulada."));
    }

    [HttpPost("reiniciar")]
    public async Task<IActionResult> Reiniciar()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync("update plantaopro.tenant_onboarding_checklist set concluido=false, concluido_em=null, status='PENDENTE', reg_update=now() where tenant_id=@tenantId and reg_status='A'; update plantaopro.tenant_onboarding set status='EM_ANDAMENTO', progresso=0, finalizado_em=null, reg_update=now() where tenant_id=@tenantId and reg_status='A';", new { tenantId = ctx.Data.TenantId.Value });
        return Ok(ApiResponse<string>.Ok("ok", "Onboarding reiniciado."));
    }

    [HttpGet("proxima-acao")]
    public async Task<IActionResult> ProximaAcao()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return StatusCode(ctx.StatusCode, ctx);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var proxima = await cn.QueryFirstOrDefaultAsync<OnboardingChecklistItemDto>(@"select id as ""Id"", coalesce(codigo,'') as ""Codigo"", coalesce(titulo,'') as ""Titulo"", coalesce(descricao,'') as ""Descricao"", ordem as ""Ordem"", obrigatorio as ""Obrigatorio"", concluido as ""Concluido"", coalesce(link_acao,'') as ""LinkAcao"", coalesce(status,'') as ""Status"" from plantaopro.tenant_onboarding_checklist where tenant_id=@tenantId and reg_status='A' and concluido=false order by ordem limit 1", new { tenantId = ctx.Data.TenantId.Value });
        return Ok(ApiResponse<OnboardingChecklistItemDto>.Ok(proxima ?? new OnboardingChecklistItemDto { Titulo = "Onboarding finalizado", Status = "FINALIZADO", LinkAcao = "/Home/Dashboard" }));
    }

    private static async Task AtualizarProgressoAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId)
    {
        var total = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.tenant_onboarding_checklist where tenant_id=@tenantId and reg_status='A'", new { tenantId }, tx);
        var feitos = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.tenant_onboarding_checklist where tenant_id=@tenantId and reg_status='A' and concluido=true", new { tenantId }, tx);
        var progresso = total == 0 ? 0 : (int)Math.Round((decimal)feitos * 100 / total);
        var proxima = await cn.ExecuteScalarAsync<string>("select coalesce(titulo,'') from plantaopro.tenant_onboarding_checklist where tenant_id=@tenantId and reg_status='A' and concluido=false order by ordem limit 1", new { tenantId }, tx) ?? string.Empty;
        var status = progresso >= 100 ? "FINALIZADO" : "EM_ANDAMENTO";
        await cn.ExecuteAsync("update plantaopro.tenant_onboarding set progresso=@progresso, proxima_acao=@proxima, status=@status, finalizado_em=case when @status='FINALIZADO' then now() else finalizado_em end, reg_update=now() where tenant_id=@tenantId and reg_status='A'", new { tenantId, progresso, proxima, status }, tx);
    }
}
