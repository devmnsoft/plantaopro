using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Controllers;
public class FinanceiroController : BaseWebController
{
    public FinanceiroController(IHttpClientFactory f, ILogger<FinanceiroController> l) : base(f, l) { }
    public async Task<IActionResult> Index(string? medico, string? hospital, string? status, DateOnly? dataPrevista, DateOnly? dataPagamento, string? formaPagamento, int page = 1, int pageSize = 20)
        => await this.RenderPaged<PagamentoResumoDto>($"api/financeiro/pagamentos?medico={medico}&hospital={hospital}&status={status}&dataPrevista={dataPrevista:O}&dataPagamento={dataPagamento:O}&formaPagamento={formaPagamento}&page={page}&pageSize={pageSize}");
    public IActionResult Details(Guid id) => View(model: id);
}
