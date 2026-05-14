using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Controllers;
public class HospitaisController : BaseWebController
{
 public HospitaisController(IHttpClientFactory f, ILogger<HospitaisController> l):base(f,l){}
 public async Task<IActionResult> Index(string? q, int page=1, int pageSize=20)=> await this.RenderList<HospitalDto>("api/hospitais", page, pageSize);
 public async Task<IActionResult> Details(Guid id)=> View(await this.RenderDetails<HospitalDto>($"api/hospitais/{id}"));
}
