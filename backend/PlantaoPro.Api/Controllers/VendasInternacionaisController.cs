using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

// ============================================================================
// R89 (venda para fora do país): superficie do faturamento internacional
// simplificado — cotacoes institucionais com moeda base BRL (USD/EUR/GBP),
// catalogo de aliquotas padrao de VAT por pais de destino, faturas SaaS
// candidatas e emissao/anulacao da nota fiscal nao-tradicional (conversao pela
// cotação + VAT acrescido do destino). Superficie global de administracao
// (mesmo plano dos trilhos de pagamento e da cobranca).
// ============================================================================

[ApiController]
[Route("api/vendas-internacionais")]
[Authorize]
[Tags("SaaS - Vendas Internacionais")]
public sealed class VendasInternacionaisAdminController : ControllerBase
{
    private readonly VendasInternacionaisService _service;
    private readonly ILogger<VendasInternacionaisAdminController> _logger;
    public VendasInternacionaisAdminController(VendasInternacionaisService service, ILogger<VendasInternacionaisAdminController> logger) { _service = service; _logger = logger; }

    /// <summary>Histórico de cotações registradas (BRL por unidade), com filtro opcional por moeda e intervalo.</summary>
    [HttpGet("cotacoes")]
    public async Task<IActionResult> Cotacoes([FromQuery] string? moeda, [FromQuery] DateOnly? de, [FromQuery] DateOnly? ate, CancellationToken ct)
    {
        var r = await _service.ListarCotacoesAsync(moeda, de, ate, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Registra/atualiza a cotação de uma moeda em uma data (upsert; o histórico é preservado).</summary>
    [HttpPost("cotacoes")]
    public async Task<IActionResult> RegistrarCotacao([FromBody] ViCotacaoCommand cmd, CancellationToken ct)
    {
        var r = await _service.RegistrarCotacaoAsync(cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Países de destino catalogados com a alíquota padrão de VAT e a fonte citável.</summary>
    [HttpGet("paises-vat")]
    public async Task<IActionResult> PaisesVat(CancellationToken ct)
    {
        var r = await _service.ListarPaisesVatAsync(ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Faturas abertas/vencidas com valor positivo, candidatas à emissão de nota internacional.</summary>
    [HttpGet("faturas")]
    public async Task<IActionResult> Faturas([FromQuery] int limite, CancellationToken ct)
    {
        var r = await _service.FaturasDisponiveisAsync(limite, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Simulação da nota internacional (conversão + VAT) sem persistir nada.</summary>
    [HttpPost("notas/simular")]
    public async Task<IActionResult> SimularNota([FromBody] ViNotaComando cmd, CancellationToken ct)
    {
        var r = await _service.SimularNotaAsync(cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Emite a nota fiscal não-tradicional da fatura (uma EMITIDA por fatura).</summary>
    [HttpPost("notas")]
    public async Task<IActionResult> EmitirNota([FromBody] ViNotaComando cmd, CancellationToken ct)
    {
        var r = await _service.EmitirNotaAsync(UserId(), cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Notas internacionais emitidas/anuladas (filtro opcional por fatura).</summary>
    [HttpGet("notas")]
    public async Task<IActionResult> Notas([FromQuery] Guid? faturaId, CancellationToken ct)
    {
        var r = await _service.ListarNotasAsync(faturaId, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Anula uma nota internacional (motivo obrigatório) e libera a fatura para reemissão.</summary>
    [HttpPost("notas/anular")]
    public async Task<IActionResult> AnularNota([FromBody] ViAnularNotaCommand cmd, CancellationToken ct)
    {
        var r = await _service.AnularNotaAsync(cmd, ct);
        return StatusCode(r.StatusCode, r);
    }

    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("UsuarioId"), out var id) ? id : null;
}
