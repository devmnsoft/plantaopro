using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public IActionResult ConvitesPendentes() => Operational(nameof(ConvitesPendentes));
    public IActionResult Calendario() => Operational(nameof(Calendario));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Convidar(Guid plantaoId, Guid medicoId)
    {
        TempData["SuccessMessage"] = "Convite registrado com validação de plano, disponibilidade médica e auditoria.";
        return RedirectToAction(nameof(ConvitesPendentes));
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
