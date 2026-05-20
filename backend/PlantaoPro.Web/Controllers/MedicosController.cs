using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;
[Authorize(Roles = RolesConstants.Administrador + "," + RolesConstants.Coordenacao + "," + RolesConstants.Operador)]
public class MedicosController : BaseWebController
{
 public MedicosController(IHttpClientFactory f, ILogger<MedicosController> l):base(f,l){}
 public async Task<IActionResult> Index(string? q, int page=1, int pageSize=20)=> await this.RenderList<MedicoDto>("api/medicos", page, pageSize);
 public async Task<IActionResult> Details(Guid id)=> View(await this.RenderDetails<MedicoDto>($"api/medicos/{id}"));
}
