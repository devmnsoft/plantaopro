using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class EspecialidadesController : BaseWebController
{
    public EspecialidadesController(IHttpClientFactory f, ILogger<EspecialidadesController> l) : base(f, l) { }
    public async Task<IActionResult> Index() => await this.RenderList<EspecialidadeDto>("api/especialidades");
}
