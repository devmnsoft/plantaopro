using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Net;
using System.Text.Json;
using System.Security.Claims;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Administrador)]
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

    public async Task<IActionResult> Saude()
    {
        var client = CreateApiClient();
        var tokenPresente = AddBearerToken(client);
        var response = await client.GetAsync("api/health");

        var baseUrl = client.BaseAddress?.ToString()?.TrimEnd('/') ?? string.Empty;
        var dados = new HealthViewModel(
            Status: response.IsSuccessStatusCode ? "Healthy" : $"HTTP {(int)response.StatusCode}",
            Ambiente: "N/D",
            Schema: "plantaopro",
            BancoConectado: false,
            DataHora: DateTime.UtcNow,
            Versao: null,
            BaseUrlApi: baseUrl,
            TokenPresente: tokenPresente,
            UsuarioAutenticado: User.Identity?.Name ?? "Não autenticado",
            SwaggerUrl: string.IsNullOrWhiteSpace(baseUrl) ? "swagger" : $"{baseUrl}/swagger");

        if (response.IsSuccessStatusCode)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = json.RootElement.GetProperty("data");
            dados = dados with
            {
                Status = data.GetProperty("status").GetString() ?? dados.Status,
                BancoConectado = data.GetProperty("bancoConectado").GetBoolean(),
                Schema = data.GetProperty("schema").GetString() ?? dados.Schema,
                Ambiente = data.GetProperty("ambiente").GetString() ?? dados.Ambiente,
                DataHora = data.GetProperty("dataHora").GetDateTime(),
                Versao = data.TryGetProperty("versao", out var versao) ? versao.GetString() : dados.Versao
            };
        }

        return View(dados);
    }
}


public record UserSettingsSummaryViewModel(Guid Id, string Nome, string Email, string? Telefone, string PreferenciasNotificacao)
{
    public UserSettingsSummaryViewModel() : this(Guid.Empty, string.Empty, string.Empty, null, "{}"){}
}
