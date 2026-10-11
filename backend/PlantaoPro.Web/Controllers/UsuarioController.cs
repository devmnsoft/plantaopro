using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Models.ViewModels;
using System.Security.Claims;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class UsuarioController : BaseWebController
{
    private readonly PlantaoPro.Web.Security.ISessionReissueService _sessionReissue;

    public UsuarioController(IHttpClientFactory httpClientFactory, ILogger<UsuarioController> logger, PlantaoPro.Web.Security.ISessionReissueService sessionReissue) : base(httpClientFactory, logger)
    {
        _sessionReissue = sessionReissue;
    }

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

            var (data, error, _) = await ReadApiResponse<List<UserListItemDto>>(client, "api/usuarios");
            if (!string.IsNullOrWhiteSpace(error))
            {
                TempData["Warning"] = error;
            }
            var users = data ?? new List<UserListItemDto>();

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
            var (data, error, statusCode) = await SendApiAsync<object, object>(client, HttpMethod.Post, $"api/usuarios/unlock/{id}", new { });
            _ = data;
            TempData[statusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous ? "Success" : "Error"] = error ?? (statusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous ? "Usuário desbloqueado com sucesso." : "Não foi possível desbloquear o usuário.");
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

            var (_, error, statusCode) = await SendApiAsync<object, object>(client, HttpMethod.Put, "api/usuarios/me", new { model.Nome, model.Email, model.Telefone, model.PreferenciasNotificacao });
            var success = statusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
            TempData[success ? "Success" : "Error"] = error ?? (success ? "Alterações salvas com sucesso." : "Não foi possível salvar alterações.");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar configurações do usuário. Email:{Email}", model.Email);
            TempData["Error"] = "Ocorreu um erro inesperado ao salvar os dados.";
            return View(model);
        }
    }

    // R7-E1: salvar o idioma da interface (v2344). Valida contra o catálogo canônico
    // (pt-BR/en/es/fr), persiste via API e REEMITE a sessão no caminho canônico de refresh:
    // o novo idioma entra nas claims imediatamente (sem re-login) e vale no próximo request.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarIdioma(string idioma, string? returnUrl = null)
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        try
        {
            Logger.LogInformation("Iniciando salvamento de idioma da interface. Email:{Email} Idioma:{Idioma}", email, idioma);
            if (!PlantaoPro.CrossCutting.Localization.UiLanguageCatalog.IsSupported(idioma))
            {
                TempData["Error"] = PlantaoPro.Web.Localization.UiText.L("Profile.LanguageInvalid");
                return RedirectToAction(nameof(Index));
            }

            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var (_, error, statusCode) = await SendApiAsync<object, object>(client, HttpMethod.Post, "api/usuarios/me/idioma", new { idioma });
            if (statusCode is not (>= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous))
            {
                TempData["Error"] = error ?? PlantaoPro.Web.Localization.UiText.L("Profile.LanguageSaveFailed");
                return RedirectToAction(nameof(Index));
            }

            var (login, apiStatus) = await _sessionReissue.RefreshFromApiAsync(HttpContext);
            if (login is null)
            {
                await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                TempData["Error"] = apiStatus == 401
                    ? PlantaoPro.Web.Localization.UiText.L("Profile.SessionExpiredFallback")
                    : PlantaoPro.Web.Localization.UiText.L("Profile.LanguageApplyFailedFallback");
                Logger.LogWarning("SalvarIdioma: reemissão de sessão recusada. Status:{Status} Email:{Email}", apiStatus, email);
                return RedirectToAction("Login", "Account");
            }

            TempData["Success"] = PlantaoPro.Web.Localization.UiText.L("Profile.LanguageSaved");
            Logger.LogInformation("Idioma da interface salvo e sessão reemitida. Email:{Email} Idioma:{Idioma}", email, idioma);
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao salvar idioma da interface. Email:{Email}", email);
            TempData["Error"] = PlantaoPro.Web.Localization.UiText.L("Profile.UnexpectedSaveError");
            return RedirectToAction(nameof(Index));
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

            var (_, error, statusCode) = await SendApiAsync<object, object>(client, HttpMethod.Post, "api/usuarios/me/alterar-senha", new { model.SenhaAtual, model.NovaSenha });
            var success = statusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
            TempData[success ? "Success" : "Error"] = error ?? (success ? "Senha alterada com sucesso." : "Não foi possível alterar senha.");
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
            var (data, _, _) = await ReadApiResponse<UserSettingsDtoWeb>(client, "api/usuarios/me");
            return data is null ? new UserSettingsViewModel() : new UserSettingsViewModel
            {
                Nome = data.Nome,
                Email = data.Email,
                Telefone = data.Telefone,
                PreferenciasNotificacao = data.PreferenciasNotificacao
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao consultar dados do usuário autenticado na API.");
            return new UserSettingsViewModel();
        }
    }
}
