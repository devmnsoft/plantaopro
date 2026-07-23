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
    [HttpGet("auth")]
    [ProducesResponseType(typeof(ApiResponse<HealthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HealthResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAuth(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            await connection.OpenAsync(cancellationToken);
            var tables = await new NpgsqlCommand("select count(*) from unnest(array['usuarios','perfis','usuarios_perfis','login_tentativas']) t where to_regclass('plantaopro.'||t) is not null", connection).ExecuteScalarAsync(cancellationToken);
            var admin = await new NpgsqlCommand("select exists(select 1 from plantaopro.usuarios u join plantaopro.usuarios_perfis up on up.usuario_id=u.id and up.reg_status='A' join plantaopro.perfis p on p.id=up.perfil_id and p.reg_status='A' where u.reg_status='A' and coalesce(p.codigo,p.nome)='ADMINISTRADOR_GLOBAL')", connection).ExecuteScalarAsync(cancellationToken);
            var jwtOk = !string.IsNullOrWhiteSpace(_configuration["Jwt:Key"]) && (_configuration["Jwt:Key"]?.Length ?? 0) >= 32;
            var schemaOk = Convert.ToInt32(tables) == 4;
            var payload = new HealthResponse(
                "PlantaoPro.Api",
                schemaOk && jwtOk ? "Healthy" : "Unhealthy",
                _environment.EnvironmentName,
                DateTime.UtcNow,
                typeof(HealthController).Assembly.GetName().Version?.ToString() ?? string.Empty,
                new HealthDatabaseResponse("ok", schemaOk ? "ok" : "invalid", admin is bool b && b ? "configured" : "pending"),
                new HealthDependencyResponse("jwt", jwtOk ? "ok" : "invalid"));
            return schemaOk && jwtOk ? Ok(ApiResponse<HealthResponse>.Ok(payload, "Diagnóstico de autenticação concluído.")) : StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<HealthResponse>.Fail("Diagnóstico de autenticação falhou.", 503, new[] { "schema/jwt/admin inválido" }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no health auth.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<HealthResponse>.Fail("Autenticação indisponível.", 503));
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
