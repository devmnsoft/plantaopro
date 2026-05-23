using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize]
public class RelatoriosController : BaseWebController
{
    public RelatoriosController(IHttpClientFactory f, ILogger<RelatoriosController> l) : base(f, l) { }
    public IActionResult Index() => View();
    public IActionResult Sla() => View();
    public IActionResult Convites() => View();
    public IActionResult Cobertura() => View();
    public IActionResult ProdutividadeMedica() => View();
    public IActionResult FaturamentoSaas() => View();
}
