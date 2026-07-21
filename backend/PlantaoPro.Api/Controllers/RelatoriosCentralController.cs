using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Security.Claims;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/relatorios")]
public sealed class RelatoriosCentralController : ControllerBase
{
    private readonly IReportCatalogService catalog;
    private readonly IReportQueryService query;
    private readonly IReportExportService export;

    public RelatoriosCentralController(IReportCatalogService catalog, IReportQueryService query, IReportExportService export)
    {
        this.catalog = catalog; this.query = query; this.export = export;
    }

    [HttpGet("catalogo")]
    public IActionResult Catalogo() { return Ok(ApiResponse<IReadOnlyCollection<ReportDefinition>>.Ok(catalog.Listar(), "Catálogo de relatórios carregado.")); }

    [HttpGet("{codigo}/preview")]
    public async Task<IActionResult> Preview(string codigo, [FromQuery] ReportFilterRequest filtros, CancellationToken cancellationToken)
    {
        var report = catalog.Obter(codigo); if (report is null) return NotFound(ApiResponse<object>.Fail("Relatório não encontrado.", 404));
        var rows = await query.ConsultarAsync(report, filtros, TenantId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<IDictionary<string, object?>>>.Ok(rows, "Prévia do relatório carregada."));
    }

    [HttpGet("{codigo}/exportar-csv")]
    public async Task<IActionResult> ExportarCsv(string codigo, [FromQuery] ReportFilterRequest filtros, CancellationToken cancellationToken)
    {
        try
        {
            var result = await export.ExportarCsvAsync(codigo, filtros, TenantId(), UsuarioId(), HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse<object>.Fail(ex.Message, 403)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpGet("exportacoes")]
    public IActionResult Exportacoes() { return Ok(ApiResponse<object>.Ok(new { itens = Array.Empty<object>() }, "Histórico de exportações carregado.")); }

    [HttpGet("exportacoes/{id:guid}")]
    public IActionResult Exportacao(Guid id) { return Ok(ApiResponse<object>.Ok(new { id, status = "AUDITADA" }, "Exportação carregada.")); }

    private Guid? UsuarioId() { return TryGuid(User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value); }
    private Guid? TenantId() { return TryGuid(User.FindFirst("cliente_id")?.Value); }
    private static Guid? TryGuid(string? value) { return Guid.TryParse(value, out var id) ? id : null; }
}
