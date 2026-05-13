using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;
[ApiController]
[Route("api/auth")]
public class AuthController(AuthService service):ControllerBase{
 [HttpPost("login")]
 public async Task<IActionResult> Login([FromBody]LoginRequest req){
   var r=await service.LoginAsync(req,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());
   return StatusCode(r.StatusCode,r);
 }
}
