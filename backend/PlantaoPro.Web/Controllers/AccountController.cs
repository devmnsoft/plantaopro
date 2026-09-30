using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
using PlantaoPro.Web.Services.Security;
using PlantaoPro.CrossCutting.Security;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlantaoPro.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AccountController> _logger;
    private readonly IRoleCatalog _roleCatalog;
    private readonly IPrimaryRoleResolver _primaryRoleResolver;
    private readonly IAccessScopeResolver _accessScopeResolver;
    private readonly ITenantContextResolver _tenantContextResolver;
    private readonly IModuleAccessService _moduleAccess;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public AccountController(IHttpClientFactory httpClientFactory, ILogger<AccountController> logger, IRoleCatalog roleCatalog, IPrimaryRoleResolver primaryRoleResolver, IAccessScopeResolver accessScopeResolver, ITenantContextResolver tenantContextResolver, IModuleAccessService moduleAccess)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _roleCatalog = roleCatalog;
        _primaryRoleResolver = primaryRoleResolver;
        _accessScopeResolver = accessScopeResolver;
        _tenantContextResolver = tenantContextResolver;
        _moduleAccess = moduleAccess;
    }

    // Template explícito nos dois overloads: sem ele, a geração de URLs para
    // Url.Action("Login","Account") era resolvida como "/" (endpoint raiz da landing),
    // quebrando todos os links "Entrar"/"Ir para login" do layout público.
    [HttpGet("Account/Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        _logger.LogInformation("Acesso tela login IP:{Ip} ReturnUrl:{ReturnUrl}", HttpContext.Connection.RemoteIpAddress?.ToString(), returnUrl);
        return View(new LoginViewModel());
    }

    [HttpPost("Account/Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var correlationId = HttpContext.TraceIdentifier;
        using var loginScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
        ViewData["ReturnUrl"] = returnUrl;
        var loginIdentifier = (model.Email ?? string.Empty).Trim();
        var identifierKind = ClassifyIdentifier(loginIdentifier);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Login POST iniciado. TipoIdentificador:{TipoIdentificador} IP:{Ip} ReturnUrl:{ReturnUrl}", identifierKind, ip, returnUrl);

        if (!ModelState.IsValid)
        {
            _logger.LogInformation("Login POST inválido por ModelState. TipoIdentificador:{TipoIdentificador}", identifierKind);
            return LoginFailureView(model);
        }

        Uri? apiBaseUrl = null;

        try
        {
            using var client = _httpClientFactory.CreateClient("PlantaoProApi");
            apiBaseUrl = client.BaseAddress;
            _logger.LogInformation("Chamando API de login. BaseUrl:{ApiBaseUrl}", apiBaseUrl);

            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", correlationId);
            using var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(loginIdentifier, model.Senha ?? string.Empty), cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Resposta da API de login. Status:{StatusCode}", (int)response.StatusCode);

            var apiResult = DeserializeApiResponse<LoginResponse>(body);

            if (!response.IsSuccessStatusCode || apiResult is null || !apiResult.Success || apiResult.Data is null || string.IsNullOrWhiteSpace(apiResult.Data.Token))
            {
                var errorMessage = response.StatusCode switch
                {
                    HttpStatusCode.Forbidden => "Seu usuário está inativo. Procure o administrador.",
                    (HttpStatusCode)423 => apiResult?.Message ?? "Usuário bloqueado temporariamente.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest => "Identificador ou senha inválidos.",
                    _ => apiResult?.Message ?? $"Não foi possível autenticar. Tente novamente. Referência: {correlationId}"
                };

                TempData["Error"] = errorMessage;
                ModelState.AddModelError(string.Empty, errorMessage);
                _logger.LogWarning("Falha no login Web. TipoIdentificador:{TipoIdentificador} Status:{Status} SuccessFlag:{SuccessFlag}", identifierKind, (int)response.StatusCode, apiResult?.Success);
                return LoginFailureView(model);
            }

            var login = apiResult.Data;
            var (principal, primaryRole, accessScope, contextMode, normalizedRoles) = SessionClaimsBuilder.Build(
                ToSessionLoginContext(login), _roleCatalog, _primaryRoleResolver, _accessScopeResolver, HttpContext.Session.Id);
            var hasGlobalAccess = normalizedRoles.Any(role => _roleCatalog.IsGlobal(role));
            var requiresTenant = !hasGlobalAccess && normalizedRoles.Any(role => _roleCatalog.RequiresTenant(role));

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
            _logger.LogInformation("Cookie de autenticação criado. UsuarioId:{UsuarioId} TipoIdentificador:{TipoIdentificador}", login.UsuarioId, identifierKind);

            HttpContext.Session.SetString("jwt", login.Token);
            HttpContext.Session.SetString("JwtToken", login.Token);
            HttpContext.Session.SetString("UsuarioNome", login.Nome ?? string.Empty);
            HttpContext.Session.SetString("UsuarioEmail", login.Email ?? string.Empty);
            HttpContext.Session.SetString("UsuarioPerfil", primaryRole);
            HttpContext.Session.SetString("AccessScope", accessScope);
            HttpContext.Session.SetString("ContextMode", contextMode);
            _logger.LogInformation("Token salvo na sessão. UsuarioId:{UsuarioId} Perfil:{Perfil} Escopo:{Escopo}", login.UsuarioId, primaryRole, accessScope);

            // A identidade e a senha já foram validadas pela API. Um vínculo ausente
            // é um problema de contexto, não de autenticação: mantenha a sessão para
            // oferecer uma saída segura sem devolver o usuário ao formulário em loop.
            if (requiresTenant && !login.TenantId.HasValue)
            {
                _logger.LogWarning("Login autenticado sem vínculo elegível. UsuarioId:{UsuarioId}", login.UsuarioId);
                return RedirectToAction(nameof(NoEligibleContext));
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                _logger.LogInformation("Redirecionando por returnUrl. UsuarioId:{UsuarioId} Destino:{Destino}", login.UsuarioId, returnUrl);
                return Redirect(returnUrl);
            }

            return RedirectToActionByPerfil(normalizedRoles);
        }
        catch (HttpRequestException ex)
        {
            return HandleApiConnectionFailure(model, identifierKind, apiBaseUrl, ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Login Web cancelado pelo cliente. TipoIdentificador:{TipoIdentificador}", identifierKind);
            return new EmptyResult();
        }
        catch (TaskCanceledException ex)
        {
            return HandleApiConnectionFailure(model, identifierKind, apiBaseUrl, ex, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no login Web. TipoIdentificador:{TipoIdentificador} BaseUrl:{ApiBaseUrl} Mensagem:{ExceptionMessage}", identifierKind, apiBaseUrl, ex.Message);
            TempData["Error"] = "Não foi possível realizar o login no momento.";
            ModelState.AddModelError(string.Empty, "Não foi possível realizar o login no momento.");
            return LoginFailureView(model);
        }
    }

    private IActionResult HandleApiConnectionFailure(LoginViewModel model, string identifierKind, Uri? apiBaseUrl, Exception exception, bool timedOut = false)
    {
        var message = timedOut
            ? "A autenticação excedeu o tempo de resposta. Tente novamente em instantes."
            : "Não foi possível conectar ao serviço de autenticação. Tente novamente em instantes.";
        var failureType = exception is TaskCanceledException ? "Timeout" : exception.GetType().Name;

        _logger.LogError(
            exception,
            "Falha de conexão com a API no login Web. TipoIdentificador:{TipoIdentificador} BaseUrl:{ApiBaseUrl} Tipo:{FailureType} Mensagem:{ExceptionMessage}",
            identifierKind,
            apiBaseUrl,
            failureType,
            exception.Message);

        TempData["Error"] = message;
        ModelState.AddModelError(string.Empty, message);
        return LoginFailureView(model);
    }

    private IActionResult LoginFailureView(LoginViewModel model)
    {
        // Passwords must never be copied back into the generated HTML after a failed POST.
        ModelState.Remove(nameof(LoginViewModel.Senha));
        model.Senha = string.Empty;
        return View("Login", model);
    }

    private ApiResponse<T>? DeserializeApiResponse<T>(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ApiResponse<T>>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Resposta JSON inválida recebida da API de autenticação. Tamanho:{ResponseLength}", body.Length);
            return null;
        }
    }

    private static string ClassifyIdentifier(string value)
    {
        if (value.Contains('@', StringComparison.Ordinal)) return "EMAIL";
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length switch { 11 => "CPF", 14 => "CNPJ", _ => "INVALIDO" };
    }

    // Mapeia a resposta da API para o contexto imutável consumido pelo SessionClaimsBuilder,
    // usado pelos dois caminhos (Login e RefreshContext) para produzir o mesmo conjunto de claims.
    private static SessionLoginContext ToSessionLoginContext(LoginResponse login) => new(
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
        login.Token);

    private IActionResult RedirectToActionByPerfil(IEnumerable<string> roles)
    {
        var normalizedRoles = roles
            .Select(NormalizeRole)
            .OfType<string>()
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Prioridade explícita para evitar 404, loop de login e destinos inconsistentes quando o usuário possui múltiplos perfis.
        var priority = new List<(string Role, string Controller, string Action)>
        {
            (RolesConstants.AdministradorGlobal, "AdminSaas", "Index"),
            (RolesConstants.AdministradorCliente, "ClientePortal", "Index"),
            (RolesConstants.Administrador, "ClientePortal", "Index"),
            (RolesConstants.Diretor, "ClientePortal", "Index"),
            (RolesConstants.Coordenador, "CentralEscala", "Index"),
            (RolesConstants.Coordenacao, "CentralEscala", "Index"),
            (RolesConstants.Financeiro, "Financeiro", "Index"),
            (RolesConstants.Medico, "MedicoArea", "Index"),
            (RolesConstants.Hospital, "HospitalArea", "Index"),
            ("RECEPCAO", "CentralAtendimento", "Index"),
            ("TRIAGEM", "Triagem", "Fila"),
            (RolesConstants.Parceiro, "ParceiroPortal", "Index"),
            (RolesConstants.Suporte, "Suporte", "Index"),
            (RolesConstants.Auditor, "Auditoria", "Index"),
            (RolesConstants.Comercial, "Comercial", "Index"),
            (RolesConstants.CustomerSuccess, "CustomerSuccess", "Index")
        };

        var primaryRole = _primaryRoleResolver.Resolve(normalizedRoles);
        var destination = priority.FirstOrDefault(p => string.Equals(primaryRole, p.Role, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(destination.Controller))
        {
            destination = ("USUARIO", "Home", "Dashboard");
        }

        _logger.LogInformation("Redirecionando usuário após login. Perfis:{Perfis} Destino:{Controller}/{Action}", string.Join(',', normalizedRoles), destination.Controller, destination.Action);
        return RedirectToAction(destination.Action, destination.Controller);
    }


    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return null;
        var value = role.Trim().ToUpperInvariant()
            .Replace("Á", "A").Replace("À", "A").Replace("Â", "A").Replace("Ã", "A")
            .Replace("É", "E").Replace("Ê", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O").Replace("Ô", "O").Replace("Õ", "O")
            .Replace("Ú", "U")
            .Replace("Ç", "C");

        return value switch
        {
            "ADMIN" or "ADMINISTRADOR" => RolesConstants.Administrador,
            "COORDENADOR" => RolesConstants.Coordenador,
            "COORDENACAO" => RolesConstants.Coordenacao,
            "ADMIN_CLIENTE" or "ADMINISTRADOR_CLIENTE" => RolesConstants.AdministradorCliente,
            _ => value
        };
    }

    [Authorize]
    [HttpGet]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var token = HttpContext.Session.GetString("JwtToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("PlantaoProApi");
                client.DefaultRequestHeaders.Authorization = new("Bearer", token);
                using var response = await client.PostAsync("api/auth/logout", null, HttpContext.RequestAborted);
                if (!response.IsSuccessStatusCode)
                    _logger.LogWarning("Logout local concluído, mas a revogação da sessão API respondeu Status:{Status}", (int)response.StatusCode);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // O cookie local sempre deve ser removido, mesmo se a API estiver indisponível.
                _logger.LogWarning(ex, "Logout local concluído sem confirmação da revogação da sessão API.");
            }
        }
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData["Success"] = "Sessão encerrada com sucesso.";
        _logger.LogInformation("Logout UsuarioId:{UsuarioId} IP:{Ip}", usuarioId, HttpContext.Connection.RemoteIpAddress?.ToString());
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied(string? module = null, string? reason = null)
    {
        // Quando a negação é disparada por [Authorize(Roles)], o middleware de cookie
        // redireciona para o AccessDeniedPath levando apenas o ReturnUrl e perde os
        // diagnósticos do guard de rota. Nesses casos, derivamos módulo e causa do
        // controller de origem para exibir um diagnóstico por causa.
        if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(module))
        {
            var returnUrl = Request.Query["ReturnUrl"].ToString();
            var segment = returnUrl.TrimStart('/').Split('/', 2)[0];
            if (!string.IsNullOrEmpty(segment) && SaasRouteGuardFilter.TryResolveModule(segment, out var derived))
            {
                module = derived;
                reason = _moduleAccess.IsModuleEnabled(derived) ? "PERMISSAO_NEGADA" : "MODULO_NAO_CONTRATADO";
            }
        }
        ViewBag.Module = module;
        ViewBag.Reason = reason;
        return View();
    }

    [Authorize]
    public IActionResult NoEligibleContext()
    {
        if (!string.IsNullOrWhiteSpace(User.FindFirstValue("tenant_id")))
            return RedirectToActionByPerfil(User.FindAll(ClaimTypes.Role).Select(claim => claim.Value));

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        var client = _httpClientFactory.CreateClient("PlantaoProApi");
        var response = await client.PostAsJsonAsync("api/auth/forgot-password", new ForgotPasswordRequest(model.Email));
        TempData["Info"] = "Se o e-mail estiver cadastrado, enviaremos instruções para recuperação.";
        _logger.LogInformation("Solicitação Web de recuperação encaminhada. TipoIdentificador:{TipoIdentificador} Status:{Status}", ClassifyIdentifier(model.Email), (int)response.StatusCode);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email, string token) => View(new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        var client = _httpClientFactory.CreateClient("PlantaoProApi");
        var response = await client.PostAsJsonAsync("api/auth/reset-password", new ResetPasswordRequest(model.Email, model.Token, model.NovaSenha));
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = DeserializeApiResponse<object>(responseBody);
        if (result?.Success == true)
        {
            TempData["Success"] = "Senha redefinida com sucesso.";
            _logger.LogInformation("Redefinição de senha Web concluída. TipoIdentificador:{TipoIdentificador}", ClassifyIdentifier(model.Email));
            return RedirectToAction(nameof(Login));
        }

        TempData["Error"] = "Token inválido ou expirado.";
        _logger.LogWarning("Redefinição de senha Web recusada. TipoIdentificador:{TipoIdentificador}", ClassifyIdentifier(model.Email));
        return View(model);
    }

    [Authorize]
    [HttpGet("Account/RefreshContext")]
    [HttpPost("Account/RefreshContext")]
    public async Task<IActionResult> RefreshContext(string? returnUrl = null, CancellationToken ct = default)
    {
        var token = HttpContext.Session.GetString("JwtToken") ?? string.Empty;
        using var client = _httpClientFactory.CreateClient("PlantaoProApi");
        if (!string.IsNullOrWhiteSpace(token)) client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await client.PostAsync("api/auth/refresh-context", null, ct);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var apiResult = DeserializeApiResponse<LoginResponse>(body);
            if (apiResult?.Data != null)
            {
                var login = apiResult.Data;
                // Mesmo construtor de claims do Login: a atualização de contexto produz o
                // conjunto completo e normalizado (inclui access_catalog_version, permissões/
                // módulos normalizados e status do cliente), eliminando o desvio entre
                // login e refresh que quebrava o gate por módulo contratado.
                var (principal, primaryRole, accessScope, contextMode, _) = SessionClaimsBuilder.Build(
                    ToSessionLoginContext(login), _roleCatalog, _primaryRoleResolver, _accessScopeResolver, HttpContext.Session.Id);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
                HttpContext.Session.SetString("jwt", login.Token);
                HttpContext.Session.SetString("JwtToken", login.Token);
                HttpContext.Session.SetString("UsuarioNome", login.Nome ?? string.Empty);
                HttpContext.Session.SetString("UsuarioEmail", login.Email ?? string.Empty);
                HttpContext.Session.SetString("UsuarioPerfil", primaryRole);
                HttpContext.Session.SetString("AccessScope", accessScope);
                HttpContext.Session.SetString("ContextMode", contextMode);
                TempData["Success"] = "Contexto e módulos atualizados com sucesso.";
                return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "MeuDia");
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData["Error"] = response.StatusCode == HttpStatusCode.Unauthorized
            ? "Sua sessão expirou ou foi revogada. Entre novamente para continuar."
            : "Não foi possível atualizar seu acesso. Entre novamente para continuar com segurança.";
        _logger.LogWarning("Atualização de contexto recusada. Status:{Status}", (int)response.StatusCode);
        return RedirectToAction(nameof(Login));
    }
}
