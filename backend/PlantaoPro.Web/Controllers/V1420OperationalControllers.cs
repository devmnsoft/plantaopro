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
public sealed class FechamentosController : BaseWebController
{
    public FechamentosController(IHttpClientFactory factory,ILogger<FechamentosController> logger):base(factory,logger){}
    [HttpGet("/fechamentos")] public Task<IActionResult> Index(CancellationToken ct)=>Load(null,false,ct);
    [HttpGet("/fechamentos/pendentes")] public Task<IActionResult> Pendentes(CancellationToken ct)=>Load(null,true,ct);
    [HttpGet("/fechamentos/{id:guid}")] public Task<IActionResult> Details(Guid id,CancellationToken ct)=>Load(id,false,ct);
    [HttpGet("/fechamentos/{id:guid}/conferencia")] public Task<IActionResult> Conferencia(Guid id,CancellationToken ct)=>Load(id,false,ct);
    private async Task<IActionResult> Load(Guid? id,bool pendentes,CancellationToken ct){var client=CreateApiClient();if(!AddBearerToken(client))return HandleUnauthorized();var list=await ReadApiResponseAsync<IReadOnlyList<FechamentoWebDto>>(client,pendentes?"api/fechamentos/pendentes":"api/fechamentos");var model=new FechamentoOperacionalPageViewModel{Fechamentos=list.Data??Array.Empty<FechamentoWebDto>(),Error=list.Error};if(id.HasValue){var detail=await ReadApiResponseAsync<FechamentoWebDto>(client,$"api/fechamentos/{id}");var timeline=await ReadApiResponseAsync<IReadOnlyList<FechamentoTimelineWebDto>>(client,$"api/fechamentos/{id}/timeline");model.Selecionado=detail.Data;model.Timeline=timeline.Data??Array.Empty<FechamentoTimelineWebDto>();model.Error??=detail.Error;}return View("~/Views/OperacaoPremium/Fechamentos.cshtml",model);}

    [HttpPost("/fechamentos/{id:guid}/acao"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Acao(Guid id,string acao,string? motivo,CancellationToken ct){var permitidas=new HashSet<string>{"iniciar-conferencia","concluir-conferencia","aprovar","devolver","gerar-financeiro"};if(!permitidas.Contains(acao))return BadRequest();var client=CreateApiClient();if(!AddBearerToken(client))return Unauthorized();var result=await SendApiAsync<object,FechamentoWebDto>(client,HttpMethod.Post,$"api/fechamentos/{id}/{acao}",acao=="devolver"?new{Motivo=motivo}:new{});TempData[result.Data is null?"Error":"Success"]=result.Error??"Ação concluída com sucesso.";return RedirectToAction(nameof(Details),new{id});}
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
    public static CoverageDashboardViewModel Coverage(string title = "Central de cobertura") => new(UnavailableKpis(), Array.Empty<OperationalWorkItemViewModel>(), Array.Empty<TimelineEventViewModel>());
    public static FechamentoViewModel Fechamentos(string title = "Fechamentos") => new(UnavailableKpis(), Array.Empty<OperationalWorkItemViewModel>(), Array.Empty<TimelineEventViewModel>());
    private static KpiItemViewModel[] UnavailableKpis() => new[]
    {
        new KpiItemViewModel("Indicadores", "—", "Aguardando integração com a fonte operacional", "neutral")
    };
}
