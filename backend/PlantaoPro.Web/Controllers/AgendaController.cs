using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL," + RolesConstants.Administrador + "," + RolesConstants.Coordenacao + "," + RolesConstants.Operador + "," + RolesConstants.Hospital + "," + RolesConstants.Medico)]
public class AgendaController : BaseWebController
{
    public AgendaController(IHttpClientFactory factory, ILogger<AgendaController> logger) : base(factory, logger) { }

    public async Task<IActionResult> Index(DateTime? inicio, DateTime? fim, string? status, int page = 1, int pageSize = 100)
    {
        var dataInicio = inicio ?? DateTime.Today;
        var dataFim = fim ?? DateTime.Today.AddDays(30);

        var endpoint = $"api/plantoes?dataInicio={dataInicio:O}&dataFim={dataFim:O}&status={status}&page={page}&pageSize={pageSize}";
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (data, error, statusCode) = await ReadApiPagedResponseAsync<PlantaoResumoDto>(client, endpoint, page, pageSize);
        if (statusCode == System.Net.HttpStatusCode.Unauthorized) return HandleUnauthorized();

        var vm = new AgendaOperacionalViewModel
        {
            Inicio = dataInicio,
            Fim = dataFim,
            Status = status,
            Itens = data.Items,
            Total = data.TotalItems,
            ErrorMessage = error
        };

        return View(vm);
    }
}
