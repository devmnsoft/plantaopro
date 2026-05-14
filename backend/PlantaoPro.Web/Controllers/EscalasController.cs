using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class EscalasController : BaseWebController
{
    public EscalasController(IHttpClientFactory f, ILogger<EscalasController> l) : base(f, l) { }

    public async Task<IActionResult> Index(string? medico, string? hospital, string? especialidade, string? status, DateTime? dataInicio, DateTime? dataFim, int page = 1, int pageSize = 20)
        => await this.RenderPaged<EscalaResumoDto>($"api/escalas?medico={medico}&hospital={hospital}&especialidade={especialidade}&status={status}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&page={page}&pageSize={pageSize}");

    public IActionResult Details(Guid id) => View(model: id);
}
