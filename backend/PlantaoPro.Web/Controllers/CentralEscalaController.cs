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
    public IActionResult Substituicoes() => Operational(nameof(Substituicoes));
    public IActionResult SubstituicaoDetails(Guid id) => Operational(nameof(SubstituicaoDetails));
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
