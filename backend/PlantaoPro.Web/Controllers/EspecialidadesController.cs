using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;
[Authorize(Roles = RolesConstants.Administrador + "," + RolesConstants.Coordenacao + "," + RolesConstants.Operador)]
public class EspecialidadesController : BaseWebController
{
 public EspecialidadesController(IHttpClientFactory f, ILogger<EspecialidadesController> l):base(f,l){}
 public async Task<IActionResult> Index(string? q, int page=1, int pageSize=20)=> await this.RenderList<EspecialidadeDto>("api/especialidades", page, pageSize);
 public async Task<IActionResult> Details(Guid id)=> View(await this.RenderDetails<EspecialidadeDto>($"api/especialidades/{id}"));
}
