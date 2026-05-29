using Dapper;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;
using System.Diagnostics;
using System.Security.Claims;

namespace PlantaoPro.Api;

public sealed class RequestLogContextFilter : IAsyncActionFilter
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<RequestLogContextFilter> _logger;

    public RequestLogContextFilter(IConfiguration cfg, ILogger<RequestLogContextFilter> logger)
    {
        _cfg = cfg;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var endpoint = http.Request.Path.Value ?? string.Empty;
        var method = http.Request.Method;
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var email = http.User.FindFirst(ClaimTypes.Email)?.Value
            ?? http.User.FindFirst(ClaimTypes.Name)?.Value
            ?? "anonimo";
        var userIdText = http.User.FindFirst("uid")?.Value ?? http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var clienteIdText = http.User.FindFirst("cliente_id")?.Value;
        var roles = string.Join(',', http.User.FindAll(ClaimTypes.Role).Select(r => r.Value));
        var perfil = string.IsNullOrWhiteSpace(roles) ? "sem-perfil" : roles;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("API request iniciado Endpoint:{Endpoint} Metodo:{Metodo} UsuarioId:{UsuarioId} Email:{Email} Perfil:{Perfil} IP:{Ip} QueryString:{QueryString} DataHoraUtc:{DataHoraUtc}", endpoint, method, userIdText ?? "anonimo", email, perfil, ip, http.Request.QueryString.Value, DateTime.UtcNow);

        ActionExecutedContext? executed = null;
        Exception? exception = null;
        try
        {
            executed = await next();
            exception = executed.Exception;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            sw.Stop();
            var statusCode = http.Response.StatusCode;
            if (exception is not null && statusCode < 500) statusCode = 500;
            var success = exception is null && statusCode < 400;
            Guid? usuarioId = Guid.TryParse(userIdText, out var uid) ? uid : null;
            Guid? clienteId = Guid.TryParse(clienteIdText, out var cid) ? cid : null;
            var mensagemErro = ResumirErro(exception?.Message);

            if (success)
            {
                _logger.LogInformation("API request finalizado Endpoint:{Endpoint} Metodo:{Metodo} UsuarioId:{UsuarioId} Email:{Email} Perfil:{Perfil} IP:{Ip} StatusCode:{StatusCode} Sucesso:{Sucesso} DuracaoMs:{DuracaoMs} DataHoraUtc:{DataHoraUtc}", endpoint, method, userIdText ?? "anonimo", email, perfil, ip, statusCode, success, sw.ElapsedMilliseconds, DateTime.UtcNow);
            }
            else
            {
                _logger.LogWarning(exception, "API request com falha Endpoint:{Endpoint} Metodo:{Metodo} UsuarioId:{UsuarioId} Email:{Email} Perfil:{Perfil} IP:{Ip} StatusCode:{StatusCode} Sucesso:{Sucesso} DuracaoMs:{DuracaoMs} DataHoraUtc:{DataHoraUtc}", endpoint, method, userIdText ?? "anonimo", email, perfil, ip, statusCode, success, sw.ElapsedMilliseconds, DateTime.UtcNow);
            }

            await RegistrarRequestAsync(endpoint, method, statusCode, sw.ElapsedMilliseconds, usuarioId, clienteId, perfil, ip, mensagemErro);
        }
    }

    private async Task RegistrarRequestAsync(string endpoint, string method, int statusCode, long durationMs, Guid? usuarioId, Guid? clienteId, string perfil, string ip, string? mensagemErro)
    {
        try
        {
            using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.api_request_logs
                (id,endpoint,method,status_code,duration_ms,cliente_id,usuario_id,perfil,ip,sucesso,error_message,reg_date)
                values (gen_random_uuid(),@endpoint,@method,@statusCode,@durationMs,@clienteId,@usuarioId,@perfil,@ip,@sucesso,@mensagemErro,now())", new
            {
                endpoint,
                method,
                statusCode,
                durationMs,
                clienteId,
                usuarioId,
                perfil,
                ip,
                sucesso = statusCode < 400,
                mensagemErro
            });

            if (statusCode >= 400)
            {
                await cn.ExecuteAsync(@"insert into plantaopro.api_error_logs
                    (id,endpoint,method,status_code,error_message,cliente_id,usuario_id,perfil,ip,reg_date)
                    values (gen_random_uuid(),@endpoint,@method,@statusCode,@errorMessage,@clienteId,@usuarioId,@perfil,@ip,now())", new
                {
                    endpoint,
                    method,
                    statusCode,
                    errorMessage = mensagemErro ?? (statusCode == 403 ? "Acesso negado." : "Falha na requisição."),
                    clienteId,
                    usuarioId,
                    perfil,
                    ip
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao persistir log de request Endpoint={Endpoint} StatusCode={StatusCode}", endpoint, statusCode);
        }
    }

    private static string? ResumirErro(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return null;
        var clean = message.Replace("\r", " ").Replace("\n", " ");
        return clean.Length <= 500 ? clean : clean.Substring(0, 500);
    }
}
