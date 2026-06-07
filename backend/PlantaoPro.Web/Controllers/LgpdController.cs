using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class LgpdController : BaseWebController
{
    public LgpdController(IHttpClientFactory f, ILogger<LgpdController> logger) : base(f, logger) { }

    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var consentimentos = await ReadApiListResponseAsync<LgpdConsentimentoViewModel>(client, "api/lgpd/consentimentos");
        var solicitacoes = await ReadApiListResponseAsync<LgpdSolicitacaoViewModel>(client, "api/lgpd/solicitacoes");
        ViewBag.Consentimentos = consentimentos.Data;
        ViewBag.Solicitacoes = solicitacoes.Data;
        return View();
    }

    public async Task<IActionResult> Politica()
    {
        var client = CreateApiClient();
        AddBearerToken(client);
        var result = await ReadApiResponseAsync<LgpdPoliticaViewModel>(client, "api/lgpd/politica-atual");
        return View(result.Data ?? new LgpdPoliticaViewModel());
    }

    public async Task<IActionResult> Consentimentos()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<LgpdConsentimentoViewModel>(client, "api/lgpd/consentimentos");
        return View(result.Data);
    }

    public async Task<IActionResult> Solicitacoes()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<LgpdSolicitacaoViewModel>(client, "api/lgpd/solicitacoes");
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarSolicitacao(CriarSolicitacaoLgpdWebRequest request)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var response = await SendApiAsync<CriarSolicitacaoLgpdWebRequest, LgpdSolicitacaoViewModel>(client, HttpMethod.Post, "api/lgpd/solicitacoes", request);
            TempData[response.Error is null ? "Success" : "Error"] = response.Error ?? "Solicitação LGPD registrada.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro web ao criar solicitação LGPD");
            TempData["Error"] = "Não foi possível registrar a solicitação.";
        }
        return RedirectToAction(nameof(Solicitacoes));
    }

    public async Task<IActionResult> Eventos()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<LgpdEventoViewModel>(client, "api/lgpd/eventos");
        return View(result.Data);
    }

    public IActionResult ExportarDados() => View();
    public IActionResult MinhaPrivacidade() => RedirectToAction(nameof(Index));

    public async Task<IActionResult> BaixarExportacao()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponseAsync<object>(client, "api/lgpd/exportar-meus-dados");
        if (result.Error is not null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(ExportarDados));
        }
        TempData["Success"] = "Exportação LGPD gerada e auditada.";
        return Json(result.Data);
    }

    public async Task<IActionResult> Retencao()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<LgpdRetencaoViewModel>(client, "api/lgpd/retencao");
        return View(result.Data);
    }
}
