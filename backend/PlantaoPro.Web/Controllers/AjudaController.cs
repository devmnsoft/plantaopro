using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class AjudaController : BaseWebController
{
    private readonly ILogger<AjudaController> _logger;

    public AjudaController(IHttpClientFactory f, ILogger<AjudaController> logger) : base(f, logger)
    {
        _logger = logger;
    }

    public IActionResult Index(string? busca = null)
    {
        _logger.LogInformation("Ajuda/Index iniciada. Busca:{Busca}", busca);
        ViewBag.Busca = busca;
        return View();
    }

    public IActionResult Modulo(string id)
    {
        _logger.LogInformation("Ajuda/Modulo iniciado. Modulo:{Modulo}", id);
        ViewBag.Modulo = id;
        return View();
    }

    public IActionResult Artigo(Guid id)
    {
        ViewBag.ArtigoId = id;
        return View();
    }

    public IActionResult Busca(string? termo = null)
    {
        ViewBag.Termo = termo;
        return View();
    }

    public IActionResult Checklist(string? perfil = null)
    {
        ViewBag.Perfil = perfil ?? User.FindFirst("perfil")?.Value ?? "TODOS";
        return View();
    }

    public IActionResult PrimeirosPassos()
    {
        return View();
    }
}
