using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Administrador + "," + RolesConstants.Coordenacao + "," + RolesConstants.Operador + "," + RolesConstants.Hospital)]
public class HospitaisController : BaseWebController
{
    public HospitaisController(IHttpClientFactory f, ILogger<HospitaisController> l) : base(f, l) { }

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 20) => await this.RenderList<HospitalDto>("api/hospitais", page, pageSize);
    public async Task<IActionResult> Details(Guid id) => View(await this.RenderDetails<HospitalDto>($"api/hospitais/{id}"));

    public IActionResult Create() => View(new HospitalFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HospitalFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        return await SaveAsync(model, null);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, statusCode) = await ReadApiResponse<HospitalDto>(client, $"api/hospitais/{id}");
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (data is null)
        {
            TempData["Error"] = error ?? "Hospital não encontrado.";
            return RedirectToAction(nameof(Index));
        }
        return View(new HospitalFormViewModel { Id = data.Id, RazaoSocial = data.RazaoSocial, NomeFantasia = data.NomeFantasia, Cnpj = data.Cnpj, Telefone = data.Telefone, Email = data.Email, Endereco = data.Endereco, Cidade = data.Cidade, Estado = data.Estado, Responsavel = data.Responsavel, RegStatus = data.RegStatus });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, HospitalFormViewModel model)
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
        var result = await DeleteApiAsync(client, $"api/hospitais/{id}", useDelete: true);
        LogRequestContext("hospital.inativar", $"api/hospitais/{id}", (int)result.StatusCode);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Hospital inativado com sucesso." : result.Error ?? "Não foi possível inativar o hospital.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SaveAsync(HospitalFormViewModel model, Guid? id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = id.HasValue ? $"api/hospitais/{id}" : "api/hospitais";
        var method = id.HasValue ? HttpMethod.Put : HttpMethod.Post;
        var payload = new { model.RazaoSocial, model.NomeFantasia, model.Cnpj, model.Telefone, model.Email, model.Endereco, model.Cidade, model.Estado, model.Responsavel, model.RegStatus };
        var (_, error, statusCode) = await SendApiAsync<object, JsonElement>(client, method, endpoint, payload);
        LogRequestContext(id.HasValue ? "hospital.atualizar" : "hospital.criar", endpoint, (int)statusCode);
        if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (statusCode is < HttpStatusCode.OK or >= HttpStatusCode.Ambiguous)
        {
            TempData["Error"] = error ?? "Não foi possível salvar o hospital.";
            return View(id.HasValue ? "Edit" : "Create", model);
        }
        TempData["Success"] = id.HasValue ? "Hospital atualizado com sucesso." : "Hospital cadastrado com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
