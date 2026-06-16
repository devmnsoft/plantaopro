using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class WorkflowSaude360Controller : Saude360WebControllerBase
{
    public WorkflowSaude360Controller(IHttpClientFactory f, ILogger<WorkflowSaude360Controller> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Workflow inteligente Saúde 360", "Workflow inteligente", "Etapas, status, responsáveis e pendências da jornada clínica completa.", "api/workflow-saude360/etapas", Links(Link("Próxima ação", "ProximaAcao", "bi-lightning-charge"), LinkTo("Pendências", "PendenciasClinicas", "Index", "bi-exclamation-triangle"))); }
    public Task<IActionResult> ProximaAcao() { return ModuloAsync("Próxima ação recomendada", "Workflow inteligente", "Ação mais importante para destravar atendimento, financeiro ou relatório.", "api/workflow-saude360/proxima-acao", Links(Link("Etapas", "Index", "bi-list-check"))); }
}

[Authorize]
public sealed class GestorDashboardController : Saude360WebControllerBase
{
    public GestorDashboardController(IHttpClientFactory f, ILogger<GestorDashboardController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Dashboard premium do gestor", "Gestão executiva", "Cards executivos, alertas, rankings e atalhos para relatórios de valor da operação clínica.", "api/gestor-dashboard/resumo", Links(LinkTo("Relatório executivo", "Relatorios", "Executivo", "bi-file-earmark-bar-graph"), LinkTo("Workflow", "WorkflowSaude360", "Index", "bi-signpost"))); }
}
