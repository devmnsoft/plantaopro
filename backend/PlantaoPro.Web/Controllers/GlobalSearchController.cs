using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class GlobalSearchController : BaseWebController
{
    public GlobalSearchController(IHttpClientFactory factory, ILogger<GlobalSearchController> logger)
        : base(factory, logger) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int limite = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest(new { message = "Informe ao menos dois caracteres para pesquisar." });

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return Unauthorized();
        var endpoint = $"api/global-search?q={Uri.EscapeDataString(q.Trim())}&limite={Math.Clamp(limite, 1, 20)}";
        using var response = await client.GetAsync(endpoint, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            Content = payload
        };
    }
}
