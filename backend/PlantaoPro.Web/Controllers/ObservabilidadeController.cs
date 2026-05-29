using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
public sealed class ObservabilidadeController : BaseWebController
{
    public ObservabilidadeController(IHttpClientFactory httpClientFactory, ILogger<ObservabilidadeController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            ViewBag.Resumo = (await ReadApiResponse<object>(client, "api/observabilidade/resumo")).Data;
            ViewBag.Performance = (await ReadApiResponse<object>(client, "api/observabilidade/performance?limit=10")).Data;
            ViewBag.Erros = (await ReadApiResponse<object>(client, "api/observabilidade/erros?limit=10")).Data;
            ViewBag.AcessosNegados = (await ReadApiResponse<object>(client, "api/observabilidade/acessos-negados?limit=10")).Data;
            ViewBag.Logins = (await ReadApiResponse<object>(client, "api/observabilidade/logins?limit=10")).Data;
            return View();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao abrir painel de observabilidade");
            TempData["Error"] = "Falha ao carregar observabilidade.";
            return View();
        }
    }

    public async Task<IActionResult> Erros()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        ViewBag.Erros = (await ReadApiResponse<object>(client, "api/observabilidade/erros?limit=50")).Data;
        return View();
    }

    public async Task<IActionResult> Performance()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        ViewBag.Performance = (await ReadApiResponse<object>(client, "api/observabilidade/performance?limit=50")).Data;
        return View();
    }
}
