using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/premium")]
[Authorize]
public class PremiumController : ControllerBase
{
    private readonly PermissionService permissionService;
    private readonly NotificationPreferenceService notificationPreferenceService;
    private readonly PremiumOperacoesService premiumOperacoesService;

    public PremiumController(PermissionService permissionService, NotificationPreferenceService notificationPreferenceService, PremiumOperacoesService premiumOperacoesService)
    {
        this.permissionService = permissionService;
        this.notificationPreferenceService = notificationPreferenceService;
        this.premiumOperacoesService = premiumOperacoesService;
    }

    [HttpGet("permissoes/matriz")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<RolePermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionMatrix()
    {
        var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        var response = await permissionService.ListRolePermissionsAsync(uid, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("notificacoes/preferencias")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NotificationPreferenceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationPreferences()
    {
        var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        var response = await notificationPreferenceService.GetAsync(uid);
        return StatusCode(response.StatusCode, response);
    }


    [HttpGet("operacoes/resumo")]
    [ProducesResponseType(typeof(ApiResponse<PremiumOperacoesResumoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperationsOverview([FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim, [FromQuery] string? perfil)
    {
        var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        var response = await premiumOperacoesService.ResumoAsync(uid, dataInicio, dataFim, perfil, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        return StatusCode(response.StatusCode, response);
    }

    [HttpPut("notificacoes/preferencias")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertNotificationPreferences([FromBody] UpsertNotificationPreferenceRequest request)
    {
        var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        var response = await notificationPreferenceService.UpsertAsync(uid, request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        return StatusCode(response.StatusCode, response);
    }
}
