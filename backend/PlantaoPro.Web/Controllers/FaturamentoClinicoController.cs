using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[Route("FaturamentoClinico")]
public sealed class FaturamentoClinicoController : BaseWebController
{
    private const string ContasEndpoint = "api/v115/faturamento/contas-receber";

    public FaturamentoClinicoController(IHttpClientFactory factory, ILogger<FaturamentoClinicoController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? status, string? competencia, string? convenio, Guid? atendimentoId)
    {
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var result = await ReadApiListResponseAsync<FaturamentoClinicoItemViewModel>(client, ContasEndpoint);
        var hasFilters = !string.IsNullOrWhiteSpace(status) || !string.IsNullOrWhiteSpace(competencia) || !string.IsNullOrWhiteSpace(convenio) || atendimentoId.HasValue;
        var items = result.Data
            .Where(item => string.IsNullOrWhiteSpace(status) || string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(competencia) || string.Equals(item.Competencia, competencia, StringComparison.OrdinalIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(convenio) || string.Equals(item.Convenio, convenio, StringComparison.OrdinalIgnoreCase))
            .Where(item => !atendimentoId.HasValue || item.OrigemId == atendimentoId)
            .ToArray();
        var model = new FaturamentoClinicoViewModel(items, result.Error, status, competencia, convenio, hasFilters, atendimentoId);
        return View(model);
    }

    [HttpGet("ContasReceber")]
    public Task<IActionResult> ContasReceber() => Index(null, null, null, null);

    [HttpGet("Titulos")]
    public IActionResult Titulos() => Produto("Títulos", "Títulos financeiros retornados pela API; boleto permanece somente demonstrativo.", "api/v114/faturamento/titulos");

    [HttpGet("RepassesMedicos")]
    public IActionResult RepassesMedicos() => Produto("Repasses Médicos", "Repasses por plantão realizado e atendimento faturado.", "api/v115/repasses-medicos");

    [HttpGet("Glosas")]
    public IActionResult Glosas() => Produto("Glosas", "Registro e acompanhamento de glosas por convênio.", "api/v115/glosas");

    [HttpGet("DemoBoleto")]
    public IActionResult DemoBoleto() => Produto("Demo Boleto", "Demonstração sem emissão de cobrança real.", "api/v114/faturamento/titulos");

    [HttpGet("Regras")]
    public IActionResult Regras() => Produto("Regras", "Regras reais de faturamento configuradas para o tenant.", "api/v115/faturamento/regras");

    [HttpGet("Recebimentos")]
    public Task<IActionResult> Recebimentos() => Index(null, null, null, null);

    [HttpGet("Configuracoes")]
    public IActionResult Configuracoes() => Produto("Configurações", "Parâmetros financeiros e dependências de provedores externos.", "api/v115/faturamento/regras");

    private IActionResult Produto(string title, string subtitle, string endpoint)
        => View("~/Views/V114/Produto.cshtml", new V114ProdutoWebPage(title, subtitle, endpoint));
}
