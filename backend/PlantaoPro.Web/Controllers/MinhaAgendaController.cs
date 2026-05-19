using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;
public class MinhaAgendaController : BaseWebController
{
    public MinhaAgendaController(IHttpClientFactory f, ILogger<MinhaAgendaController> l) : base(f, l) { }
    public async Task<IActionResult> Index()
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var p = await ReadApiResponse<PagedResult<PlantaoResumoDto>>(client, "api/plantoes?page=1&pageSize=5");
        var pg = await ReadApiResponse<PagedResult<PagamentoResumoDto>>(client, "api/financeiro/pagamentos?page=1&pageSize=5");
        var n = await ReadApiResponse<PagedResult<NotificacaoDto>>(client, "api/notificacoes?page=1&pageSize=5");

        var err = p.Error ?? pg.Error ?? n.Error;
        var vm = new MinhaAgendaViewModel(p.Data?.Items ?? Array.Empty<PlantaoResumoDto>(), pg.Data?.Items ?? Array.Empty<PagamentoResumoDto>(), n.Data?.Items ?? Array.Empty<NotificacaoDto>(), err);
        return View(vm);
    }
}
