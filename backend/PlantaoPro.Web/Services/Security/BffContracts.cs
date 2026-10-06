using System.Net;
using Microsoft.AspNetCore.Http;

namespace PlantaoPro.Web.Services.Security;

/// <summary>
/// Contrato canônico de respostas do BFF (/bff/*): consumidores de API sempre recebem
/// status HTTP adequado + envelope JSON — nunca redirect para HTML, nem página de erro
/// HTML, mesmo quando o problema é de autenticação/autorização/indisponibilidade da API.
/// </summary>
public static class BffContracts
{
    public const string RazaoSessaoExpirada = "SESSAO_EXPIRADA";
    public const string RazaoAcessoNegado = "ACESSO_NEGADO";
    public const string RazaoClienteBloqueado = "CLIENTE_BLOQUEADO";
    public const string RazaoServicoIndisponivel = "SERVICO_INDISPONIVEL";
    public const string RazaoErroServico = "ERRO_SERVICO";
    public const string RazaoDadosInvalidos = "DADOS_INVALIDOS";

    public const string MensagemSessaoExpirada = "Sessão expirada ou não autenticada. Entre novamente para continuar.";
    public const string MensagemAcessoNegado = "Você não tem permissão para acessar este recurso.";
    public const string MensagemClienteBloqueado = "Acesso suspenso para esta conta. Fale com o suporte para regularizar a assinatura.";
    public const string MensagemServicoIndisponivel = "O serviço operacional está temporariamente indisponível.";

    public static bool IsBffPath(PathString path) => path.StartsWithSegments("/bff", StringComparison.OrdinalIgnoreCase);

    public static object Envelope(int status, string reason, string message) => new { status, reason, message };

    public static async Task RespondAsync(HttpContext context, int status, string reason, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(Envelope(status, reason, message));
    }

    /// <summary>
    /// A API operacional não deve redirecionar um consumidor de API. Se redirecionar para
    /// login, expiração/revogação vira 401 JSON; qualquer outro destino vira 502 JSON
    /// (o BFF é a fronteira JSON e não repassa 3xx para o chamador).
    /// </summary>
    public static (int Status, string Reason, string Message)? MapUpstreamRedirect(HttpResponseMessage response)
    {
        var codigo = (int)response.StatusCode;
        if (codigo < 300 || codigo > 399) return null;
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        if (location.Contains("login", StringComparison.OrdinalIgnoreCase))
            return (StatusCodes.Status401Unauthorized, RazaoSessaoExpirada, MensagemSessaoExpirada);
        return (StatusCodes.Status502BadGateway, RazaoServicoIndisponivel, MensagemServicoIndisponivel);
    }

    public static bool IsHtmlBody(HttpResponseMessage response)
    {
        var mediaType = response.Content?.Headers.ContentType?.MediaType ?? string.Empty;
        return mediaType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
    }
}
