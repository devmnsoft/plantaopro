using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Contracts.Notifications;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Operation360.Notifications;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificacoes")]
public sealed class NotificacoesController : ControllerBase
{
    private readonly IOperationNotificationService service;
    private readonly ILogger<NotificacoesController> logger;

    public NotificacoesController(IOperationNotificationService service, ILogger<NotificacoesController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> List(
        [FromQuery] string? tipo, [FromQuery] string? modulo, [FromQuery] string? prioridade,
        [FromQuery] string? status, [FromQuery] DateTimeOffset? de, [FromQuery] DateTimeOffset? ate,
        [FromQuery] int limite = 100, CancellationToken ct = default)
    {
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(await service.ListAsync(new(tipo, modulo, prioridade, status, de, ate, limite), ct)));
    }

    [HttpGet("nao-lidas")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> Unread(CancellationToken ct)
    {
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(await service.ListAsync(new(null, null, null, "NAO_LIDA", null, null, 10), ct)));
    }

    [HttpPost("{id:guid}/lida")]
    public async Task<ActionResult<ApiResponse<NotificationReadResult>>> Read(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await service.ReadAsync(id, ct);
            if (result is null)
                return NotFound(ApiResponse<NotificationReadResult>.Fail("Notificação não encontrada.", 404));

            return Ok(ApiResponse<NotificationReadResult>.Ok(result, result.AlreadyRead ? "Notificação já estava lida." : "Notificação marcada como lida."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao marcar notificação {NotificationId} como lida.", id);
            return StatusCode(500, ApiResponse<NotificationReadResult>.Fail("Não foi possível atualizar a notificação.", 500));
        }
    }

    [HttpPost("marcar-todas-lidas")]
    [HttpPut("lidas")]
    public async Task<ActionResult<ApiResponse<int>>> ReadAll(CancellationToken ct)
    {
        return Ok(ApiResponse<int>.Ok(await service.ReadAllAsync(ct), "Notificações atualizadas."));
    }

    [HttpPost("{id:guid}/arquivar")]
    public async Task<ActionResult<ApiResponse<bool>>> Archive(Guid id, CancellationToken ct)
    {
        if (!await service.SetStatusAsync(id, "ARQUIVADA", ct))
        {
            return NotFound(ApiResponse<bool>.Fail("Notificação não encontrada.", 404));
        }

        return Ok(ApiResponse<bool>.Ok(true, "Notificação arquivada."));
    }

    [HttpPost("{id:guid}/resolver")]
    public async Task<ActionResult<ApiResponse<bool>>> Resolve(Guid id, CancellationToken ct)
    {
        if (!await service.SetStatusAsync(id, "RESOLVIDA", ct)) return NotFound(ApiResponse<bool>.Fail("Notificação não encontrada.", 404));
        return Ok(ApiResponse<bool>.Ok(true, "Notificação resolvida."));
    }

    [HttpGet("preferencias")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationPreferenceDto>>>> Preferences(CancellationToken ct)
    {
        var preferences = await service.PreferencesAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<NotificationPreferenceDto>>.Ok(preferences));
    }

    [HttpPut("preferencias")]
    public async Task<ActionResult<ApiResponse<bool>>> Save([FromBody] NotificationPreferencesRequest request, CancellationToken ct)
    {
        await service.SavePreferencesAsync(request, ct);
        return Ok(ApiResponse<bool>.Ok(true, "Preferências atualizadas."));
    }
}

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor)]
[Route("api/alertas-operacionais")]
public sealed class AlertasOperacionaisController : ControllerBase
{
    private readonly IAlertRuleService rules; private readonly ICurrentUserService user; private readonly ILogger<AlertasOperacionaisController> logger;
    public AlertasOperacionaisController(IAlertRuleService rules, ICurrentUserService user, ILogger<AlertasOperacionaisController> logger) { this.rules=rules; this.user=user; this.logger=logger; }
    [HttpPost("avaliar")]
    public async Task<ActionResult<ApiResponse<int>>> Evaluate(CancellationToken ct)
    {
        try { var tenant=user.TenantId ?? throw new UnauthorizedAccessException(); return Ok(ApiResponse<int>.Ok(await rules.EvaluateAsync(tenant,ct),"Regras operacionais avaliadas.")); }
        catch(Exception ex) { logger.LogError(ex,"Falha ao avaliar regras operacionais."); return StatusCode(500,ApiResponse<int>.Fail("Não foi possível avaliar os alertas agora.",500)); }
    }
}
