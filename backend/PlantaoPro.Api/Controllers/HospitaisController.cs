using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers
{
[ApiController]
[Route("api/hospitais")]
public class HospitaisController(HospitalService service):ControllerBase{
 [HttpGet] public async Task<IActionResult> Get(){var r=await service.GetAllAsync();return StatusCode(r.StatusCode,r);}
 [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id){var r=await service.GetByIdAsync(id);return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPost] public async Task<IActionResult> Create(CreateHospitalRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.CreateAsync(req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id,UpdateHospitalRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.UpdateAsync(id,req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.DeleteAsync(id,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
}
}
