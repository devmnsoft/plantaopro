using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,HOSPITAL")]
public sealed class HospitalAreaController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Área do hospital", "Plantões, escalas e visão da unidade atual sem expor outras unidades.", "HospitalArea", "Escalas"));
    public IActionResult Plantoes() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Plantões da unidade", "Cobertura e pendências da unidade hospitalar vinculada.", "HospitalArea", "Escalas"));
    public IActionResult Escalas() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Escalas da unidade", "Escalas confirmadas, profissionais alocados e próximos turnos.", "HospitalArea", "Plantoes"));
}
