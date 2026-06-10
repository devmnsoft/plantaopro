using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Controllers;
public class NotificacoesController : BaseWebController
{
    public NotificacoesController(IHttpClientFactory f, ILogger<NotificacoesController> l) : base(f, l) { }
    public async Task<IActionResult> Index(string? tipo, bool? lida, DateTime? dataInicio, DateTime? dataFim, int page = 1, int pageSize = 20)
        => await this.RenderPaged<NotificacaoDto>($"api/notificacoes?tipo={tipo}&lida={lida}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&page={page}&pageSize={pageSize}");
    public async Task<IActionResult> Details(Guid id) => View(await this.RenderDetails<NotificacaoDto>($"api/notificacoes/{id}"));
    public IActionResult Preferencias() => View();
}
