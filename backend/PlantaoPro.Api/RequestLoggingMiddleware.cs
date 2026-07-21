using Dapper;
using Npgsql;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;

namespace PlantaoPro.Api;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private static long _loggingSuspendedUntilTicks;
    private static long _lastFallbackLogTicks;

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
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                _logger.LogError(ex, "Exceção não tratada na requisição {Method} {Endpoint}", context.Request.Method, context.Request.Path.Value ?? "/");
                throw;
            }
            finally
            {
                sw.Stop();
                try
                {
                    await RegistrarAsync(context, cfg, sw.ElapsedMilliseconds, exception);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Falha ao registrar log estruturado de request");
                }
            }
        }
        catch when (exception is null)
        {
            _logger.LogError("Falha inesperada no middleware de logging estruturado para {Method} {Endpoint}", context.Request.Method, context.Request.Path.Value ?? "/");
            throw;
        }
    }

    private async Task RegistrarAsync(HttpContext context, IConfiguration cfg, long durationMs, Exception? exception)
    {
        try
        {
            var statusCode = context.Response?.StatusCode ?? 0;
            if (exception is not null)
            {
                statusCode = statusCode >= 400 ? statusCode : 500;
            }

            var endpoint = context.Request.Path.HasValue
                ? context.Request.Path.Value ?? string.Empty
                : string.Empty;
            var metodo = context.Request.Method ?? string.Empty;
            var usuarioId = GetGuidClaim(context, "uid") ?? GetGuidClaim(context, ClaimTypes.NameIdentifier);
            var clienteId = GetGuidClaim(context, "cliente_id");
            var email = context.User?.FindFirst("email")?.Value
                ?? context.User?.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;
            email = MascararTexto(email) ?? string.Empty;

            var perfil = context.User?.FindFirst("perfil")?.Value
                ?? context.User?.FindFirst(ClaimTypes.Role)?.Value
                ?? string.Empty;
            if (string.IsNullOrWhiteSpace(perfil))
            {
                perfil = string.Join(',', context.User?.FindAll(ClaimTypes.Role).Select(x => x.Value) ?? Enumerable.Empty<string>());
            }

            var perfilSeguro = MascararTexto(perfil) ?? string.Empty;
            var ipOrigem = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            var userAgent = MascararTexto(context.Request.Headers["User-Agent"].ToString()) ?? string.Empty;
            var rawQueryString = context.Request.QueryString.HasValue
                ? context.Request.QueryString.Value ?? string.Empty
                : string.Empty;
            var queryString = SanitizarQueryString(rawQueryString);
            var errorMessage = exception?.Message;
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                errorMessage = statusCode >= 400
                    ? $"Request finalizado com status HTTP {statusCode}."
                    : string.Empty;
            }

            errorMessage = MascararTexto(errorMessage) ?? string.Empty;
            var stackTrace = MascararTexto(exception?.StackTrace) ?? string.Empty;
            var exceptionType = exception?.GetType().Name ?? string.Empty;
            var success = exception is null && statusCode < 400;
            var duration = durationMs < 0 ? 0 : durationMs;
            var erro = ObterMensagemErro(statusCode, exception) ?? string.Empty;

            if (LoggingCircuitOpen())
            {
                LogFallbackOnce("Persistência de logs temporariamente suspensa; banco indisponível ou schema ausente.");
                return;
            }

            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"insert into plantaopro.api_request_logs
    (endpoint, metodo, method, status_code, sucesso, duracao_ms, duration_ms, usuario_id, cliente_id, email, perfil, ip_origem, ip, user_agent, query_string, erro, error_message, reg_date)
values
    (@Endpoint, @Metodo, @Metodo, @StatusCode, @Sucesso, @DuracaoMs, @DuracaoMs, @UsuarioId, @ClienteId, @Email, @Perfil, @IpOrigem, @IpOrigem, @UserAgent, @QueryString, @Erro, @ErrorMessage, now())", new
            {
                Endpoint = endpoint,
                Metodo = metodo,
                StatusCode = statusCode,
                Sucesso = success,
                DuracaoMs = duration,
                UsuarioId = usuarioId,
                ClienteId = clienteId,
                Email = email,
                Perfil = perfilSeguro,
                IpOrigem = ipOrigem,
                UserAgent = userAgent,
                QueryString = queryString,
                Erro = erro,
                ErrorMessage = errorMessage
            });

            if (duration >= 2000)
            {
                _logger.LogWarning("Endpoint lento Endpoint:{Endpoint} Metodo:{Metodo} StatusCode:{StatusCode} DuracaoMs:{DuracaoMs} UsuarioId:{UsuarioId} ClienteId:{ClienteId} Perfil:{Perfil} IP:{Ip}", endpoint, metodo, statusCode, duration, usuarioId, clienteId, perfilSeguro, ipOrigem);
            }

            if (success)
            {
                return;
            }

            await cn.ExecuteAsync(@"insert into plantaopro.api_error_logs
    (endpoint, metodo, method, status_code, success, usuario_id, cliente_id, email, perfil, ip_origem, ip, user_agent, query_string, duration_ms, mensagem, error_message, exception_type, stack_trace, reg_date)
values
    (@Endpoint, @Metodo, @Metodo, @StatusCode, @Sucesso, @UsuarioId, @ClienteId, @Email, @Perfil, @IpOrigem, @IpOrigem, @UserAgent, @QueryString, @DuracaoMs, @Mensagem, @Mensagem, @ExceptionType, @StackTrace, now())", new
            {
                Endpoint = endpoint,
                Metodo = metodo,
                StatusCode = statusCode,
                Sucesso = false,
                DuracaoMs = duration,
                UsuarioId = usuarioId,
                ClienteId = clienteId,
                Email = email,
                Perfil = perfilSeguro,
                IpOrigem = ipOrigem,
                UserAgent = userAgent,
                QueryString = queryString,
                Mensagem = errorMessage,
                ExceptionType = exceptionType,
                StackTrace = stackTrace
            });

            if (statusCode == 403 || (statusCode == 401 && !endpoint.Contains("/auth/login", StringComparison.OrdinalIgnoreCase)))
            {
                var acao = statusCode == 403 ? AuditoriaConstants.Acoes.AcessoNegado : AuditoriaConstants.Acoes.LoginFalha;
                var detalhes = JsonSerializer.Serialize(new { endpoint, statusCode });
                await cn.ExecuteAsync(@"insert into plantaopro.auditoria_acoes_criticas
    (usuario_id, cliente_id, entidade, entidade_id, acao, detalhes, sucesso, ip_origem, perfil, user_agent, reg_date)
values
    (@UsuarioId, @ClienteId, @Entidade, null, @Acao, cast(@Detalhes as jsonb), false, @IpOrigem, @Perfil, @UserAgent, now())", new
                {
                    UsuarioId = usuarioId,
                    ClienteId = clienteId,
                    Entidade = statusCode == 403 ? AuditoriaConstants.Entidades.Permissao : AuditoriaConstants.Entidades.Usuario,
                    Acao = acao,
                    Detalhes = detalhes,
                    IpOrigem = ipOrigem,
                    Perfil = perfilSeguro,
                    UserAgent = userAgent
                });
            }
        }
        catch (Exception ex)
        {
            OpenLoggingCircuit();
            LogFallbackOnce("Falha ao persistir log estruturado; evento sanitizado descartado até recuperação do banco.");
        }
    }

    private static string ObterMensagemErro(int statusCode, Exception? exception)
    {
        if (exception is not null)
        {
            return "erro_interno";
        }

        return statusCode >= 400 ? "HTTP " + statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
    }

    private static bool LoggingCircuitOpen()
    {
        var until = Interlocked.Read(ref _loggingSuspendedUntilTicks);
        return until > DateTimeOffset.UtcNow.UtcTicks;
    }

    private static void OpenLoggingCircuit()
    {
        Interlocked.Exchange(ref _loggingSuspendedUntilTicks, DateTimeOffset.UtcNow.AddSeconds(30).UtcTicks);
    }

    private void LogFallbackOnce(string message)
    {
        var now = DateTimeOffset.UtcNow.UtcTicks;
        var last = Interlocked.Read(ref _lastFallbackLogTicks);
        if (last != 0 && new TimeSpan(now - last) < TimeSpan.FromSeconds(30))
        {
            return;
        }

        Interlocked.Exchange(ref _lastFallbackLogTicks, now);
        _logger.LogWarning("{Message}", message);
    }

    private static Guid? GetGuidClaim(HttpContext context, string claimType)
    {
        var value = context.User.FindFirst(claimType)?.Value;
        Guid id;
        return Guid.TryParse(value, out id) ? id : null;
    }


    private static string SanitizarQueryString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var query = value.Trim();

        var termosSensiveis = new[]
        {
            "token",
            "access_token",
            "refresh_token",
            "password",
            "senha",
            "secret",
            "api_key",
            "apikey",
            "authorization"
        };

        foreach (var termo in termosSensiveis)
        {
            if (query.Contains(termo, StringComparison.OrdinalIgnoreCase))
            {
                return "[query_string_sensivel_omitida]";
            }
        }

        return query.Length > 1000 ? query.Substring(0, 1000) : query;
    }

    private static string? MascararTexto(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return valor;
        var sensiveis = new[] { "senha", "password", "token", "hash", "secret", "segredo", "authorization", "bearer", "access_token", "refresh_token" };
        foreach (var termo in sensiveis)
        {
            if (valor.IndexOf(termo, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "[DADO_SENSIVEL_MASCARADO]";
            }
        }

        return valor.Length > 1000 ? valor.Substring(0, 1000) : valor;
    }
}
