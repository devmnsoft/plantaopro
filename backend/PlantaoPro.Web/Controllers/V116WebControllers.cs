using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[Route("Convenios/Autorizacoes")]
public sealed class ConvenioAutorizacoesController : Controller { [HttpGet("")] public IActionResult Index() => View("~/Views/V116/Autorizacoes.cshtml"); }
[Authorize]
[Route("Convenios/Guias")]
public sealed class GuiasConvenioController : Controller { [HttpGet("")] public IActionResult Index() => View("~/Views/V116/Guias.cshtml"); }
[Authorize]
[Route("FaturamentoClinico/Lotes")]
public sealed class LotesFaturamentoController : Controller { [HttpGet("")] public IActionResult Index() => View("~/Views/V116/Lotes.cshtml"); }
[Authorize]
[Route("Caixa")]
public sealed class CaixaController : Controller { [HttpGet("")] public IActionResult Index() => View("~/Views/V116/Caixa.cshtml"); }
[Authorize]
[Route("Timelines")]
public sealed class TimelinesController : Controller { [HttpGet("{entidade}/{id}")] public IActionResult Details(string entidade, Guid id) => View("~/Views/V116/Timeline.cshtml", new { Entidade = entidade, Id = id }); }
[Authorize]
[Route("NotificacoesOperacionais")]
public sealed class NotificacoesOperacionaisController : Controller { [HttpGet("")] public IActionResult Index() => View("~/Views/V116/Notificacoes.cshtml"); }
[Authorize]
[Route("RelatoriosExecutivos")]
public sealed class RelatoriosExecutivosController : Controller { [HttpGet("")] public IActionResult Index() => View("~/Views/V116/Relatorios.cshtml"); }
