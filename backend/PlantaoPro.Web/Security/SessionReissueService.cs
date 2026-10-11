using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using PlantaoPro.CrossCutting.Security;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Security;

/// <summary>
/// R7-E1 — Reemissão canônica da sessão a partir da API (api/auth/refresh-context).
/// Caminho ÚNICO compartilhado por AccountController.RefreshContext e pelos comandos que
/// mudam dados de sessão (ex.: salvar idioma em UsuarioController.SalvarIdioma): a mesma
/// resposta da API passa pelo mesmo SessionClaimsBuilder, produz o MESMO cookie do login e
/// atualiza o HttpContext.User na mesma request (comportamento .NET 10 documentado no Login).
/// Retorna null quando a API recusa — o caller decide o fallback (signout/login).
/// </summary>
public interface ISessionReissueService
{
    /// <summary>Login atualizado quando a reemissão teve sucesso; StatusCode da API em qualquer caso (null = falha de comunicação).</summary>
    Task<(LoginResponse? Login, int? StatusCode)> RefreshFromApiAsync(HttpContext httpContext, CancellationToken ct = default);
}

public sealed class SessionReissueService : ISessionReissueService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRoleCatalog _roleCatalog;
    private readonly IPrimaryRoleResolver _primaryRoleResolver;
    private readonly IAccessScopeResolver _accessScopeResolver;
    private readonly ILogger<SessionReissueService> _logger;

    public SessionReissueService(IHttpClientFactory httpClientFactory, IRoleCatalog roleCatalog, IPrimaryRoleResolver primaryRoleResolver, IAccessScopeResolver accessScopeResolver, ILogger<SessionReissueService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _roleCatalog = roleCatalog;
        _primaryRoleResolver = primaryRoleResolver;
        _accessScopeResolver = accessScopeResolver;
        _logger = logger;
    }

    public async Task<(LoginResponse? Login, int? StatusCode)> RefreshFromApiAsync(HttpContext httpContext, CancellationToken ct = default)
    {
        var token = httpContext.Session.GetString("jwt") ?? httpContext.Session.GetString("JwtToken") ?? string.Empty;
        using var client = _httpClientFactory.CreateClient("PlantaoProApi");
        if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("api/auth/refresh-context", null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh-context falhou por comunicação HTTP.");
            return (null, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Refresh-context recusado Status:{Status}", (int)response.StatusCode);
            return (null, (int)response.StatusCode);
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        ApiResponse<LoginResponse>? apiResult;
        try
        {
            apiResult = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Refresh-context retornou payload inesperado.");
            return (null, (int)response.StatusCode);
        }

        var login = apiResult?.Success == true ? apiResult.Data : null;
        if (login is null || string.IsNullOrWhiteSpace(login.Token))
        {
            _logger.LogWarning("Refresh-context sem dados utilizáveis.");
            return (null, (int)response.StatusCode);
        }

        // Mesmo construtor de claims do Login: a atualização de contexto produz o conjunto
        // completo e normalizado (inclui os idiomas R7-E1 quando definidos na fonte).
        var (principal, primaryRole, accessScope, contextMode, _) = SessionClaimsBuilder.Build(
            AccountContextMapper.ToSessionLoginContext(login), _roleCatalog, _primaryRoleResolver, _accessScopeResolver, httpContext.Session.Id);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        });
        // .NET 10: o SignInAsync não atualiza HttpContext.User dentro da mesma request.
        httpContext.User = principal;
        httpContext.Session.SetString("jwt", login.Token);
        httpContext.Session.SetString("JwtToken", login.Token);
        httpContext.Session.SetString("UsuarioNome", login.Nome ?? string.Empty);
        httpContext.Session.SetString("UsuarioEmail", login.Email ?? string.Empty);
        httpContext.Session.SetString("UsuarioPerfil", primaryRole);
        httpContext.Session.SetString("AccessScope", accessScope);
        httpContext.Session.SetString("ContextMode", contextMode);

        _logger.LogInformation("Sessão reemitida via refresh-context. UsuarioId:{UsuarioId} Idioma:{Idioma} OrgIdioma:{OrgIdioma}",
            login.UsuarioId, principal.FindFirstValue("idioma"), principal.FindFirstValue("org_idioma"));
        return (login, 200);
    }
}

/// <summary>
/// Mapeia a resposta da API para o contexto imutável consumido pelo SessionClaimsBuilder —
/// compartilhado pelos dois caminhos (Login e RefreshContext/reemissão) para produzirem o MESMO
/// conjunto de claims para a mesma resposta da API (com os idiomas R7-E1 quando definidos).
/// </summary>
public static class AccountContextMapper
{
    public static SessionLoginContext ToSessionLoginContext(LoginResponse login) => new(
        login.UsuarioId,
        login.Nome,
        login.Email,
        login.Roles,
        login.Permissions,
        login.Modules,
        login.PrimaryRole,
        login.AccessScope,
        login.TenantContextSelected,
        login.ContextMode,
        login.ClienteId,
        login.ClienteNome,
        login.ClienteStatus,
        login.TenantId,
        login.TenantNome,
        login.SessionId,
        login.Token,
        login.IdiomaPreferido,
        login.IdiomaOrganizacao);
}
