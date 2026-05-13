using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;
[ApiController]
[Route("api/plantoes")]
public class PlantoesController(PlantaoService service):ControllerBase{
 [HttpGet] public async Task<IActionResult> Get()=>Ok(await service.GetAllAsync());
 [HttpGet("disponiveis")] public async Task<IActionResult> Disponiveis(){var r=await service.GetAllAsync(); return StatusCode(r.StatusCode,ApiResponse<IEnumerable<PlantaoDto>>.Ok(r.Data?.Where(x=>x.Status=="aberto")??Enumerable.Empty<PlantaoDto>(),"Plantões disponíveis."));}
 [Authorize][HttpPost] public async Task<IActionResult> Create(CreatePlantaoRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.CreateAsync(req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPost("{id:guid}/publicar")] public async Task<IActionResult> Publicar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"aberto",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPost("{id:guid}/cancelar")] public async Task<IActionResult> Cancelar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"cancelado",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPost("{id:guid}/encerrar")] public async Task<IActionResult> Encerrar(Guid id,[FromBody]StatusRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ChangeStatusAsync(id,"realizado",req.Justificativa,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
}
