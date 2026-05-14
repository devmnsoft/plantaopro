using Microsoft.AspNetCore.Mvc;
namespace PlantaoPro.Api.Controllers;
[ApiController]
[Route("api/auditoria")]
public class AuditoriaController : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery]int page = 1, [FromQuery]int pageSize = 20)
    {
        var items = Enumerable.Range(1, pageSize).Select(i => new { dataHora = DateTime.UtcNow.AddMinutes(-i), usuario = "admin@plantaopro", acao = "CONSULTA", entidade = "Plantao", registro = i.ToString(), ip = "127.0.0.1", descricao = "Ação de auditoria" });
        return Ok(new { success = true, message = "ok", data = new { items, page, pageSize, total = 200 } });
    }
}
