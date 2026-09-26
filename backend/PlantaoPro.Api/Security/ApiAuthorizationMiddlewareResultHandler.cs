using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace PlantaoPro.Api.Security;

public sealed class ApiAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var diagnostic = context.Items["EffectiveAccessDiagnostic"] as EffectivePermissionDiagnostic;
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";

            var responseObj = new
            {
                success = false,
                message = diagnostic?.Motivo ?? "Acesso não autorizado para o recurso solicitado.",
                code = diagnostic?.Codigo ?? "FORBIDDEN",
                motivo = diagnostic?.Motivo ?? "Permissão insuficiente ou módulo não contratado para este tenant.",
                origem = diagnostic?.Origem ?? "AUTORIZACAO",
                data = (object?)null
            };

            await context.Response.WriteAsJsonAsync(responseObj);
            return;
        }

        if (authorizeResult.Challenged)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Autenticação obrigatória.",
                code = "UNAUTHORIZED",
                origem = "AUTENTICACAO",
                data = (object?)null
            });
            return;
        }

        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
