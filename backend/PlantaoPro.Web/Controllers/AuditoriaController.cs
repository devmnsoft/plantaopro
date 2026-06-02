using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL," + RolesConstants.Administrador)]
public class AuditoriaController : BaseWebController
{
    public AuditoriaController(IHttpClientFactory f, ILogger<AuditoriaController> l) : base(f, l) { }

    public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, string? usuarioId, string? clienteId, string? perfil, string? entidade, string? acao, bool? sucesso, string? texto, int page = 1, int pageSize = 20)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var query = $"api/auditoria?page={page}&pageSize={pageSize}&dataInicio={Uri.EscapeDataString(dataInicio?.ToString("O") ?? string.Empty)}&dataFim={Uri.EscapeDataString(dataFim?.ToString("O") ?? string.Empty)}&usuarioId={usuarioId}&clienteId={clienteId}&perfil={Uri.EscapeDataString(perfil ?? string.Empty)}&entidade={Uri.EscapeDataString(entidade ?? string.Empty)}&acao={Uri.EscapeDataString(acao ?? string.Empty)}&sucesso={sucesso}&texto={Uri.EscapeDataString(texto ?? string.Empty)}";
        var (data, error, statusCode) = await ReadApiPagedResponseAsync<AuditoriaDto>(client, query, page, pageSize);
        if (statusCode == System.Net.HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (statusCode == System.Net.HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account");
        var resumo = await ReadApiResponse<AuditoriaResumoDto>(client, "api/auditoria/resumo");
        ViewBag.Resumo = resumo.Data;
        ViewBag.ErrorMessage = error;
        return View(new ListPageViewModel<AuditoriaDto>(data.Items, error, null, data.TotalItems, page, pageSize));
    }

    public async Task<IActionResult> ExportarCsv(DateTime? dataInicio, DateTime? dataFim, string? usuarioId, string? clienteId, string? perfil, string? entidade, string? acao, bool? sucesso, string? texto, int page = 1, int pageSize = 100)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var query = $"api/auditoria/exportar-csv?page={page}&pageSize={pageSize}&dataInicio={Uri.EscapeDataString(dataInicio?.ToString("O") ?? string.Empty)}&dataFim={Uri.EscapeDataString(dataFim?.ToString("O") ?? string.Empty)}&usuarioId={usuarioId}&clienteId={clienteId}&perfil={Uri.EscapeDataString(perfil ?? string.Empty)}&entidade={Uri.EscapeDataString(entidade ?? string.Empty)}&acao={Uri.EscapeDataString(acao ?? string.Empty)}&sucesso={sucesso}&texto={Uri.EscapeDataString(texto ?? string.Empty)}";
        var response = await client.GetAsync(query);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Não foi possível exportar auditoria no momento.";
            return RedirectToAction(nameof(Index));
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return File(bytes, "text/csv", "auditoria.csv");
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponse<AuditoriaDetalheDto>(client, $"api/auditoria/{id}");
        if (result.StatusCode == System.Net.HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account");
        if (result.Data is null) return View("NotFound");
        return View(result.Data);
    }
}
