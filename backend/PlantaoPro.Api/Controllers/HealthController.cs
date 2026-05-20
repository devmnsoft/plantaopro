using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _cfg;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IWebHostEnvironment env, IConfiguration cfg, ILogger<HealthController> logger)
    {
        _env = env;
        _cfg = cfg;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var now = DateTime.UtcNow;
        var dbOk = false;
        string? dbError = null;

        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await cn.ExecuteScalarAsync<int>("select 1");
            dbOk = true;
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
            _logger.LogError(ex, "Falha no health check do banco.");
        }

        var jwtSection = _cfg.GetSection("Jwt");
        var jwtOk = !string.IsNullOrWhiteSpace(jwtSection["Issuer"]) && !string.IsNullOrWhiteSpace(jwtSection["Audience"]) && !string.IsNullOrWhiteSpace(jwtSection["Key"]);

        var healthy = dbOk && jwtOk;
        return StatusCode(healthy ? 200 : 503, new
        {
            success = healthy,
            message = healthy ? "ok" : "degraded",
            data = new
            {
                status = healthy ? "Healthy" : "Degraded",
                ambiente = _env.EnvironmentName,
                dataHora = now,
                servicos = new
                {
                    banco = new { status = dbOk ? "up" : "down", erro = dbError },
                    jwt = new { status = jwtOk ? "up" : "down" }
                },
                schema = "plantaopro",
                versao = "1.1.0",
                nomeAplicacao = "PlantaoPro"
            }
        });
    }
}
