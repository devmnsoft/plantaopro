using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Security.Claims;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class UsuarioController : BaseWebController
{
    public UsuarioController(IHttpClientFactory httpClientFactory, ILogger<UsuarioController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        Logger.LogInformation("Acessando configurações do usuário {Email}", User.FindFirstValue(ClaimTypes.Email));
        var model = await LoadSettings();
        return View(model);
    }

    [Authorize(Roles = PlantaoPro.Web.Security.RolesConstants.Administrador)]
    [HttpGet]
    public async Task<IActionResult> Admin(string? search = null, string? status = null)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.GetFromJsonAsync<ApiResponse<List<UserListVMWeb>>>("api/usuarios");
        var users = response?.Data ?? new List<UserListVMWeb>();

        if (!string.IsNullOrWhiteSpace(search))
            users = users.Where(x => x.Username.Contains(search, StringComparison.OrdinalIgnoreCase) || x.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(status))
            users = users.Where(x => status == "blocked" ? x.Locked : !x.Locked).ToList();

        return View(users);
    }

    [Authorize(Roles = PlantaoPro.Web.Security.RolesConstants.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.PostAsync($"api/usuarios/unlock/{id}", null);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = result?.Message ?? "Não foi possível desbloquear o usuário.";
        return RedirectToAction(nameof(Admin));
    }

    [HttpGet]
    public async Task<IActionResult> Edit() => View(await LoadSettings());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise os campos destacados e tente novamente.";
            return View(model);
        }
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.PutAsJsonAsync("api/usuarios/me", new { model.Nome, model.Email, model.Telefone, model.PreferenciasNotificacao });
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        TempData[response.IsSuccessStatusCode && result?.Success == true ? "Success" : "Error"] = result?.Message ?? "Não foi possível salvar alterações.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult AlterarSenha() => View(new AlterarSenhaViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise os campos da nova senha.";
            return View(model);
        }
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.PostAsJsonAsync("api/usuarios/me/alterar-senha", new { model.SenhaAtual, model.NovaSenha });
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        TempData[response.IsSuccessStatusCode && result?.Success == true ? "Success" : "Error"] = result?.Message ?? "Não foi possível alterar senha.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<UserSettingsViewModel> LoadSettings()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return new();
        var response = await client.GetFromJsonAsync<ApiResponse<UserSettingsDtoWeb>>("api/usuarios/me");
        return response?.Data is null ? new UserSettingsViewModel() : new UserSettingsViewModel
        {
            Nome = response.Data.Nome,
            Email = response.Data.Email,
            Telefone = response.Data.Telefone,
            PreferenciasNotificacao = response.Data.PreferenciasNotificacao
        };
    }
}
