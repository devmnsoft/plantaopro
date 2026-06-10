using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class PendenciasController : Controller
{
    private readonly IFase2OperationalFlowService flowService;
    public PendenciasController(IFase2OperationalFlowService flowService) { this.flowService = flowService; }
    public IActionResult Index() => Render(nameof(Index));
    public IActionResult Details(Guid id) => Render(nameof(Details));
    public IActionResult MinhasPendencias() => Render(nameof(MinhasPendencias));
    private IActionResult Render(string section) => View("~/Views/Fase2Operational/Dashboard.cshtml", flowService.Build("PENDENCIAS", section));
}
