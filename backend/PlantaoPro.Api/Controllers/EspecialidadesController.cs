using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;
[ApiController]
[Route("api/especialidades")]
public class EspecialidadesController(EspecialidadeService service):ControllerBase{
 [HttpGet] public async Task<IActionResult> Get(){var r=await service.GetAllAsync();return StatusCode(r.StatusCode,r);}
 [Authorize][HttpPost] public async Task<IActionResult> Create(CreateEspecialidadeRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.CreateAsync(req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
}
