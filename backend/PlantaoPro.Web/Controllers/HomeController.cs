using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using PlantaoPro.Web.Security;

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

            try
            {
                using var client = _httpClientFactory.CreateClient("PlantaoProApi");
                _logger.LogInformation("Chamando API de login. BaseUrl:{ApiBaseUrl}", client.BaseAddress);

                var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest(normalizedEmail, model.Senha ?? string.Empty));
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Resposta da API de login. Status:{StatusCode}", (int)response.StatusCode);

                var apiResult = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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

                return RedirectToActionByPerfil(perfil);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no login Web. Email:{Email}", normalizedEmail);
                TempData["Error"] = "Não foi possível realizar o login no momento.";
                return View(model);
            }
        }

        private IActionResult RedirectToActionByPerfil(string? perfil)
        {
            var normalized = NormalizeRole(perfil) ?? string.Empty;
            var destination = normalized switch
            {
                "MEDICO" => (Action: "Index", Controller: "MinhaAgenda"),
                "FINANCEIRO" => (Action: "Index", Controller: "Financeiro"),
                "COORDENACAO" => (Action: "Dashboard", Controller: "Home"),
                "OPERADOR" => (Action: "Dashboard", Controller: "Home"),
                "HOSPITAL" => (Action: "Index", Controller: "Agenda"),
                "ADMINISTRADOR_GLOBAL" => (Action: "Dashboard", Controller: "Home"),
                "ADMINISTRADOR" => (Action: "Dashboard", Controller: "Home"),
                _ => (Action: "Dashboard", Controller: "Home")
            };

            _logger.LogInformation("Redirecionando usuário após login. Perfil:{Perfil} Destino:{Controller}/{Action}", normalized, destination.Controller, destination.Action);
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
                "COORDENADOR" or "COORDENACAO" => RolesConstants.Coordenacao,
                _ => value
            };
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
        public IActionResult AccessDenied() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ErrorStatusCode(int code)
        {
            if (code == 404) return View("~/Views/Shared/NotFound.cshtml");
            if (code == 403) return RedirectToAction(nameof(AccessDenied));
            return View("~/Views/Shared/Error.cshtml");
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
    public class HomeController : BaseWebController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger) : base(httpClientFactory, logger)
        {
            _logger = logger;
        }

        public IActionResult Produto() => View();

        public async Task<IActionResult> Dashboard([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, [FromQuery] string? hospital, [FromQuery] string? especialidade)
        {
            if (User.IsMedico()) return RedirectToAction("Index", "MinhaAgenda");
            var fallback = CriarDashboardVazio();

            try
            {
                var client = CreateApiClient();
                if (!AddBearerToken(client)) return HandleUnauthorized();

                const string endpoint = "api/dashboard";
                var (data, error, statusCode) = await ReadApiResponse<DashboardOverviewDto>(client, endpoint);

                if (statusCode == HttpStatusCode.Unauthorized)
                    return HandleUnauthorized("Sessão expirada. Faça login novamente.");

                if (statusCode == HttpStatusCode.Forbidden)
                    return EmptyViewWithError(fallback, "Acesso negado ao dashboard.");

                if (statusCode == HttpStatusCode.NotFound)
                    return EmptyViewWithError(fallback, "Rota de dashboard não encontrada na API.");

                if ((int)statusCode >= 500)
                    return EmptyViewWithError(fallback, "A API está indisponível no momento.");

                _logger.LogInformation("Acesso dashboard usuário:{User} FiltrosInicio:{Inicio} FiltrosFim:{Fim} Hospital:{Hospital} Especialidade:{Especialidade}",
                    User.Identity?.Name, inicio, fim, hospital, especialidade);
                var safeData = data is null ? fallback : data with
                {
                    ProximosPlantoes = data.ProximosPlantoes ?? Array.Empty<PlantaoResumoDto>(),
                    UltimosPagamentos = data.UltimosPagamentos ?? Array.Empty<PagamentoResumoDto>(),
                    UltimasNotificacoes = data.UltimasNotificacoes ?? Array.Empty<NotificacaoDto>(),
                    PlantoesPorMes = data.PlantoesPorMes ?? Array.Empty<DashboardChartItem>(),
                    PagamentosPorMes = data.PagamentosPorMes ?? Array.Empty<DashboardChartItem>(),
                    PlantoesPorEspecialidade = data.PlantoesPorEspecialidade ?? Array.Empty<DashboardChartItem>(),
                    PlantoesPorHospital = data.PlantoesPorHospital ?? Array.Empty<DashboardChartItem>()
                };
                safeData = AplicarFiltrosDashboard(safeData, inicio, fim, hospital, especialidade);
                if (!string.IsNullOrWhiteSpace(error)) TempData["Info"] = error;
                return View(safeData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar dashboard.");
                TempData["Error"] = "Erro ao carregar dashboard.";
                return View(fallback);
            }
        }
        private static DashboardOverviewDto CriarDashboardVazio()
        {
            return new DashboardOverviewDto(
                new DashboardDto(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0m, 0m, 0L),
                Array.Empty<PlantaoResumoDto>(),
                Array.Empty<PagamentoResumoDto>(),
                Array.Empty<NotificacaoDto>(),
                Array.Empty<DashboardChartItem>(),
                Array.Empty<DashboardChartItem>(),
                Array.Empty<DashboardChartItem>(),
                Array.Empty<DashboardChartItem>());
        }

        private static DashboardOverviewDto AplicarFiltrosDashboard(
            DashboardOverviewDto source,
            DateTime? inicio,
            DateTime? fim,
            string? hospital,
            string? especialidade)
        {
            var inicioEfetivo = inicio?.Date;
            var fimEfetivo = fim?.Date.AddDays(1).AddTicks(-1);
            var hospitalFiltro = (hospital ?? string.Empty).Trim();
            var especialidadeFiltro = (especialidade ?? string.Empty).Trim();

            bool MatchPeriodo(DateTime data) =>
                (!inicioEfetivo.HasValue || data >= inicioEfetivo.Value) &&
                (!fimEfetivo.HasValue || data <= fimEfetivo.Value);

            bool MatchPlantao(PlantaoResumoDto p) =>
                MatchPeriodo(p.DataInicio) &&
                (string.IsNullOrWhiteSpace(hospitalFiltro) || p.HospitalNome.Contains(hospitalFiltro, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(especialidadeFiltro) || p.EspecialidadeNome.Contains(especialidadeFiltro, StringComparison.OrdinalIgnoreCase));

            bool MatchPagamento(PagamentoResumoDto p) =>
                MatchPeriodo(p.DataPlantao) &&
                (string.IsNullOrWhiteSpace(hospitalFiltro) || p.HospitalNome.Contains(hospitalFiltro, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(especialidadeFiltro) || p.EspecialidadeNome.Contains(especialidadeFiltro, StringComparison.OrdinalIgnoreCase));

            var plantoesFiltrados = source.ProximosPlantoes.Where(MatchPlantao).ToArray();
            var pagamentosFiltrados = source.UltimosPagamentos.Where(MatchPagamento).ToArray();
            var indicadores = source.Indicadores with
            {
                TotalPlantoes = plantoesFiltrados.LongLength,
                PlantoesConfirmados = plantoesFiltrados.LongCount(p => p.Status.Equals("CONFIRMADO", StringComparison.OrdinalIgnoreCase)),
                PagamentosPendentes = pagamentosFiltrados.LongCount(p => p.Status.Equals("PENDENTE", StringComparison.OrdinalIgnoreCase)),
                ValorPendente = pagamentosFiltrados.Where(p => p.Status.Equals("PENDENTE", StringComparison.OrdinalIgnoreCase)).Sum(p => p.ValorPrevisto),
                ValorPagoMes = pagamentosFiltrados.Where(p => p.Status.Equals("PAGO", StringComparison.OrdinalIgnoreCase)).Sum(p => p.ValorPago ?? 0)
            };

            return source with
            {
                Indicadores = indicadores,
                ProximosPlantoes = plantoesFiltrados,
                UltimosPagamentos = pagamentosFiltrados
            };
        }

    }
}
