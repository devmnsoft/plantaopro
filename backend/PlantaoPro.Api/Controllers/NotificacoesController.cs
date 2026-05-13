using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using PlantaoPro.Api.Data;using PlantaoPro.Api.Models;
namespace PlantaoPro.Api.Controllers
{
[ApiController][Route("api/notificacoes")]
public class NotificacoesController(NotificacaoService service):ControllerBase{
 [Authorize][HttpGet] public async Task<IActionResult> Get([FromQuery]NotificationFilterRequest f){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ListarAsync(uid,f);return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpGet("nao-lidas")] public async Task<IActionResult> NaoLidas([FromQuery]int page=1,[FromQuery]int pageSize=20){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ListarAsync(uid,new(null,false,null,null,page,pageSize));return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPut("{id:guid}/lida")] public async Task<IActionResult> Lida(Guid id){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.MarcarLidaAsync(uid,id,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} 
 [Authorize][HttpPut("lidas")] public async Task<IActionResult> Lidas(){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.MarcarTodasLidasAsync(uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} }
}
