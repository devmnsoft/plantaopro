using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Controllers;
public class MedicosController : BaseWebController
{
 public MedicosController(IHttpClientFactory f, ILogger<MedicosController> l):base(f,l){}
 public async Task<IActionResult> Index(string? q, int page=1, int pageSize=20)=> await this.RenderList<MedicoDto>("api/medicos", page, pageSize);
 public async Task<IActionResult> Details(Guid id)=> View(await this.RenderDetails<MedicoDto>($"api/medicos/{id}"));
}
