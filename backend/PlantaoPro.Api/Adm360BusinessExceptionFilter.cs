using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

/// <summary>
/// Mapeia erros de negócio CONHECIDOS do módulo Administrativo 360 para o código HTTP
/// correspondente, em vez do 500 genérico do handler global:
///   - Administrativo360BusinessException / ArgumentException → 400 (regra de negócio violada ou entrada inválida);
///   - UnauthorizedAccessException → 403 (acesso negado no domínio);
///   - KeyNotFoundException → 404 (registro inexistente).
/// Falhas técnicas (qualquer outro tipo, inclusive InvalidOperationException sem tipo específico)
/// passam por aqui sem tratamento e chegam ao handler global, que responde 500 e registra log
/// estruturado com correlação. Aplica-se apenas a rotas que contêm "administrativo360".
/// </summary>
public sealed class Adm360BusinessExceptionFilter : IExceptionFilter
{
    private static readonly Regex ParameterNameSuffix = new(@" \(Parameter '[^']*'\)\s*$", RegexOptions.Compiled);

    public void OnException(ExceptionContext context)
    {
        var path = context.HttpContext.Request.Path.Value;
        if (path is null || !path.Contains("/administrativo360", StringComparison.OrdinalIgnoreCase)) return;

        var ex = context.Exception;
        int status; string mensagemPadrao; string message;
        switch (ex)
        {
            case Administrativo360BusinessException: // regra de negócio explícita do módulo
            case ArgumentException:                  // entrada inválida (inclui ArgumentOutOfRangeException)
                status = StatusCodes.Status400BadRequest;
                mensagemPadrao = "Solicitação inválida.";
                message = MensagemUtil(ex.Message);
                break;
            case KeyNotFoundException kfn:           // registro não encontrado
                status = StatusCodes.Status404NotFound;
                mensagemPadrao = "Registro não encontrado.";
                // Se a exceção não carrega mensagem significativa (padrão técnico do .NET), usa a amigável em pt-BR.
                message = kfn.Message == new KeyNotFoundException().Message ? mensagemPadrao : MensagemUtil(kfn.Message);
                break;
            case UnauthorizedAccessException uaa:    // acesso negado no domínio
                status = StatusCodes.Status403Forbidden;
                mensagemPadrao = "Acesso não autorizado.";
                message = uaa.Message == new UnauthorizedAccessException().Message ? mensagemPadrao : MensagemUtil(uaa.Message);
                break;
            default:
                // Falha técnica: mantém o fluxo padrão (handler global → HTTP 500 + log estruturado com correlação).
                return;
        }

        if (string.IsNullOrWhiteSpace(message)) message = mensagemPadrao;
        context.Result = new JsonResult(ApiResponse<string>.Fail(message, status))
        {
            StatusCode = status
        };
        context.ExceptionHandled = true;
    }

    /// <summary>Tira o sufixo técnico "(Parameter 'x')" e espaços; aceita null.</summary>
    private static string MensagemUtil(string? mensagem) =>
        ParameterNameSuffix.Replace(mensagem ?? string.Empty, string.Empty).Trim();
}
