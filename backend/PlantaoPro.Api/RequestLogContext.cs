using Microsoft.AspNetCore.Mvc.Filters;

namespace PlantaoPro.Api;

public sealed class RequestLogContextFilter : IActionFilter
{
    private readonly ILogger<RequestLogContextFilter> _logger;

    public RequestLogContextFilter(ILogger<RequestLogContextFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var http = context.HttpContext;
        var endpoint = $"{http.Request.Method} {http.Request.Path}";
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var email = http.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "anonimo";
        var roles = string.Join(',', http.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(r => r.Value));

        _logger.LogInformation("API request iniciado Endpoint:{Endpoint} Email:{Email} Perfil:{Perfil} IP:{Ip} DataHoraUtc:{DataHoraUtc}", endpoint, email, string.IsNullOrWhiteSpace(roles) ? "sem-perfil" : roles, ip, DateTime.UtcNow);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        var http = context.HttpContext;
        var endpoint = $"{http.Request.Method} {http.Request.Path}";
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var email = http.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "anonimo";
        var roles = string.Join(',', http.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(r => r.Value));
        var success = context.Exception is null && http.Response.StatusCode < 400;

        _logger.LogInformation("API request finalizado Endpoint:{Endpoint} Email:{Email} Perfil:{Perfil} IP:{Ip} StatusCode:{StatusCode} Sucesso:{Sucesso} DataHoraUtc:{DataHoraUtc}", endpoint, email, string.IsNullOrWhiteSpace(roles) ? "sem-perfil" : roles, ip, http.Response.StatusCode, success, DateTime.UtcNow);
    }
}
