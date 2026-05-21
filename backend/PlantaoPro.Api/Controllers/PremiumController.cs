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

    public PremiumController(PermissionService permissionService, NotificationPreferenceService notificationPreferenceService)
    {
        this.permissionService = permissionService;
        this.notificationPreferenceService = notificationPreferenceService;
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

    [HttpPut("notificacoes/preferencias")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertNotificationPreferences([FromBody] UpsertNotificationPreferenceRequest request)
    {
        var uid = Guid.Parse(User.Claims.First(c => c.Type == "uid").Value);
        var response = await notificationPreferenceService.UpsertAsync(uid, request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString());
        return StatusCode(response.StatusCode, response);
    }
}
