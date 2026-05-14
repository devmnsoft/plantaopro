using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class MedicosController : BaseWebController
{
    public MedicosController(IHttpClientFactory f, ILogger<MedicosController> l) : base(f, l) { }
    public async Task<IActionResult> Index() => await this.RenderList<MedicoDto>("api/medicos");
}
