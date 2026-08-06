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

    [HttpGet("/agenda")]
    public Task<IActionResult> Index(DateTime? inicio, DateTime? fim, string? status, int page = 1, int pageSize = 100)
        => Carregar("semana", inicio, fim, status, page, pageSize);

    [HttpGet("/agenda/dia")]
    public Task<IActionResult> Dia(DateTime? inicio, string? status, int page = 1, int pageSize = 100)
        => Carregar("dia", inicio, inicio?.Date.AddDays(1).AddTicks(-1), status, page, pageSize);

    [HttpGet("/agenda/semana")]
    public Task<IActionResult> Semana(DateTime? inicio, string? status, int page = 1, int pageSize = 100)
        => Carregar("semana", inicio, inicio?.Date.AddDays(7).AddTicks(-1), status, page, pageSize);

    [HttpGet("/agenda/mes")]
    public Task<IActionResult> Mes(DateTime? inicio, string? status, int page = 1, int pageSize = 100)
        => Carregar("mes", inicio, inicio?.Date.AddMonths(1).AddTicks(-1), status, page, pageSize);

    [HttpGet("/agenda/conflitos")]
    public Task<IActionResult> Conflitos(DateTime? inicio, DateTime? fim, int page = 1, int pageSize = 100)
        => Carregar("conflitos", inicio, fim, "CONFLITO", page, pageSize);

    [HttpGet("/agenda/calendario")]
    public Task<IActionResult> Calendario(DateTime? inicio, DateTime? fim, string? status, int page = 1, int pageSize = 100)
        => Carregar("calendario", inicio, fim, status, page, pageSize);

    [HttpGet("/agenda/medicos")]
    public Task<IActionResult> Medicos(DateTime? inicio, DateTime? fim, string? status, int page = 1, int pageSize = 100)
        => Carregar("medicos", inicio, fim, status, page, pageSize);

    [HttpGet("/agenda/hospitais")]
    public Task<IActionResult> Hospitais(DateTime? inicio, DateTime? fim, string? status, int page = 1, int pageSize = 100)
        => Carregar("hospitais", inicio, fim, status, page, pageSize);

    private async Task<IActionResult> Carregar(string modo, DateTime? inicio, DateTime? fim, string? status, int page, int pageSize)
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

        ViewData["AgendaModo"] = modo;

        return View(vm);
    }
}
