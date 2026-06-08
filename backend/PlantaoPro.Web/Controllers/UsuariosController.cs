using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class UsuariosController : Controller
{
    [HttpGet]
    public IActionResult Perfis() => View();
}
