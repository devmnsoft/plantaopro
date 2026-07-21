using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Net;

namespace PlantaoPro.Web.Controllers;

[Authorize(Policy = "CentralAtendimento.Ver")]
public sealed class CentralAtendimentoController : BaseWebController
{
    public CentralAtendimentoController(IHttpClientFactory factory, ILogger<CentralAtendimentoController> logger) : base(factory, logger) { }

    public async Task<IActionResult> Index(DateOnly? data, string? status, string? prioridade, string? paciente)
    {
        var vm = new CentralAtendimentoViewModel { Data = data ?? DateOnly.FromDateTime(DateTime.Today), Status = status ?? string.Empty, Prioridade = prioridade ?? string.Empty, Paciente = paciente ?? string.Empty };
        HttpContext.Session.SetString("CentralAtendimento.Filtros", $"{vm.Data:yyyy-MM-dd}|{vm.Status}|{vm.Prioridade}|{vm.Paciente}");
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = $"api/central-atendimento?data={vm.Data:yyyy-MM-dd}&status={WebUtility.UrlEncode(status)}&prioridade={WebUtility.UrlEncode(prioridade)}&paciente={WebUtility.UrlEncode(paciente)}";
        var result = await ReadApiResponseAsync<CentralAtendimentoViewModel>(client, endpoint);
        if (result.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (result.Data != null) vm = result.Data;
        vm.ErrorMessage = result.Error ?? string.Empty;
        return View(vm);
    }
}
