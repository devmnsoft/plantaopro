using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[AllowAnonymous]
public sealed class ErrorController : Controller
{
    // P2: sem restricao de verbo em /erro/* porque UseStatusCodePagesWithReExecute("/erro/{0}")
    // e UseExceptionHandler reexecutam o pipeline PRESERVANDO o metodo original (POST, PUT...).
    // Se fossem GET-only, a re-execucao gerava 405 Allow:[GET] que mascarava o codigo original
    // (ex.: 400 de antiforgery no POST /Account/Login aparecia como 405 para o usuario).
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

    // Mesmo motivo do HttpStatus: UseExceptionHandler reexecuta preservando o metodo original.
    [Route("erro")]
    public IActionResult Error()
    {
        return View("~/Views/Shared/Error.cshtml");
    }
}
