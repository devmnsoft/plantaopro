using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using PlantaoPro.Api.Data;using PlantaoPro.Api.Models;
namespace PlantaoPro.Api.Controllers;
[ApiController][Route("api/financeiro")]
public class FinanceiroController(FinanceiroService service):ControllerBase{ [Authorize][HttpPost("pagamentos/gerar")] public async Task<IActionResult> Gerar([FromBody]GerarPagamentoRequest req){var uid=Guid.Parse(User.Claims.First(c=>c.Type=="uid").Value);var r=await service.GerarAsync(req,uid,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString());return StatusCode(r.StatusCode,r);} }
