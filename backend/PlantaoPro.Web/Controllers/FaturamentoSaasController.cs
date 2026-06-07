using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,FINANCEIRO")]
public sealed class FaturamentoSaasController : BaseWebController
{
    public FaturamentoSaasController(IHttpClientFactory httpClientFactory, ILogger<FaturamentoSaasController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index(string? status = null, string? competencia = null, int page = 1)
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var resumo = await ReadApiResponseAsync<FaturamentoSaasResumoViewModel>(client, "api/faturamento-saas/resumo");
            var endpoint = $"api/faturamento-saas/faturas?page={Math.Max(1, page)}&pageSize=20";
            if (!string.IsNullOrWhiteSpace(status)) endpoint += $"&status={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrWhiteSpace(competencia)) endpoint += $"&competencia={Uri.EscapeDataString(competencia)}";
            var faturas = await ReadApiPagedResponseAsync<FaturaSaasViewModel>(client, endpoint, page, 20);

            ViewBag.ErrorMessage = resumo.Error ?? faturas.Error;
            return View(new FaturamentoSaasIndexViewModel
            {
                Resumo = resumo.Data ?? new FaturamentoSaasResumoViewModel(),
                Faturas = faturas.Data,
                Status = status,
                Competencia = competencia
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar faturamento SaaS");
            TempData["ErrorMessage"] = "Não foi possível carregar faturamento SaaS.";
            return View(new FaturamentoSaasIndexViewModel());
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiResponseAsync<FaturaSaasViewModel>(client, $"api/faturamento-saas/faturas/{id}");
        if (data is null)
        {
            TempData["ErrorMessage"] = error ?? "Fatura não encontrada.";
            return RedirectToAction(nameof(Index));
        }

        return View(data);
    }

    public IActionResult GerarMensal() => View(DateOnly.FromDateTime(DateTime.Today));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GerarMensal(DateOnly competencia)
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (_, error, statusCode) = await SendApiAsync<object, IEnumerable<Guid>>(client, HttpMethod.Post, "api/faturamento-saas/gerar-mensal", new { competencia });
            var sucesso = (int)statusCode >= 200 && (int)statusCode <= 299;
            TempData[sucesso ? "SuccessMessage" : "ErrorMessage"] = sucesso ? "Faturamento mensal gerado com sucesso." : error ?? "Não foi possível gerar faturas.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao gerar mensalidade SaaS");
            TempData["ErrorMessage"] = "Não foi possível gerar faturas mensais.";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Inadimplencia(int page = 1)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiPagedResponseAsync<InadimplenciaSaasViewModel>(client, $"api/faturamento-saas/inadimplencia?page={Math.Max(1, page)}&pageSize=20", page, 20);
        ViewBag.ErrorMessage = error;
        return View(data);
    }
}
