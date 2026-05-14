using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class EscalasController : BaseWebController
{
    public EscalasController(IHttpClientFactory f, ILogger<EscalasController> l) : base(f, l) { }
    public async Task<IActionResult> Index(Guid? medicoId, Guid? plantaoId, string? status, DateTime? dataInicio, DateTime? dataFim, Guid? hospitalId, Guid? especialidadeId, int page = 1, int pageSize = 20)
        => await this.RenderPaged<EscalaDto>($"api/escalas?medicoId={medicoId}&plantaoId={plantaoId}&status={status}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&hospitalId={hospitalId}&especialidadeId={especialidadeId}&page={page}&pageSize={pageSize}");
    public async Task<IActionResult> Details(Guid id) => View(await this.RenderDetails<EscalaDto>($"api/escalas/{id}"));
}
