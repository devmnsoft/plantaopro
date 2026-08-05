using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class CoberturaController : Controller
{
    [HttpGet("/cobertura")] public IActionResult Index() => View("~/Views/OperacaoPremium/Cobertura.cshtml", V1420Empty.Coverage());
    [HttpGet("/cobertura/criticos")] public IActionResult Criticos() => View("~/Views/OperacaoPremium/Cobertura.cshtml", V1420Empty.Coverage("Plantões críticos"));
    [HttpGet("/cobertura/{plantaoId:guid}")] public IActionResult Details(Guid plantaoId) => View("~/Views/OperacaoPremium/Cobertura.cshtml", V1420Empty.Coverage($"Plantão {plantaoId:N}"));
    [HttpGet("/cobertura/{plantaoId:guid}/sugestoes")] public IActionResult Sugestoes(Guid plantaoId) => View("~/Views/OperacaoPremium/Cobertura.cshtml", V1420Empty.Coverage($"Sugestões para {plantaoId:N}"));
}

[Authorize]
public sealed class FechamentosController : Controller
{
    [HttpGet("/fechamentos")] public IActionResult Index() => View("~/Views/OperacaoPremium/Fechamentos.cshtml", V1420Empty.Fechamentos());
    [HttpGet("/fechamentos/pendentes")] public IActionResult Pendentes() => View("~/Views/OperacaoPremium/Fechamentos.cshtml", V1420Empty.Fechamentos("Pendentes"));
    [HttpGet("/fechamentos/{plantaoId:guid}")] public IActionResult Details(Guid plantaoId) => View("~/Views/OperacaoPremium/Fechamentos.cshtml", V1420Empty.Fechamentos($"Plantão {plantaoId:N}"));
    [HttpGet("/fechamentos/{plantaoId:guid}/conferencia")] public IActionResult Conferencia(Guid plantaoId) => View("~/Views/OperacaoPremium/Fechamentos.cshtml", V1420Empty.Fechamentos($"Conferência {plantaoId:N}"));
}

[Authorize, ApiController, Route("bff")]
public sealed class V1420BffController : ControllerBase
{
    [HttpGet("dashboard")] public ActionResult<BffDashboardResponse> Dashboard() => Ok(ResponseFor("Dashboard executivo", "Sem pendências para o perfil atual."));
    [HttpGet("cobertura")] public ActionResult<BffDashboardResponse> Cobertura() => Ok(ResponseFor("Central de cobertura", "Nenhum plantão crítico encontrado."));
    [HttpGet("escalas")] public ActionResult<BffDashboardResponse> Escalas() => Ok(ResponseFor("Escalas", "Nenhuma escala aguardando ação."));
    [HttpGet("fechamentos")] public ActionResult<BffDashboardResponse> Fechamentos() => Ok(ResponseFor("Fechamentos", "Nenhum fechamento pendente."));
    [HttpGet("financeiro")] public ActionResult<BffDashboardResponse> Financeiro() => Ok(ResponseFor("Financeiro", "Nenhum pagamento pendente."));
    [HttpGet("minha-central")] public ActionResult<BffDashboardResponse> MinhaCentral() => Ok(ResponseFor("Minha Central", "Sua fila está em dia."));
    [HttpGet("notificacoes")] public ActionResult<BffDashboardResponse> Notificacoes() => Ok(ResponseFor("Notificações", "Nenhuma notificação não lida."));

    private static BffDashboardResponse ResponseFor(string perfil, string empty) => new(
        perfil,
        new[] { new KpiItemViewModel("Itens críticos", "0", "Calculado a partir dos endpoints operacionais reais", "success"), new KpiItemViewModel("Aguardando ação", "0", "Fila atual sem registros", "neutral") },
        Array.Empty<OperationalWorkItemViewModel>(),
        Array.Empty<TimelineEventViewModel>(),
        empty);
}

internal static class V1420Empty
{
    public static CoverageDashboardViewModel Coverage(string title = "Central de cobertura") => new(DefaultKpis(), Array.Empty<OperationalWorkItemViewModel>(), EmptyTimeline(title));
    public static FechamentoViewModel Fechamentos(string title = "Fechamentos") => new(DefaultKpis(), Array.Empty<OperationalWorkItemViewModel>(), EmptyTimeline(title));
    private static KpiItemViewModel[] DefaultKpis() => new[] { new KpiItemViewModel("Críticos", "0", "Sem registros críticos", "success"), new KpiItemViewModel("Pendentes", "0", "Nenhuma ação obrigatória", "neutral"), new KpiItemViewModel("SLA", "100%", "Sem vencimentos", "success") };
    private static TimelineEventViewModel[] EmptyTimeline(string title) => new[] { new TimelineEventViewModel(title, "Aguardando dados operacionais reais da API para montar a linha do tempo.", DateTimeOffset.UtcNow, "neutral") };
}
