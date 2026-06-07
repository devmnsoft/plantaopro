using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class JornadaClientesController : BaseWebController
{
    public JornadaClientesController(IHttpClientFactory f, ILogger<JornadaClientesController> logger) : base(f, logger) { }

    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<JornadaClienteViewModel>(client, "api/jornada-clientes");
        return View(result.Data);
    }

    public async Task<IActionResult> Details(Guid clienteId)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponseAsync<JornadaClienteViewModel>(client, "api/jornada-clientes/" + clienteId);
        if (result.Error is not null) TempData["Error"] = result.Error;
        return View(result.Data ?? new JornadaClienteViewModel { ClienteId = clienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avancar(Guid clienteId, MudarEtapaJornadaWebRequest request)
    {
        await EnviarMudanca(clienteId, request, "avancar", "Jornada avançada.");
        return RedirectToAction(nameof(Details), new { clienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retroceder(Guid clienteId, MudarEtapaJornadaWebRequest request)
    {
        await EnviarMudanca(clienteId, request, "retroceder", "Jornada retrocedida.");
        return RedirectToAction(nameof(Details), new { clienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarTarefa(CriarTarefaJornadaWebRequest request)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await SendApiAsync<CriarTarefaJornadaWebRequest, JornadaTarefaViewModel>(client, HttpMethod.Post, "api/jornada-clientes/" + request.ClienteId + "/tarefas", request);
        TempData[response.Error is null ? "Success" : "Error"] = response.Error ?? "Tarefa criada.";
        return RedirectToAction(nameof(Details), new { clienteId = request.ClienteId });
    }

    public async Task<IActionResult> Funil()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<FunilItemViewModel>(client, "api/jornada-clientes/funil");
        return View(result.Data);
    }

    public async Task<IActionResult> Tarefas()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiListResponseAsync<JornadaTarefaViewModel>(client, "api/jornada-clientes/tarefas");
        return View(result.Data);
    }

    private async Task EnviarMudanca(Guid clienteId, MudarEtapaJornadaWebRequest request, string acao, string sucesso)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return;
            var response = await SendApiAsync<MudarEtapaJornadaWebRequest, JornadaClienteViewModel>(client, HttpMethod.Post, "api/jornada-clientes/" + clienteId + "/" + acao, request);
            TempData[response.Error is null ? "Success" : "Error"] = response.Error ?? sucesso;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao mudar etapa da jornada");
            TempData["Error"] = "Não foi possível mudar a etapa.";
        }
    }
}
