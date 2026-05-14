using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class PlantoesController : BaseWebController
{
    public PlantoesController(IHttpClientFactory f, ILogger<PlantoesController> l) : base(f, l) { }
    public async Task<IActionResult> Index() => await this.RenderPaged<PlantaoDto>("api/plantoes?page=1&pageSize=20");
}
