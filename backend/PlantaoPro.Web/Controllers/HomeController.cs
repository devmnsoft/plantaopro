using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace PlantaoPro.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountController> _logger;
        private readonly IWebHostEnvironment _environment;

        public AccountController(IHttpClientFactory httpClientFactory, ILogger<AccountController> logger, IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            _logger.LogInformation("Acesso tela login IP:{Ip}", HttpContext.Connection.RemoteIpAddress?.ToString());
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogInformation("Tentativa de login no Web Email:{Email} IP:{Ip}", model.Email, ip);

            try
            {
                var client = _httpClientFactory.CreateClient("PlantaoProApi");
                _logger.LogInformation("BaseUrl API utilizada no login: {ApiBaseUrl}", client.BaseAddress);

                var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(model.Email, model.Senha));
                _logger.LogInformation("Status code retornado pela API no login: {StatusCode}", (int)response.StatusCode);

                var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

                if (response.IsSuccessStatusCode && apiResult?.Success == true && apiResult.Data is not null)
                {
                    var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, apiResult.Data.UsuarioId.ToString()), new(ClaimTypes.Name, apiResult.Data.Nome), new("jwt", apiResult.Data.Token), new(ClaimTypes.Email, model.Email) };
                    claims.AddRange(apiResult.Data.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
                    TempData["Success"] = "Login realizado com sucesso.";
                    _logger.LogInformation("Login sucesso Email:{Email} IP:{Ip}", model.Email, ip);
                    return RedirectToAction("Dashboard", "Home");
                }

                var nonSensitiveMessage = apiResult?.Message;
                if (!string.IsNullOrWhiteSpace(nonSensitiveMessage))
                    _logger.LogWarning("Falha de login retornada pela API: {ApiMessage}", nonSensitiveMessage);

                TempData["Error"] = response.StatusCode switch
                {
                    HttpStatusCode.Forbidden => "Usuário inativo. Contate o administrador.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest => "E-mail ou senha inválidos.",
                    HttpStatusCode.InternalServerError => "Erro interno ao autenticar. Consulte os logs.",
                    _ => "Erro ao autenticar. Tente novamente."
                };

                _logger.LogWarning("Login inválido Email:{Email} IP:{Ip}", model.Email, ip);
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha de comunicação com a API no login Email:{Email}", model.Email);
                TempData["Error"] = "Não foi possível conectar à API do PlantãoPro. Verifique se a API está em execução.";
                return View(model);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout ao chamar API no login Email:{Email}", model.Email);
                TempData["Error"] = "A autenticação demorou mais que o esperado. Tente novamente.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no login Email:{Email}", model.Email);
                TempData["Error"] = "Erro ao conectar ao servidor.";
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Sessão encerrada com sucesso.";
            _logger.LogInformation("Logout Email:{Email} IP:{Ip}", email, HttpContext.Connection.RemoteIpAddress?.ToString());
            return RedirectToAction(nameof(Login));
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
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<JsonElement>>();
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
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
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

    [Authorize]
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PlantaoProApi");
                var token = User.FindFirst("jwt")?.Value;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var overview = await client.GetFromJsonAsync<ApiResponse<DashboardOverviewDto>>("api/dashboard/overview");
                _logger.LogInformation("Acesso dashboard usuário:{User}", User.Identity?.Name);
                return View(overview?.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard.");
                TempData["Error"] = "Erro ao carregar dashboard.";
                return View(new DashboardOverviewDto(new DashboardDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), Array.Empty<PlantaoDto>(), Array.Empty<PagamentoDto>(), Array.Empty<NotificacaoDto>(), Array.Empty<DashboardChartItem>(), Array.Empty<DashboardChartItem>(), Array.Empty<DashboardChartItem>(), Array.Empty<DashboardChartItem>()));
            }
        }
    }
}
