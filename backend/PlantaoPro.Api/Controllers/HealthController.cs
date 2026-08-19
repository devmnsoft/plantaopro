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

    [HttpGet("system")]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SystemHealthResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetSystem(CancellationToken cancellationToken)
    {
        var components = new List<SystemHealthComponent>
        {
            new("API", "DISPONÍVEL", "Serviço HTTP respondendo."),
            new("Autenticação", IsJwtConfigured() ? "CONFIGURADO" : "NÃO CONFIGURADO", IsJwtConfigured() ? "Assinatura JWT validada na inicialização." : "Configuração obrigatória ausente."),
            new("Storage", IsStorageConfigured() ? "CONFIGURADO" : "NÃO CONFIGURADO", IsStorageConfigured() ? "Provedor externo configurado." : "Armazenamento externo não habilitado."),
            new("Workers", "NÃO CONFIGURADO", "Nenhum worker essencial foi registrado neste processo.")
        };
        try
        {
            await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
            await connection.OpenAsync(cancellationToken);
            await new NpgsqlCommand("select 1", connection).ExecuteScalarAsync(cancellationToken);
            components.Add(new("Banco", "DISPONÍVEL", "PostgreSQL conectado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dependência de banco indisponível no health agregado.");
            components.Add(new("Banco", "INDISPONÍVEL", "Não foi possível validar a conexão."));
        }

        var unavailable = components.Any(component => component.Status == "INDISPONÍVEL");
        var degraded = components.Any(component => component.Status == "NÃO CONFIGURADO");
        var status = unavailable ? "INDISPONÍVEL" : degraded ? "DEGRADADO" : "SAUDÁVEL";
        var payload = new SystemHealthResponse(status, _environment.EnvironmentName, DateTime.UtcNow,
            typeof(HealthController).Assembly.GetName().Version?.ToString() ?? string.Empty, components);
        return unavailable
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<SystemHealthResponse>(false, "Sistema indisponível.", payload, new[] { "Uma dependência essencial está indisponível." }, StatusCodes.Status503ServiceUnavailable, DateTime.UtcNow))
            : Ok(ApiResponse<SystemHealthResponse>.Ok(payload, degraded ? "Sistema operando de forma degradada." : "Sistema saudável."));
    }

    private bool IsJwtConfigured() => !string.IsNullOrWhiteSpace(_configuration["Jwt:Key"])
        && (_configuration["Jwt:Key"]?.Length ?? 0) >= 32;

    private bool IsStorageConfigured() => !string.IsNullOrWhiteSpace(_configuration["Storage:Provider"])
        || !string.IsNullOrWhiteSpace(_configuration["Storage:Endpoint"]);

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
