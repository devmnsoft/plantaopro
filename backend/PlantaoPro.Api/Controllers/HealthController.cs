using Microsoft.AspNetCore.Mvc;
namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    public HealthController(IWebHostEnvironment env)
    {
        _env = env;
    }
    [HttpGet]
    public IActionResult Get() => Ok(new { success = true, message = "ok", data = new { status = "Healthy", ambiente = _env.EnvironmentName, dataHora = DateTime.UtcNow, bancoConectado = true, schema = "plantaopro", versao = "1.0.0", nomeAplicacao = "PlantaoPro" } });
}
