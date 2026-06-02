using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

public sealed class AccountController : Controller
{
    public IActionResult AccessDenied()
    {
        return View();
    }

    public IActionResult ErrorHttpStatus(int code)
    {
        if (code == 401) return RedirectToAction("Login", "Home");
        if (code == 403) return RedirectToAction(nameof(AccessDenied));
        if (code == 404) return View("~/Views/Shared/NotFound.cshtml");
        return View("~/Views/Shared/Error.cshtml");
    }
}
