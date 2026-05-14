using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class PlantoesController : BaseWebController
{
    public PlantoesController(IHttpClientFactory f, ILogger<PlantoesController> l) : base(f, l) { }
    public async Task<IActionResult> Index(Guid? hospitalId, Guid? especialidadeId, string? status, DateTime? dataInicio, DateTime? dataFim, string? cidade, string? estado, int page = 1, int pageSize = 20)
    {
        var q = $"api/plantoes?hospitalId={hospitalId}&especialidadeId={especialidadeId}&status={status}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&cidade={cidade}&estado={estado}&page={page}&pageSize={pageSize}";
        return await this.RenderPaged<PlantaoDto>(q);
    }
    public async Task<IActionResult> Details(Guid id) => View(await this.RenderDetails<PlantaoDto>($"api/plantoes/{id}"));
    public IActionResult Calendario() => View();
}
