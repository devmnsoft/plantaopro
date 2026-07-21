using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Suporte + "," + RolesConstants.Auditor)]
public sealed class SegurancaController : Controller
{
    public IActionResult Index() => View("Index");
    public IActionResult Usuarios() => View("Usuarios");
    public IActionResult UsuarioDetalhes(Guid? id) => View("UsuarioDetalhes", id);
    public IActionResult Perfis() => View("Perfis");
    public IActionResult PerfilDetalhes(Guid? id) => View("PerfilDetalhes", id);
    public IActionResult Permissoes() => View("Permissoes");
    public IActionResult Matriz() => View("Matriz");
    public IActionResult Sessoes() => View("Sessoes");
    public IActionResult TentativasLogin() => View("TentativasLogin");
    public IActionResult AcessosNegados() => View("AcessosNegados");
    public IActionResult Auditoria() => View("Auditoria");
    public IActionResult PoliticasSenha() => View("PoliticasSenha");
}
