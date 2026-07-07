using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class OperacaoInteligenteController : Controller
{
    private readonly ILogger<OperacaoInteligenteController> _logger;

    public OperacaoInteligenteController(ILogger<OperacaoInteligenteController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        try
        {
            var model = OperacaoInteligenteViewModel.Demo();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar cockpit de operação inteligente.");
            TempData["Error"] = "Não foi possível carregar a operação inteligente agora.";
            return View(OperacaoInteligenteViewModel.Empty());
        }
    }
}
