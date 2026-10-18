using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[Route("MeuDia")]
public sealed class MeuDiaController : Controller
{
    private readonly ProductivityWebService _productivity;

    public MeuDiaController(ProductivityWebService productivity) => _productivity = productivity;

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
        var model = await _productivity.GetMyDayAsync(token, cancellationToken);
        // R41 (E9 primeira experiência): quem administra a conta vê no Meu Dia o andamento da
        // implantação. Leitura best-effort — se falhar, a página segue normal sem o aviso.
        if (User.IsInRole(RolesConstants.AdministradorCliente) || User.IsInRole(RolesConstants.Administrador))
        {
            var (ok, status) = await _productivity.GetOnboardingStatusAsync(token, cancellationToken);
            if (ok)
            {
                model.Onboarding = status;
                model.OnboardingCarregado = true;
            }
        }
        return View(model);
    }
}
