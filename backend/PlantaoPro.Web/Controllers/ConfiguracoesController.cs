using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace PlantaoPro.Web.Controllers;

public class ConfiguracoesController : BaseWebController
{
    public ConfiguracoesController(IHttpClientFactory f, ILogger<ConfiguracoesController> l) : base(f, l) { }

    public IActionResult Index() => View();

    public async Task<IActionResult> Saude()
    {
        var client = CreateApiClient();
        var tokenPresente = AddBearerToken(client);
        var response = await client.GetAsync("api/health");

        var dados = new Dictionary<string, string?>
        {
            ["BaseUrl"] = client.BaseAddress?.ToString(),
            ["StatusApi"] = ((int)response.StatusCode).ToString(),
            ["UsuarioAutenticado"] = User.Identity?.Name,
            ["Token"] = tokenPresente ? "Presente" : "Ausente",
            ["Swagger"] = $"{client.BaseAddress}swagger"
        };

        if (response.IsSuccessStatusCode)
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = json.RootElement.GetProperty("data");
            dados["StatusAplicacao"] = data.GetProperty("status").GetString();
            dados["StatusBanco"] = data.GetProperty("bancoConectado").GetBoolean() ? "Conectado" : "Indisponível";
            dados["Schema"] = data.GetProperty("schema").GetString();
            dados["Ambiente"] = data.GetProperty("ambiente").GetString();
            dados["DataHora"] = data.GetProperty("dataHora").GetString();
        }

        return View(dados);
    }
}
