using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;
[ApiController]
[Route("api/medicos")]
public class MedicosController(MedicoService service):ControllerBase{
 [HttpGet]
 public async Task<IActionResult> Get()=>Ok(await service.ListarAsync());
 [Authorize]
 [HttpPost("cadastro")]
 public async Task<IActionResult> Cadastro([FromBody]CreateMedicoRequest req){
   var uid=Guid.Parse(User.Claims.First(x=>x.Type=="uid").Value);
   var r=await service.CriarAsync(req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());
   return StatusCode(r.StatusCode,r);
 }
}
