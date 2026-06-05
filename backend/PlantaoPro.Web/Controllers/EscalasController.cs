using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Administrador + "," + RolesConstants.Coordenacao + "," + RolesConstants.Operador + "," + RolesConstants.Hospital)]
public class EscalasController : BaseWebController
{
    public EscalasController(IHttpClientFactory f, ILogger<EscalasController> l) : base(f, l) { }

    public async Task<IActionResult> Index(Guid? medicoId, Guid? hospitalId, Guid? especialidadeId, string? status, DateTime? dataInicio, DateTime? dataFim, int page = 1, int pageSize = 20)
        => await this.RenderPaged<EscalaResumoDto>($"api/escalas?medicoId={medicoId}&hospitalId={hospitalId}&especialidadeId={especialidadeId}&status={status}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&page={page}&pageSize={pageSize}");

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await this.RenderDetails<EscalaDetailsDto>($"api/escalas/{id}");
        if (model.ErrorMessage == "Sessão expirada.") return HandleUnauthorized();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Confirmar(Guid id) => await PostStatus($"api/escalas/{id}/confirmar", new { justificativa = "Confirmada pela coordenação" }, "Escala confirmada.", id);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Recusar(Guid id, string justificativa) => await PostStatus($"api/escalas/{id}/recusar", new { justificativa }, "Escala recusada.", id);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Cancelar(Guid id, string justificativa) => await PostStatus($"api/escalas/{id}/cancelar", new { justificativa }, "Escala cancelada.", id);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> MarcarRealizado(Guid id) => await PostStatus($"api/escalas/{id}/marcar-realizado", new { justificativa = "Escala concluída" }, "Escala marcada como realizada.", id);

    [HttpGet] public IActionResult Substituir(Guid id) => View(model: new StatusActionViewModel(id, string.Empty));
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Substituir(Guid id, Guid novoMedicoId, string justificativa)
        => await PostStatus($"api/escalas/{id}/substituir", new { novoMedicoId, justificativa }, "Escala substituída.", id);

    private async Task<IActionResult> PostStatus(string endpoint, object payload, string success, Guid id)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var json = JsonSerializer.Serialize(payload);
            var response = await client.PostAsync(endpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            LogRequestContext("Escala.Status", endpoint, (int)response.StatusCode);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("Falha ao atualizar escala {EscalaId}. Endpoint:{Endpoint}. Payload:{Payload}. Response:{Response}", id, endpoint, json, content);
                TempData["Error"] = "Falha na operação.";
                return RedirectToAction(nameof(Details), new { id });
            }
            TempData["Success"] = success;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Erro de comunicação ao atualizar escala {EscalaId} no endpoint {Endpoint}", id, endpoint);
            TempData["Error"] = "Falha de comunicação com a API.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro inesperado ao atualizar escala {EscalaId} no endpoint {Endpoint}", id, endpoint);
            TempData["Error"] = "Erro inesperado ao processar solicitação.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
