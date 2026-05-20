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

    public IActionResult Dashboard(DateTime? inicio, DateTime? fim, string? hospital, string? especialidade, Guid? medicoId)
    {
        var filtro = new DashboardFiltroViewModel(
            Inicio: (inicio ?? DateTime.UtcNow.Date.AddDays(-30)).Date,
            Fim: (fim ?? DateTime.UtcNow.Date.AddDays(1)).Date.AddDays(1).AddSeconds(-1),
            Hospital: string.IsNullOrWhiteSpace(hospital) ? null : hospital,
            Especialidade: string.IsNullOrWhiteSpace(especialidade) ? null : especialidade,
            MedicoId: medicoId);

        var m1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var m2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var historico = new List<EscalaHistoricoDto>
        {
            new(m1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1200, "Clínica", "Hospital A"),
            new(m1, DateTime.UtcNow.AddDays(-4), DateTime.UtcNow.AddDays(-4).AddHours(10), 980, "Clínica", "Hospital A"),
            new(m2, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2).AddHours(12), 1500, "Pediatria", "Hospital B")
        };

        var auditoria = new List<NotificacaoEventoDto>
        {
            new("coord@plantaopro.com", "Escala substituída automaticamente.", DateTime.UtcNow.AddMinutes(-30), "InApp"),
            new("medico@plantaopro.com", "Conflito detectado antes da confirmação.", DateTime.UtcNow.AddMinutes(-10), "Email")
        };

        var vm = _service.ConstruirDashboard(historico, auditoria, filtro);
        return View(vm);
    }
}
