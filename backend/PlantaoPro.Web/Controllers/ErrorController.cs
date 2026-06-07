using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[AllowAnonymous]
public sealed class ErrorController : Controller
{
    [HttpGet]
    [Route("erro/{statusCode:int}")]
    public IActionResult HttpStatus(int statusCode)
    {
        ViewData["StatusCode"] = statusCode;

        return statusCode switch
        {
            401 => RedirectToAction("Login", "Account"),
            403 => RedirectToAction("AccessDenied", "Account"),
            404 => View("~/Views/Shared/NotFound.cshtml"),
            _ => View("~/Views/Shared/Error.cshtml")
        };
    }

    [HttpGet]
    [Route("erro")]
    public IActionResult Error()
    {
        return View("~/Views/Shared/Error.cshtml");
    }
}
