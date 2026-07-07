using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class AgendamentosController : Controller
{
    public IActionResult Calendario() => View("AgendaPremium", "Calendário clínico|AGENDADO|CONFIRMADO|CHECKIN_REALIZADO|AGUARDANDO_TRIAGEM");
    public IActionResult AgendaDia() => View("AgendaPremium", "Agenda do dia|AGUARDANDO_CONSULTA|EM_ATENDIMENTO|ATENDIDO|FALTOU");
    public IActionResult AgendaMedico() => View("AgendaPremium", "Agenda do médico|CONFIRMADO|EM_ATENDIMENTO|ATENDIDO|CANCELADO");
    public IActionResult CheckIn() => View("AgendaPremium", "Check-in assistido|CONFIRMADO|CHECKIN_REALIZADO|AGUARDANDO_TRIAGEM|EM_TRIAGEM");
}
