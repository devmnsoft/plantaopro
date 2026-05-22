using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class OperacaoController : BaseWebController
{
    public OperacaoController(IHttpClientFactory httpClientFactory, ILogger<OperacaoController> logger) : base(httpClientFactory, logger)
    {
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client))
            {
                return HandleUnauthorized();
            }

            var (data, error, statusCode) = await ReadApiResponse<OperacaoResumoDto>(client, "api/operacao/resumo");
            LogRequestContext("WEB_OPERACAO_INDEX", "api/operacao/resumo", (int)statusCode);

            if (data is null)
            {
                ViewBag.ErrorMessage = error ?? "Não foi possível carregar a central operacional.";
                return View(OperacaoResumoDto.Empty());
            }

            return View(data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Falha ao carregar a central operacional.");
            ViewBag.ErrorMessage = "Falha inesperada ao carregar a central operacional.";
            return View(OperacaoResumoDto.Empty());
        }
    }
}
