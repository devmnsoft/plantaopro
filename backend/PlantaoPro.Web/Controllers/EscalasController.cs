using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Operacao)]
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
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Recusar(Guid id, string justificativa) => await PostWithRequiredReason($"api/escalas/{id}/recusar", justificativa, "Escala recusada.", id);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Cancelar(Guid id, string justificativa) => await PostWithRequiredReason($"api/escalas/{id}/cancelar", justificativa, "Escala cancelada.", id);
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> MarcarRealizado(Guid id) => await PostStatus($"api/escalas/{id}/marcar-realizado", new { justificativa = "Escala concluída" }, "Escala marcada como realizada.", id);

    [HttpGet] public async Task<IActionResult> Substituir(Guid id)
    {
        var model = await BuildSubstituicao(new SubstituicaoEscalaViewModel { Id = id });
        if (model.Escala is null) return RedirectToAction(nameof(Details), new { id });
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Substituir(SubstituicaoEscalaViewModel model)
    {
        model = await BuildSubstituicao(model);
        if (model.Escala is null) ModelState.AddModelError(string.Empty, "A escala não existe ou não está disponível para o seu perfil.");
        if (model.Escala is not null && !string.Equals(model.Escala.Status, "confirmado", StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(string.Empty, "Somente escalas confirmadas permitem substituição.");
        if (!model.ProfissionaisOptions.Any(x => x.Id == model.NovoMedicoId))
            ModelState.AddModelError(nameof(model.NovoMedicoId), "Selecione um profissional ativo, do tenant e habilitado para esta especialidade.");
        if (!ModelState.IsValid) return View(model);
        return await PostStatus($"api/escalas/{model.Id}/substituir", new { novoMedicoId = model.NovoMedicoId, justificativa = model.Justificativa }, "Escala substituída.", model.Id);
    }

    private async Task<SubstituicaoEscalaViewModel> BuildSubstituicao(SubstituicaoEscalaViewModel model)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return model;
        var (escala, _, _) = await ReadApiResponse<EscalaDetailsDto>(client, $"api/escalas/{model.Id}");
        model.Escala = escala;
        if (escala is null) return model;
        var (plantao, _, _) = await ReadApiResponse<PlantaoDetailsDto>(client, $"api/plantoes/{escala.PlantaoId}");
        var (medicos, _, _) = await ReadApiListResponseAsync<MedicoDto>(client, "api/medicos");
        model.ProfissionaisOptions = medicos
            .Where(x => string.Equals(x.RegStatus, "A", StringComparison.OrdinalIgnoreCase)
                && x.Id != escala.MedicoId
                && plantao is not null && x.EspecialidadeId == plantao.EspecialidadeId)
            .OrderBy(x => x.Nome).ToArray();
        return model;
    }

    private Task<IActionResult> PostWithRequiredReason(string endpoint, string justificativa, string success, Guid id)
    {
        if (string.IsNullOrWhiteSpace(justificativa))
        {
            TempData["Error"] = "Informe o motivo para concluir esta ação.";
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(Details), new { id }));
        }
        return PostStatus(endpoint, new { justificativa = justificativa.Trim() }, success, id);
    }

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
