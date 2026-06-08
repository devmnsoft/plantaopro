using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/observabilidade")]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
public sealed class ObservabilidadeController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<ObservabilidadeController> _logger;

    public ObservabilidadeController(IConfiguration cfg, ILogger<ObservabilidadeController> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var data = await cn.QueryFirstAsync(@"select
                (select count(*) from plantaopro.api_request_logs where reg_date::date = current_date) as TotalRequestsHoje,
                (select count(*) from plantaopro.api_error_logs where reg_date::date = current_date) as TotalErrosHoje,
                coalesce((select avg(duracao_ms) from plantaopro.api_request_logs where reg_date::date = current_date),0) as TempoMedioMs,
                (select count(*) from plantaopro.usuarios where reg_status = 'A') as UsuariosAtivos");
            return Ok(ApiResponse<object>.Ok(data, "Resumo de observabilidade carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar resumo de observabilidade");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar resumo de observabilidade.", 500));
        }
    }

    [HttpGet("erros")]
    public async Task<IActionResult> Erros([FromQuery] int limit = 50)
    {
        try
        {
            var safeLimit = NormalizarLimit(limit);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var data = await cn.QueryAsync(@"select coalesce(endpoint,'') as Endpoint, coalesce(metodo,'') as Metodo, status_code as StatusCode,
                coalesce(mensagem,'') as ErrorMessage, reg_date as Data
                from plantaopro.api_error_logs order by reg_date desc limit @limit", new { limit = safeLimit });
            return Ok(ApiResponse<object>.Ok(data, "Erros carregados."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar erros de observabilidade");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar erros.", 500));
        }
    }

    [HttpGet("performance")]
    public async Task<IActionResult> Performance([FromQuery] int limit = 20)
    {
        try
        {
            var safeLimit = NormalizarLimit(limit);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var data = await cn.QueryAsync(@"select coalesce(endpoint,'') as Endpoint, coalesce(metodo,'') as Metodo,
                coalesce(avg(duracao_ms),0) as TempoMedioMs, coalesce(max(duracao_ms),0) as TempoMaximoMs, count(*)::bigint as Total
                from plantaopro.api_request_logs
                group by endpoint, metodo
                order by avg(duracao_ms) desc
                limit @limit", new { limit = safeLimit });
            return Ok(ApiResponse<object>.Ok(data, "Performance carregada."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar performance");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar performance.", 500));
        }
    }

    [HttpGet("requests")]
    public async Task<IActionResult> Requests([FromQuery] int limit = 50)
    {
        try
        {
            var safeLimit = NormalizarLimit(limit);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var data = await cn.QueryAsync(@"select coalesce(endpoint,'') as Endpoint, coalesce(metodo,'') as Metodo, status_code as StatusCode, usuario_id as UsuarioId, cliente_id as ClienteId, coalesce(perfil,'') as Perfil, coalesce(ip_origem,'') as Ip, duracao_ms as DuracaoMs, sucesso as Sucesso, reg_date as Data
                from plantaopro.api_request_logs order by reg_date desc limit @limit", new { limit = safeLimit });
            return Ok(ApiResponse<object>.Ok(data, "Requests carregados."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar requests");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar requests.", 500));
        }
    }

    [HttpGet("acessos-negados")]
    public async Task<IActionResult> AcessosNegados([FromQuery] int limit = 50)
    {
        try
        {
            var safeLimit = NormalizarLimit(limit);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var data = await cn.QueryAsync(@"select usuario_id as UsuarioId, cliente_id as ClienteId, coalesce(perfil,'') as Perfil, coalesce(entidade,'') as Entidade, entidade_id as EntidadeId, coalesce(acao,'') as Acao, coalesce(detalhes::text,'{}') as Detalhes, coalesce(ip_origem,'') as Ip, reg_date as Data
                from plantaopro.auditoria_acoes_criticas
                where acao in ('ACESSO_NEGADO','BLOQUEIO_TENANT','BLOQUEIO_PERMISSAO')
                order by reg_date desc limit @limit", new { limit = safeLimit });
            return Ok(ApiResponse<object>.Ok(data, "Acessos negados carregados."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar acessos negados");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar acessos negados.", 500));
        }
    }

    [HttpGet("logins")]
    public async Task<IActionResult> Logins([FromQuery] int limit = 50)
    {
        try
        {
            var safeLimit = NormalizarLimit(limit);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var data = await cn.QueryAsync(@"select usuario_id as UsuarioId, coalesce(perfil,'') as Perfil, coalesce(acao,'') as Acao, sucesso as Sucesso, coalesce(ip_origem,'') as Ip, reg_date as Data
                from plantaopro.auditoria_acoes_criticas
                where acao in ('LOGIN_SUCESSO','LOGIN_FALHA')
                order by reg_date desc limit @limit", new { limit = safeLimit });
            return Ok(ApiResponse<object>.Ok(data, "Logins carregados."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar logins");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar logins.", 500));
        }
    }

    [HttpGet("endpoints")]
    public Task<IActionResult> Endpoints([FromQuery] int limit = 20) => Performance(NormalizarLimit(limit));

    [HttpGet("banco")]
    public async Task<IActionResult> Banco()
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var ok = await cn.ExecuteScalarAsync<int>("select 1");
            return Ok(ApiResponse<object>.Ok(new { Status = ok == 1 ? "Saudável" : "Atenção", Timestamp = DateTime.UtcNow }, "Status do banco carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar status do banco");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao consultar status do banco.", 500));
        }
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> Jobs([FromQuery] int limit = 20)
    {
        try
        {
            var safeLimit = NormalizarLimit(limit);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var data = await cn.QueryAsync(@"select coalesce(job_name,'') as Nome, coalesce(status,'') as Status, coalesce(message,'') as Mensagem,
                duration_ms as DuracaoMs, reg_date as Data
                from plantaopro.background_job_logs order by reg_date desc limit @limit", new { limit = safeLimit });
            return Ok(ApiResponse<object>.Ok(data, "Jobs carregados."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar jobs");
            return StatusCode(500, ApiResponse<string>.Fail("Falha ao carregar jobs.", 500));
        }
    }

    private static int NormalizarLimit(int limit)
    {
        return Math.Clamp(limit, 1, 100);
    }
}
