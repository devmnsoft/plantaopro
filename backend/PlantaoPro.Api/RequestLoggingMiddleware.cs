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

            using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await GarantirCompatibilidadeLogsAsync(cn);
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
            _logger.LogError(ex, "Falha ao registrar log estruturado de request");
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

    private static async Task GarantirCompatibilidadeLogsAsync(NpgsqlConnection cn)
    {
        await cn.ExecuteAsync(@"create schema if not exists plantaopro;
create extension if not exists pgcrypto;

create table if not exists plantaopro.api_request_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text not null default '',
    metodo varchar(10) not null default 'GET',
    method varchar(12) not null default 'GET',
    status_code integer not null default 0,
    sucesso boolean not null default true,
    duracao_ms bigint not null default 0,
    duration_ms bigint not null default 0,
    usuario_id uuid null,
    cliente_id uuid null,
    email varchar(255) null,
    perfil varchar(120) null,
    ip_origem varchar(64) null,
    ip varchar(80) null,
    user_agent text null,
    query_string text null,
    erro text null,
    error_message text not null default '',
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

create table if not exists plantaopro.api_error_logs (
    id uuid primary key default gen_random_uuid(),
    endpoint text not null default '',
    metodo varchar(10) not null default 'GET',
    method varchar(12) not null default 'GET',
    status_code integer not null default 0,
    usuario_id uuid null,
    cliente_id uuid null,
    email varchar(255) null,
    perfil varchar(120) null,
    ip_origem varchar(64) null,
    ip varchar(80) null,
    user_agent text null,
    mensagem text not null default '',
    error_message text not null default '',
    exception_type varchar(255) null,
    stack_trace text not null default '',
    success boolean not null default false,
    query_string text not null default '',
    duration_ms bigint not null default 0,
    reg_date timestamptz not null default now(),
    reg_status char(1) not null default 'A'
);

alter table if exists plantaopro.api_request_logs
    add column if not exists metodo varchar(10) not null default 'GET',
    add column if not exists method varchar(12) not null default 'GET',
    add column if not exists duracao_ms bigint not null default 0,
    add column if not exists duration_ms bigint not null default 0,
    add column if not exists ip_origem varchar(64) null,
    add column if not exists ip varchar(80) null,
    add column if not exists erro text null,
    add column if not exists error_message text not null default '',
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.api_request_logs
    alter column id set default gen_random_uuid(),
    alter column error_message set default '',
    alter column method set default 'GET',
    alter column duration_ms set default 0;

update plantaopro.api_request_logs set error_message = '' where error_message is null;

alter table if exists plantaopro.api_error_logs
    add column if not exists metodo varchar(10) not null default 'GET',
    add column if not exists method varchar(12) not null default 'GET',
    add column if not exists ip_origem varchar(64) null,
    add column if not exists ip varchar(80) null,
    add column if not exists mensagem text not null default '',
    add column if not exists error_message text not null default '',
    add column if not exists stack_trace text not null default '',
    add column if not exists success boolean not null default false,
    add column if not exists query_string text not null default '',
    add column if not exists duration_ms bigint not null default 0,
    add column if not exists reg_status char(1) not null default 'A';

alter table if exists plantaopro.api_error_logs
    alter column id set default gen_random_uuid(),
    alter column endpoint set default '',
    alter column metodo set default '',
    alter column mensagem set default '',
    alter column error_message set default '',
    alter column stack_trace set default '',
    alter column method set default '',
    alter column email set default '',
    alter column perfil set default '',
    alter column ip set default '',
    alter column ip_origem set default '',
    alter column user_agent set default '',
    alter column success set default false,
    alter column query_string set default '',
    alter column duration_ms set default 0;

update plantaopro.api_error_logs set endpoint = '' where endpoint is null;
update plantaopro.api_error_logs set metodo = '' where metodo is null;
update plantaopro.api_error_logs set method = '' where method is null;
update plantaopro.api_error_logs set mensagem = '' where mensagem is null;
update plantaopro.api_error_logs set error_message = '' where error_message is null;
update plantaopro.api_error_logs set stack_trace = '' where stack_trace is null;
update plantaopro.api_error_logs set email = '' where email is null;
update plantaopro.api_error_logs set perfil = '' where perfil is null;
update plantaopro.api_error_logs set ip = '' where ip is null;
update plantaopro.api_error_logs set ip_origem = '' where ip_origem is null;
update plantaopro.api_error_logs set user_agent = '' where user_agent is null;
update plantaopro.api_error_logs set success = false where success is null;
update plantaopro.api_error_logs set query_string = '' where query_string is null;
update plantaopro.api_error_logs set duration_ms = 0 where duration_ms is null;");
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
