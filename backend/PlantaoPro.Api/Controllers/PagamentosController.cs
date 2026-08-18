using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/pagamentos")]
[Authorize(Roles = RolesConstants.FinanceiroGestao)]
public sealed class PagamentosController : ControllerBase
{
    private readonly FinanceiroService service;
    private readonly ILogger<PagamentosController> logger;
    private readonly PagamentoContestacaoService contestacoes;

    public PagamentosController(FinanceiroService service, PagamentoContestacaoService contestacoes, ILogger<PagamentosController> logger)
    {
        this.service = service;
        this.contestacoes = contestacoes;
        this.logger = logger;
    }

    [HttpPost("{id:guid}/marcar-pago")]
    public Task<IActionResult> MarcarPago(Guid id, [FromBody] MarcarPagamentoPagoRequest request) =>
        ExecuteAsync(id, "marcar como pago", uid => service.MarcarPagoAsync(id, request, uid, Ip(), Request.Headers.UserAgent.ToString()));

    [HttpPost("{id:guid}/contestar")]
    public Task<IActionResult> Contestar(Guid id, [FromBody] ContestarPagamentoRequest request) =>
        ExecuteAsync(id, "contestar", uid => service.ContestarAsync(id, request, uid, Ip(), Request.Headers.UserAgent.ToString()));

    [HttpPost("{id:guid}/resolver-contestacao")]
    public Task<IActionResult> ResolverContestacao(Guid id, [FromBody] ResolverContestacaoPagamentoRequest request) =>
        ExecuteAsync(id, "resolver contestação", _ => contestacoes.ResolveAsync(id, request));

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private async Task<IActionResult> ExecuteAsync(Guid id, string action, Func<Guid, Task<ApiResponse<PagamentoActionResponse>>> operation)
    {
        try
        {
            var claim = User.Claims.FirstOrDefault(item => item.Type == "uid")?.Value;
            if (!Guid.TryParse(claim, out var userId)) return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado.", 401));
            var result = await operation(userId);
            return StatusCode(result.StatusCode, result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao {Action} pagamento {PagamentoId}", action, id);
            return StatusCode(500, ApiResponse<object>.Fail("Não foi possível processar a ação de pagamento.", 500));
        }
    }
}
