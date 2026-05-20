using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Text.Json;

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

        var response = await client.GetFromJsonAsync<ApiResponse<UserSettingsSummaryViewModel>>("api/usuarios/me");
        var model = response?.Data is null
            ? new UserSettingsSummaryViewModel()
            : response.Data;

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
    public UserSettingsSummaryViewModel() : this(Guid.Empty, string.Empty, string.Empty, null, "Email"){}
}
