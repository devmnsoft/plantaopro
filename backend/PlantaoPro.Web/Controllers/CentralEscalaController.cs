using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class CentralEscalaController : BaseWebController
{
    private readonly IFase2OperationalFlowService flowService;

    public CentralEscalaController(IHttpClientFactory httpClientFactory, ILogger<CentralEscalaController> logger, IFase2OperationalFlowService flowService) : base(httpClientFactory, logger)
    {
        this.flowService = flowService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client))
            {
                return HandleUnauthorized();
            }

            var (data, error, statusCode) = await ReadApiResponse<OperacaoResumoDto>(client, "api/central-escala/resumo");
            LogRequestContext("WEB_CENTRAL_ESCALA_INDEX", "api/central-escala/resumo", (int)statusCode);

            if (data is null)
            {
                ViewBag.ErrorMessage = error ?? "Não foi possível carregar a Central de Escala.";
                return View(OperacaoResumoDto.Empty());
            }

            return View(data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Falha ao carregar Central de Escala.");
            ViewBag.ErrorMessage = "Falha inesperada ao carregar a Central de Escala.";
            return View(OperacaoResumoDto.Empty());
        }
    }

    public IActionResult PlantaoDescoberto() => Operational(nameof(PlantaoDescoberto));
    public IActionResult Risco() => Operational(nameof(Risco));
    public IActionResult MedicosDisponiveis() => Operational(nameof(MedicosDisponiveis));
    public IActionResult Sugestoes() => Operational(nameof(Sugestoes));
    public IActionResult MedicosSugeridos() => Operational(nameof(MedicosSugeridos));
    public async Task<IActionResult> Substituicoes(Guid? unidadeId, string? status, DateOnly? inicio, DateOnly? fim, string? profissional, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 50);
        if (inicio.HasValue != fim.HasValue || inicio > fim) ModelState.AddModelError(string.Empty, "Informe um período de envio completo e válido.");
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = ModelState.IsValid
            ? await ReadApiResponse<IEnumerable<ManagerSubstitutionDto>>(client, "api/substituicoes")
            : (null, "Revise os filtros informados.", HttpStatusCode.UnprocessableEntity);
        var query = (result.Item1 ?? Array.Empty<ManagerSubstitutionDto>()).AsEnumerable();
        if (unidadeId.HasValue) query = query.Where(x => x.HospitalId == unidadeId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (inicio.HasValue) query = query.Where(x => DateOnly.FromDateTime(x.RegDate) >= inicio.Value && DateOnly.FromDateTime(x.RegDate) <= fim!.Value);
        if (!string.IsNullOrWhiteSpace(profissional)) query = query.Where(x => x.MedicoSolicitanteNome.Contains(profissional.Trim(), StringComparison.OrdinalIgnoreCase));
        var ordered = query.OrderByDescending(x => x.RegDate).ThenByDescending(x => x.Id).ToArray();
        return View(new ManagerRequestQueueViewModel(ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), result.Item2, status, unidadeId, inicio, fim, profissional, page, pageSize, ordered.LongLength));
    }

    public async Task<IActionResult> SubstituicaoDetails(Guid id, string? returnUrl)
    {
        if (id == Guid.Empty) return NotFound();
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponse<ManagerSubstitutionDto>(client, $"api/substituicoes/{id}");
        ViewBag.ErrorMessage = result.Error; ViewBag.ReturnUrl = LocalReturnUrl(returnUrl);
        return result.Data is null && result.StatusCode == HttpStatusCode.NotFound ? NotFound() : View(result.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DecidirSubstituicao(Guid id, long versaoEsperada, string? decisao, string? justificativa, string? returnUrl)
    {
        justificativa = justificativa?.Trim();
        if (id == Guid.Empty || versaoEsperada <= 0 || decisao is not ("APROVAR" or "RECUSAR") || justificativa?.Length is < 3 or > 1000)
        { TempData["Error"] = "Selecione uma decisão explícita e informe justificativa entre 3 e 1.000 caracteres."; TempData["DecisionReason"] = justificativa; return RedirectToAction(nameof(SubstituicaoDetails), new { id, returnUrl = LocalReturnUrl(returnUrl) }); }
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var endpoint = $"api/substituicoes/{id}/{(decisao == "APROVAR" ? "aprovar" : "recusar")}";
        var response = await client.PostAsJsonAsync(endpoint, new { Justificativa = justificativa, VersaoEsperada = versaoEsperada });
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode ? "Decisão registrada. O profissional já pode acompanhar a situação no portal." : response.StatusCode switch
        { HttpStatusCode.Conflict => "A solicitação mudou ou já foi decidida. Revise o estado atual antes de tentar novamente.", HttpStatusCode.Forbidden => "Sua permissão ou vínculo com a unidade não permite esta decisão.", HttpStatusCode.NotFound => "Solicitação indisponível no contexto atual.", _ => "Não foi possível confirmar o resultado. Consulte o estado antes de repetir." };
        if (!response.IsSuccessStatusCode) TempData["DecisionReason"] = justificativa;
        return RedirectToAction(nameof(SubstituicaoDetails), new { id, returnUrl = LocalReturnUrl(returnUrl) });
    }

    private string LocalReturnUrl(string? value) => Url.IsLocalUrl(value) ? value! : Url.Action(nameof(Substituicoes))!;
    public IActionResult ConvitesPendentes() => Operational(nameof(ConvitesPendentes));
    public IActionResult Calendario() => Operational(nameof(Calendario));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convidar(Guid plantaoId, Guid medicoId)
    {
        if (plantaoId == Guid.Empty || medicoId == Guid.Empty)
        {
            TempData["Error"] = "Selecione um plantão e um médico válidos para enviar o convite.";
            return RedirectToAction(nameof(ConvitesPendentes));
        }

        var endpoint = $"api/plantoes/{plantaoId}/convidar-recomendados";
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client))
            {
                return HandleUnauthorized();
            }

            var payload = new
            {
                MedicoIds = new List<Guid> { medicoId },
                Mensagem = "Convite enviado pela Central de Cobertura."
            };
            var (data, error, statusCode) = await SendApiAsync<object, int>(client, HttpMethod.Post, endpoint, payload);
            LogRequestContext("cobertura.convite.enviar", endpoint, (int)statusCode);

            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return HandleUnauthorized();
            }

            if (statusCode is < HttpStatusCode.OK or >= HttpStatusCode.Ambiguous || data <= 0)
            {
                TempData["Error"] = error ?? "O convite não foi enviado. Verifique a disponibilidade e se já existe um convite pendente.";
                return RedirectToAction(nameof(Plantao), new { id = plantaoId });
            }

            TempData["Success"] = "Convite enviado e registrado com sucesso.";
            return RedirectToAction(nameof(Plantao), new { id = plantaoId });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Falha ao enviar convite pela Central de Cobertura. PlantaoId:{PlantaoId} MedicoId:{MedicoId}", plantaoId, medicoId);
            TempData["Error"] = "Não foi possível enviar o convite agora. Tente novamente.";
            return RedirectToAction(nameof(Plantao), new { id = plantaoId });
        }
    }

    private IActionResult Operational(string section) => View("~/Views/Fase2Operational/Dashboard.cshtml", flowService.Build("CENTRAL", section));

    public async Task<IActionResult> Plantao(Guid id)
    {
        try
        {
            using var client = CreateApiClient();
            if (!AddBearerToken(client))
            {
                return HandleUnauthorized();
            }

            var (data, error, statusCode) = await ReadApiResponse<PlantaoDetailsDto>(client, $"api/plantoes/{id}");
            LogRequestContext("WEB_CENTRAL_ESCALA_PLANTAO", $"api/plantoes/{id}", (int)statusCode);

            if (data is null)
            {
                ViewBag.ErrorMessage = error ?? "Plantão não encontrado.";
            }

            return View(data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Falha ao carregar plantão {PlantaoId} na Central de Escala.", id);
            ViewBag.ErrorMessage = "Falha inesperada ao carregar o plantão.";
            return View(null);
        }
    }
}
