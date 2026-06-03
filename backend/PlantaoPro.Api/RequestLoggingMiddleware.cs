using Dapper;
using Npgsql;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

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
            var metodo = context.Request.Method;
            var usuarioId = GetGuidClaim(context, "uid") ?? GetGuidClaim(context, ClaimTypes.NameIdentifier);
            var clienteId = GetGuidClaim(context, "cliente_id");
            var email = MascararTexto(context.User.FindFirst(ClaimTypes.Email)?.Value ?? context.User.FindFirst("email")?.Value);
            var perfil = string.Join(',', context.User.FindAll(ClaimTypes.Role).Select(x => x.Value));
            var perfilSeguro = string.IsNullOrWhiteSpace(perfil) ? "sem-perfil" : MascararTexto(perfil);
            var ipOrigem = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = MascararTexto(context.Request.Headers["User-Agent"].ToString());
            var queryString = MascararTexto(context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null);
            var erro = ObterMensagemErro(statusCode, exception);

            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.api_request_logs
    (endpoint, metodo, status_code, sucesso, duracao_ms, usuario_id, cliente_id, email, perfil, ip_origem, user_agent, query_string, erro, reg_date)
values
    (@Endpoint, @Metodo, @StatusCode, @Sucesso, @DuracaoMs, @UsuarioId, @ClienteId, @Email, @Perfil, @IpOrigem, @UserAgent, @QueryString, @Erro, now())", new
            {
                Endpoint = endpoint,
                Metodo = metodo,
                StatusCode = statusCode,
                Sucesso = exception is null && statusCode < 400,
                DuracaoMs = durationMs,
                UsuarioId = usuarioId,
                ClienteId = clienteId,
                Email = email,
                Perfil = perfilSeguro,
                IpOrigem = ipOrigem,
                UserAgent = userAgent,
                QueryString = queryString,
                Erro = erro
            });

            if (exception is not null || statusCode >= 500)
            {
                await cn.ExecuteAsync(@"insert into plantaopro.api_error_logs
    (endpoint, metodo, status_code, usuario_id, cliente_id, email, perfil, ip_origem, user_agent, mensagem, exception_type, stack_trace, reg_date)
values
    (@Endpoint, @Metodo, @StatusCode, @UsuarioId, @ClienteId, @Email, @Perfil, @IpOrigem, @UserAgent, @Mensagem, @ExceptionType, @StackTrace, now())", new
                {
                    Endpoint = endpoint,
                    Metodo = metodo,
                    StatusCode = statusCode,
                    UsuarioId = usuarioId,
                    ClienteId = clienteId,
                    Email = email,
                    Perfil = perfilSeguro,
                    IpOrigem = ipOrigem,
                    UserAgent = userAgent,
                    Mensagem = erro ?? "erro_interno",
                    ExceptionType = exception?.GetType().Name,
                    StackTrace = (string?)null
                });
            }

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

            if (durationMs >= 2000)
            {
                _logger.LogWarning("Endpoint lento Endpoint:{Endpoint} Metodo:{Metodo} StatusCode:{StatusCode} DuracaoMs:{DuracaoMs} UsuarioId:{UsuarioId} ClienteId:{ClienteId} Perfil:{Perfil} IP:{Ip}", endpoint, metodo, statusCode, durationMs, usuarioId, clienteId, perfilSeguro, ipOrigem);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar log estruturado de request");
        }
    }

    private static string? ObterMensagemErro(int statusCode, Exception? exception)
    {
        if (exception is not null)
        {
            return "erro_interno";
        }

        return statusCode >= 400 ? "HTTP " + statusCode.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
    }

    private static Guid? GetGuidClaim(HttpContext context, string claimType)
    {
        var value = context.User.FindFirst(claimType)?.Value;
        Guid id;
        return Guid.TryParse(value, out id) ? id : null;
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
