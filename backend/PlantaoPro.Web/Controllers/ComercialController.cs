using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class ComercialController : BaseWebController
{
    public ComercialController(IHttpClientFactory f, ILogger<ComercialController> logger) : base(f, logger) { }
    public IActionResult Leads() => View();
    public IActionResult Oportunidades() => View();
    public IActionResult Propostas() => View();
    public IActionResult Index() => RedirectToAction(nameof(Funil));
    public IActionResult Funil() => View();
    public IActionResult PrevisaoReceita() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarLead(ComercialLeadFormViewModel model)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var result = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/comercial/leads", model);
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Lead criado e plano sugerido pelo motor comercial." : result.Error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar lead comercial");
            TempData["Error"] = "Não foi possível criar lead.";
        }
        return RedirectToAction("Leads");
    }
}
