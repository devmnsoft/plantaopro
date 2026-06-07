using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class LgpdController : BaseWebController
{
    public LgpdController(IHttpClientFactory f, ILogger<LgpdController> logger) : base(f, logger) { }
    public IActionResult Index() => View();
    public IActionResult MinhaPrivacidade() => View("Index");
    public IActionResult Politica() => View();
    public IActionResult Consentimentos() => View();
    public IActionResult Solicitacoes() => View();
    public IActionResult Eventos() => View();
    public IActionResult ExportarDados() => View();
    public IActionResult Retencao() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarSolicitacao(LgpdSolicitacaoFormViewModel model)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var result = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/lgpd/solicitacoes", new { model.Tipo, model.Descricao });
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Solicitação LGPD registrada." : result.Error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar solicitação LGPD via Web");
            TempData["Error"] = "Não foi possível registrar a solicitação.";
        }
        return RedirectToAction("Solicitacoes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarConsentimento(string finalidade, string baseLegal, string versaoPolitica, bool consentido)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var result = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/lgpd/consentimentos/registrar", new { Finalidade = finalidade, BaseLegal = baseLegal, VersaoPolitica = versaoPolitica, Consentido = consentido });
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Consentimento registrado." : result.Error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao registrar consentimento via Web");
            TempData["Error"] = "Não foi possível registrar consentimento.";
        }
        return RedirectToAction("Consentimentos");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Exportar()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponseAsync<object>(client, "api/lgpd/exportar-meus-dados");
        TempData[result.Error is null ? "Success" : "Error"] = result.Error is null ? "Exportação registrada. Consulte o histórico LGPD." : result.Error;
        return RedirectToAction("ExportarDados");
    }
}
