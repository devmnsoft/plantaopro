using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using PlantaoPro.Web.Services;
namespace PlantaoPro.Web.Controllers;
[Authorize(Roles="Admin,Gestor,Administrador"),Route("CommandCenter")]
public sealed class CommandCenterController:Controller
{
 private readonly ManagerCommandCenterWebService service; public CommandCenterController(ManagerCommandCenterWebService service)=>this.service=service;
 [HttpGet("")] public async Task<IActionResult> Index(DateOnly? from,DateOnly? to,string? status,CancellationToken ct){var start=from??DateOnly.FromDateTime(DateTime.UtcNow);var end=to??start.AddDays(7);if(end<start){ModelState.AddModelError("to","A data final deve ser posterior à inicial.");end=start;}ViewBag.From=start;ViewBag.To=end;ViewBag.Status=status;return View(await service.GetAsync(HttpContext.Session.GetString("JwtToken")??string.Empty,start,end,status,ct));}
}
