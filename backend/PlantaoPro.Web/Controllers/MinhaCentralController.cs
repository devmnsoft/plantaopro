using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using PlantaoPro.Web.Models; using PlantaoPro.Web.Services;
namespace PlantaoPro.Web.Controllers;
[Authorize,Route("MinhaCentral")]
public sealed class MinhaCentralController:Controller
{
 private readonly MinhaCentralWebService service; public MinhaCentralController(MinhaCentralWebService service)=>this.service=service;
 [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct){ViewData["Title"]="Minha Central";ViewData["WorkspaceHeader"]=new WorkspaceHeaderViewModel{Breadcrumbs=new[]{new BreadcrumbViewModel("Meu trabalho",null)},Title="Minha Central",Description="Concentre tarefas, alertas, agenda e decisões que precisam da sua atenção.",Context=User.FindFirst("unidade")?.Value,HelpContext="central-operacional"};var model=await service.GetAsync(HttpContext.Session.GetString("JwtToken")??string.Empty,ct);return View(model);}
}
