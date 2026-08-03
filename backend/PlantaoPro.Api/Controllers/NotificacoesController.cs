using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using PlantaoPro.Api.Operation360.Notifications;
namespace PlantaoPro.Api.Controllers;
[ApiController,Authorize,Route("api/notificacoes")]
public sealed class NotificacoesController : ControllerBase
{
 private readonly IOperationNotificationService service; private readonly ILogger<NotificacoesController> logger; public NotificacoesController(IOperationNotificationService service,ILogger<NotificacoesController> logger){this.service=service;this.logger=logger;}
 [HttpGet] public async Task<IActionResult> List(CancellationToken ct)=>Ok(await service.ListAsync(false,ct));
 [HttpGet("nao-lidas")] public async Task<IActionResult> Unread(CancellationToken ct)=>Ok(await service.ListAsync(true,ct));
 [HttpPost("{id:guid}/lida")] public async Task<IActionResult> Read(Guid id,CancellationToken ct)=>await service.ReadAsync(id,ct)?NoContent():NotFound();
 [HttpPost("marcar-todas-lidas")] public async Task<IActionResult> ReadAll(CancellationToken ct)=>Ok(new { updated=await service.ReadAllAsync(ct) });
 [HttpPut("lidas")] public Task<IActionResult> ReadAllCompatibility(CancellationToken ct)=>ReadAll(ct);
 [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id,CancellationToken ct)=>await service.DeleteAsync(id,ct)?NoContent():NotFound();
 [HttpGet("preferencias")] public async Task<IActionResult> Preferences(CancellationToken ct)=>Ok(await service.PreferencesAsync(ct));
 [HttpPut("preferencias")] public async Task<IActionResult> Save([FromBody] NotificationPreferencesRequest request,CancellationToken ct){await service.SavePreferencesAsync(request,ct);return NoContent();}
 private IActionResult LegacyFailure(Exception ex){logger.LogError(ex,"Falha ao atualizar notificações persistidas");return StatusCode(500,PlantaoPro.Api.Models.ApiResponse<string>.Fail("As notificações não puderam ser atualizadas agora.",500));}
}
