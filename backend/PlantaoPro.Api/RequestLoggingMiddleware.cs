using Dapper;
using Npgsql;
using System.Diagnostics;
using System.Security.Claims;

namespace PlantaoPro.Api;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration cfg)
    {
        var sw = Stopwatch.StartNew();
        Exception? exception = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            sw.Stop();
            await RegistrarAsync(context, cfg, sw.ElapsedMilliseconds, exception);
        }
    }

    private async Task RegistrarAsync(HttpContext context, IConfiguration cfg, long durationMs, Exception? exception)
    {
        try
        {
            var statusCode = exception is null ? context.Response.StatusCode : 500;
            var endpoint = context.Request.Path.Value ?? "/";
            var method = context.Request.Method;
            var usuarioId = GetGuidClaim(context, "uid") ?? GetGuidClaim(context, ClaimTypes.NameIdentifier);
            var clienteId = GetGuidClaim(context, "cliente_id");
            var perfil = string.Join(',', context.User.FindAll(ClaimTypes.Role).Select(x => x.Value));
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var perfilSeguro = string.IsNullOrWhiteSpace(perfil) ? "sem-perfil" : perfil;
            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.api_request_logs(id, endpoint, method, status_code, usuario_id, cliente_id, perfil, ip, duration_ms, sucesso, error_message, reg_date, reg_status)
values (gen_random_uuid(), @endpoint, @method, @statusCode, @usuarioId, @clienteId, @perfil, @ip, @durationMs, @sucesso, @errorMessage, now(), 'A')", new
            {
                endpoint,
                method,
                statusCode,
                usuarioId,
                clienteId,
                perfil = perfilSeguro,
                ip,
                durationMs,
                sucesso = exception is null && statusCode < 400,
                errorMessage = exception is null ? (statusCode >= 400 ? "HTTP " + statusCode : null) : "erro_interno"
            });

            if (exception is not null || statusCode >= 500)
            {
                await cn.ExecuteAsync(@"insert into plantaopro.api_error_logs(id, endpoint, method, status_code, usuario_id, cliente_id, perfil, ip, error_message, reg_date, reg_status)
values (gen_random_uuid(), @endpoint, @method, @statusCode, @usuarioId, @clienteId, @perfil, @ip, @errorMessage, now(), 'A')", new
                {
                    endpoint,
                    method,
                    statusCode,
                    usuarioId,
                    clienteId,
                    perfil = perfilSeguro,
                    ip,
                    errorMessage = exception is null ? "Erro HTTP " + statusCode : "erro_interno"
                });
            }

            if (statusCode == 403 || (statusCode == 401 && !endpoint.Contains("/auth/login", StringComparison.OrdinalIgnoreCase)))
            {
                var acao = statusCode == 403 ? AuditoriaConstants.Acoes.AcessoNegado : AuditoriaConstants.Acoes.LoginFalha;
                await cn.ExecuteAsync(@"insert into plantaopro.auditoria_acoes_criticas(id, usuario_id, cliente_id, entidade, entidade_id, acao, detalhes, sucesso, ip_origem, perfil, reg_date, reg_status)
values (gen_random_uuid(), @usuarioId, @clienteId, @entidade, null, @acao, cast(@detalhes as jsonb), false, @ip, @perfil, now(), 'A')", new
                {
                    usuarioId,
                    clienteId,
                    entidade = statusCode == 403 ? AuditoriaConstants.Entidades.Permissao : AuditoriaConstants.Entidades.Usuario,
                    acao,
                    detalhes = "{\"endpoint\":" + System.Text.Json.JsonSerializer.Serialize(endpoint) + ",\"statusCode\":" + statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}",
                    ip,
                    perfil = perfilSeguro
                });
            }

            if (durationMs >= 2000)
            {
                _logger.LogWarning("Endpoint lento Endpoint:{Endpoint} Metodo:{Metodo} StatusCode:{StatusCode} DuracaoMs:{DuracaoMs} UsuarioId:{UsuarioId} ClienteId:{ClienteId} Perfil:{Perfil} IP:{Ip}", endpoint, method, statusCode, durationMs, usuarioId, clienteId, perfilSeguro, ip);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar log estruturado de request");
        }
    }

    private static Guid? GetGuidClaim(HttpContext context, string claimType)
    {
        var value = context.User.FindFirst(claimType)?.Value;
        Guid id;
        return Guid.TryParse(value, out id) ? id : null;
    }
}
