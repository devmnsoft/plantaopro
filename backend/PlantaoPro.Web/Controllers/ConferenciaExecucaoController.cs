using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.EscalasGestao + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)]
public sealed class ConferenciaExecucaoController : BaseWebController
{
    public ConferenciaExecucaoController(IHttpClientFactory factory, ILogger<ConferenciaExecucaoController> logger)
        : base(factory, logger) { }

    public async Task<IActionResult> Index(DateOnly? inicio, DateOnly? fim, Guid? unidadeId,
        Guid? profissionalId, string? status, int page = 1, int pageSize = 25)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}&unidadeId={unidadeId}&profissionalId={profissionalId}&status={Uri.EscapeDataString(status ?? string.Empty)}&page={Math.Max(1, page)}&pageSize={Math.Clamp(pageSize, 1, 100)}";
        var result = await ReadApiResponse<ExecutionConferencePageDto>(client, "api/conferencia-execucao" + query);
        ViewBag.Inicio = inicio; ViewBag.Fim = fim; ViewBag.UnidadeId = unidadeId;
        ViewBag.ProfissionalId = profissionalId; ViewBag.Status = status;
        return View(new DetailsPageViewModel<ExecutionConferencePageDto>(result.Data, result.Error, result.Data is null));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decidir(Guid presencaId, Guid? correcaoId, long versao,
        long versaoPresenca, bool aprovar, string justificativa)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = correcaoId.HasValue
            ? await client.PostAsJsonAsync($"api/conferencia-execucao/correcoes/{correcaoId}/decidir", new { Aprovar = aprovar, Justificativa = justificativa, Versao = versao })
            : await client.PostAsJsonAsync($"api/conferencia-execucao/presencas/{presencaId}/aprovar", new { Justificativa = justificativa, Versao = versaoPresenca });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Decisão operacional registrada."
            : "O estado mudou ou a decisão não é permitida. Consulte os dados atuais.";
        return RedirectToAction(nameof(Index));
    }
}
