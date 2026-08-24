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
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> List(CancellationToken ct)
    {
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(await service.ListAsync(false, ct)));
    }

    [HttpGet("nao-lidas")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> Unread(CancellationToken ct)
    {
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(await service.ListAsync(true, ct)));
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

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken ct)
    {
        if (!await service.DeleteAsync(id, ct))
        {
            return NotFound(ApiResponse<bool>.Fail("Notificação não encontrada.", 404));
        }

        return Ok(ApiResponse<bool>.Ok(true, "Notificação excluída."));
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
