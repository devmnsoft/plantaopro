using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.EscalasGestao)]
public sealed class ConferenciaExecucaoController : BaseWebController
{
    public ConferenciaExecucaoController(IHttpClientFactory factory, ILogger<ConferenciaExecucaoController> logger)
        : base(factory, logger) { }

    public async Task<IActionResult> Index(DateOnly? inicio, DateOnly? fim, Guid? unidadeId,
        Guid? profissionalId, string? status, bool? divergencia, int page = 1, int pageSize = 25)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        if (inicio.HasValue != fim.HasValue || inicio > fim)
            ModelState.AddModelError(string.Empty, "Informe o início e o fim de um período válido.");
        var allowedStatuses = new[] { "REGISTRO_INCOMPLETO", "PENDENTE", "CORRECAO_PENDENTE", "APROVADA", "AJUSTE_POS_APURACAO", "RECUSADA" };
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        if (status is not null && !allowedStatuses.Contains(status))
            ModelState.AddModelError(nameof(status), "Situação inválida.");
        var endpoint = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString("api/conferencia-execucao",
            new Dictionary<string, string?> {
                ["inicio"] = inicio?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["fim"] = fim?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["unidadeId"] = unidadeId?.ToString(), ["profissionalId"] = profissionalId?.ToString(),
                ["status"] = status, ["divergencia"] = divergencia?.ToString(), ["page"] = Math.Max(1, page).ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["pageSize"] = Math.Clamp(pageSize, 1, 100).ToString(System.Globalization.CultureInfo.InvariantCulture)
            }.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key, pair => pair.Value!));
        (ExecutionConferencePageDto? Data, string? Error, System.Net.HttpStatusCode StatusCode) result = ModelState.IsValid
            ? await ReadApiResponse<ExecutionConferencePageDto>(client, endpoint)
            : (null, "Revise os filtros informados.", System.Net.HttpStatusCode.UnprocessableEntity);
        ViewBag.Inicio = inicio; ViewBag.Fim = fim; ViewBag.UnidadeId = unidadeId;
        ViewBag.ProfissionalId = profissionalId; ViewBag.Status = status; ViewBag.Divergencia = divergencia;
        return View(new DetailsPageViewModel<ExecutionConferencePageDto>(result.Data, result.Error, result.Data is null));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decidir(Guid presencaId, Guid? correcaoId, long versao,
        long versaoPresenca, ExecutionConferenceDecision? decisao, string justificativa,
        DateOnly? inicio, DateOnly? fim, Guid? unidadeId, Guid? profissionalId, string? status, bool? divergencia, int page = 1)
    {
        foreach (var error in ExecutionConferenceDecisionValidator.Validate(presencaId, correcaoId, versao, versaoPresenca, decisao, justificativa))
            ModelState.AddModelError(error.Field, error.Message);
        var route = new { inicio, fim, unidadeId, profissionalId, status, divergencia, page };
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage).Where(message => message.Length > 0));
            TempData["DecisionReason"] = justificativa;
            return RedirectToAction(nameof(Index), route);
        }
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = correcaoId.HasValue
            ? await client.PostAsJsonAsync($"api/conferencia-execucao/correcoes/{correcaoId}/decidir", new { PresencaId = presencaId, Aprovar = decisao == ExecutionConferenceDecision.AprovarCorrecao, Justificativa = justificativa.Trim(), Versao = versao })
            : await client.PostAsJsonAsync($"api/conferencia-execucao/presencas/{presencaId}/aprovar", new { Justificativa = justificativa, Versao = versaoPresenca });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Decisão operacional registrada; o registro pode ter mudado de lista."
            : response.StatusCode switch { System.Net.HttpStatusCode.Forbidden => "Você não possui vínculo ou permissão para esta decisão.", System.Net.HttpStatusCode.NotFound => "Registro indisponível no contexto atual.", System.Net.HttpStatusCode.Conflict => "O registro mudou. A justificativa foi preservada; revise o estado atualizado.", System.Net.HttpStatusCode.UnprocessableEntity => "A decisão contém dados inválidos.", _ => "Não foi possível confirmar o resultado. Consulte o estado antes de repetir." };
        if (!response.IsSuccessStatusCode) TempData["DecisionReason"] = justificativa;
        return RedirectToAction(nameof(Index), route);
    }
}

public enum ExecutionConferenceDecision { AprovarExecucao, AprovarCorrecao, RecusarCorrecao }

public static class ExecutionConferenceDecisionValidator
{
    public static IReadOnlyList<(string Field, string Message)> Validate(Guid presencaId, Guid? correcaoId,
        long versao, long versaoPresenca, ExecutionConferenceDecision? decisao, string? justificativa)
    {
        var errors = new List<(string, string)>();
        if (presencaId == Guid.Empty) errors.Add(("presencaId", "Presença inválida."));
        if (decisao is null) errors.Add(("decisao", "Selecione uma decisão explícita."));
        if (versaoPresenca <= 0 || (correcaoId.HasValue && versao <= 0)) errors.Add(("versao", "Versão inválida."));
        if (!correcaoId.HasValue && decisao != ExecutionConferenceDecision.AprovarExecucao)
            errors.Add(("decisao", "A execução normal admite somente aprovação explícita."));
        if (correcaoId.HasValue && decisao == ExecutionConferenceDecision.AprovarExecucao)
            errors.Add(("decisao", "Use uma decisão de correção para esta solicitação."));
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Trim().Length is < 3 or > 1000)
            errors.Add(("justificativa", "A justificativa deve conter entre 3 e 1000 caracteres."));
        return errors;
    }
}
