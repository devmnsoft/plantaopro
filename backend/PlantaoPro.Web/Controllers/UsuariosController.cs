using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor + "," + RolesConstants.Suporte)]
public sealed class UsuariosController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Perfis() => View();
}
