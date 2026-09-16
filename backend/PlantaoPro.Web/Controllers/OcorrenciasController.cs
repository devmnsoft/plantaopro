using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class OcorrenciasController : BaseWebController
{
    public OcorrenciasController(IHttpClientFactory factory, ILogger<OcorrenciasController> logger)
        : base(factory, logger) { }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? id, CancellationToken ct)
    {
        if (!id.HasValue || id == Guid.Empty)
            return RedirectToAction("Index", "Pendencias", new { module = "OCORRENCIAS" });

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.GetAsync($"api/ocorrencias/{id:D}", ct);
        if (response.StatusCode == HttpStatusCode.Forbidden) return Forbid();
        if (response.StatusCode == HttpStatusCode.NotFound) return NotFound();
        if (!response.IsSuccessStatusCode)
            return View(new OccurrenceDetailViewModel { Error = "Não foi possível consultar o estado atual da ocorrência." });
        var model = await response.Content.ReadFromJsonAsync<OccurrenceDetailViewModel>(cancellationToken: ct) ?? new();
        var historyResponse = await client.GetAsync($"api/ocorrencias/{id:D}/historico", ct);
        if (historyResponse.IsSuccessStatusCode)
            model.Historico = await historyResponse.Content.ReadFromJsonAsync<IReadOnlyList<OccurrenceEventViewModel>>(cancellationToken: ct)
                ?? Array.Empty<OccurrenceEventViewModel>();
        else
            ViewBag.HistoryError = "O detalhe foi carregado, mas o histórico não pôde ser consultado agora.";
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(Guid id, int version, string resolution, CancellationToken ct)
    {
        if (id == Guid.Empty || version <= 0 || string.IsNullOrWhiteSpace(resolution) || resolution.Trim().Length > 4000)
        {
            TempData["Error"] = "Descreva a providência adotada antes de resolver a ocorrência.";
            TempData["OccurrenceResolution"] = resolution;
            return RedirectToAction(nameof(Index), new { id });
        }
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.PostAsJsonAsync($"api/ocorrencias/{id:D}/situacao",
            new { Situacao = "RESOLVIDA", Descricao = resolution.Trim(), Versao = version }, ct);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Ocorrência resolvida na origem. A Central de Pendências já refletirá o novo estado."
            : response.StatusCode switch
            {
                HttpStatusCode.Conflict => "A ocorrência mudou ou já foi resolvida. Revise o estado atual antes de repetir.",
                HttpStatusCode.Forbidden => "Seu vínculo atual não permite resolver esta ocorrência.",
                HttpStatusCode.NotFound => "A ocorrência não está disponível no contexto atual.",
                _ => "Não foi possível confirmar o resultado. Consulte o estado antes de repetir."
            };
        if (!response.IsSuccessStatusCode) TempData["OccurrenceResolution"] = resolution;
        return RedirectToAction(nameof(Index), new { id });
    }
}
