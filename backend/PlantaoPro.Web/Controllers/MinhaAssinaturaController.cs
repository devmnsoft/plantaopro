using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Route("MinhaAssinatura")]
public sealed class MinhaAssinaturaController : BaseWebController
{
    public MinhaAssinaturaController(IHttpClientFactory factory, ILogger<MinhaAssinaturaController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await ReadApiResponseAsync<MinhaAssinaturaViewModel>(client, "api/minha-assinatura");
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();

        var model = result.Data ?? new MinhaAssinaturaViewModel();
        if (result.StatusCode == HttpStatusCode.Forbidden)
            model.ErrorMessage = "Você não tem permissão para consultar os dados da assinatura.";
        else if (result.StatusCode == HttpStatusCode.NotFound)
            model.ErrorMessage = null;
        else if (!string.IsNullOrWhiteSpace(result.Error) && result.Data is null)
            model.ErrorMessage = result.Error;

        return View(model);
    }

    [HttpGet("Uso")]
    public IActionResult Uso() => View("Uso");

    [HttpGet("Modulos")]
    public IActionResult Modulos() => View("Modulos");

    [HttpGet("Limites")]
    public IActionResult Limites() => View("Limites");

    [HttpGet("Upgrade")]
    public IActionResult Upgrade() => View("Upgrade", PlanosPublicosController.Planos());

    [HttpGet("Downgrade")]
    public IActionResult Downgrade() => View("Downgrade", PlanosPublicosController.Planos());

    [HttpGet("Faturas")]
    public IActionResult Faturas() => View("Faturas");

    [HttpGet("Cancelamento")]
    public IActionResult Cancelamento() => View("Cancelamento");
}
