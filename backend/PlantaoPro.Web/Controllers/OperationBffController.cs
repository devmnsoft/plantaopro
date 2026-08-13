using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[ApiController]
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
            return Unauthorized(new { message = "Sessão expirada ou não autenticada. Entre novamente para continuar." });

        if (string.IsNullOrWhiteSpace(path) || path.Contains("..", StringComparison.Ordinal))
            return BadRequest(new { message = "O recurso solicitado é inválido." });

        var target = $"api/{path}{Request.QueryString}";
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), target);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (Request.ContentLength is > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            request.Content = new StreamContent(Request.Body);
            if (!string.IsNullOrWhiteSpace(Request.ContentType))
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
        }

        try
        {
            var client = _factory.CreateClient("PlantaoProApi");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de comunicação ao encaminhar {Method} para {Target}", Request.Method, target);
            return StatusCode((int)HttpStatusCode.BadGateway, new { message = "O serviço operacional está temporariamente indisponível." });
        }
    }

    private string? ResolveToken()
    {
        foreach (var key in new[] { "jwt", "JwtToken", "AccessToken" })
        {
            var value = HttpContext.Session.GetString(key);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return User.FindFirst("jwt")?.Value
            ?? User.FindFirst("Token")?.Value
            ?? User.FindFirst("access_token")?.Value;
    }
}
