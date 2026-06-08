using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public sealed class PilotoController : BaseWebController
{
    private readonly ILogger<PilotoController> _logger;

    public PilotoController(IHttpClientFactory httpClientFactory, ILogger<PilotoController> logger) : base(httpClientFactory, logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);

            var (data, error, statusCode) = await ReadApiResponseAsync<PilotoResumoViewModel>(client, "api/piloto/resumo");
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return HandleUnauthorized();
            }

            var model = data ?? new PilotoResumoViewModel();
            model.ErrorMessage = error ?? string.Empty;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar painel do piloto.");
            TempData["Error"] = "Não foi possível carregar o painel do piloto.";
            return View(new PilotoResumoViewModel());
        }
    }

    public async Task<IActionResult> Checklist()
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);

            var (itens, error, statusCode) = await ReadApiListResponseAsync<PilotoChecklistItemViewModel>(client, "api/piloto/checklist");
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return HandleUnauthorized();
            }

            var lista = itens.ToList();
            var concluidos = lista.Count(x => x.Concluido);
            var total = lista.Count;

            return View(new PilotoChecklistViewModel
            {
                Itens = lista,
                Total = total,
                Concluidos = concluidos,
                Percentual = total == 0 ? 0 : (int)Math.Round((double)concluidos * 100 / total),
                ErrorMessage = error ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar checklist do piloto.");
            TempData["Error"] = "Não foi possível carregar o checklist do piloto.";
            return View(new PilotoChecklistViewModel());
        }
    }

    public async Task<IActionResult> Ocorrencias()
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);

            var (ocorrencias, error, statusCode) = await ReadApiListResponseAsync<PilotoOcorrenciaViewModel>(client, "api/piloto/ocorrencias");
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return HandleUnauthorized();
            }

            return View(new PilotoOcorrenciasViewModel
            {
                Ocorrencias = ocorrencias.ToList(),
                NovaOcorrencia = new NovaPilotoOcorrenciaViewModel(),
                ErrorMessage = error ?? string.Empty
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar ocorrências do piloto.");
            TempData["Error"] = "Não foi possível carregar as ocorrências do piloto.";
            return View(new PilotoOcorrenciasViewModel());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConcluirChecklist(int id)
    {
        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);

            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/piloto/checklist/{id}/concluir", new { });
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return HandleUnauthorized();
            }

            TempData[ok ? "Success" : "Error"] = ok ? "Item concluído com sucesso." : (string.IsNullOrWhiteSpace(error) ? "Não foi possível concluir o item." : error);
            return RedirectToAction(nameof(Checklist));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao concluir item do checklist do piloto. Id={Id}", id);
            TempData["Error"] = "Não foi possível concluir o item do checklist.";
            return RedirectToAction(nameof(Checklist));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarOcorrencia(NovaPilotoOcorrenciaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Verifique os campos obrigatórios.";
            return RedirectToAction(nameof(Ocorrencias));
        }

        try
        {
            using var client = CreateApiClient();
            AddBearerToken(client);

            var (ok, error, statusCode) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/piloto/ocorrencias", model);
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return HandleUnauthorized();
            }

            TempData[ok ? "Success" : "Error"] = ok ? "Ocorrência registrada com sucesso." : (error ?? "Não foi possível registrar ocorrência.");
            return RedirectToAction(nameof(Ocorrencias));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar ocorrência do piloto.");
            TempData["Error"] = "Não foi possível registrar ocorrência.";
            return RedirectToAction(nameof(Ocorrencias));
        }
    }

    public IActionResult Programas() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Programas piloto", "Período, objetivo, critérios de sucesso e responsável do piloto comercial.", "Piloto", "Clientes"));
    public IActionResult ProgramaDetails(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes do programa piloto", "Indicadores, critérios e clientes vinculados ao programa.", "Piloto", "Programas"));
    public IActionResult Clientes() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Clientes piloto", "Trial especial, desconto, andamento, conclusão e conversão para cliente pago.", "Piloto", "Feedbacks"));
    public IActionResult ClienteDetails(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes do cliente piloto", "Adoção, feedbacks, bugs, reuniões e decisão final de conversão.", "Piloto", "Clientes"));
    public IActionResult Feedbacks() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Feedbacks do beta", "Severidade, categoria, tarefa de produto e resolução auditada.", "Piloto", "Indicadores"));
    public IActionResult Bugs() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Bugs do piloto", "Bugs críticos geram alerta operacional e acompanhamento de resolução.", "Piloto", "Feedbacks"));
    public IActionResult Indicadores() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Indicadores do piloto", "Conversão, feedback crítico, NPS e critérios de sucesso.", "Piloto", "Programas"));
}
