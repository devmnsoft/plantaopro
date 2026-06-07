using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Security.Claims;

namespace PlantaoPro.Api;

public sealed class RequestLogContextFilter : IActionFilter
{
    private readonly ILogger<RequestLogContextFilter> _logger;
    private const string StopwatchKey = "__requestStopwatch";

    public RequestLogContextFilter(ILogger<RequestLogContextFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var http = context.HttpContext;
        var sw = Stopwatch.StartNew();
        http.Items[StopwatchKey] = sw;
        var endpoint = $"{http.Request.Method} {http.Request.Path}";
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var email = http.User.FindFirst(ClaimTypes.Email)?.Value
            ?? http.User.FindFirst(ClaimTypes.Name)?.Value
            ?? "anonimo";
        var userId = http.User.FindFirst("uid")?.Value ?? "anonimo";
        var roles = string.Join(',', http.User.FindAll(ClaimTypes.Role).Select(r => r.Value));

        _logger.LogInformation("API request iniciado Endpoint:{Endpoint} UsuarioId:{UsuarioId} Email:{Email} Perfil:{Perfil} IP:{Ip} QueryString:{QueryString} DataHoraUtc:{DataHoraUtc}", endpoint, userId, email, string.IsNullOrWhiteSpace(roles) ? "sem-perfil" : roles, ip, http.Request.QueryString.Value, DateTime.UtcNow);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        var http = context.HttpContext;
        var endpoint = $"{http.Request.Method} {http.Request.Path}";
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var email = http.User.FindFirst(ClaimTypes.Email)?.Value
            ?? http.User.FindFirst(ClaimTypes.Name)?.Value
            ?? "anonimo";
        var userId = http.User.FindFirst("uid")?.Value ?? "anonimo";
        var roles = string.Join(',', http.User.FindAll(ClaimTypes.Role).Select(r => r.Value));
        var success = context.Exception is null && http.Response.StatusCode < 400;
        var elapsedMs = (http.Items[StopwatchKey] as Stopwatch)?.ElapsedMilliseconds ?? 0;
        if (http.Items[StopwatchKey] is Stopwatch sw) sw.Stop();

        _logger.LogInformation("API request finalizado Endpoint:{Endpoint} UsuarioId:{UsuarioId} Email:{Email} Perfil:{Perfil} IP:{Ip} StatusCode:{StatusCode} Sucesso:{Sucesso} DuracaoMs:{DuracaoMs} DataHoraUtc:{DataHoraUtc}", endpoint, userId, email, string.IsNullOrWhiteSpace(roles) ? "sem-perfil" : roles, ip, http.Response.StatusCode, success, elapsedMs, DateTime.UtcNow);
    }
}
