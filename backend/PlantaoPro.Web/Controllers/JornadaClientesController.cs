using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class JornadaClientesController : BaseWebController
{
    public JornadaClientesController(IHttpClientFactory f, ILogger<JornadaClientesController> logger) : base(f, logger) { }
    public IActionResult Index() => View();
    public IActionResult Details(Guid id) { ViewBag.ClienteId = id; return View(); }
    public IActionResult Funil() => View();
    public IActionResult Tarefas() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avancar(JornadaClienteFormViewModel model)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var r = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/jornada-clientes/" + model.ClienteId + "/avancar", new { model.Motivo });
            TempData[r.Success ? "Success" : "Error"] = r.Success ? "Etapa avançada." : r.Error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao avançar jornada do cliente {ClienteId}", model.ClienteId);
            TempData["Error"] = "Não foi possível avançar etapa.";
        }
        return RedirectToAction("Details", new { id = model.ClienteId });
    }
}
