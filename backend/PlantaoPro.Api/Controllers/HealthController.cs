using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public HealthController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthDto>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var health = new HealthDto(
            "PlantaoPro.Api",
            "Healthy",
            _environment.EnvironmentName,
            DateTime.UtcNow,
            typeof(HealthController).Assembly.GetName().Version?.ToString() ?? string.Empty);

        return Ok(ApiResponse<HealthDto>.Ok(health, "PlantaoPro.Api online"));
    }
}
