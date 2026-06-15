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
    public IActionResult Operacional() => View("Index");
    public IActionResult Clinico() => View("Index");
    public IActionResult Convenios() => View("Index");
    public IActionResult Saas() => View("Index");
    public IActionResult Widgets() => View("Index");
    public IActionResult Alertas() => View("Index");
}
