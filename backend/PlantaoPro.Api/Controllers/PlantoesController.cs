using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers
{
[ApiController]
[Route("api/plantoes")]
public class PlantoesController : ControllerBase{ private readonly PlantaoService service; public PlantoesController(PlantaoService service){ this.service=service; }
 [HttpGet] public async Task<IActionResult> Get([FromQuery]PlantaoFilterRequest filter){var r=await service.GetAllAsync(filter); return StatusCode(r.StatusCode,r);} 
 [HttpGet("disponiveis")] public async Task<IActionResult> Disponiveis([FromQuery]PlantaoFilterRequest filter){var r=await service.GetAllAsync(filter with{Status="aberto"}); return StatusCode(r.StatusCode,r);} 
 [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id){var r=await service.GetByIdAsync(id); return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPost] public async Task<IActionResult> Create(CreatePlantaoRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.CreateAsync(req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,UpdatePlantaoRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.UpdateAsync(id,req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPost("{id:guid}/publicar")] public async Task<IActionResult> Publicar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"aberto",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"cancelado",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPost("{id:guid}/encerrar")] public async Task<IActionResult> Encerrar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"realizado",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
}
}
