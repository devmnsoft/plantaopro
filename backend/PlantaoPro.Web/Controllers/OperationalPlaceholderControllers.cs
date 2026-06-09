using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Coordenacao + "," + RolesConstants.Coordenador + "," + RolesConstants.Operador + "," + RolesConstants.Medico)]
public sealed class ConvitesController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Convites de plantão", "Convites respeitam tenant, perfil, agenda, disponibilidade e aceite do médico.", "Convites", "Index"));
}

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Financeiro + "," + RolesConstants.Medico)]
public sealed class PagamentosController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Pagamentos", "Pagamentos operacionais e médicos com filtros por tenant, competência, status e auditoria.", "Pagamentos", "Index"));
}
