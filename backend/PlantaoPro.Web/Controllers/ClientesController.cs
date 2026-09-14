using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
public class ClientesController : BaseWebController
{
    public ClientesController(IHttpClientFactory httpClientFactory, ILogger<ClientesController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Details(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, statusCode) = await ReadApiResponse<ClienteDto>(client, $"api/clientes/{id}");
        if (data is null)
        {
            TempData["ErrorMessage"] = error ?? "Cliente não encontrado ou contexto não autorizado.";
            Logger.LogWarning("Central global não abriu cliente {ClienteId}. Status {Status}", id, (int)statusCode);
            return RedirectToAction(nameof(Index));
        }
        return View(data);
    }

    public async Task<IActionResult> Index(string? busca, string? status, int pagina = 1, int tamanhoPagina = 20)
    {
        try
        {
            Logger.LogInformation("Iniciando listagem de clientes");
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var query = $"api/clientes/central?busca={Uri.EscapeDataString(busca ?? string.Empty)}&status={Uri.EscapeDataString(status ?? string.Empty)}&pagina={Math.Max(1, pagina)}&tamanhoPagina={Math.Clamp(tamanhoPagina, 10, 100)}";
            var (data, error, _) = await ReadApiResponse<ClienteCentralPageDto>(client, query);
            ViewBag.ErrorMessage = error;
            ViewBag.Busca = busca;
            ViewBag.Status = status;
            Logger.LogInformation("Listagem de clientes concluída com sucesso");
            return View(data ?? new ClienteCentralPageDto(Array.Empty<ClienteCentralDto>(), 1, 20, 0, new(0, 0, 0, 0, 0)));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro inesperado ao carregar tela de clientes");
            TempData["ErrorMessage"] = "Não foi possível carregar os clientes no momento.";
            return View(new ClienteCentralPageDto(Array.Empty<ClienteCentralDto>(), 1, 20, 0, new(0, 0, 0, 0, 0)));
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
    public async Task<IActionResult> AlterarStatus(Guid id, string acao, string motivo, string? busca, string? status, int pagina = 1)
    {
        try
        {
            Logger.LogInformation("Alterando status de cliente {ClienteId} com ação {Acao}", id, acao);
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            var endpoint = $"api/clientes/{id}/situacao";

            if (string.IsNullOrWhiteSpace(acao) || string.IsNullOrWhiteSpace(motivo))
            {
                TempData["ErrorMessage"] = "Ação inválida para alteração de status.";
                return RedirectToAction(nameof(Index));
            }

            var response = await client.PostAsync(endpoint, new StringContent(JsonSerializer.Serialize(new AlterarStatusClienteRequest(acao, motivo)), Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = $"Não foi possível concluir a ação solicitada. {body}";
                Logger.LogWarning("Validação bloqueada ao alterar status do cliente {ClienteId}", id);
                return RedirectToAction(nameof(Index), new { busca, status, pagina });
            }

            TempData["SuccessMessage"] = "Status do cliente atualizado com sucesso.";
            Logger.LogInformation("Status do cliente {ClienteId} atualizado com sucesso", id);
            return RedirectToAction(nameof(Index), new { busca, status, pagina });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro inesperado ao alterar status do cliente {ClienteId}", id);
            TempData["ErrorMessage"] = "Erro inesperado ao alterar status do cliente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
