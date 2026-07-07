using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class DashboardsPremiumController : Controller
{
    public IActionResult AdminGlobal() => View("Perfil", "Admin Global|clientes ativos|tenants ativos|receita SaaS estimada|faturas abertas|clientes em risco|módulos mais usados|chamados críticos|adoção por módulo");
    public IActionResult AdministradorCliente() => View("Perfil", "Administrador Cliente|plantões do mês|escalas pendentes|médicos ativos|pacientes atendidos|agenda do dia|contas a receber|uso do plano|pendências críticas");
    public IActionResult Coordenacao() => View("Perfil", "Coordenação|plantões abertos|plantões sem médico|escalas solicitadas|conflitos|convites pendentes|substituições|próximos plantões");
    public IActionResult Medico() => View("Perfil", "Médico|próximo plantão|convites pendentes|escalas confirmadas|pagamentos a receber|notificações|disponibilidade pendente");
    public IActionResult Financeiro() => View("Perfil", "Financeiro|pagamentos médicos pendentes|contas a receber|caixa do dia|glosas|recebimentos por forma de pagamento|inadimplência");
    public IActionResult Saude360() => View("Perfil", "Saúde 360|pacientes do dia|agendamentos|check-ins|fila de triagem|consultas em andamento|prescrições emitidas|atendimentos finalizados");
}
