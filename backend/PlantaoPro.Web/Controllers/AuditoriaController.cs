using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;
[Authorize(Roles = RolesConstants.Administrador)]
public class AuditoriaController : BaseWebController
{
    public AuditoriaController(IHttpClientFactory f, ILogger<AuditoriaController> l) : base(f, l) { }
    public async Task<IActionResult> Index(int page=1, int pageSize=20) => await this.RenderPaged<AuditoriaDto>($"api/auditoria?page={page}&pageSize={pageSize}");
}
