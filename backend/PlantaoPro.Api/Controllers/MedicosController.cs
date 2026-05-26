using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Security.Claims;
using PlantaoPro.Api;

namespace PlantaoPro.Api.Controllers
{
[ApiController]
[Route("api/medicos")]
public class MedicosController : ControllerBase{ private readonly MedicoService service; public MedicosController(MedicoService service){ this.service=service; }
 [Authorize(Roles = RolesConstants.CadastrosOperacao)]
 [HttpGet]
 public async Task<IActionResult> Get(){var r=await service.ListarAsync();return StatusCode(r.StatusCode,r);}
 [Authorize(Roles = RolesConstants.CadastrosOperacao)]
 [HttpGet("{id:guid}")]
 public async Task<IActionResult> GetById(Guid id){var r=await service.GetByIdAsync(id);return StatusCode(r.StatusCode,r);}
 [Authorize(Roles = RolesConstants.CadastrosOperacao)]
 [HttpPost("cadastro")]
 public async Task<IActionResult> Cadastro([FromBody]CreateMedicoRequest req){
   var uid=Guid.Parse(User.Claims.First(x=>x.Type=="uid").Value);
   var r=await service.CriarAsync(req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());
   return StatusCode(r.StatusCode,r);
 }
}
}
