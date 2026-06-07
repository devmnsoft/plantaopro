using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class ComercialController : BaseWebController
{
    public ComercialController(IHttpClientFactory f, ILogger<ComercialController> logger) : base(f, logger) { }

    public IActionResult Index() => RedirectToAction(nameof(Funil));

    public async Task<IActionResult> Leads()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<ComercialLeadViewModel>(client, "api/comercial/leads");
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarLead(ComercialLeadWebRequest request)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await SendApiAsync<ComercialLeadWebRequest, ComercialLeadViewModel>(client, HttpMethod.Post, "api/comercial/leads", request);
        TempData[response.Error is null ? "Success" : "Error"] = response.Error ?? "Lead criado.";
        return RedirectToAction(nameof(Leads));
    }

    public async Task<IActionResult> Oportunidades()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<ComercialOportunidadeViewModel>(client, "api/comercial/oportunidades");
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarOportunidade(ComercialOportunidadeWebRequest request)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await SendApiAsync<ComercialOportunidadeWebRequest, ComercialOportunidadeViewModel>(client, HttpMethod.Post, "api/comercial/oportunidades", request);
        TempData[response.Error is null ? "Success" : "Error"] = response.Error ?? "Oportunidade criada.";
        return RedirectToAction(nameof(Oportunidades));
    }

    public async Task<IActionResult> Propostas()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<ComercialPropostaViewModel>(client, "api/comercial/propostas");
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarProposta(ComercialPropostaWebRequest request)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await SendApiAsync<ComercialPropostaWebRequest, ComercialPropostaViewModel>(client, HttpMethod.Post, "api/comercial/propostas", request);
        TempData[response.Error is null ? "Success" : "Error"] = response.Error ?? "Proposta criada.";
        return RedirectToAction(nameof(Propostas));
    }

    public async Task<IActionResult> Funil()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<FunilItemViewModel>(client, "api/comercial/funil");
        return View(result.Data);
    }

    public async Task<IActionResult> PrevisaoReceita()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponseAsync<object>(client, "api/comercial/previsao-receita");
        ViewBag.Previsao = result.Data;
        return View();
    }
}
