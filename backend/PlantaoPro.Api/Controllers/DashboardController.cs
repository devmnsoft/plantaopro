using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;

namespace PlantaoPro.Api.Controllers;
[ApiController]
[Route("api")]
public class DashboardController(DashboardService service):ControllerBase{
 [Authorize][HttpGet("dashboard")] public async Task<IActionResult> Dashboard(){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value); return Ok(await service.GetAsync(uid));}
 [Authorize][HttpGet("mobile/home")] public async Task<IActionResult> MobileHome(){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value); return Ok(await service.GetAsync(uid));}
}
