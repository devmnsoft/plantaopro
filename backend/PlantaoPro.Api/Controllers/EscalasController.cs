using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using PlantaoPro.Api.Data;using PlantaoPro.Api.Models;
namespace PlantaoPro.Api.Controllers;
[ApiController][Route("api")]
public class EscalasController(EscalaService service):ControllerBase{
 [Authorize][HttpPost("plantoes/{id:guid}/aceitar")] public async Task<IActionResult> Aceitar(Guid id,[FromQuery]Guid medicoId){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.AceitarAsync(id,medicoId,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} }
