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

    public IActionResult Contas() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Contas Customer Success", "Health score, adoção, faturas, onboarding e plantões publicados.", "CustomerSuccess", "Riscos"));
    public IActionResult ContaDetails(Guid tenantId) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes da conta CS", "Interações, plano de ação, NPS, riscos e oportunidades do tenant.", "CustomerSuccess", "Contas"));
    public IActionResult Riscos() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Riscos e churn", "Sinais determinísticos de risco, inadimplência, NPS baixo e cancelamento provável.", "CustomerSuccess", "PlanosAcao"));
    public IActionResult Oportunidades() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Oportunidades de expansão", "Uso acima de 80%, módulos adicionais e upgrade de plano.", "CustomerSuccess", "Contas"));
    public IActionResult Nps() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("NPS por período", "Registro de satisfação e geração de risco quando nota baixa.", "CustomerSuccess", "Riscos"));
    public IActionResult Playbooks() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Playbooks CS", "Ações padronizadas para onboarding, risco, expansão e retenção.", "CustomerSuccess", "PlanosAcao"));
    public IActionResult PlanosAcao() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Planos de ação", "Tarefas, responsáveis e prazos para recuperação de saúde do cliente.", "CustomerSuccess", "Contas"));
}
