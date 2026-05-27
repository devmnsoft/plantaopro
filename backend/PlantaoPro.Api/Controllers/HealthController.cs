using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    public HealthController()
    {
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            success = true,
            message = "PlantaoPro.Api online",
            data = new
            {
                status = "Healthy"
            }
        });
    }
}
