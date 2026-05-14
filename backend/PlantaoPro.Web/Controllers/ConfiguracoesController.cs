using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class ConfiguracoesController : BaseWebController
{
    public ConfiguracoesController(IHttpClientFactory f, ILogger<ConfiguracoesController> l) : base(f, l) { }
    public IActionResult Index() => View();

    public async Task<IActionResult> Saude()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponse<HealthViewModel>(client, "api/health");
        var m = result.Data ?? new HealthViewModel("Indisponível","N/D","plantaopro",false,DateTime.UtcNow,null,client.BaseAddress?.ToString() ?? string.Empty,!string.IsNullOrWhiteSpace(GetJwtToken()),User.Identity?.Name ?? "Não autenticado",$"{client.BaseAddress}swagger");
        m = m with { BaseUrlApi = client.BaseAddress?.ToString() ?? string.Empty, TokenPresente = !string.IsNullOrWhiteSpace(GetJwtToken()), UsuarioAutenticado = User.Identity?.Name ?? "Não autenticado", SwaggerUrl = $"{client.BaseAddress}swagger"};
        return View(m);
    }
}
