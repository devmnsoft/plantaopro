using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor + "," + RolesConstants.Financeiro)]
public sealed class BillingController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "FaturamentoSaas");
    public IActionResult Faturas() => RedirectToAction("Index", "FaturamentoSaas");
    public IActionResult Inadimplencia() => RedirectToAction("Inadimplencia", "FaturamentoSaas");
}
