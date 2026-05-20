using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class MinhaAgendaController : BaseWebController
{
    public MinhaAgendaController(
        IHttpClientFactory httpClientFactory,
        ILogger<MinhaAgendaController> logger
    ) : base(httpClientFactory, logger)
    {
    }

    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();

        if (!AddBearerToken(client))
            return HandleUnauthorized();

        var resumo = await ReadApiResponse<MedicoAreaResumoDto>(
            client,
            "api/medico-area/resumo"
        );

        if (resumo.StatusCode == HttpStatusCode.Unauthorized)
            return HandleUnauthorized();

        if (resumo.StatusCode == HttpStatusCode.NotFound)
        {
            TempData["Error"] = "Seu usuário ainda não está vinculado a um cadastro médico. Entre em contato com a coordenação.";
        }
        else if ((int)resumo.StatusCode >= 400)
        {
            TempData["Error"] = resumo.Error ?? "Não foi possível carregar a área do médico.";
        }

        return View(resumo.Data);
    }

    public async Task<IActionResult> PlantoesDisponiveis(int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var client = CreateApiClient();

        if (!AddBearerToken(client))
            return HandleUnauthorized();

        var r = await ReadApiResponse<PagedResult<MedicoPlantaoDisponivelDto>>(
            client,
            $"api/medico-area/plantoes-disponiveis?page={page}&pageSize={pageSize}"
        );

        if (r.StatusCode == HttpStatusCode.Unauthorized)
            return HandleUnauthorized();

        var errorMessage = r.StatusCode == HttpStatusCode.OK
            ? null
            : r.Error ?? "Não foi possível carregar os plantões disponíveis.";

        if (r.StatusCode == HttpStatusCode.NotFound)
        {
            TempData["Error"] = r.Error ?? "Médico não encontrado para o usuário autenticado.";
        }

        var model = new ListPageViewModel<MedicoPlantaoDisponivelDto>(
            Items: r.Data?.Items ?? Array.Empty<MedicoPlantaoDisponivelDto>(),
            ErrorMessage: errorMessage,
            InfoMessage: null,
            Total: r.Data?.Total ?? 0,
            Page: r.Data?.Page ?? page,
            PageSize: r.Data?.PageSize ?? pageSize
        );

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarPlantao(Guid plantaoId)
    {
        var client = CreateApiClient();

        if (!AddBearerToken(client))
            return HandleUnauthorized();

        var response = await client.PostAsJsonAsync(
            $"api/medico-area/plantoes/{plantaoId}/solicitar",
            new
            {
            }
        );

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return HandleUnauthorized();

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Solicitação enviada com sucesso.";
        }
        else
        {
            var content = await response.Content.ReadAsStringAsync();
            Logger.LogWarning(
                "Falha ao solicitar plantão. Status:{Status} Response:{Response}",
                (int)response.StatusCode,
                content
            );

            TempData["Error"] = "Não foi possível solicitar o plantão.";
        }

        return RedirectToAction(nameof(PlantoesDisponiveis));
    }
}