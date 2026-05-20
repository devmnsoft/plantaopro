using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class InteligenciaController : Controller
{
    private readonly IInteligenciaNegocioService _service;

    public InteligenciaController(IInteligenciaNegocioService service)
    {
        _service = service;
    }

    public IActionResult Dashboard()
    {
        // TODO: substituir mock por dados reais da API.
        var historico = new List<EscalaHistoricoDto>
        {
            new(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1200, "Clínica", "Hospital A"),
            new(Guid.NewGuid(), DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2).AddHours(12), 1500, "Pediatria", "Hospital B")
        };

        var auditoria = new List<NotificacaoEventoDto>
        {
            new("coord@plantaopro.com", "Escala substituída automaticamente.", DateTime.UtcNow.AddMinutes(-30), "InApp"),
            new("medico@plantaopro.com", "Conflito detectado antes da confirmação.", DateTime.UtcNow.AddMinutes(-10), "Email")
        };

        var vm = _service.ConstruirDashboard(historico, auditoria);
        return View(vm);
    }
}
