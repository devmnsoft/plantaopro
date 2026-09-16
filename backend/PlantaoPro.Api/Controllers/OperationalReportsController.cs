using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/relatorios-operacionais")]
public sealed class OperationalReportsController : ControllerBase
{
    private readonly OperationalReportService _service;

    public OperationalReportsController(OperationalReportService service)
    {
        _service = service;
    }

    [HttpGet("{kind}")]
    public async Task<IActionResult> Get(
        OperationalReportKind kind,
        [FromQuery] OperationalReportFilter filter,
        CancellationToken ct)
    {
        var response = await _service.GetAsync(kind, filter, ct);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{kind}/csv")]
    public async Task<IActionResult> Csv(
        OperationalReportKind kind,
        [FromQuery] OperationalReportFilter filter,
        CancellationToken ct)
    {
        try
        {
            var export = await _service.CsvAsync(kind, filter, ct);
            return File(export.Content, "text/csv; charset=utf-8", export.Name);
        }
        catch (OperationalReportExportException exception)
        {
            return StatusCode(
                exception.StatusCode,
                ApiResponse<object>.Fail(exception.Message, exception.StatusCode));
        }
    }
}
