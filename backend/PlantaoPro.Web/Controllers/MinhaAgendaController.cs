using System.Net;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;
public class MinhaAgendaController : BaseWebController
{
    public MinhaAgendaController(IHttpClientFactory f, ILogger<MinhaAgendaController> l) : base(f, l) { }

    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var resumo = await ReadApiResponse<MedicoAreaResumoDto>(client, "api/medico-area/resumo");
        if (resumo.StatusCode == HttpStatusCode.NotFound) TempData["Error"] = "Seu usuário ainda não está vinculado a um cadastro médico. Entre em contato com a coordenação.";
        return View(resumo.Data);
    }

    public async Task<IActionResult> PlantoesDisponiveis(int page=1,int pageSize=20)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var r = await ReadApiResponse<PagedResult<MedicoPlantaoDisponivelDto>>(client, $"api/medico-area/plantoes-disponiveis?page={page}&pageSize={pageSize}");
        if (r.StatusCode==HttpStatusCode.NotFound) TempData["Error"] = r.Error;
        return View(new ListPageViewModel<MedicoPlantaoDisponivelDto>(r.Data?.Items ?? [], r.Data?.Total ?? 0, r.Data?.Page ?? page, r.Data?.PageSize ?? pageSize));
    }
}
