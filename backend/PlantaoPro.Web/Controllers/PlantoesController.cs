using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class PlantoesController : BaseWebController
{
    public PlantoesController(IHttpClientFactory f, ILogger<PlantoesController> l) : base(f, l) { }

    public async Task<IActionResult> Index(string? hospital, string? especialidade, string? status, string? tipo, DateTime? dataInicio, DateTime? dataFim, int page = 1, int pageSize = 20)
        => await this.RenderPaged<PlantaoResumoDto>($"api/plantoes?hospital={hospital}&especialidade={especialidade}&status={status}&tipo={tipo}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&page={page}&pageSize={pageSize}");

    public IActionResult Details(Guid id) => View(model: id);
}
