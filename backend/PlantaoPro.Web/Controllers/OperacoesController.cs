using Microsoft.AspNetCore.Mvc;
namespace PlantaoPro.Web.Controllers;
public sealed class OperacoesController : Controller
{
    [HttpGet("/Operacoes")]
    public IActionResult Index() => View();
}
