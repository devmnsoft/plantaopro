using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class BiController : BaseWebController
{
    public BiController(IHttpClientFactory f, ILogger<BiController> l) : base(f, l) { }

    public IActionResult Index() => View();
    public IActionResult Financeiro() => View();
    public IActionResult Medicos() => View();
    public IActionResult Clientes() => View();
    public IActionResult Sla() => View();
}
