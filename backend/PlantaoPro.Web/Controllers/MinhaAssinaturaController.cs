using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Route("MinhaAssinatura")]
public sealed class MinhaAssinaturaController : BaseWebController
{
    public MinhaAssinaturaController(IHttpClientFactory factory, ILogger<MinhaAssinaturaController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await ReadApiResponseAsync<MinhaAssinaturaViewModel>(client, "api/minha-assinatura");
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();

        var model = result.Data ?? new MinhaAssinaturaViewModel();
        if (result.StatusCode == HttpStatusCode.Forbidden)
            model.ErrorMessage = "Você não tem permissão para consultar os dados da assinatura.";
        else if (result.StatusCode == HttpStatusCode.NotFound)
            model.ErrorMessage = null;
        else if (!string.IsNullOrWhiteSpace(result.Error) && result.Data is null)
            model.ErrorMessage = result.Error;

        return View(model);
    }

    [HttpGet("Uso")]
    public IActionResult Uso() => View("Uso");

    [HttpGet("Modulos")]
    public async Task<IActionResult> Modulos()
    {
        var model = new ClientModulesPageViewModel();
        await PopulateModulesAsync(model);
        return View("Modulos", model);
    }

    [HttpPost("Modulos/Revisar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevisarModulos(ClientModuleReviewInput input)
    {
        var model = new ClientModulesPageViewModel();
        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Selecione ao menos um módulo disponível para revisar.";
            await PopulateModulesAsync(model);
            return View("Modulos", model);
        }

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await SendApiAsync<ClientModuleReviewInput, ClientModuleReviewViewModel>(client, HttpMethod.Post, "api/portal-cliente/modulos/revisao", input);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        model.Revisao = result.Data;
        if (model.Revisao is not null) model.Revisao.IdempotencyKey = Guid.NewGuid().ToString("N");
        else model.ErrorMessage = result.Error ?? "Não foi possível revisar as condições comerciais.";
        await PopulateModulesAsync(model);
        return View("Modulos", model);
    }

    [HttpPost("Modulos/Confirmar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarModulos(ClientModuleConfirmInput input)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "A revisão expirou ou está incompleta. Revise a oferta novamente."; return RedirectToAction(nameof(Modulos)); }
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await SendApiAsync<ClientModuleConfirmInput, ClientModuleRequestViewModel>(client, HttpMethod.Post, "api/portal-cliente/modulos/solicitacoes", input);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (result.Data is null)
        {
            TempData["Error"] = result.StatusCode == HttpStatusCode.Conflict
                ? "As condições mudaram ou a solicitação já foi tratada. Revise a oferta atual antes de confirmar."
                : result.Error ?? "Não foi possível registrar a solicitação.";
        }
        else TempData["Success"] = $"Solicitação {result.Data.Protocolo} registrada para análise. O módulo ainda não foi ativado.";
        return RedirectToAction(nameof(Modulos));
    }

    [HttpPost("Modulos/Solicitacoes/{id:guid}/Cancelar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarSolicitacaoModulo(Guid id, string justificativa)
    {
        if (string.IsNullOrWhiteSpace(justificativa)) { TempData["Error"] = "Informe o motivo do cancelamento."; return RedirectToAction(nameof(Modulos)); }
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await SendApiAsync<object, object>(client, HttpMethod.Post, $"api/portal-cliente/modulos/solicitacoes/{id}/cancelar", new { Justificativa = justificativa, ConditionsVersion = string.Empty });
        TempData[result.Data is null ? "Error" : "Success"] = result.Data is null ? result.Error ?? "Não foi possível cancelar a solicitação." : "Solicitação cancelada. Nenhum dado operacional foi removido.";
        return RedirectToAction(nameof(Modulos));
    }

    private async Task PopulateModulesAsync(ClientModulesPageViewModel model)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) { model.ErrorMessage ??= "Sessão expirada."; return; }
        var catalog = await ReadApiListResponseAsync<ClientModuleViewModel>(client, "api/portal-cliente/modulos/catalogo");
        var requests = await ReadApiListResponseAsync<ClientModuleRequestViewModel>(client, "api/portal-cliente/modulos/solicitacoes");
        model.Catalogo = catalog.Data.ToArray();
        model.Solicitacoes = requests.Data.ToArray();
        model.ErrorMessage ??= catalog.Error ?? requests.Error;
    }

    [HttpGet("Limites")]
    public IActionResult Limites() => View("Limites");

    [HttpGet("Upgrade")]
    public IActionResult Upgrade() => View("Upgrade", PlanosPublicosController.Planos());

    [HttpGet("Downgrade")]
    public IActionResult Downgrade() => View("Downgrade", PlanosPublicosController.Planos());

    [HttpGet("Faturas")]
    public IActionResult Faturas() => View("Faturas");

    [HttpGet("Cancelamento")]
    public IActionResult Cancelamento() => View("Cancelamento");
}
