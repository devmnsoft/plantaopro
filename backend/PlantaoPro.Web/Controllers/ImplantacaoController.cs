using Microsoft.AspNetCore.Mvc;
namespace PlantaoPro.Web.Controllers;
public sealed class ImplantacaoController : Controller
{
    [HttpGet("/Implantacao")]
    public IActionResult Index() => View();
}
