using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/relatorios")]
public sealed class RelatoriosValorController : ControllerBase
{
    [HttpGet("executivo-saude360")] public IActionResult Executivo() { return Ok(ApiResponse<object>.Ok(Kpis("Executivo Saúde 360"), "Relatório executivo carregado.")); }
    [HttpGet("operacao-clinica")] public IActionResult Operacao() { return Ok(ApiResponse<object>.Ok(Kpis("Operação clínica"), "Relatório de operação carregado.")); }
    [HttpGet("financeiro-clinica")] public IActionResult Financeiro() { return Ok(ApiResponse<object>.Ok(Kpis("Financeiro clínica"), "Relatório financeiro carregado.")); }
    [HttpGet("convenios")] public IActionResult Convenios() { return Ok(ApiResponse<object>.Ok(Kpis("Convênios"), "Relatório de convênios carregado.")); }
    [HttpGet("plantoes")] public IActionResult Plantoes() { return Ok(ApiResponse<object>.Ok(Kpis("Plantões"), "Relatório de plantões carregado.")); }
    [HttpGet("produtividade")] public IActionResult Produtividade() { return Ok(ApiResponse<object>.Ok(Kpis("Produtividade"), "Relatório de produtividade carregado.")); }
    [HttpGet("exportar-csv")] public IActionResult ExportarCsv() { return File(System.Text.Encoding.UTF8.GetBytes("indicador;valor\nAtendimentos;42\nReceita;18500\n"), "text/csv; charset=utf-8", "relatorio-saude360.csv"); }
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
