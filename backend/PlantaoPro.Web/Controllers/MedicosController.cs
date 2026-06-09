using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.CadastrosCoordenacao)]
public class MedicosController : BaseWebController
{
    public MedicosController(IHttpClientFactory f, ILogger<MedicosController> l) : base(f, l) { }

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 20) => await this.RenderList<MedicoDto>("api/medicos", page, pageSize);
    public async Task<IActionResult> Details(Guid id) => View(await this.RenderDetails<MedicoDto>($"api/medicos/{id}"));

    public async Task<IActionResult> Create() => View(await BuildFormAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicoFormViewModel model)
    {
        if (!ModelState.IsValid) return View(await BuildFormAsync(model));
        return await SaveAsync(model, null);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, statusCode) = await ReadApiResponse<MedicoDto>(client, $"api/medicos/{id}");
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (data is null)
        {
            TempData["Error"] = error ?? "Médico não encontrado.";
            return RedirectToAction(nameof(Index));
        }
        var model = new MedicoFormViewModel { Id = data.Id, Nome = data.Nome, Cpf = data.Cpf, Crm = data.Crm, UfCrm = data.UfCrm, Email = data.Email, Telefone = data.Telefone, Cidade = data.Cidade, Estado = data.Estado, EspecialidadeId = data.EspecialidadeId, RegStatus = data.RegStatus };
        return View(await BuildFormAsync(model));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MedicoFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid) return View(await BuildFormAsync(model));
        return await SaveAsync(model, id);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await DeleteApiAsync(client, $"api/medicos/{id}", useDelete: true);
        LogRequestContext("medico.inativar", $"api/medicos/{id}", (int)result.StatusCode);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Médico inativado com sucesso." : result.Error ?? "Não foi possível inativar o médico.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(MedicoFormViewModel model, Guid? id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = id.HasValue ? $"api/medicos/{id}" : "api/medicos/cadastro";
        var method = id.HasValue ? HttpMethod.Put : HttpMethod.Post;
        var payload = new { model.Nome, model.Cpf, model.Crm, model.UfCrm, model.Email, model.Telefone, model.Cidade, model.Estado, model.EspecialidadeId, model.RegStatus };
        var (_, error, statusCode) = await SendApiAsync<object, JsonElement>(client, method, endpoint, payload);
        LogRequestContext(id.HasValue ? "medico.atualizar" : "medico.criar", endpoint, (int)statusCode);
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (statusCode is < HttpStatusCode.OK or >= HttpStatusCode.Ambiguous)
        {
            TempData["Error"] = error ?? "Não foi possível salvar o médico.";
            return View(id.HasValue ? "Edit" : "Create", await BuildFormAsync(model));
        }
        TempData["Success"] = id.HasValue ? "Médico atualizado com sucesso." : "Médico cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<MedicoFormViewModel> BuildFormAsync(MedicoFormViewModel? model = null)
    {
        model ??= new MedicoFormViewModel();
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return model;
        var (especialidades, _, _) = await ReadApiListResponseAsync<EspecialidadeDto>(client, "api/especialidades");
        model.Especialidades = especialidades;
        return model;
    }
}
