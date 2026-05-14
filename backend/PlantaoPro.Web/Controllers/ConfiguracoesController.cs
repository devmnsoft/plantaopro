using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

public class ConfiguracoesController : BaseWebController
{
    public ConfiguracoesController(IHttpClientFactory f, ILogger<ConfiguracoesController> l) : base(f, l) { }
    public IActionResult Index() => View();
}
