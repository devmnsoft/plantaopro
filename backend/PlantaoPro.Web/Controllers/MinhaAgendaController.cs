using Microsoft.AspNetCore.Authorization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Medico)]
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

        var resumo = await ReadApiResponse<ProfessionalDashboardDto>(
            client,
            "api/medico-area/meu-dia"
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

        var model = new DetailsPageViewModel<ProfessionalDashboardDto>(
            Data: resumo.Data,
            ErrorMessage: TempData["Error"] as string,
            IsPlaceholder: resumo.Data is null
        );

        return View(model);
    }

    public async Task<IActionResult> MeusPagamentos(int page = 1, int pageSize = 20)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponse<PagedResult<MedicoPagamentoDto>>(client, $"api/medico-area/meus-pagamentos?page={Math.Max(1,page)}&pageSize={Math.Clamp(pageSize,1,100)}");
        return View(new ListPageViewModel<MedicoPagamentoDto>(result.Data?.Items ?? Array.Empty<MedicoPagamentoDto>(), result.Error, null, result.Data?.Total ?? 0, result.Data?.Page ?? page, result.Data?.PageSize ?? pageSize));
    }

    public async Task<IActionResult> Presencas()
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponse<IEnumerable<ProfessionalCheckInDto>>(client, "api/medico-area/presencas");
        return View(new ListPageViewModel<ProfessionalCheckInDto>(result.Data ?? Array.Empty<ProfessionalCheckInDto>(), result.Error, null, result.Data?.Count() ?? 0, 1, 50));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPresenca(Guid escalaId, string operacao)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var action = string.Equals(operacao, "checkout", StringComparison.OrdinalIgnoreCase) ? "check-out" : "check-in";
        var response = await client.PostAsJsonAsync($"api/medico-area/escalas/{escalaId}/{action}", new { });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Presença registrada com segurança." : "Não foi possível registrar a presença. Verifique se a ação já foi realizada.";
        return RedirectToAction(nameof(Presencas));
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
