using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize]
[Route("Pendencias")]
public sealed class PendenciasController : Controller
{
    private readonly ProductivityWebService _productivity;
    private readonly AiWebService _ai;
    public PendenciasController(ProductivityWebService productivity, AiWebService ai) => (_productivity, _ai) = (productivity, ai);

    [HttpGet("")]
    [HttpGet("MinhasPendencias")]
    public async Task<IActionResult> Index([FromQuery] ProductivityQueryViewModel query, CancellationToken ct)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);
        ViewData["Title"] = "Central de Ações";
        ViewData["Query"] = query;
        ViewData["WorkspaceHeader"] = new WorkspaceHeaderViewModel
        {
            Breadcrumbs = new[] { new BreadcrumbViewModel("Meu trabalho", null) },
            Title = "Central de Ações",
            Description = "Prioridades derivadas da operação real, em um só lugar.",
            HelpContext = "central-acoes"
        };
        return View(await _productivity.GetActionsAsync(Token(), query, ct));
    }

    [HttpPost("adiar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Snooze([FromForm] string key, [FromForm] DateTimeOffset snoozedUntil, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key) || snoozedUntil <= DateTimeOffset.UtcNow)
            return BadRequest(new { message = "Informe uma data futura válida." });

        using var response = await _productivity.SnoozeAsync(Token(), key, snoozedUntil, ct);
        if (response.IsSuccessStatusCode) return Ok(new { message = "Ação adiada." });
        var message = response.StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => "Esta ação não está mais ativa.",
            System.Net.HttpStatusCode.Forbidden => "Você não pode adiar esta ação.",
            _ => "Não foi possível adiar a ação."
        };
        return StatusCode((int)response.StatusCode, new { message });
    }

    /// <summary>P2 IA — resumo opcional das pendências (BFF GET; contexto e tenant ficam no servidor).</summary>
    [HttpGet("ResumoIa")]
    public async Task<IActionResult> ResumoIa(CancellationToken ct)
    {
        var resultado = await _ai.ResumoMeuDiaAsync(Token(), ct);
        if (resultado.StatusKind == AiWebService.StatusNaoAutenticado)
            return Unauthorized(new { mensagem = resultado.Mensagem });
        return Json(resultado);
    }

    private string Token() => HttpContext.Session.GetString("JwtToken") ?? string.Empty;
}
