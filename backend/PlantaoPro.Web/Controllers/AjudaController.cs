using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class AjudaController : BaseWebController
{
    private readonly ILogger<AjudaController> _logger;

    public AjudaController(IHttpClientFactory f, ILogger<AjudaController> logger) : base(f, logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? busca = null)
    {
        _logger.LogInformation("Ajuda/Index iniciada. Busca:{Busca}", busca);
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        if (!string.IsNullOrWhiteSpace(busca))
        {
            var encontrados = await ReadApiListResponseAsync<AjudaArtigoViewModel>(client, "api/ajuda/buscar?termo=" + Uri.EscapeDataString(busca));
            ViewBag.Busca = busca;
            ViewBag.Artigos = encontrados.Data;
        }
        else
        {
            var topicos = await ReadApiListResponseAsync<AjudaTopicoViewModel>(client, "api/ajuda/topicos");
            ViewBag.Topicos = topicos.Data;
            var artigos = await ReadApiListResponseAsync<AjudaArtigoViewModel>(client, "api/ajuda/artigos");
            ViewBag.Artigos = artigos.Data;
        }
        return View();
    }

    public async Task<IActionResult> Artigo(Guid id)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var result = await ReadApiResponseAsync<AjudaArtigoViewModel>(client, "api/ajuda/artigos/" + id);
        if (result.Error is not null) TempData["Error"] = result.Error;
        return View(result.Data ?? new AjudaArtigoViewModel());
    }

    public IActionResult Busca(string termo) => RedirectToAction(nameof(Index), new { busca = termo });

    public async Task<IActionResult> Checklist(string perfil)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var perfilSeguro = string.IsNullOrWhiteSpace(perfil) ? (User.FindFirst("perfil")?.Value ?? "ADMINISTRADOR_GLOBAL") : perfil;
        var result = await ReadApiListResponseAsync<AjudaChecklistViewModel>(client, "api/ajuda/checklists/perfil/" + Uri.EscapeDataString(perfilSeguro));
        ViewBag.Perfil = perfilSeguro;
        return View(result.Data);
    }

    public IActionResult PrimeirosPassos() => RedirectToAction(nameof(Checklist), new { perfil = User.FindFirst("perfil")?.Value ?? "ADMINISTRADOR_GLOBAL" });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(Guid artigoId, bool util, string? comentario)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var request = new { Util = util, Comentario = comentario };
        var response = await SendApiAsync<object, string>(client, HttpMethod.Post, "api/ajuda/artigos/" + artigoId + "/feedback", request);
        TempData[response.Error is null ? "Success" : "Error"] = response.Error ?? "Obrigado pelo feedback.";
        return RedirectToAction(nameof(Artigo), new { id = artigoId });
    }

    public IActionResult Modulo(string id)
    {
        _logger.LogInformation("Ajuda/Modulo iniciado. Modulo:{Modulo}", id);
        ViewBag.Modulo = id;
        return View();
    }
}
