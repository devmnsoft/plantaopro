using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,COORDENACAO")]
public sealed class OperacaoAssistidaController : BaseWebController
{
    private readonly ILogger<OperacaoAssistidaController> _logger;

    public OperacaoAssistidaController(IHttpClientFactory httpClientFactory, ILogger<OperacaoAssistidaController> logger) : base(httpClientFactory, logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);
            var endpoint = string.IsNullOrWhiteSpace(status) ? "api/operacao-assistida/clientes" : $"api/operacao-assistida/clientes?status={Uri.EscapeDataString(status)}";
            var (paged, error, statusCode) = await ReadApiPagedResponseAsync<OperacaoAssistidaClienteViewModel>(client, endpoint, 1, 24);
            if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            return View(new OperacaoAssistidaIndexViewModel { Clientes = paged.Items, Status = status, ErrorMessage = error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar operação assistida.");
            TempData["Error"] = "Não foi possível carregar a operação assistida.";
            return View(new OperacaoAssistidaIndexViewModel());
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var detalhe = await CarregarDetalheAsync(id);
        if (detalhe is null) return RedirectToAction(nameof(Index));
        return View(detalhe);
    }

    public async Task<IActionResult> Checklist(Guid clienteId)
    {
        var detalhe = await CarregarDetalheAsync(clienteId);
        if (detalhe is null) return RedirectToAction(nameof(Index));
        var itens = detalhe.Checklist.ToList();
        return View(new OperacaoAssistidaChecklistPageViewModel
        {
            ClienteId = clienteId,
            ClienteNome = detalhe.Cliente.ClienteNome,
            Itens = itens,
            Percentual = itens.Count == 0 ? 0 : (int)Math.Round(itens.Count(x => x.Concluido) * 100d / itens.Count),
            ErrorMessage = detalhe.ErrorMessage
        });
    }

    public async Task<IActionResult> Ocorrencias(Guid clienteId, string? status = null, string? prioridade = null)
    {
        var detalhe = await CarregarDetalheAsync(clienteId);
        if (detalhe is null) return RedirectToAction(nameof(Index));
        var ocorrencias = detalhe.Ocorrencias.Where(x => (string.IsNullOrWhiteSpace(status) || x.Status == status) && (string.IsNullOrWhiteSpace(prioridade) || x.Prioridade == prioridade)).ToList();
        return View(new OperacaoAssistidaOcorrenciasPageViewModel
        {
            ClienteId = clienteId,
            ClienteNome = detalhe.Cliente.ClienteNome,
            Status = status,
            Prioridade = prioridade,
            Ocorrencias = ocorrencias,
            NovaOcorrencia = new NovaOperacaoAssistidaOcorrenciaViewModel { ClienteId = clienteId },
            ErrorMessage = detalhe.ErrorMessage
        });
    }

    public async Task<IActionResult> Treinamentos(Guid clienteId)
    {
        var detalhe = await CarregarDetalheAsync(clienteId);
        if (detalhe is null) return RedirectToAction(nameof(Index));
        return View(new OperacaoAssistidaTreinamentosPageViewModel
        {
            ClienteId = clienteId,
            ClienteNome = detalhe.Cliente.ClienteNome,
            Treinamentos = detalhe.Treinamentos,
            NovoTreinamento = new NovoOperacaoAssistidaTreinamentoViewModel { ClienteId = clienteId, RealizadoEm = DateTime.Today },
            ErrorMessage = detalhe.ErrorMessage
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcluirChecklist(Guid id, Guid clienteId, string? observacao)
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/operacao-assistida/checklist/{id}/concluir", new { responsavel = User.Identity?.Name, observacao });
            if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            TempData[ok ? "Success" : "Error"] = ok ? "Item concluído com sucesso." : error ?? "Não foi possível concluir o item.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao concluir checklist da operação assistida. Id={Id}", id);
            TempData["Error"] = "Não foi possível concluir o item.";
        }
        return RedirectToAction(nameof(Checklist), new { clienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReabrirChecklist(Guid id, Guid clienteId, string justificativa)
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/operacao-assistida/checklist/{id}/reabrir", new { justificativa });
            if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            TempData[ok ? "Success" : "Error"] = ok ? "Item reaberto com sucesso." : error ?? "Não foi possível reabrir o item.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reabrir checklist da operação assistida. Id={Id}", id);
            TempData["Error"] = "Não foi possível reabrir o item.";
        }
        return RedirectToAction(nameof(Checklist), new { clienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarOcorrencia(NovaOperacaoAssistidaOcorrenciaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Informe os dados obrigatórios da ocorrência.";
            return RedirectToAction(nameof(Ocorrencias), new { clienteId = model.ClienteId });
        }

        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/operacao-assistida/clientes/{model.ClienteId}/ocorrencias", model);
            if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            TempData[ok ? "Success" : "Error"] = ok ? "Ocorrência registrada com sucesso." : error ?? "Não foi possível registrar ocorrência.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar ocorrência da operação assistida. ClienteId={ClienteId}", model.ClienteId);
            TempData["Error"] = "Não foi possível registrar ocorrência.";
        }
        return RedirectToAction(nameof(Ocorrencias), new { clienteId = model.ClienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolverOcorrencia(Guid id, Guid clienteId, string solucao)
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/operacao-assistida/ocorrencias/{id}/resolver", new { solucao, responsavel = User.Identity?.Name });
            if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            TempData[ok ? "Success" : "Error"] = ok ? "Ocorrência resolvida com sucesso." : error ?? "Não foi possível resolver a ocorrência.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao resolver ocorrência da operação assistida. Id={Id}", id);
            TempData["Error"] = "Não foi possível resolver a ocorrência.";
        }
        return RedirectToAction(nameof(Ocorrencias), new { clienteId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarTreinamento(NovoOperacaoAssistidaTreinamentoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Informe o tema do treinamento.";
            return RedirectToAction(nameof(Treinamentos), new { clienteId = model.ClienteId });
        }

        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);
            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/operacao-assistida/clientes/{model.ClienteId}/treinamentos", model);
            if (statusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
            TempData[ok ? "Success" : "Error"] = ok ? "Treinamento registrado com sucesso." : error ?? "Não foi possível registrar treinamento.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar treinamento da operação assistida. ClienteId={ClienteId}", model.ClienteId);
            TempData["Error"] = "Não foi possível registrar treinamento.";
        }
        return RedirectToAction(nameof(Treinamentos), new { clienteId = model.ClienteId });
    }

    private async Task<OperacaoAssistidaDetalheViewModel?> CarregarDetalheAsync(Guid clienteId)
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);
            var (data, error, statusCode) = await ReadApiResponseAsync<OperacaoAssistidaDetalheViewModel>(client, $"api/operacao-assistida/clientes/{clienteId}");
            if (statusCode == HttpStatusCode.Unauthorized) return null;
            if (data is null) TempData["Error"] = error ?? "Operação assistida não encontrada.";
            if (data is not null) data.ErrorMessage = error;
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar detalhe da operação assistida. ClienteId={ClienteId}", clienteId);
            TempData["Error"] = "Não foi possível carregar o detalhe da operação assistida.";
            return null;
        }
    }
}
