using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public sealed class CustomerSuccessController : BaseWebController
{
    public CustomerSuccessController(IHttpClientFactory httpClientFactory, ILogger<CustomerSuccessController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (resumo, error, _) = await ReadApiResponseAsync<SaasResumoExecutivoViewModel>(client, "api/saas-inteligencia/resumo");
        var (alertas, alertasError, _) = await ReadApiListResponseAsync<ClienteAlertaSaasViewModel>(client, "api/saas-dashboard/alertas");
        ViewBag.ErrorMessage = error ?? alertasError;
        return View(new CustomerSuccessIndexViewModel { Resumo = resumo ?? new SaasResumoExecutivoViewModel(), Alertas = alertas });
    }

    public async Task<IActionResult> Details(Guid clienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiResponseAsync<ClienteSaudeSaasViewModel>(client, "api/saas-inteligencia/clientes/" + clienteId + "/saude");
        ViewBag.ErrorMessage = error;
        return View(data ?? new ClienteSaudeSaasViewModel { ClienteId = clienteId });
    }

    public async Task<IActionResult> Alertas(Guid clienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiListResponseAsync<ClienteAlertaSaasViewModel>(client, "api/saas-inteligencia/clientes/" + clienteId + "/alertas");
        ViewBag.ErrorMessage = error;
        return View(data);
    }

    public async Task<IActionResult> Recomendacoes(Guid clienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiListResponseAsync<SaasRecomendacaoViewModel>(client, "api/saas-inteligencia/clientes/" + clienteId + "/recomendacoes");
        ViewBag.ErrorMessage = error;
        return View(data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalcular(Guid clienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (_, error, status) = await SendApiAsync<object, ClienteSaudeSaasViewModel>(client, HttpMethod.Post, "api/saas-inteligencia/clientes/" + clienteId + "/recalcular", new { });
        TempData[(int)status >= 200 && (int)status <= 299 ? "SuccessMessage" : "ErrorMessage"] = (int)status >= 200 && (int)status <= 299 ? "Saúde recalculada." : error ?? "Não foi possível recalcular saúde.";
        return RedirectToAction(nameof(Details), new { clienteId });
    }
}
