using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

public class RelatoriosController : BaseWebController
{
    public RelatoriosController(IHttpClientFactory f, ILogger<RelatoriosController> l) : base(f, l) { }
    public IActionResult Index() => View();
}
