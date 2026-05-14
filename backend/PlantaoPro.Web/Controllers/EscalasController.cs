using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class EscalasController : BaseWebController
{
    public EscalasController(IHttpClientFactory f, ILogger<EscalasController> l) : base(f, l) { }
    public async Task<IActionResult> Index() => await this.RenderPaged<EscalaDto>("api/escalas?page=1&pageSize=20");
}
