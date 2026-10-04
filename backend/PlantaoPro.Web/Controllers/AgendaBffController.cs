using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// Same-origin facade for the operational calendar. Authentication remains in the
/// server session and the browser never receives the API bearer token.
/// </summary>
[Authorize]
[ApiController]
// P0 CSRF: valida token antiforgery em todas as ações inseguras (hoje só GET; protege futuras ações de escrita).
[AutoValidateAntiforgeryToken]
[Route("bff/agenda")]
public sealed class AgendaBffController : ControllerBase
{
    private readonly IHttpClientFactory _factory;

    public AgendaBffController(IHttpClientFactory factory) => _factory = factory;

    [HttpGet]
    public Task<IActionResult> Resumo(CancellationToken ct) => ForwardAsync("api/agenda", ct);

    [HttpGet("eventos")]
    public Task<IActionResult> Eventos(CancellationToken ct) => ForwardAsync("api/agenda/eventos", ct);

    [HttpGet("conflitos")]
    public Task<IActionResult> Conflitos(CancellationToken ct) => ForwardAsync("api/agenda/conflitos", ct);

    [HttpGet("medicos")]
    public Task<IActionResult> Medicos(CancellationToken ct) => ForwardAsync("api/agenda/medicos", ct);

    [HttpGet("hospitais")]
    public Task<IActionResult> Hospitais(CancellationToken ct) => ForwardAsync("api/agenda/hospitais", ct);

    private async Task<IActionResult> ForwardAsync(string endpoint, CancellationToken ct)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrWhiteSpace(token))
            return StatusCode(StatusCodes.Status401Unauthorized, BffContracts.Envelope(StatusCodes.Status401Unauthorized, BffContracts.RazaoSessaoExpirada, BffContracts.MensagemSessaoExpirada));

        var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var client = _factory.CreateClient("PlantaoProApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync(endpoint + query, ct);

        var redirecionamento = BffContracts.MapUpstreamRedirect(response);
        if (redirecionamento is { } mapeado)
            return StatusCode(mapeado.Status, BffContracts.Envelope(mapeado.Status, mapeado.Reason, mapeado.Message));

        var payload = await response.Content.ReadAsByteArrayAsync(ct);
        Response.StatusCode = (int)response.StatusCode;
        return File(payload, response.Content.Headers.ContentType?.ToString() ?? "application/json");
    }
}
