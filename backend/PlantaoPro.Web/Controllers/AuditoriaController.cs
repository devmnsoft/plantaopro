using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public class AuditoriaController : BaseWebController
{
    public AuditoriaController(IHttpClientFactory f, ILogger<AuditoriaController> l) : base(f, l) { }

    public async Task<IActionResult> Index(DateTime? dataInicio, DateTime? dataFim, string? perfil, string? entidade, string? acao, bool? sucesso, string? texto, int page = 1, int pageSize = 20)
    {
        var query = new List<string>
        {
            "page=" + page,
            "pageSize=" + pageSize
        };
        if (dataInicio.HasValue) query.Add("dataInicio=" + Uri.EscapeDataString(dataInicio.Value.ToString("yyyy-MM-dd")));
        if (dataFim.HasValue) query.Add("dataFim=" + Uri.EscapeDataString(dataFim.Value.ToString("yyyy-MM-dd")));
        if (!string.IsNullOrWhiteSpace(perfil)) query.Add("perfil=" + Uri.EscapeDataString(perfil));
        if (!string.IsNullOrWhiteSpace(entidade)) query.Add("entidade=" + Uri.EscapeDataString(entidade));
        if (!string.IsNullOrWhiteSpace(acao)) query.Add("acao=" + Uri.EscapeDataString(acao));
        if (sucesso.HasValue) query.Add("sucesso=" + sucesso.Value.ToString().ToLowerInvariant());
        if (!string.IsNullOrWhiteSpace(texto)) query.Add("texto=" + Uri.EscapeDataString(texto));

        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var resumo = await ReadApiResponse<AuditoriaResumoDto>(client, "api/auditoria/resumo");
        ViewBag.Resumo = resumo.Data;
        ViewBag.DataInicio = dataInicio?.ToString("yyyy-MM-dd");
        ViewBag.DataFim = dataFim?.ToString("yyyy-MM-dd");
        ViewBag.Perfil = perfil;
        ViewBag.Entidade = entidade;
        ViewBag.Acao = acao;
        ViewBag.Sucesso = sucesso;
        ViewBag.Texto = texto;
        return await this.RenderPaged<AuditoriaDto>("api/auditoria?" + string.Join("&", query));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await this.RenderDetails<AuditoriaDto>($"api/auditoria/{id}");
        return View(model);
    }
}
