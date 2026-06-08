using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public sealed class PlanosController : BaseWebController
{
    public PlanosController(IHttpClientFactory httpClientFactory, ILogger<PlanosController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index(string? status = null, int page = 1)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = "api/planos?page=" + Math.Max(1, page) + "&pageSize=20";
        if (!string.IsNullOrWhiteSpace(status)) endpoint += "&status=" + Uri.EscapeDataString(status);
        var (data, error, _) = await ReadApiPagedResponseAsync<PlanoSaasViewModel>(client, endpoint, page, 20);
        ViewBag.ErrorMessage = error;
        return View(new PlanosSaasIndexViewModel { Planos = data, Status = status });
    }

    public IActionResult Create() => View(new PlanoSaasViewModel { Status = "ATIVO" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlanoSaasViewModel model)
    {
        return await EnviarPlanoAsync(HttpMethod.Post, "api/planos", model, "Plano criado com sucesso.", nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await ObterPlanoAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PlanoSaasViewModel model)
    {
        return await EnviarPlanoAsync(HttpMethod.Put, "api/planos/" + id, model, "Plano atualizado com sucesso.", nameof(Index));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await ObterPlanoAsync(id);
        if (model is null) return RedirectToAction(nameof(Index));
        return View(model);
    }

    public async Task<IActionResult> Recursos(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiListResponseAsync<PlanoRecursoSaasViewModel>(client, "api/planos/" + id + "/recursos");
        ViewBag.PlanoId = id;
        ViewBag.ErrorMessage = error;
        return View(data);
    }

    public async Task<IActionResult> Comparativo()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiPagedResponseAsync<PlanoSaasViewModel>(client, "api/planos?page=1&pageSize=100&status=ATIVO", 1, 100);
        ViewBag.ErrorMessage = error;
        return View(data.Items ?? Array.Empty<PlanoSaasViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(Guid id, string acao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = string.Equals(acao, "REATIVAR", StringComparison.OrdinalIgnoreCase) ? "api/planos/" + id + "/reativar" : "api/planos/" + id + "/inativar";
        var (_, error, status) = await SendApiAsync<object, string>(client, HttpMethod.Post, endpoint, new { justificativa = "Ação comercial pela Web" });
        TempData[(int)status >= 200 && (int)status <= 299 ? "SuccessMessage" : "ErrorMessage"] = (int)status >= 200 && (int)status <= 299 ? "Status do plano atualizado." : error ?? "Não foi possível alterar status.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PlanoSaasViewModel?> ObterPlanoAsync(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return null;
        var (data, error, _) = await ReadApiResponseAsync<PlanoSaasViewModel>(client, "api/planos/" + id);
        if (data is null) TempData["ErrorMessage"] = error ?? "Plano não encontrado.";
        return data;
    }

    private async Task<IActionResult> EnviarPlanoAsync(HttpMethod method, string endpoint, PlanoSaasViewModel model, string successMessage, string successAction)
    {
        try
        {
            if (model.ValorMensal < 0 || model.LimiteMedicos < 0 || model.LimiteHospitais < 0 || model.LimitePlantoesMes < 0 || model.LimiteUsuarios < 0 || model.LimiteConvitesMes < 0)
            {
                ModelState.AddModelError(string.Empty, "Valores e limites não podem ser negativos.");
                return View(model);
            }
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (_, error, status) = await SendApiAsync<PlanoSaasViewModel, object>(client, method, endpoint, model);
            if ((int)status < 200 || (int)status > 299)
            {
                TempData["ErrorMessage"] = error ?? "Não foi possível salvar plano.";
                return View(model);
            }
            TempData["SuccessMessage"] = successMessage;
            return RedirectToAction(successAction);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao salvar plano SaaS");
            TempData["ErrorMessage"] = "Não foi possível salvar plano.";
            return View(model);
        }
    }
}
