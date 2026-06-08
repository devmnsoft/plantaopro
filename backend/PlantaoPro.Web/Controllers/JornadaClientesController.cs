using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class JornadaClientesController : BaseWebController
{
    public JornadaClientesController(IHttpClientFactory httpClientFactory, ILogger<JornadaClientesController> logger) : base(httpClientFactory, logger) { }

    public async Task<IActionResult> Index()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (data, error, _) = await ReadApiResponse<IEnumerable<JornadaClienteResumoViewModel>>(client, "api/jornada-clientes");
        return View(new JornadaClientesIndexViewModel { Jornadas = data ?? Array.Empty<JornadaClienteResumoViewModel>(), ErrorMessage = error });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (data, error, statusCode) = await ReadApiResponse<JornadaClienteDetalheViewModel>(client, "api/jornada-clientes/" + id);
        if (data is null)
        {
            Logger.LogWarning("Falha ao carregar detalhe da jornada. ClienteId:{ClienteId} Status:{Status} Erro:{Erro}", id, (int)statusCode, error);
            TempData["Error"] = error ?? "Não foi possível carregar a jornada do cliente.";
            return View(new JornadaClienteDetalheViewModel { Jornada = new JornadaClienteResumoViewModel { ClienteId = id } });
        }

        return View(data);
    }

    public async Task<IActionResult> Funil()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (data, error, _) = await ReadApiResponse<IEnumerable<FunilEtapaViewModel>>(client, "api/jornada-clientes/funil");
        return View(new JornadaClientesFunilViewModel { Etapas = data ?? Array.Empty<FunilEtapaViewModel>(), ErrorMessage = error });
    }

    public async Task<IActionResult> Tarefas(Guid? clienteId)
    {
        if (!clienteId.HasValue)
        {
            TempData["Error"] = "Selecione um cliente para consultar tarefas da jornada.";
            return RedirectToAction(nameof(Index));
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (data, error, _) = await ReadApiResponse<IEnumerable<JornadaClienteTarefaViewModel>>(client, "api/jornada-clientes/" + clienteId.Value + "/tarefas");
        if (!string.IsNullOrWhiteSpace(error)) TempData["Error"] = error;
        ViewBag.ClienteId = clienteId.Value;
        return View(data ?? Array.Empty<JornadaClienteTarefaViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avancar(JornadaClienteFormViewModel model)
    {
        return await MudarEtapaAsync(model, true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Retroceder(JornadaClienteFormViewModel model)
    {
        return await MudarEtapaAsync(model, false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarEvento(CriarJornadaEventoFormViewModel model)
    {
        if (model.ClienteId == Guid.Empty || string.IsNullOrWhiteSpace(model.Resumo))
        {
            TempData["Error"] = "Informe resumo para registrar evento da jornada.";
            return RedirectToAction(nameof(Details), new { id = model.ClienteId });
        }

        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/jornada-clientes/" + model.ClienteId + "/eventos", new { model.Tipo, model.Resumo });
            TempData[ok ? "Success" : "Error"] = ok ? "Evento registrado com sucesso." : error ?? "Não foi possível registrar evento.";
            Logger.LogInformation("Registro de evento da jornada ClienteId:{ClienteId} Status:{Status} Sucesso:{Sucesso}", model.ClienteId, (int)statusCode, ok);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao registrar evento da jornada {ClienteId}", model.ClienteId);
            TempData["Error"] = "Não foi possível registrar evento.";
        }

        return RedirectToAction(nameof(Details), new { id = model.ClienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarTarefa(CriarJornadaTarefaFormViewModel model)
    {
        if (model.ClienteId == Guid.Empty || string.IsNullOrWhiteSpace(model.Titulo))
        {
            TempData["Error"] = "Informe título para criar tarefa da jornada.";
            return RedirectToAction(nameof(Details), new { id = model.ClienteId });
        }

        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/jornada-clientes/" + model.ClienteId + "/tarefas", new { model.Titulo, model.Responsavel, model.Vencimento });
            TempData[ok ? "Success" : "Error"] = ok ? "Tarefa criada com sucesso." : error ?? "Não foi possível criar tarefa.";
            Logger.LogInformation("Criação de tarefa da jornada ClienteId:{ClienteId} Status:{Status} Sucesso:{Sucesso}", model.ClienteId, (int)statusCode, ok);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar tarefa da jornada {ClienteId}", model.ClienteId);
            TempData["Error"] = "Não foi possível criar tarefa.";
        }

        return RedirectToAction(nameof(Details), new { id = model.ClienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcluirTarefa(Guid id, Guid clienteId)
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/jornada-clientes/tarefas/" + id + "/concluir", new { });
            TempData[ok ? "Success" : "Error"] = ok ? "Tarefa concluída." : error ?? "Não foi possível concluir tarefa.";
            Logger.LogInformation("Conclusão de tarefa da jornada TarefaId:{TarefaId} Status:{Status} Sucesso:{Sucesso}", id, (int)statusCode, ok);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao concluir tarefa da jornada {TarefaId}", id);
            TempData["Error"] = "Não foi possível concluir tarefa.";
        }

        return RedirectToAction(nameof(Details), new { id = clienteId });
    }

    private async Task<IActionResult> MudarEtapaAsync(JornadaClienteFormViewModel model, bool avancar)
    {
        if (model.ClienteId == Guid.Empty || string.IsNullOrWhiteSpace(model.Motivo))
        {
            TempData["Error"] = avancar ? "Avançar etapa exige motivo/resumo." : "Retroceder etapa exige justificativa.";
            return RedirectToAction(nameof(Details), new { id = model.ClienteId });
        }

        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var acao = avancar ? "avancar" : "retroceder";
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/jornada-clientes/" + model.ClienteId + "/" + acao, new { model.Motivo });
            TempData[ok ? "Success" : "Error"] = ok ? (avancar ? "Etapa avançada." : "Etapa retrocedida.") : error ?? "Não foi possível atualizar etapa.";
            Logger.LogInformation("Mudança de etapa da jornada ClienteId:{ClienteId} Acao:{Acao} Status:{Status} Sucesso:{Sucesso}", model.ClienteId, acao, (int)statusCode, ok);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar jornada do cliente {ClienteId}", model.ClienteId);
            TempData["Error"] = "Não foi possível atualizar etapa.";
        }

        return RedirectToAction(nameof(Details), new { id = model.ClienteId });
    }
}
