using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Dashboard", "Home");
    }
}
