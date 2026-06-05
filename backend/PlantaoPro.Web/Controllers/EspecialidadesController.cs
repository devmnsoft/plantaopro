using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Administrador + "," + RolesConstants.Coordenacao + "," + RolesConstants.Operador)]
public class EspecialidadesController : BaseWebController
{
    public EspecialidadesController(IHttpClientFactory f, ILogger<EspecialidadesController> l) : base(f, l) { }

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 20) => await this.RenderList<EspecialidadeDto>("api/especialidades", page, pageSize);

    public async Task<IActionResult> Details(Guid id) => View(await this.RenderDetails<EspecialidadeDto>($"api/especialidades/{id}"));

    public IActionResult Create() => View(new EspecialidadeFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EspecialidadeFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        return await SaveAsync(model, null);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, statusCode) = await ReadApiResponse<EspecialidadeDto>(client, $"api/especialidades/{id}");
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (data is null)
        {
            TempData["Error"] = error ?? "Especialidade não encontrada.";
            return RedirectToAction(nameof(Index));
        }

        return View(new EspecialidadeFormViewModel { Id = data.Id, Nome = data.Nome, Descricao = data.Descricao, RegStatus = data.RegStatus });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EspecialidadeFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid) return View(model);
        return await SaveAsync(model, id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await DeleteApiAsync(client, $"api/especialidades/{id}", useDelete: true);
        LogRequestContext("especialidade.inativar", $"api/especialidades/{id}", (int)result.StatusCode);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Especialidade inativada com sucesso." : result.Error ?? "Não foi possível inativar a especialidade.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(EspecialidadeFormViewModel model, Guid? id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = id.HasValue ? $"api/especialidades/{id}" : "api/especialidades";
        var method = id.HasValue ? HttpMethod.Put : HttpMethod.Post;
        var payload = new { model.Nome, model.Descricao, model.RegStatus };
        var (_, error, statusCode) = await SendApiAsync<object, JsonElement>(client, method, endpoint, payload);
        LogRequestContext(id.HasValue ? "especialidade.atualizar" : "especialidade.criar", endpoint, (int)statusCode);
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (statusCode is < HttpStatusCode.OK or >= HttpStatusCode.Ambiguous)
        {
            TempData["Error"] = error ?? "Não foi possível salvar a especialidade.";
            return View(id.HasValue ? "Edit" : "Create", model);
        }

        TempData["Success"] = id.HasValue ? "Especialidade atualizada com sucesso." : "Especialidade cadastrada com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
