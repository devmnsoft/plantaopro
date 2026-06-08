using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
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

    public async Task<IActionResult> Jornada(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (data, error, statusCode) = await ReadApiResponse<JornadaClienteDetalheViewModel>(client, "api/jornada-clientes/" + id);
        if (data is null)
        {
            Logger.LogWarning("Falha ao carregar jornada contextual do cliente {ClienteId}. Status:{Status} Erro:{Erro}", id, (int)statusCode, error);
            return View(new JornadaClienteDetalheViewModel { Jornada = new JornadaClienteResumoViewModel { ClienteId = id }, Eventos = Array.Empty<JornadaClienteEventoViewModel>(), Tarefas = Array.Empty<JornadaClienteTarefaViewModel>() });
        }

        ViewBag.ErrorMessage = error;
        return View(data);
    }

    public async Task<IActionResult> Inteligencia(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (saude, saudeError, saudeStatus) = await ReadApiResponse<ClienteSaudeSaasViewModel>(client, "api/clientes/" + id + "/saude");
        var (uso, usoError, usoStatus) = await ReadApiResponse<UsoPlanoViewModel>(client, "api/clientes/" + id + "/uso-plano");
        var (alertas, alertasError, alertasStatus) = await ReadApiResponse<IEnumerable<ClienteAlertaSaasViewModel>>(client, "api/clientes/" + id + "/alertas");

        if (!string.IsNullOrWhiteSpace(saudeError)) Logger.LogWarning("Falha ao carregar saúde do cliente {ClienteId}. Status:{Status} Erro:{Erro}", id, (int)saudeStatus, saudeError);
        if (!string.IsNullOrWhiteSpace(usoError)) Logger.LogWarning("Falha ao carregar uso do plano do cliente {ClienteId}. Status:{Status} Erro:{Erro}", id, (int)usoStatus, usoError);
        if (!string.IsNullOrWhiteSpace(alertasError)) Logger.LogWarning("Falha ao carregar alertas do cliente {ClienteId}. Status:{Status} Erro:{Erro}", id, (int)alertasStatus, alertasError);

        var erro = saudeError ?? usoError ?? alertasError;
        return View(new ClienteInteligenciaPageViewModel
        {
            ClienteId = id,
            Saude = saude ?? new ClienteSaudeSaasViewModel { ClienteId = id },
            Uso = uso ?? new UsoPlanoViewModel { ClienteId = id },
            Alertas = alertas ?? Array.Empty<ClienteAlertaSaasViewModel>(),
            ErrorMessage = erro
        });
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
}
