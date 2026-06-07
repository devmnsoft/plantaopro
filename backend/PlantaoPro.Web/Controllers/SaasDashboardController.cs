using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public sealed class SaasDashboardController : BaseWebController
{
    public SaasDashboardController(IHttpClientFactory httpClientFactory, ILogger<SaasDashboardController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (resumo, error, _) = await ReadApiResponseAsync<SaasResumoExecutivoViewModel>(client, "api/saas-dashboard/resumo");
        var (alertas, alertasError, _) = await ReadApiListResponseAsync<ClienteAlertaSaasViewModel>(client, "api/saas-dashboard/alertas");
        ViewBag.ErrorMessage = error ?? alertasError;
        ViewBag.Alertas = alertas;
        return View(resumo ?? new SaasResumoExecutivoViewModel());
    }
}
