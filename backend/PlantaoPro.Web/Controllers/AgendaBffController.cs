using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// Same-origin facade for the operational calendar. Authentication remains in the
/// server session and the browser never receives the API bearer token.
/// </summary>
[Authorize]
[ApiController]
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
        if (string.IsNullOrWhiteSpace(token)) return Unauthorized();

        var query = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var client = _factory.CreateClient("PlantaoProApi");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.GetAsync(endpoint + query, ct);
        var payload = await response.Content.ReadAsByteArrayAsync(ct);
        Response.StatusCode = (int)response.StatusCode;
        return File(payload, response.Content.Headers.ContentType?.ToString() ?? "application/json");
    }
}
