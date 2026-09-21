using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public AccountController(IHttpClientFactory httpClientFactory, ILogger<AccountController> logger, IRoleCatalog roleCatalog, IPrimaryRoleResolver primaryRoleResolver, IAccessScopeResolver accessScopeResolver, ITenantContextResolver tenantContextResolver)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _roleCatalog = roleCatalog;
        _primaryRoleResolver = primaryRoleResolver;
        _accessScopeResolver = accessScopeResolver;
        _tenantContextResolver = tenantContextResolver;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        _logger.LogInformation("Acesso tela login IP:{Ip} ReturnUrl:{ReturnUrl}", HttpContext.Connection.RemoteIpAddress?.ToString(), returnUrl);
        return View(new LoginViewModel());
    }

    [HttpPost]
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
                    HttpStatusCode.Forbidden => "Usuário inativo. Contate o administrador.",
                    (HttpStatusCode)423 => apiResult?.Message ?? "Usuário bloqueado temporariamente.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest => "Identificador ou senha inválidos.",
                    _ => "Não foi possível autenticar. Tente novamente."
                };

                TempData["Error"] = errorMessage;
                ModelState.AddModelError(string.Empty, errorMessage);
                _logger.LogWarning("Falha no login Web. TipoIdentificador:{TipoIdentificador} Status:{Status} SuccessFlag:{SuccessFlag}", identifierKind, (int)response.StatusCode, apiResult?.Success);
                return LoginFailureView(model);
            }

            var login = apiResult.Data;
            var normalizedRoles = (login.Roles ?? Array.Empty<string>())
                .Select(_roleCatalog.Normalize)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var primaryRole = string.IsNullOrWhiteSpace(login.PrimaryRole) ? _primaryRoleResolver.Resolve(normalizedRoles) : _roleCatalog.Normalize(login.PrimaryRole) ?? _primaryRoleResolver.Resolve(normalizedRoles);
            var accessScope = string.IsNullOrWhiteSpace(login.AccessScope) ? _accessScopeResolver.Resolve(normalizedRoles, login.TenantContextSelected) : login.AccessScope;
            var contextMode = string.IsNullOrWhiteSpace(login.ContextMode) ? (login.TenantId.HasValue ? AccessScopes.Tenant : AccessScopes.Global) : login.ContextMode;

            var hasGlobalAccess = normalizedRoles.Any(role => _roleCatalog.IsGlobal(role));
            var requiresTenant = !hasGlobalAccess && normalizedRoles.Any(role => _roleCatalog.RequiresTenant(role));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, login.UsuarioId.ToString()),
                new Claim("sub", login.UsuarioId.ToString()),
                new Claim("uid", login.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(login.Nome) ? "Usuário PlantãoPro" : login.Nome),
                new Claim(ClaimTypes.Email, login.Email ?? string.Empty),
                new Claim("email", login.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, primaryRole),
                new Claim("role", primaryRole),
                new Claim("Perfil", primaryRole),
                new Claim("primary_role", primaryRole),
                new Claim("roles", string.Join(',', normalizedRoles)),
                new Claim("access_scope", accessScope),
                new Claim("context_mode", contextMode),
                new Claim("session_id", string.IsNullOrWhiteSpace(login.SessionId) ? HttpContext.Session.Id : login.SessionId),
                new Claim("jwt", login.Token),
                new Claim("access_catalog_version", "v2149")
            };

            claims.AddRange((login.Permissions ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new Claim("permission", NormalizeAccessCode(value)))
                .DistinctBy(claim => claim.Value));
            claims.AddRange((login.Modules ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new Claim("module", NormalizeAccessCode(value)))
                .DistinctBy(claim => claim.Value));

            if (login.ClienteId.HasValue)
            {
                var clienteId = login.ClienteId.Value.ToString();
                claims.Add(new Claim("cliente_id", clienteId));
                claims.Add(new Claim("cliente", login.ClienteNome ?? "Cliente PlantãoPro"));
                if (!string.IsNullOrWhiteSpace(login.ClienteStatus))
                {
                    claims.Add(new Claim("cliente_status", login.ClienteStatus.Trim().ToUpperInvariant()));
                }
            }
            if (login.TenantId.HasValue)
            {
                claims.Add(new Claim("tenant_id", login.TenantId.Value.ToString()));
                claims.Add(new Claim("tenant", login.TenantNome ?? login.ClienteNome ?? "Tenant PlantãoPro"));
            }

            foreach (var role in normalizedRoles)
            {
                var normalizedRole = _roleCatalog.Normalize(role);
                if (!string.IsNullOrWhiteSpace(normalizedRole) && !claims.Any(c => c.Type == ClaimTypes.Role && c.Value == normalizedRole))
                {
                    claims.Add(new Claim(ClaimTypes.Role, normalizedRole));
                }
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

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

    private static string NormalizeAccessCode(string value) => value.Trim().ToUpperInvariant().Replace(':', '.');

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
    public async Task<IActionResult> Logout()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
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
        _logger.LogInformation("Logout Email:{Email} IP:{Ip}", email, HttpContext.Connection.RemoteIpAddress?.ToString());
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied(string? module = null, string? reason = null)
    {
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
            _logger.LogInformation("Senha redefinida Email:{Email}", model.Email);
            return RedirectToAction(nameof(Login));
        }

        TempData["Error"] = "Token inválido ou expirado.";
        _logger.LogWarning("Falha redefinir senha Email:{Email}", model.Email);
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
                HttpContext.Session.SetString("jwt", login.Token);
                HttpContext.Session.SetString("JwtToken", login.Token);
                var normalizedRoles = (login.Roles ?? Array.Empty<string>())
                    .Select(_roleCatalog.Normalize)
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var primaryRole = string.IsNullOrWhiteSpace(login.PrimaryRole) ? _primaryRoleResolver.Resolve(normalizedRoles) : _roleCatalog.Normalize(login.PrimaryRole) ?? _primaryRoleResolver.Resolve(normalizedRoles);
                var accessScope = string.IsNullOrWhiteSpace(login.AccessScope) ? _accessScopeResolver.Resolve(normalizedRoles, login.TenantContextSelected) : login.AccessScope;
                var contextMode = string.IsNullOrWhiteSpace(login.ContextMode) ? (login.TenantId.HasValue ? AccessScopes.Tenant : AccessScopes.Global) : login.ContextMode;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, login.UsuarioId.ToString()),
                    new Claim(ClaimTypes.Name, login.Nome ?? string.Empty),
                    new Claim(ClaimTypes.Email, login.Email ?? string.Empty),
                    new Claim("uid", login.UsuarioId.ToString()),
                    new Claim("primary_role", primaryRole),
                    new Claim("access_scope", accessScope),
                    new Claim("context_mode", contextMode)
                };
                claims.AddRange((login.Permissions ?? Array.Empty<string>()).Select(p => new Claim("permission", p)).DistinctBy(c => c.Value));
                claims.AddRange((login.Modules ?? Array.Empty<string>()).Select(m => new Claim("module", m)).DistinctBy(c => c.Value));
                if (login.ClienteId.HasValue) claims.Add(new Claim("cliente_id", login.ClienteId.Value.ToString()));
                if (login.TenantId.HasValue) claims.Add(new Claim("tenant_id", login.TenantId.Value.ToString()));
                foreach (var role in normalizedRoles) claims.Add(new Claim(ClaimTypes.Role, role));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
                TempData["Success"] = "Contexto e módulos atualizados com sucesso.";
            }
        }
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "MeuDia");
    }
}
