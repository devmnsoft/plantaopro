using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class HospitaisController : BaseWebController
{
    public HospitaisController(IHttpClientFactory f, ILogger<HospitaisController> l) : base(f, l) { }
    public async Task<IActionResult> Index() => await this.RenderList<HospitalDto>("api/hospitais");
}
