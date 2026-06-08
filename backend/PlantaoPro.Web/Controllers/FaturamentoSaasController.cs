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


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarPaga(Guid id, decimal valorPago, DateOnly dataPagamento, string formaPagamento, string? observacoes)
    {
        try
        {
            if (valorPago <= 0 || string.IsNullOrWhiteSpace(formaPagamento))
            {
                TempData["ErrorMessage"] = "Informe valor pago maior que zero e forma de pagamento.";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (_, error, statusCode) = await SendApiAsync<object, string>(client, HttpMethod.Post, $"api/faturamento-saas/faturas/{id}/marcar-paga", new { valorPago, dataPagamento, formaPagamento, observacoes });
            DefinirToast(statusCode, error, "Fatura marcada como paga.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao marcar fatura SaaS como paga {FaturaId}", id);
            TempData["ErrorMessage"] = "Não foi possível marcar a fatura como paga.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(Guid id, string justificativa)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(justificativa))
            {
                TempData["ErrorMessage"] = "Justificativa é obrigatória para cancelar fatura.";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (_, error, statusCode) = await SendApiAsync<object, string>(client, HttpMethod.Post, $"api/faturamento-saas/faturas/{id}/cancelar", new { justificativa });
            DefinirToast(statusCode, error, "Fatura cancelada com sucesso.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao cancelar fatura SaaS {FaturaId}", id);
            TempData["ErrorMessage"] = "Não foi possível cancelar a fatura.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contestar(Guid id, string motivo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["ErrorMessage"] = "Motivo é obrigatório para contestar fatura.";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (_, error, statusCode) = await SendApiAsync<object, string>(client, HttpMethod.Post, $"api/faturamento-saas/faturas/{id}/contestar", new { motivo });
            DefinirToast(statusCode, error, "Fatura enviada para contestação.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao contestar fatura SaaS {FaturaId}", id);
            TempData["ErrorMessage"] = "Não foi possível contestar a fatura.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolverContestacao(Guid id, string resposta)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(resposta))
            {
                TempData["ErrorMessage"] = "Resposta é obrigatória para resolver contestação.";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (_, error, statusCode) = await SendApiAsync<object, string>(client, HttpMethod.Post, $"api/faturamento-saas/faturas/{id}/resolver-contestacao", new { resposta });
            DefinirToast(statusCode, error, "Contestação resolvida com sucesso.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao resolver contestação de fatura SaaS {FaturaId}", id);
            TempData["ErrorMessage"] = "Não foi possível resolver a contestação.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Notificar(Guid id)
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (_, error, statusCode) = await SendApiAsync<object, string>(client, HttpMethod.Post, $"api/faturamento-saas/faturas/{id}/notificar", new { });
            DefinirToast(statusCode, error, "Notificação de cobrança registrada.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao notificar cobrança SaaS {FaturaId}", id);
            TempData["ErrorMessage"] = "Não foi possível notificar a cobrança.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private void DefinirToast(System.Net.HttpStatusCode statusCode, string? error, string sucesso)
    {
        var ok = (int)statusCode >= 200 && (int)statusCode <= 299;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? sucesso : error ?? "A ação não pôde ser concluída.";
    }
}
