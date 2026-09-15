using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
namespace PlantaoPro.Api.Controllers;
[ApiController,Authorize,Route("api/relatorios-operacionais")]
public sealed class OperationalReportsController(OperationalReportService service):ControllerBase
{
 [HttpGet("{kind}")] public async Task<IActionResult> Get(OperationalReportKind kind,[FromQuery] OperationalReportFilter filter,CancellationToken ct){var r=await service.GetAsync(kind,filter,ct);return StatusCode(r.StatusCode,r);}
 [HttpGet("{kind}/csv")] public async Task<IActionResult> Csv(OperationalReportKind kind,[FromQuery] OperationalReportFilter filter,CancellationToken ct){try{var r=await service.CsvAsync(kind,filter,ct);return File(r.Content,"text/csv; charset=utf-8",r.Name);}catch(UnauthorizedAccessException e){return StatusCode(403,ApiResponse<object>.Fail(e.Message,403));}}
}
