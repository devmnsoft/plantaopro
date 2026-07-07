using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<HealthController> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
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

    [HttpGet("db")]
    [ProducesResponseType(typeof(ApiResponse<HealthDbDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HealthDbDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDatabase(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            var health = new HealthDbDto(
                "PlantaoPro.Api",
                "Healthy",
                _environment.EnvironmentName,
                DateTime.UtcNow,
                typeof(HealthController).Assembly.GetName().Version?.ToString() ?? string.Empty,
                "Connected");

            return Ok(ApiResponse<HealthDbDto>.Ok(health, "Banco PostgreSQL conectado"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no health check de banco PostgreSQL.");
            var health = new HealthDbDto(
                "PlantaoPro.Api",
                "Unhealthy",
                _environment.EnvironmentName,
                DateTime.UtcNow,
                typeof(HealthController).Assembly.GetName().Version?.ToString() ?? string.Empty,
                "Unavailable");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<HealthDbDto>(
                false,
                "Banco PostgreSQL indisponível.",
                health,
                new[] { "Não foi possível abrir conexão PostgreSQL." },
                StatusCodes.Status503ServiceUnavailable,
                DateTime.UtcNow));
        }
    }
}

public sealed record HealthDbDto(
    string Application,
    string Status,
    string Environment,
    DateTime Timestamp,
    string Version,
    string Database);
