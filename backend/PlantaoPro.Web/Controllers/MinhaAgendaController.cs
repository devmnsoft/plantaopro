using Microsoft.AspNetCore.Mvc;
namespace PlantaoPro.Web.Controllers;
public class MinhaAgendaController : BaseWebController
{
    public MinhaAgendaController(IHttpClientFactory f, ILogger<MinhaAgendaController> l) : base(f, l) { }
    public IActionResult Index() => View();
}
