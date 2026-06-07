using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR")]
public class ClientesController : BaseWebController
{
    public ClientesController(IHttpClientFactory httpClientFactory, ILogger<ClientesController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        try
        {
            Logger.LogInformation("Iniciando listagem de clientes");
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (data, error, _) = await ReadApiResponse<IEnumerable<ClienteDto>>(client, "api/clientes");
            ViewBag.ErrorMessage = error;
            Logger.LogInformation("Listagem de clientes concluída com sucesso");
            return View(data ?? Array.Empty<ClienteDto>());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro inesperado ao carregar tela de clientes");
            TempData["ErrorMessage"] = "Não foi possível carregar os clientes no momento.";
            return View(Array.Empty<ClienteDto>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarStatus(Guid id, string acao)
    {
        try
        {
            Logger.LogInformation("Alterando status de cliente {ClienteId} com ação {Acao}", id, acao);
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var endpoint = acao?.ToUpperInvariant() switch
            {
                "SUSPENDER" => $"api/clientes/{id}/suspender",
                "REATIVAR" => $"api/clientes/{id}/reativar",
                "CANCELAR" => $"api/clientes/{id}/cancelar",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                TempData["ErrorMessage"] = "Ação inválida para alteração de status.";
                return RedirectToAction(nameof(Index));
            }

            var response = await client.PostAsync(endpoint, new StringContent(string.Empty, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Não foi possível concluir a ação solicitada. {body}";
                Logger.LogWarning("Validação bloqueada ao alterar status do cliente {ClienteId}", id);
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Status do cliente atualizado com sucesso.";
            Logger.LogInformation("Status do cliente {ClienteId} atualizado com sucesso", id);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro inesperado ao alterar status do cliente {ClienteId}", id);
            TempData["ErrorMessage"] = "Erro inesperado ao alterar status do cliente.";
            return RedirectToAction(nameof(Index));
        }
    }

    public IActionResult Jornada(Guid id)
    {
        return RedirectToAction("Details", "JornadaClientes", new { clienteId = id });
    }

    public IActionResult Inteligencia(Guid id)
    {
        return RedirectToAction("ClientesRisco", "Inteligencia", new { clienteId = id });
    }

}
