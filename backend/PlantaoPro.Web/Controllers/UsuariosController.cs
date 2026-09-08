using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)]
public sealed class UsuariosController : BaseWebController
{
    public UsuariosController(IHttpClientFactory httpClientFactory, ILogger<UsuariosController> logger) : base(httpClientFactory, logger) { }

    [HttpGet]
    public async Task<IActionResult> Index(string? busca = null, int page = 1)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = $"api/seguranca/usuarios?page={Math.Max(page, 1)}&pageSize=50";
        if (!string.IsNullOrWhiteSpace(busca)) endpoint += "&busca=" + Uri.EscapeDataString(busca.Trim());
        var (users, error, _) = await ReadApiListResponseAsync<UsuarioSaasViewModel>(client, endpoint);
        ViewBag.Busca = busca;
        ViewBag.ErrorMessage = error;
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new UsuarioEditorViewModel { IsGlobalAdmin = User.IsInRole(RolesConstants.AdministradorGlobal) };
        await PopulateEditorAsync(model);
        return View("Form", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (user, error, status) = await ReadApiResponseAsync<UsuarioSaasViewModel>(client, "api/seguranca/usuarios/" + id);
        if (user is null)
        {
            TempData["ErrorMessage"] = error ?? "Usuário não encontrado no tenant permitido.";
            return (int)status == StatusCodes.Status401Unauthorized ? HandleUnauthorized() : RedirectToAction(nameof(Index));
        }
        var (selectedProfiles, _, _) = await ReadApiListResponseAsync<Guid>(client, $"api/seguranca/usuarios/{id}/perfis");
        var model = new UsuarioEditorViewModel
        {
            Id = user.Id,
            TenantId = user.TenantId ?? user.ClienteId,
            Nome = user.Nome,
            Email = user.Email,
            Telefone = user.Telefone,
            PerfilIds = selectedProfiles.ToArray(),
            IsGlobalAdmin = User.IsInRole(RolesConstants.AdministradorGlobal)
        };
        await PopulateEditorAsync(model);
        return View("Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UsuarioEditorViewModel model)
    {
        model.IsGlobalAdmin = User.IsInRole(RolesConstants.AdministradorGlobal);
        if (!ModelState.IsValid)
        {
            await PopulateEditorAsync(model);
            return View("Form", model);
        }
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = model.Id == Guid.Empty ? "api/seguranca/usuarios" : "api/seguranca/usuarios/" + model.Id;
        var method = model.Id == Guid.Empty ? HttpMethod.Post : HttpMethod.Put;
        var payload = new { model.TenantId, model.Nome, model.Email, model.Telefone, model.SenhaTemporaria, model.PerfilIds };
        var (savedId, error, status) = await SendApiAsync<object, Guid>(client, method, endpoint, payload);
        if ((int)status is < 200 or > 299)
        {
            ModelState.AddModelError(string.Empty, error ?? "Não foi possível salvar o usuário.");
            await PopulateEditorAsync(model);
            return View("Form", model);
        }
        TempData["SuccessMessage"] = model.Id == Guid.Empty
            ? "Usuário criado. A troca da senha temporária será exigida no primeiro acesso."
            : "Usuário e perfis atualizados; as sessões anteriores foram revogadas.";
        return RedirectToAction(nameof(Edit), new { id = savedId == Guid.Empty ? model.Id : savedId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(Guid id, string acao)
    {
        var normalized = (acao ?? string.Empty).Trim().ToUpperInvariant();
        var operation = normalized switch { "BLOQUEAR" => "bloquear", "DESBLOQUEAR" => "desbloquear", "INATIVAR" => "inativar", _ => string.Empty };
        if (string.IsNullOrWhiteSpace(operation))
        {
            TempData["ErrorMessage"] = "Ação de usuário inválida.";
            return RedirectToAction(nameof(Index));
        }
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (_, error, status) = await SendApiAsync<object, Guid>(client, HttpMethod.Post, $"api/seguranca/usuarios/{id}/{operation}", new { });
        TempData[(int)status is >= 200 and <= 299 ? "SuccessMessage" : "ErrorMessage"] = (int)status is >= 200 and <= 299 ? "Status do usuário atualizado." : error ?? "Não foi possível alterar o usuário.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Perfis() => RedirectToAction("Index", "Perfis");

    private async Task PopulateEditorAsync(UsuarioEditorViewModel model)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return;
        var profileEndpoint = "api/seguranca/perfis-atribuiveis";
        if (model.TenantId.HasValue) profileEndpoint += "?tenantId=" + model.TenantId.Value;
        var (profiles, profileError, _) = await ReadApiListResponseAsync<PerfilOpcaoUsuarioViewModel>(client, profileEndpoint);
        model.PerfisDisponiveis = profiles;
        if (model.IsGlobalAdmin)
        {
            var (clients, clientError, _) = await ReadApiListResponseAsync<ClienteDto>(client, "api/clientes");
            model.Clientes = clients.Where(item => item.RegStatus == "A" && item.Status is not ("CANCELADO" or "INATIVO"));
            model.TenantNome = model.Clientes.FirstOrDefault(item => item.Id == model.TenantId)?.NomeFantasia ?? string.Empty;
            ViewBag.ErrorMessage = profileError ?? clientError;
        }
        else
        {
            model.TenantNome = User.FindFirst("tenant")?.Value ?? "Cliente atual";
            ViewBag.ErrorMessage = profileError;
        }
    }
}
