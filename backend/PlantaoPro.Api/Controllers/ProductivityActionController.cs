using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Productivity;

namespace PlantaoPro.Api.Controllers;

[ApiController, Authorize, Route("api/produtividade")]
public sealed class ProductivityActionController(IProductivityActionService service, ICurrentUserService current) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ProductivityQuery query, CancellationToken ct) =>
        Ok(await PageAsync(query, ct));

    [HttpGet("meu-dia")]
    public async Task<IActionResult> MyDay(CancellationToken ct) =>
        Ok(await PageAsync(new ProductivityQuery(DueTo: DateTimeOffset.UtcNow.AddDays(1), PageSize: 25), ct));

    [HttpGet("resumo")]
    public async Task<IActionResult> Summary(CancellationToken ct) => Ok(await service.SummaryAsync(ct));

    [HttpGet("contadores")]
    public async Task<IActionResult> Counters(CancellationToken ct)
    {
        var value = await service.SummaryAsync(ct);
        return Ok(new { active = value.Active, badge = value.Active > 99 ? "99+" : value.Active.ToString() });
    }

    [HttpPost("{key}/adiar")]
    public async Task<IActionResult> Snooze(string key, SnoozeProductivityRequest request, CancellationToken ct)
    {
        try { await service.SnoozeAsync(key, request.SnoozedUntil, ct); return NoContent(); }
        catch (ArgumentException e) { return ValidationProblem(detail: e.Message); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException e) { return UnprocessableEntity(new { error = e.Message }); }
    }

    [HttpPost("{key}/dispensar")]
    public IActionResult Dismiss(string key) => UnprocessableEntity(new
    {
        error = "As ações de negócio atuais são derivadas da entidade de origem e não podem ser dispensadas."
    });

    private async Task<object> PageAsync(ProductivityQuery query, CancellationToken ct)
    {
        var page = await service.ListAsync(query, ct); var summary = await service.SummaryAsync(ct);
        return new
        {
            items = page.Items.Select(x => new
            {
                x.Key,x.Module,x.EntityType,x.EntityId,x.ActionCode,x.Title,x.Description,x.Priority,x.Status,x.DueAt,x.CreatedAt,
                x.Icon,x.ContextLabel,
                primaryAction = SafeAction(x.PrimaryAction),x.CanSnooze,x.CanDismiss,x.SourceUpdatedAt,x.IsSnoozed
            }),
            summary, quickActions = service.QuickActions(), agenda = Array.Empty<object>(),
            page.Page,page.PageSize,page.Total,page.TotalPages,
            canViewTeam = current.IsTenantAdmin() || current.HasRole(RolesConstants.Coordenacao) || current.HasRole(RolesConstants.Coordenador)
        };
    }

    private static object SafeAction(string path)
    {
        var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var route = parts.Length > 2 ? new Dictionary<string,string>{{"id",parts[2]}} : new Dictionary<string,string>();
        return new { label="Abrir", controller=parts.ElementAtOrDefault(0)??"Home", action=parts.ElementAtOrDefault(1)??"Index", routeValues=route };
    }
}
