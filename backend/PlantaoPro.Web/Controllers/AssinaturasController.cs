using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,FINANCEIRO")]
public sealed class AssinaturasController : BaseWebController
{
    public AssinaturasController(IHttpClientFactory httpClientFactory, ILogger<AssinaturasController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index(string? status = null, int page = 1)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = "api/assinaturas?page=" + Math.Max(1, page) + "&pageSize=20";
        if (!string.IsNullOrWhiteSpace(status)) endpoint += "&status=" + Uri.EscapeDataString(status);
        var (data, error, _) = await ReadApiPagedResponseAsync<AssinaturaSaasViewModel>(client, endpoint, page, 20);
        ViewBag.ErrorMessage = error;
        return View(new AssinaturasSaasIndexViewModel { Assinaturas = data, Status = status });
    }

    public IActionResult Create() => View(new AssinaturaSaasViewModel { DataInicio = DateTime.Today, DataFim = DateTime.Today.AddYears(1), DiaVencimento = 10, Status = "ATIVA" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssinaturaSaasViewModel model)
    {
        return await EnviarAsync(HttpMethod.Post, "api/assinaturas", model, "Assinatura criada com sucesso.");
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await ObterAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AssinaturaSaasViewModel model)
    {
        return await EnviarAsync(HttpMethod.Put, "api/assinaturas/" + id, model, "Assinatura atualizada com sucesso.");
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await ObterAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));
        return View(model);
    }

    public async Task<IActionResult> Uso(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiResponseAsync<UsoPlanoViewModel>(client, "api/assinaturas/" + id + "/uso");
        ViewBag.ErrorMessage = error;
        return View(data ?? new UsoPlanoViewModel());
    }

    public async Task<IActionResult> AlterarPlano(Guid id)
    {
        var model = await ObterAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarPlano(Guid id, Guid planoId, string justificativa)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (_, error, status) = await SendApiAsync<object, string>(client, HttpMethod.Post, "api/assinaturas/" + id + "/alterar-plano", new { planoId, justificativa });
        TempData[(int)status >= 200 && (int)status <= 299 ? "SuccessMessage" : "ErrorMessage"] = (int)status >= 200 && (int)status <= 299 ? "Plano alterado." : error ?? "Não foi possível alterar plano.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(Guid id, string acao, string justificativa)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = "api/assinaturas/" + id + "/" + (acao ?? string.Empty).ToLowerInvariant();
        var (_, error, status) = await SendApiAsync<object, string>(client, HttpMethod.Post, endpoint, new { justificativa });
        TempData[(int)status >= 200 && (int)status <= 299 ? "SuccessMessage" : "ErrorMessage"] = (int)status >= 200 && (int)status <= 299 ? "Status da assinatura atualizado." : error ?? "Não foi possível alterar status.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<AssinaturaSaasViewModel?> ObterAsync(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return null;
        var (data, error, _) = await ReadApiResponseAsync<AssinaturaSaasViewModel>(client, "api/assinaturas/" + id);
        if (data is null) TempData["ErrorMessage"] = error ?? "Assinatura não encontrada.";
        return data;
    }

    private async Task<IActionResult> EnviarAsync(HttpMethod method, string endpoint, AssinaturaSaasViewModel model, string successMessage)
    {
        if (model.DataFim <= model.DataInicio || model.ValorContratado < 0)
        {
            ModelState.AddModelError(string.Empty, "Revise datas e valor contratado.");
            return View(model);
        }
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (_, error, status) = await SendApiAsync<AssinaturaSaasViewModel, object>(client, method, endpoint, model);
        if ((int)status < 200 || (int)status > 299)
        {
            TempData["ErrorMessage"] = error ?? "Não foi possível salvar assinatura.";
            return View(model);
        }
        TempData["SuccessMessage"] = successMessage;
        return RedirectToAction(nameof(Index));
    }
}
