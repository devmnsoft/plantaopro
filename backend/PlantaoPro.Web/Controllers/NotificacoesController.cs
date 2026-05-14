using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public class NotificacoesController : BaseWebController
{
    public NotificacoesController(IHttpClientFactory f, ILogger<NotificacoesController> l) : base(f, l) { }
    public async Task<IActionResult> Index() => await this.RenderPaged<NotificacaoDto>("api/notificacoes?page=1&pageSize=20");
}
