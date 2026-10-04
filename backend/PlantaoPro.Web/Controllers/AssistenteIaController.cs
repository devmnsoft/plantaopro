using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// P2 IA — página de configuração do assistente por cliente
/// (provedores, modelos, limites, cota e teste de conexão).
/// Autorização: administradores do tenant (ou global); toda escrita é
/// revalidada pela API com os mesmos papéis e a chave do provedor nunca
/// retorna inteira (apenas mascarada). O teste de conexão é chamado via
/// fetch com o mesmo token antiforgery do formulário.
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE")]
[Route("AssistenteIa")]
public sealed class AssistenteIaController : BaseWebController
{
    private readonly AiWebService _ai;

    public AssistenteIaController(IHttpClientFactory factory, ILogger<AssistenteIaController> logger, AiWebService ai)
        : base(factory, logger) => _ai = ai;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Assistente IA";
        var model = await _ai.ObterConfiguracoesAsync(GetJwtToken() ?? string.Empty, ct);
        if (!string.IsNullOrWhiteSpace(model.Erro)) TempData["Error"] = model.Erro;
        return View(model);
    }

    [HttpPost("Salvar/{task}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(string task, [FromForm] AiConfigFormModel form, CancellationToken ct)
    {
        if (form is null) form = new AiConfigFormModel();
        var (ok, mensagem) = await _ai.SalvarConfiguracaoAsync(GetJwtToken() ?? string.Empty, task, form, ct);
        if (ok)
        {
            TempData["Success"] = "Configuração salva.";
            return RedirectToAction(nameof(Index));
        }
        if (string.Equals(mensagem, AiWebService.MensagemSessaoExpirada, StringComparison.Ordinal))
            return HandleUnauthorized();
        TempData["Error"] = mensagem ?? "Não foi possível salvar. Tente novamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("TestarConexao")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestarConexao([FromForm] string? provedor, CancellationToken ct)
    {
        var resultado = await _ai.TestarConexaoAsync(GetJwtToken() ?? string.Empty, provedor ?? string.Empty, ct);
        if (resultado.StatusKind == AiWebService.StatusNaoAutenticado)
            return Unauthorized(new { mensagem = resultado.Mensagem });
        return Json(resultado);
    }
}
