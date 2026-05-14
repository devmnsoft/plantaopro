using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Controllers;
public class FinanceiroController : BaseWebController
{
    public FinanceiroController(IHttpClientFactory f, ILogger<FinanceiroController> l) : base(f, l) { }
    public async Task<IActionResult> Index(Guid? medicoId, Guid? hospitalId, string? status, DateTime? dataInicio, DateTime? dataFim, Guid? especialidadeId, int page = 1, int pageSize = 20)
        => await this.RenderPaged<PagamentoDto>($"api/financeiro/pagamentos?medicoId={medicoId}&hospitalId={hospitalId}&status={status}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&especialidadeId={especialidadeId}&page={page}&pageSize={pageSize}");
    public async Task<IActionResult> Details(Guid id) => View(await this.RenderDetails<PagamentoDto>($"api/financeiro/pagamentos/{id}"));
}
