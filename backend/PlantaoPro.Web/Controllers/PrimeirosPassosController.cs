using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class PrimeirosPassosController : Controller
{
    public IActionResult Index() => View();
}
