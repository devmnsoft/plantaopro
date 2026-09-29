using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

/// <summary>
/// Mapeia excecoes de negocio do modulo Administrativo 360 (guards de dominio/repositorio)
/// para HTTP 400 com a mensagem tecnica, em vez do 500 generico do handler global.
/// Aplica-se apenas aos controllers cuja rota contem "administrativo360".
/// </summary>
public sealed class Adm360BusinessExceptionFilter : IExceptionFilter
{
    private static readonly Regex ParameterNameSuffix = new(@" \(Parameter '[^']*'\)\s*$", RegexOptions.Compiled);

    public void OnException(ExceptionContext context)
    {
        var path = context.HttpContext.Request.Path.Value;
        if (path is null || !path.Contains("/administrativo360", StringComparison.OrdinalIgnoreCase)) return;

        var ex = context.Exception;
        if (ex is not (ArgumentException or InvalidOperationException or KeyNotFoundException)) return;
        if (string.IsNullOrWhiteSpace(ex.Message)) return;

        var message = ParameterNameSuffix.Replace(ex.Message, string.Empty).Trim();
        context.Result = new JsonResult(ApiResponse<string>.Fail(message, 400))
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
        context.ExceptionHandled = true;
    }
}
