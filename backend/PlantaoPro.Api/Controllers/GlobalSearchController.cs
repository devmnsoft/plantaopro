using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize, Route("api/global-search")]
public sealed class GlobalSearchController : ControllerBase
{
    private readonly IGlobalSearchService service;

    public GlobalSearchController(IGlobalSearchService service) => this.service = service;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery(Name = "q")] string? query,
        [FromQuery(Name = "limite")] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await service.SearchAsync(query, limit, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
