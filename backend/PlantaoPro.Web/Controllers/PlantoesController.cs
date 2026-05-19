using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class PlantoesController : BaseWebController
{
    public PlantoesController(IHttpClientFactory f, ILogger<PlantoesController> l) : base(f, l) { }

    public async Task<IActionResult> Index(string? hospital, string? especialidade, string? status, string? tipo, DateTime? dataInicio, DateTime? dataFim, int page = 1, int pageSize = 20)
        => await this.RenderPaged<PlantaoResumoDto>($"api/plantoes?hospital={hospital}&especialidade={especialidade}&status={status}&tipo={tipo}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&page={page}&pageSize={pageSize}");

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
        if (model.DataFim <= model.DataInicio) ModelState.AddModelError(nameof(model.DataFim), "Data fim deve ser maior que data início.");
        if (!ModelState.IsValid) return View(await BuildForm(model));
        return await SendPlantao(model, null);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, statusCode) = await ReadApiResponse<PlantaoDetailsDto>(client, $"api/plantoes/{id}");
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (data is null) { TempData["Error"] = error ?? "Plantão não encontrado."; return RedirectToAction(nameof(Index)); }
        var vm = new PlantaoFormViewModel { Id = data.Id, HospitalId = data.HospitalId, EspecialidadeId = data.EspecialidadeId, DataInicio = data.DataInicio, DataFim = data.DataFim, Valor = data.Valor, Vagas = data.Vagas, Tipo = data.Tipo, Observacoes = data.Observacoes ?? string.Empty };
        return View(await BuildForm(vm));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PlantaoFormViewModel model)
    {
        if (model.DataFim <= model.DataInicio) ModelState.AddModelError(nameof(model.DataFim), "Data fim deve ser maior que data início.");
        if (!ModelState.IsValid) return View(await BuildForm(model));
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

    private async Task<IActionResult> SendPlantao(PlantaoFormViewModel model, Guid? id)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var payload = new { model.HospitalId, model.EspecialidadeId, model.DataInicio, model.DataFim, model.Valor, model.Vagas, model.Tipo, Observacoes = model.Observacoes ?? string.Empty };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = id.HasValue ? await client.PutAsync($"api/plantoes/{id}", content) : await client.PostAsync("api/plantoes", content);
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        var api = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (!response.IsSuccessStatusCode || api?.Success != true) { TempData["Error"] = api?.Message ?? "Falha ao salvar plantão."; return View(id.HasValue ? "Edit" : "Create", await BuildForm(model)); }
        TempData["Success"] = api.Message;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SendStatusAction(Guid id, string endpointFmt, string success, string error, string justificativa = "")
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var content = new StringContent(JsonSerializer.Serialize(new { Justificativa = justificativa }), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(string.Format(endpointFmt, id), content);
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        var api = JsonSerializer.Deserialize<ApiResponse<JsonElement>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        TempData[api?.Success == true ? "Success" : "Error"] = api?.Message ?? (api?.Success == true ? success : error);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<PlantaoFormViewModel> BuildForm(PlantaoFormViewModel? model = null)
    {
        model ??= new PlantaoFormViewModel { DataInicio = DateTime.Now.AddHours(1), DataFim = DateTime.Now.AddHours(13), Vagas = 1 };
        var client = CreateApiClient(); if (!AddBearerToken(client)) return model;
        var (hospitais, _, _) = await ReadApiResponse<PagedResult<HospitalDto>>(client, "api/hospitais?page=1&pageSize=200");
        var (especialidades, _, _) = await ReadApiResponse<PagedResult<EspecialidadeDto>>(client, "api/especialidades?page=1&pageSize=200");
        model.Hospitais = hospitais?.Items ?? Enumerable.Empty<HospitalDto>();
        model.Especialidades = especialidades?.Items ?? Enumerable.Empty<EspecialidadeDto>();
        return model;
    }
}
