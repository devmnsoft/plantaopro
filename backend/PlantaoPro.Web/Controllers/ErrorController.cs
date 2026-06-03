using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[AllowAnonymous]
public sealed class ErrorController : Controller
{
    [Route("erro/{statusCode:int}")]
    public IActionResult HttpStatus(int statusCode)
    {
        ViewData["StatusCode"] = statusCode;

        if (statusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (statusCode == 403)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (statusCode == 404)
        {
            return View("~/Views/Shared/NotFound.cshtml");
        }

        return View("~/Views/Shared/Error.cshtml");
    }

    [Route("erro")]
    public IActionResult Error()
    {
        return View("~/Views/Shared/Error.cshtml");
    }
}
