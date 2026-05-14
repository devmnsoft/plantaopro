using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Controllers;
public class AuditoriaController : BaseWebController
{
    public AuditoriaController(IHttpClientFactory f, ILogger<AuditoriaController> l) : base(f, l) { }
    public async Task<IActionResult> Index(int page=1, int pageSize=20) => await this.RenderPaged<object>($"api/auditoria?page={page}&pageSize={pageSize}");
}
