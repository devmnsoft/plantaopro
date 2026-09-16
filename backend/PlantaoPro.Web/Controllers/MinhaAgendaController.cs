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

    public async Task<IActionResult> Agenda(DateOnly? inicio, DateOnly? fim, string? unidade, string? situacao, string visualizacao = "semana")
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = inicio ?? today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var end = fim ?? start.AddDays(6);
        if (end < start || end.DayNumber - start.DayNumber > 366)
            ModelState.AddModelError(string.Empty, "Informe um período válido de até 366 dias.");
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var url = $"api/medico-area/agenda?inicio={start:yyyy-MM-dd}&fim={end:yyyy-MM-dd}&unidade={Uri.EscapeDataString(unidade ?? string.Empty)}&situacao={Uri.EscapeDataString(situacao ?? string.Empty)}";
        (IEnumerable<ProfessionalShiftDto>? Data, string? Error, HttpStatusCode StatusCode) result = ModelState.IsValid
            ? await ReadApiResponse<IEnumerable<ProfessionalShiftDto>>(client, url)
            : (null, null, HttpStatusCode.UnprocessableEntity);
        return View(new ProfessionalAgendaPageViewModel(result.Data?.ToList() ?? new List<ProfessionalShiftDto>(), result.Error, start, end, unidade, situacao, visualizacao));
    }

    public async Task<IActionResult> Detalhe(Guid id, string? retorno)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponse<ProfessionalShiftDetailDto>(client, $"api/medico-area/escalas/{id}");
        if (result.StatusCode == HttpStatusCode.NotFound) return NotFound();
        ViewData["Retorno"] = string.IsNullOrWhiteSpace(retorno) ? Url.Action(nameof(Agenda)) : retorno;
        return View(new DetailsPageViewModel<ProfessionalShiftDetailDto>(result.Data, result.Error, result.Data is null));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(Guid id, string? retorno)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.PostAsJsonAsync($"api/medico-area/escalas/{id}/confirmar", new { });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Plantão confirmado"
            : response.StatusCode == HttpStatusCode.Conflict ? "Este plantão foi alterado; atualize as informações" : "Ação indisponível nesta situação";
        return RedirectToAction(nameof(Detalhe), new { id, retorno });
    }

    public async Task<IActionResult> MeusPagamentos(int page = 1, int pageSize = 20)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponse<PagedResult<MedicoPagamentoDto>>(client, $"api/medico-area/meus-pagamentos?page={Math.Max(1,page)}&pageSize={Math.Clamp(pageSize,1,100)}");
        return View(new ListPageViewModel<MedicoPagamentoDto>(result.Data?.Items ?? Array.Empty<MedicoPagamentoDto>(), result.Error, null, result.Data?.Total ?? 0, result.Data?.Page ?? page, result.Data?.PageSize ?? pageSize));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarAnalise(Guid pagamentoId,string justificativa)
    { var client=CreateApiClient();if(!AddBearerToken(client))return HandleUnauthorized();var result=await SendApiAsync<object,string>(client,HttpMethod.Post,"api/medico-area/meus-pagamentos/divergencias",new{PagamentoId=pagamentoId,Justificativa=justificativa});TempData[result.Data is null?"Error":"Success"]=result.Error??"Solicitação recebida para análise. O valor permanece inalterado.";return RedirectToAction(nameof(MeusPagamentos)); }

    public async Task<IActionResult> Presencas()
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponse<IEnumerable<ProfessionalCheckInDto>>(client, "api/medico-area/presencas");
        return View(new ListPageViewModel<ProfessionalCheckInDto>(result.Data ?? Array.Empty<ProfessionalCheckInDto>(), result.Error, null, result.Data?.Count() ?? 0, 1, 50));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPresenca(Guid escalaId, string operacao, string timezone)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var action = string.Equals(operacao, "checkout", StringComparison.OrdinalIgnoreCase) ? "check-out" : "check-in";
        var response = await client.PostAsJsonAsync($"api/medico-area/escalas/{escalaId}/{action}", new { Timezone=timezone });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Presença registrada com segurança." : "Não foi possível registrar a presença. Verifique se a ação já foi realizada.";
        return RedirectToAction(nameof(Presencas));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SolicitarCorrecao(Guid escalaId,DateTimeOffset? inicioPropostoEm,DateTimeOffset? fimPropostoEm,string justificativa,long versao)
    {
        var client=CreateApiClient();if(!AddBearerToken(client))return HandleUnauthorized();
        var response=await client.PostAsJsonAsync($"api/medico-area/escalas/{escalaId}/correcoes",new{InicioPropostoEm=inicioPropostoEm,FimPropostoEm=fimPropostoEm,Justificativa=justificativa,Versao=versao});
        TempData[response.IsSuccessStatusCode?"Success":"Error"]=response.IsSuccessStatusCode?"Correção enviada para conferência.":"Não foi possível enviar. Atualize a página e confira os horários.";
        return RedirectToAction(nameof(Presencas));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarCorrecao(Guid escalaId, Guid correcaoId, long versaoCorrecao, long versaoPresenca)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.PostAsJsonAsync($"api/medico-area/escalas/{escalaId}/correcoes/{correcaoId}/cancelar",
            new { VersaoCorrecao = versaoCorrecao, VersaoPresenca = versaoPresenca });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Solicitação cancelada. Os horários registrados permanecem inalterados."
            : response.StatusCode == HttpStatusCode.Conflict
                ? "Este registro foi atualizado. Revise os dados antes de decidir."
                : "Não foi possível cancelar a solicitação. Consulte o estado atual antes de repetir.";
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
