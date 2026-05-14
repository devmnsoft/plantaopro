using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class FinanceiroController : BaseWebController
{
    public FinanceiroController(IHttpClientFactory f, ILogger<FinanceiroController> l) : base(f, l) { }
    public async Task<IActionResult> Index() => await this.RenderPaged<PagamentoResumoDto>("api/financeiro/pagamentos?page=1&pageSize=20");
    public IActionResult Details(Guid id) => View(model: id);
}
