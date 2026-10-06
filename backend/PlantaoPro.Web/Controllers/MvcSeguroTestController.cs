using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// Endpoint exclusivo de teste (somente ambiente Testing; nunca Development/Production):
/// exercita os tres ramos do ModelStateInvalidoFiltro com um decimal financeiro ligado de
/// query/form — GET (redirect para a propria URL), POST form (Referer local validado ou
/// 400) e rota absoluta /bff/mvcfiltro (JSON 400 DADOS_INVALIDOS). A acao em si nao chama
/// API nem o banco: as afirmacoes de "sem efeito no sistema" valem sobre o stub da API.
/// </summary>
[Authorize]
[Route("__test/mvcfiltro")]
public sealed class MvcSeguroTestController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public MvcSeguroTestController(IWebHostEnvironment environment) => _environment = environment;

    [HttpGet("get")]
    public IActionResult Get(decimal valor) => Guardar();

    [HttpPost("post")]
    [ValidateAntiForgeryToken]
    public IActionResult Post(decimal valor) => Guardar();

    private IActionResult Guardar()
    {
        if (!_environment.IsEnvironment("Testing")) return NotFound();
        return Ok(new { ok = true });
    }
}

/// <summary>
/// Rota /bff/mvcfiltro (mesmo padrao dos BFFs da casa): exercita o ramo JSON do
/// ModelStateInvalidoFiltro para consumidores de API. Somente Testing.
/// </summary>
[Authorize]
[ApiController]
[AutoValidateAntiforgeryToken]
[Route("bff/mvcfiltro")]
public sealed class MvcSeguroTestBffController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public MvcSeguroTestBffController(IWebHostEnvironment environment) => _environment = environment;

    [HttpPost]
    public IActionResult Post(decimal valor)
    {
        if (!_environment.IsEnvironment("Testing")) return NotFound();
        return Ok(new { ok = true });
    }
}
