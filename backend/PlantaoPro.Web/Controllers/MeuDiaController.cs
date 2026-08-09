using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[Route("MeuDia")]
public sealed class MeuDiaController : Controller
{
    private readonly MinhaCentralWebService _central;

    public MeuDiaController(MinhaCentralWebService central) => _central = central;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Meu Dia";
        ViewData["WorkspaceHeader"] = new WorkspaceHeaderViewModel
        {
            Breadcrumbs = new[] { new BreadcrumbViewModel("Meu trabalho", null) },
            Title = "Meu Dia",
            Description = "Uma fila pessoal para decidir o que fazer agora, sem perder o contexto da operação.",
            HelpContext = "meu-dia"
        };
        var token = HttpContext.Session.GetString("JwtToken") ?? string.Empty;
        return View(await _central.GetAsync(token, cancellationToken));
    }
}
