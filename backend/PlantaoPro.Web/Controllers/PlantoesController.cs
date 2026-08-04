using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize]
[Authorize(Roles = RolesConstants.Operacao)]
public class PlantoesController : BaseWebController
{
    public PlantoesController(IHttpClientFactory f, ILogger<PlantoesController> l) : base(f, l) { }

    public async Task<IActionResult> Index(string? cidade, string? status, DateTime? dataInicio, DateTime? dataFim,
        bool? comVagas, decimal? valorMinimo, decimal? valorMaximo, string? ordenarPor, string? direcao,
        int page = 1, int pageSize = 20)
    {
        var query = new Dictionary<string, string?>
        {
            ["cidade"] = cidade,
            ["status"] = status,
            ["dataInicio"] = dataInicio?.ToString("O"),
            ["dataFim"] = dataFim?.ToString("O"),
            ["comVagas"] = comVagas?.ToString(),
            ["valorMinimo"] = valorMinimo?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["valorMaximo"] = valorMaximo?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ordenarPor"] = ordenarPor,
            ["direcao"] = direcao,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        };
        var parameters = string.Join("&", query.Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));
        return await this.RenderPaged<PlantaoResumoDto>($"api/plantoes?{parameters}");
    }

    public IActionResult Calendario() => View();

    public async Task<IActionResult> Details(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, statusCode) = await ReadApiResponse<PlantaoDetailsDto>(client, $"api/plantoes/{id}");
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        return View(new DetailsPageViewModel<PlantaoDetailsDto>(data, error));
    }

    public async Task<IActionResult> Create() => View(await BuildForm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlantaoFormViewModel model)
    {
        Normalize(model);
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return AjaxError("Revise os campos obrigatórios do plantão.", 400);
            return View(await BuildForm(model));
        }
        return await SendPlantao(model, null);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, statusCode) = await ReadApiResponse<PlantaoDetailsDto>(client, $"api/plantoes/{id}");
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (data is null) { TempData["Error"] = error ?? "Plantão não encontrado."; return RedirectToAction(nameof(Index)); }
        var vm = new PlantaoFormViewModel { Id = data.Id, HospitalId = data.HospitalId, EspecialidadeId = data.EspecialidadeId, DataInicio = data.DataInicio, DataFim = data.DataFim, Valor = data.Valor, Vagas = data.Vagas, Tipo = data.Tipo, Observacoes = data.Observacoes, Status = data.Status };
        return View(await BuildForm(vm));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PlantaoFormViewModel model)
    {
        Normalize(model);
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return AjaxError("Revise os campos obrigatórios do plantão.", 400);
            return View(await BuildForm(model));
        }
        return await SendPlantao(model, id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publicar(Guid id)
    {
        return await SendStatusAction(id, "api/plantoes/{0}/publicar", "Plantão publicado com sucesso.", "Publicação não realizada.");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(StatusActionViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Justificativa)) { TempData["Error"] = "Justificativa obrigatória para cancelar."; return RedirectToAction(nameof(Details), new { id = model.Id }); }
        return await SendStatusAction(model.Id, "api/plantoes/{0}/cancelar", "Plantão cancelado.", "Cancelamento não realizado.", model.Justificativa);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicar(Guid id)
    {
        var endpoint = $"api/plantoes/{id}/duplicar";
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var result = await SendApiWithoutResponseAsync(client, HttpMethod.Post, endpoint,
                new { DataInicio = (DateTime?)null, DataFim = (DateTime?)null, Justificativa = "Duplicação solicitada pela central operacional" });
            TempData[result.Success ? "Success" : "Error"] = result.Success
                ? "Plantão duplicado como rascunho para a próxima semana."
                : result.Error ?? "Não foi possível duplicar o plantão.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao duplicar plantão {PlantaoId}", id);
            TempData["Error"] = "Não foi possível duplicar o plantão agora.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private static void Normalize(PlantaoFormViewModel model)
    {
        model.Tipo = model.Tipo.Trim();
        model.Observacoes = string.IsNullOrWhiteSpace(model.Observacoes) ? null : model.Observacoes.Trim();
    }

    private async Task<IActionResult> SendPlantao(PlantaoFormViewModel model, Guid? id)
    {
        var endpoint = id.HasValue ? $"api/plantoes/{id}" : "api/plantoes";
        var method = id.HasValue ? HttpMethod.Put : HttpMethod.Post;

        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client))
            {
                if (IsAjaxRequest()) return AjaxError("Sessão expirada. Faça login novamente.", 401);
                return HandleUnauthorized();
            }

            var payload = new
            {
                model.HospitalId,
                model.EspecialidadeId,
                model.DataInicio,
                model.DataFim,
                model.Valor,
                model.Vagas,
                model.Tipo,
                Observacoes = model.Observacoes ?? string.Empty
            };

            var (_, error, statusCode) = await SendApiAsync<object, JsonElement>(client, method, endpoint, payload);
            LogRequestContext(id.HasValue ? "plantao.atualizar" : "plantao.criar", endpoint, (int)statusCode);

            if (statusCode == HttpStatusCode.Unauthorized)
            {
                if (IsAjaxRequest()) return AjaxError("Sessão expirada. Faça login novamente.", 401);
                return HandleUnauthorized();
            }
            if (statusCode is < HttpStatusCode.OK or >= HttpStatusCode.Ambiguous)
            {
                var message = error ?? "Falha ao salvar plantão.";
                if (IsAjaxRequest()) return AjaxError(message, (int)statusCode);
                TempData["Error"] = message;
                return View(id.HasValue ? "Edit" : "Create", await BuildForm(model));
            }

            var successMessage = id.HasValue ? "Plantão atualizado com sucesso." : "Plantão cadastrado com sucesso.";
            if (IsAjaxRequest()) return Json(new { success = true, message = successMessage, redirectUrl = Url.Action(nameof(Index)) });
            TempData["Success"] = successMessage;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro crítico ao salvar plantão. Endpoint:{Endpoint} PlantaoId:{PlantaoId}", endpoint, id);
            var message = "Não foi possível salvar o plantão agora. Tente novamente.";
            if (IsAjaxRequest()) return AjaxError(message, 500);
            TempData["Error"] = message;
            return View(id.HasValue ? "Edit" : "Create", await BuildForm(model));
        }
    }

    private IActionResult AjaxError(string message, int statusCode)
    {
        Response.StatusCode = statusCode;
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(e => !string.IsNullOrWhiteSpace(e)).ToArray();
        return Json(new { success = false, message, errors });
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IActionResult> SendStatusAction(Guid id, string endpointFmt, string success, string error, string justificativa = "")
    {
        var endpoint = string.Format(endpointFmt, id);

        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var result = await SendApiWithoutResponseAsync(client, HttpMethod.Post, endpoint, new { Justificativa = justificativa });
            LogRequestContext("plantao.status", endpoint, (int)result.StatusCode);

            if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            TempData[result.Success ? "Success" : "Error"] = result.Success ? success : result.Error ?? error;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro crítico ao alterar status do plantão. Endpoint:{Endpoint} PlantaoId:{PlantaoId}", endpoint, id);
            TempData["Error"] = error;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task<PlantaoFormViewModel> BuildForm(PlantaoFormViewModel? model = null)
    {
        model ??= new PlantaoFormViewModel { DataInicio = DateTime.Now.AddHours(1), DataFim = DateTime.Now.AddHours(13), Vagas = 1 };
        var client = CreateApiClient(); if (!AddBearerToken(client)) return model;
        var (hospitais, _, _) = await ReadApiListResponseAsync<HospitalDto>(client, "api/hospitais");
        var (especialidades, _, _) = await ReadApiListResponseAsync<EspecialidadeDto>(client, "api/especialidades");
        model.Hospitais = hospitais;
        model.Especialidades = especialidades;
        return model;
    }
}
