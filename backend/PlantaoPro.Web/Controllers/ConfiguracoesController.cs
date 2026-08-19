using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Net;
using System.Text.Json;
using System.Security.Claims;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor)]
public class ConfiguracoesController : BaseWebController
{
    public ConfiguracoesController(IHttpClientFactory f, ILogger<ConfiguracoesController> l) : base(f, l) { }

    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        var tokenPresente = AddBearerToken(client);
        if (!tokenPresente) return HandleUnauthorized();

        var model = new UserSettingsSummaryViewModel();
        var endpoint = "api/usuarios/me";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/D";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "N/D";
        var perfil = User.FindFirstValue(ClaimTypes.Role) ?? "N/D";
        var dataHoraUtc = DateTime.UtcNow;

        try
        {
            var (data, error, statusCode) = await ReadApiResponse<UserSettingsSummaryViewModel>(client, endpoint);
            model = data ?? new UserSettingsSummaryViewModel();
            model = model with
            {
                PreferenciasNotificacao = string.IsNullOrWhiteSpace(model.PreferenciasNotificacao) ? "{}" : model.PreferenciasNotificacao
            };

            if (statusCode >= HttpStatusCode.BadRequest)
            {
                ViewBag.ErrorMessage = error ?? "Não foi possível carregar as configurações agora. Tente novamente em instantes.";
                Logger.LogWarning("Configurações Index com erro de API. Endpoint:{Endpoint} Status:{Status} IP:{Ip} Email:{Email} Perfil:{Perfil} DataHoraUtc:{DataHoraUtc}", endpoint, (int)statusCode, ip, email, perfil, dataHoraUtc);
            }
            else
            {
                Logger.LogInformation("Configurações Index carregado com sucesso. Endpoint:{Endpoint} Status:{Status} IP:{Ip} Email:{Email} Perfil:{Perfil} DataHoraUtc:{DataHoraUtc}", endpoint, (int)statusCode, ip, email, perfil, dataHoraUtc);
            }
        }
        catch (HttpRequestException ex)
        {
            ViewBag.ErrorMessage = "A API de usuário está indisponível no momento. Tente novamente em instantes.";
            Logger.LogError(ex, "Falha de comunicação HTTP ao carregar Configurações. Endpoint:{Endpoint} Status:{Status} IP:{Ip} Email:{Email} Perfil:{Perfil} DataHoraUtc:{DataHoraUtc}", endpoint, 500, ip, email, perfil, dataHoraUtc);
        }
        catch (TaskCanceledException ex)
        {
            ViewBag.ErrorMessage = "Tempo de resposta excedido ao carregar as configurações. Tente novamente.";
            Logger.LogError(ex, "Timeout ao carregar Configurações. Endpoint:{Endpoint} Status:{Status} IP:{Ip} Email:{Email} Perfil:{Perfil} DataHoraUtc:{DataHoraUtc}", endpoint, 408, ip, email, perfil, dataHoraUtc);
        }

        return View(model);
    }

    [HttpGet("/SystemHealth")]
    public async Task<IActionResult> Saude()
    {
        var client = CreateApiClient();
        AddBearerToken(client);
        var endpoint = "api/health/system";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/D";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "N/D";
        var dataHoraUtc = DateTime.UtcNow;

        var dados = new HealthViewModel(
            Status: "INDISPONÍVEL",
            Ambiente: "N/D",
            DataHora: DateTime.UtcNow,
            Versao: null,
            Componentes: new[] { new HealthComponentViewModel("API", "INDISPONÍVEL", "Não foi possível consultar o serviço.") });

        try
        {
            var response = await client.GetAsync(endpoint);
            dados = dados with { Status = response.IsSuccessStatusCode ? "SAUDÁVEL" : "INDISPONÍVEL" };

            var rawJson = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(rawJson))
            {
                using var json = JsonDocument.Parse(rawJson);
                if (json.RootElement.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String)
                        dados = dados with { Status = status.GetString() ?? dados.Status };

                    // O contrato público de health usa nomes neutros em inglês. Mantemos
                    // compatibilidade com o contrato antigo sem inventar estado de banco.
                    if (data.TryGetProperty("environment", out var environment) && environment.ValueKind == JsonValueKind.String)
                        dados = dados with { Ambiente = environment.GetString() ?? dados.Ambiente };

                    if (data.TryGetProperty("timestampUtc", out var timestamp) && timestamp.ValueKind == JsonValueKind.String && DateTime.TryParse(timestamp.GetString(), out var timestampUtc))
                        dados = dados with { DataHora = timestampUtc };

                    if (data.TryGetProperty("version", out var version) && version.ValueKind == JsonValueKind.String)
                        dados = dados with { Versao = version.GetString() };
                    if (data.TryGetProperty("components", out var components) && components.ValueKind == JsonValueKind.Array)
                        dados = dados with { Componentes = components.EnumerateArray().Select(component => new HealthComponentViewModel(
                            component.GetProperty("name").GetString() ?? "Componente",
                            component.GetProperty("status").GetString() ?? "INDISPONÍVEL",
                            component.GetProperty("detail").GetString() ?? "Sem detalhe disponível.")).ToArray() };
                }
            }

            Logger.LogInformation("Healthcheck web concluído. Endpoint:{Endpoint} StatusCode:{StatusCode} Sucesso:{Sucesso} IP:{Ip} Email:{Email} DataHoraUtc:{DataHoraUtc}", endpoint, (int)response.StatusCode, response.IsSuccessStatusCode, ip, email, dataHoraUtc);
        }
        catch (HttpRequestException ex)
        {
            TempData["Error"] = "Não foi possível consultar a saúde da API no momento.";
            Logger.LogError(ex, "Falha HTTP em Saude. Endpoint:{Endpoint} StatusCode:{StatusCode} Sucesso:{Sucesso} IP:{Ip} Email:{Email} DataHoraUtc:{DataHoraUtc}", endpoint, 503, false, ip, email, dataHoraUtc);
        }
        catch (JsonException ex)
        {
            TempData["Error"] = "O retorno de saúde da API veio em formato inesperado.";
            Logger.LogError(ex, "Falha de parse JSON em Saude. Endpoint:{Endpoint} StatusCode:{StatusCode} Sucesso:{Sucesso} IP:{Ip} Email:{Email} DataHoraUtc:{DataHoraUtc}", endpoint, 500, false, ip, email, dataHoraUtc);
        }

        return View(dados);
    }
}


public record UserSettingsSummaryViewModel(Guid Id, string Nome, string Email, string? Telefone, string PreferenciasNotificacao)
{
    public UserSettingsSummaryViewModel() : this(Guid.Empty, string.Empty, string.Empty, null, "{}"){}
}
