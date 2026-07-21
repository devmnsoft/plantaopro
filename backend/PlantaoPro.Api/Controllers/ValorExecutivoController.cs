using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Data;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/relatorios/valor")]
public sealed class RelatoriosValorController : ControllerBase
{
    private readonly IReportExportService reportExport;
    public RelatoriosValorController(IReportExportService reportExport) { this.reportExport = reportExport; }
    [HttpGet("executivo-saude360")] public IActionResult Executivo() { return Ok(ApiResponse<object>.Ok(Kpis("Executivo Saúde 360"), "Relatório executivo carregado.")); }
    [HttpGet("operacao-clinica")] public IActionResult Operacao() { return Ok(ApiResponse<object>.Ok(Kpis("Operação clínica"), "Relatório de operação carregado.")); }
    [HttpGet("financeiro-clinica")] public IActionResult Financeiro() { return Ok(ApiResponse<object>.Ok(Kpis("Financeiro clínica"), "Relatório financeiro carregado.")); }
    [HttpGet("convenios")] public IActionResult Convenios() { return Ok(ApiResponse<object>.Ok(Kpis("Convênios"), "Relatório de convênios carregado.")); }
    [HttpGet("plantoes")] public IActionResult Plantoes() { return Ok(ApiResponse<object>.Ok(Kpis("Plantões"), "Relatório de plantões carregado.")); }
    [HttpGet("produtividade")] public IActionResult Produtividade() { return Ok(ApiResponse<object>.Ok(Kpis("Produtividade"), "Relatório de produtividade carregado.")); }
    [HttpGet("exportar-csv")] public async Task<IActionResult> ExportarCsv([FromQuery] DateTime? inicio = null, [FromQuery] DateTime? fim = null, CancellationToken cancellationToken = default) { var uid = TryGuid(User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value); var tenant = TryGuid(User.FindFirst("cliente_id")?.Value); var result = await reportExport.ExportarCsvAsync("FINANCEIRO_CLINICA", new ReportFilterRequest { Inicio = inicio, Fim = fim }, tenant, uid, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken); return File(result.Content, result.ContentType, result.FileName); }
    private static Guid? TryGuid(string? value) { return Guid.TryParse(value, out var id) ? id : null; }
    private static object Kpis(string titulo) { return new { Titulo = titulo, Atendimentos = 42, ConsultasFinalizadas = 18, TaxaFaltas = 7.5m, TempoMedioEspera = "18 min", ReceitaPeriodo = 18500m, ContasVencidas = 3, PendenciasCriticas = 2, GeradoEm = DateTime.UtcNow }; }
}

[ApiController]
[Authorize]
[Route("api/gestor-dashboard")]
public sealed class GestorDashboardApiController : ControllerBase
{
    [HttpGet("resumo")]
    public IActionResult Resumo()
    {
        var data = new { AtendimentosHoje = 24, AtendimentosMes = 420, ReceitaMes = 126000m, ContasVencidas = 9, TempoMedioEspera = "17 min", TaxaFaltas = 6.2m, ConsultasPorMedico = new[] { new { Nome = "Dra. Ana", Total = 38 }, new { Nome = "Dr. Bruno", Total = 31 } }, CidsMaisUsados = new[] { "J00", "I10", "E11" }, ConveniosMaiorVolume = new[] { "Saúde Demo", "Vida Plus" }, PendenciasCriticas = 3, PlantoesDescobertos = 1, Nps = "Configurar pesquisa de satisfação" };
        return Ok(ApiResponse<object>.Ok(data, "Dashboard do gestor carregado."));
    }
}
