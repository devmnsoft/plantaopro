using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Security;

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
            var resumo = await ReadApiResponse<object>(client, "api/observabilidade/resumo");
            var performance = await ReadApiResponse<object>(client, "api/observabilidade/performance?limit=10");
            var erros = await ReadApiResponse<object>(client, "api/observabilidade/erros?limit=10");
            ViewBag.Resumo = resumo.Data;
            ViewBag.Performance = performance.Data;
            ViewBag.Erros = erros.Data;
            return View();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao abrir painel de observabilidade");
            TempData["Error"] = "Falha ao carregar observabilidade.";
            return View();
        }
    }
}
