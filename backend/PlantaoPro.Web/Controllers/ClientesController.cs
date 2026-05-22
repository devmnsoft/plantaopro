using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR")]
public class ClientesController : BaseWebController
{
    public ClientesController(IHttpClientFactory httpClientFactory, ILogger<ClientesController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiResponse<IEnumerable<ClienteDto>>(client, "api/clientes");
        ViewBag.ErrorMessage = error;
        return View(data ?? Array.Empty<ClienteDto>());
    }
}
