using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[ApiController]
// P0 CSRF: valida token antiforgery em todas as ações inseguras (o BFF nunca era protegido antes).
[AutoValidateAntiforgeryToken]
[Route("bff/operacao")]
public sealed class OperationBffController : ControllerBase
{
    private static readonly HashSet<string> ForwardedResponseHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cache-Control", "ETag", "Last-Modified", "Retry-After"
    };

    private readonly IHttpClientFactory _factory;
    private readonly ILogger<OperationBffController> _logger;

    public OperationBffController(IHttpClientFactory factory, ILogger<OperationBffController> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE")]
    [Route("{**path}")]
    public async Task<IActionResult> Proxy(string? path, CancellationToken cancellationToken)
    {
        var token = ResolveToken();
        if (string.IsNullOrWhiteSpace(token))
            return StatusCode(StatusCodes.Status401Unauthorized, BffContracts.Envelope(StatusCodes.Status401Unauthorized, BffContracts.RazaoSessaoExpirada, BffContracts.MensagemSessaoExpirada));

        if (string.IsNullOrWhiteSpace(path) || path.Contains("..", StringComparison.Ordinal))
            return BadRequest(new { message = "O recurso solicitado é inválido." });

        var target = $"api/{path}{Request.QueryString}";
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (Request.ContentLength is > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            request.Content = new StreamContent(Request.Body);
            if (MediaTypeHeaderValue.TryParse(Request.ContentType, out var contentType))
                request.Content.Headers.ContentType = contentType;
        }

        try
        {
            var client = _factory.CreateClient("PlantaoProApi");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // A API não redireciona consumidores de API: 302 p/ login vira 401 JSON e
            // qualquer outro redirect vira 502 JSON — nunca repassamos 3xx para o chamador.
            var redirecionamento = BffContracts.MapUpstreamRedirect(response);
            if (redirecionamento is { } mapeado)
            {
                _logger.LogWarning("API respondeu redirect {Status} -> {Location} em {Target}; convertido para JSON {StatusJson}.",
                    (int)response.StatusCode, response.Headers.Location, target, mapeado.Status);
                return StatusCode(mapeado.Status, BffContracts.Envelope(mapeado.Status, mapeado.Reason, mapeado.Message));
            }

            // Defensivo: em erro, nunca encaminhar página HTML (ex.: página de erro do servidor)
            // para um consumidor que espera JSON.
            var statusHttp = (int)response.StatusCode;
            if (statusHttp >= 400 && BffContracts.IsHtmlBody(response))
            {
                return StatusCode(statusHttp, BffContracts.Envelope(statusHttp, BffContracts.RazaoErroServico, $"O serviço operacional respondeu o status {statusHttp}."));
            }

            foreach (var header in response.Headers.Where(header => ForwardedResponseHeaders.Contains(header.Key)))
                Response.Headers[header.Key] = header.Value.ToArray();

            var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            Response.StatusCode = (int)response.StatusCode;
            return new FileContentResult(payload, contentType);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout ao encaminhar {Method} para {Target}", Request.Method, target);
            return StatusCode((int)HttpStatusCode.GatewayTimeout, new { message = "A operação demorou mais que o esperado. Tente novamente." });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de comunicação ao encaminhar {Method} para {Target}", Request.Method, target);
            return StatusCode((int)HttpStatusCode.BadGateway, new { message = "O serviço operacional está temporariamente indisponível." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha inesperada ao encaminhar {Method} para {Target}", Request.Method, target);
            return StatusCode((int)HttpStatusCode.InternalServerError, new { message = "Não foi possível concluir a operação." });
        }
    }

    private string? ResolveToken()
    {
        foreach (var key in new[] { "jwt", "JwtToken", "AccessToken", "access_token", "accessToken", "token" })
        {
            var value = HttpContext.Session.GetString(key);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return User.FindFirst("jwt")?.Value
            ?? User.FindFirst("Token")?.Value
            ?? User.FindFirst("access_token")?.Value;
    }
}
