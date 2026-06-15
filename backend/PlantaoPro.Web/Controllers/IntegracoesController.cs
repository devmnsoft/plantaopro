using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class IntegracoesController : BaseWebController
{
    public IntegracoesController(IHttpClientFactory f, ILogger<IntegracoesController> l) : base(f, l) { }
    public IActionResult Index() => View("ApiKeys");
    public IActionResult ApiKeys() => View();
    public IActionResult Webhooks() => View();
    public IActionResult WebhookDetails(Guid id) { ViewBag.WebhookId = id; return View(); }
    public IActionResult Logs() => View();
}
