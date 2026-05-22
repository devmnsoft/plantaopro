using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class OnboardingController : BaseWebController
{
    public OnboardingController(IHttpClientFactory httpClientFactory, ILogger<OnboardingController> logger) : base(httpClientFactory, logger) { }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult NovoCliente() => View(new OnboardingClienteViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NovoCliente(OnboardingClienteViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var req = model.ToRequest();
            var response = await client.PostAsJsonAsync("api/onboarding/cliente", req);
            var payload = await response.Content.ReadFromJsonAsync<ApiResponse<OnboardingResumoDto>>();

            if (!response.IsSuccessStatusCode || payload?.Data is null)
            {
                ModelState.AddModelError(string.Empty, payload?.Message ?? "Não foi possível concluir o onboarding.");
                return View(model);
            }

            return RedirectToAction(nameof(Sucesso), new { clienteId = payload.Data.ClienteId });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao executar onboarding web");
            ModelState.AddModelError(string.Empty, "Erro inesperado ao criar cliente.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Sucesso(Guid clienteId)
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var (data, error, _) = await ReadApiResponse<OnboardingResumoDto>(client, $"api/onboarding/resumo?clienteId={clienteId}");
            if (data is null) return EmptyViewWithError(new OnboardingResumoDto(), error ?? "Resumo não disponível.");
            return View(data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar página de sucesso do onboarding.");
            return EmptyViewWithError(new OnboardingResumoDto(), "Erro ao carregar resumo.");
        }
    }
}
