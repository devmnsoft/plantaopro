using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
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
    private readonly IWebHostEnvironment _environment;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public AccountController(IHttpClientFactory httpClientFactory, ILogger<AccountController> logger, IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _environment = environment;
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
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        var normalizedEmail = (model.Email ?? string.Empty).Trim();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Login POST iniciado. Email:{Email} IP:{Ip} ReturnUrl:{ReturnUrl}", normalizedEmail, ip, returnUrl);

        if (!ModelState.IsValid)
        {
            _logger.LogInformation("Login POST inválido por ModelState. Email:{Email}", normalizedEmail);
            return View(model);
        }

        Uri? apiBaseUrl = null;

        try
        {
            using var client = _httpClientFactory.CreateClient("PlantaoProApi");
            apiBaseUrl = client.BaseAddress;
            _logger.LogInformation("Chamando API de login. BaseUrl:{ApiBaseUrl}", apiBaseUrl);

            var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(normalizedEmail, model.Senha ?? string.Empty));
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Resposta da API de login. Status:{StatusCode}", (int)response.StatusCode);

            var apiResult = DeserializeApiResponse<LoginResponse>(body);

            if (!response.IsSuccessStatusCode || apiResult is null || !apiResult.Success || apiResult.Data is null || string.IsNullOrWhiteSpace(apiResult.Data.Token))
            {
                var errorMessage = response.StatusCode switch
                {
                    HttpStatusCode.Forbidden => "Usuário inativo. Contate o administrador.",
                    (HttpStatusCode)423 => apiResult?.Message ?? "Usuário bloqueado temporariamente.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest => "E-mail ou senha inválidos.",
                    _ => "Não foi possível autenticar. Tente novamente."
                };

                TempData["Error"] = errorMessage;
                ModelState.AddModelError(string.Empty, errorMessage);
                _logger.LogWarning("Falha no login Web. Email:{Email} Status:{Status} SuccessFlag:{SuccessFlag}", normalizedEmail, (int)response.StatusCode, apiResult?.Success);
                return View(model);
            }

            var login = apiResult.Data;
            var perfil = NormalizeRole((login.Roles ?? Array.Empty<string>()).FirstOrDefault()) ?? string.Empty;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, login.UsuarioId.ToString()),
                new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(login.Nome) ? normalizedEmail : login.Nome),
                new Claim(ClaimTypes.Email, normalizedEmail),
                new Claim(ClaimTypes.Role, perfil),
                new Claim("Perfil", perfil),
                new Claim("jwt", login.Token)
            };

            if (login.ClienteId.HasValue)
            {
                var clienteId = login.ClienteId.Value.ToString();
                claims.Add(new Claim("cliente_id", clienteId));
                claims.Add(new Claim("tenant_id", clienteId));
                claims.Add(new Claim("tenant", login.ClienteNome ?? "Tenant PlantãoPro"));
                claims.Add(new Claim("cliente", login.ClienteNome ?? "Cliente PlantãoPro"));
            }

            foreach (var role in login.Roles ?? Array.Empty<string>())
            {
                var normalizedRole = NormalizeRole(role);
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
            _logger.LogInformation("Cookie de autenticação criado. Email:{Email}", normalizedEmail);

            HttpContext.Session.SetString("jwt", login.Token);
            HttpContext.Session.SetString("JwtToken", login.Token);
            HttpContext.Session.SetString("UsuarioNome", login.Nome ?? string.Empty);
            HttpContext.Session.SetString("UsuarioEmail", normalizedEmail);
            HttpContext.Session.SetString("UsuarioPerfil", perfil);
            _logger.LogInformation("Token salvo na sessão. Email:{Email} Perfil:{Perfil}", normalizedEmail, perfil);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                _logger.LogInformation("Redirecionando por returnUrl. Email:{Email} Destino:{Destino}", normalizedEmail, returnUrl);
                return Redirect(returnUrl);
            }

            return RedirectToActionByPerfil(login.Roles ?? Array.Empty<string>());
        }
        catch (HttpRequestException ex)
        {
            return HandleApiConnectionFailure(model, normalizedEmail, apiBaseUrl, ex);
        }
        catch (TaskCanceledException ex)
        {
            return HandleApiConnectionFailure(model, normalizedEmail, apiBaseUrl, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado no login Web. Email:{Email} BaseUrl:{ApiBaseUrl} Mensagem:{ExceptionMessage}", normalizedEmail, apiBaseUrl, ex.Message);
            TempData["Error"] = "Não foi possível realizar o login no momento.";
            return View(model);
        }
    }

    private IActionResult HandleApiConnectionFailure(LoginViewModel model, string email, Uri? apiBaseUrl, Exception exception)
    {
        const string message = "Não foi possível conectar à API do PlantãoPro. Verifique se o backend está em execução.";
        var failureType = exception is TaskCanceledException ? "Timeout" : exception.GetType().Name;

        _logger.LogError(
            exception,
            "Falha de conexão com a API no login Web. Email:{Email} BaseUrl:{ApiBaseUrl} Tipo:{FailureType} Mensagem:{ExceptionMessage}",
            email,
            apiBaseUrl,
            failureType,
            exception.Message);

        TempData["Error"] = message;
        ModelState.AddModelError(string.Empty, message);
        return View(model);
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
            var sample = body.Length > 400 ? body[..400] + "..." : body;
            _logger.LogError(ex, "Resposta JSON inválida recebida da API de autenticação. ResponseSample:{ResponseSample}", sample);
            return null;
        }
    }

    private IActionResult RedirectToActionByPerfil(IEnumerable<string> roles)
    {
        var normalizedRoles = roles
            .Select(NormalizeRole)
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
            (RolesConstants.Parceiro, "ParceiroPortal", "Index"),
            (RolesConstants.Suporte, "Suporte", "Index"),
            (RolesConstants.Auditor, "Auditoria", "Index"),
            (RolesConstants.Comercial, "Comercial", "Index"),
            (RolesConstants.CustomerSuccess, "CustomerSuccess", "Index")
        };

        var destination = priority.FirstOrDefault(p => normalizedRoles.Any(r => string.Equals(r, p.Role, StringComparison.OrdinalIgnoreCase)));
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
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData["Success"] = "Sessão encerrada com sucesso.";
        _logger.LogInformation("Logout Email:{Email} IP:{Ip}", email, HttpContext.Connection.RemoteIpAddress?.ToString());
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

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
        var responseBody = await response.Content.ReadAsStringAsync();
        var content = DeserializeApiResponse<JsonElement>(responseBody);
        TempData["Info"] = "Se o e-mail estiver cadastrado, enviaremos instruções para recuperação.";
        if (_environment.IsDevelopment() && content?.Data.TryGetProperty("tokenDev", out var tokenDev) == true && tokenDev.GetString() is { Length: > 0 } token)
        {
            TempData["Warning"] = $"Token de desenvolvimento: {token}";
        }
        _logger.LogInformation("Solicitação de recuperação Email:{Email}", model.Email);
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
}
