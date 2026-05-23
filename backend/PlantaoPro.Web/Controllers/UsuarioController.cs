using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Models.ViewModels;
using System.Security.Claims;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class UsuarioController : BaseWebController
{
    public UsuarioController(IHttpClientFactory httpClientFactory, ILogger<UsuarioController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        try
        {
            Logger.LogInformation("Iniciando carregamento da página de configurações do usuário. Email:{Email}", email);
            var model = await LoadSettings();
            Logger.LogInformation("Configurações do usuário carregadas com sucesso. Email:{Email}", email);
            return View(model);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar configurações do usuário. Email:{Email}", email);
            TempData["Error"] = "Não foi possível carregar suas configurações no momento.";
            return View(new UserSettingsViewModel());
        }
    }

    [Authorize(Roles = PlantaoPro.Web.Security.RolesConstants.Administrador)]
    [HttpGet]
    public async Task<IActionResult> Admin(string? search = null, string? status = null)
    {
        try
        {
            Logger.LogInformation("Iniciando listagem administrativa de usuários. Search:{Search} Status:{Status}", search, status);
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var response = await client.GetFromJsonAsync<ApiResponse<List<UserListItemDto>>>("api/usuarios");
            var users = response?.Data ?? new List<UserListItemDto>();

            if (!string.IsNullOrWhiteSpace(search))
                users = users.Where(x => x.Username.Contains(search, StringComparison.OrdinalIgnoreCase) || x.Email.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(status))
                users = users.Where(x => status == "blocked" ? x.Locked : !x.Locked).ToList();

            Logger.LogInformation("Listagem administrativa de usuários carregada. Total:{Total}", users.Count);
            var model = new UserListVMWeb(users, Total: users.Count, Page: 1, PageSize: Math.Max(users.Count, 1));
            return View(model);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar listagem administrativa de usuários.");
            TempData["Error"] = "Não foi possível carregar a listagem de usuários.";
            return View(new UserListVMWeb(Array.Empty<UserListItemDto>(), ErrorMessage: "Falha ao carregar dados."));
        }
    }

    [Authorize(Roles = PlantaoPro.Web.Security.RolesConstants.Administrador)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(Guid id)
    {
        try
        {
            Logger.LogInformation("Iniciando desbloqueio de usuário. UsuarioId:{UsuarioId}", id);
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var response = await client.PostAsync($"api/usuarios/unlock/{id}", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = result?.Message ?? "Não foi possível desbloquear o usuário.";
            return RedirectToAction(nameof(Admin));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao desbloquear usuário. UsuarioId:{UsuarioId}", id);
            TempData["Error"] = "Ocorreu um erro inesperado ao desbloquear o usuário.";
            return RedirectToAction(nameof(Admin));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit() => View(await LoadSettings());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogWarning("Falha de validação ao editar configurações do usuário. Email:{Email}", model.Email);
            TempData["Error"] = "Revise os campos destacados e tente novamente.";
            return View(model);
        }

        try
        {
            Logger.LogInformation("Iniciando atualização de configurações do usuário. Email:{Email}", model.Email);
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var response = await client.PutAsJsonAsync("api/usuarios/me", new { model.Nome, model.Email, model.Telefone, model.PreferenciasNotificacao });
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            TempData[response.IsSuccessStatusCode && result?.Success == true ? "Success" : "Error"] = result?.Message ?? "Não foi possível salvar alterações.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar configurações do usuário. Email:{Email}", model.Email);
            TempData["Error"] = "Ocorreu um erro inesperado ao salvar os dados.";
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult AlterarSenha() => View(new AlterarSenhaViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogWarning("Falha de validação na alteração de senha do usuário autenticado.");
            TempData["Error"] = "Revise os campos da nova senha.";
            return View(model);
        }

        try
        {
            Logger.LogInformation("Iniciando alteração de senha do usuário autenticado.");
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var response = await client.PostAsJsonAsync("api/usuarios/me/alterar-senha", new { model.SenhaAtual, model.NovaSenha });
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            TempData[response.IsSuccessStatusCode && result?.Success == true ? "Success" : "Error"] = result?.Message ?? "Não foi possível alterar senha.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao alterar senha do usuário autenticado.");
            TempData["Error"] = "Ocorreu um erro inesperado ao alterar a senha.";
            return View(model);
        }
    }

    private async Task<UserSettingsViewModel> LoadSettings()
    {
        try
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
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao consultar dados do usuário autenticado na API.");
            return new UserSettingsViewModel();
        }
    }
}
