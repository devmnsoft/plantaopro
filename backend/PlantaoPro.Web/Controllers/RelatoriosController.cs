using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize]
public class RelatoriosController : BaseWebController
{
    public RelatoriosController(IHttpClientFactory f, ILogger<RelatoriosController> l) : base(f, l) { }
    public IActionResult Index() => View();
}
