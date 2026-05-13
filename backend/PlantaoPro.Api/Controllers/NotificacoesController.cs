using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using PlantaoPro.Api.Data;
namespace PlantaoPro.Api.Controllers;
[ApiController][Route("api/notificacoes")]
public class NotificacoesController(NotificacaoService service):ControllerBase{ [Authorize][HttpGet] public async Task<IActionResult> Get(){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.ListarAsync(uid);return StatusCode(r.StatusCode,r);} }
