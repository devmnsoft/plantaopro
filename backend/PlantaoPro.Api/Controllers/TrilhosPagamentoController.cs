using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

// ============================================================================
// R86 - Superficie de administracao dos trilhos de pagamento (plano SaaS, como a
// cobranca): configuracao global do banco/conta/chave PIX, PIX copia-e-cola por
// fatura ou estatico, validacao de payload EMV e de linha digitavel, listagem
// das faturas abertas e geracao do arquivo CNAB 400 de remessa.
// ============================================================================

[ApiController]
[Route("api/trilhos-pagamento")]
[Authorize]
[Tags("SaaS - Trilhos de Pagamento")]
public sealed class TrilhosPagamentoAdminController : ControllerBase
{
    private readonly TrilhosPagamentoService _service;
    public TrilhosPagamentoAdminController(TrilhosPagamentoService service) => _service = service;

    /// <summary>Configuração atual dos trilhos (banco, conta, chave PIX).</summary>
    [HttpGet("config")]
    public async Task<IActionResult> Config(CancellationToken ct)
    {
        var r = await _service.ObterConfiguracaoAsync(ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Cria/atualiza a única configuração ativa dos trilhos.</summary>
    [HttpPost("config")]
    public async Task<IActionResult> SalvarConfig([FromBody] TrilhosConfigCommand comando, CancellationToken ct)
    {
        var r = await _service.SalvarConfiguracaoAsync(comando, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Faturas abertas/vencidas candidatas aos trilhos (vencimento + valor + status).</summary>
    [HttpGet("faturas-abertas")]
    public async Task<IActionResult> FaturasAbertas([FromQuery] int limite, CancellationToken ct)
    {
        var r = await _service.ListarFaturasAbertasAsync(limite, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Payload PIX copia-e-cola estático (chave geral, sem valor).</summary>
    [HttpGet("pix/estatico")]
    public async Task<IActionResult> PixEstatico(CancellationToken ct)
    {
        var r = await _service.GerarPixEstaticoAsync(ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Payload PIX copia-e-cola dinâmico da fatura (valor + txid derivado do id).</summary>
    [HttpPost("pix/faturas/{id:guid}")]
    public async Task<IActionResult> PixFatura(Guid id, CancellationToken ct)
    {
        var r = await _service.GerarPixFaturaAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Valida um payload PIX EMV (estrutura, campos obrigatórios e CRC16).</summary>
    [HttpPost("pix/validar")]
    public IActionResult ValidarPix([FromBody] ValidarPixRequest request, CancellationToken ct)
    {
        var r = _service.ValidarPixEmvAsync(request?.Payload ?? string.Empty, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Valida uma linha digitável de boleto (três DV mod-10, DV geral mod-11, fator, valor).</summary>
    [HttpPost("boletos/validar-linha")]
    public async Task<IActionResult> ValidarLinha([FromBody] ValidarLinhaDigitavelRequest request, CancellationToken ct)
    {
        var r = await _service.ValidarLinhaDigitavelAsync(request?.Linha ?? string.Empty, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Gera o arquivo CNAB 400 de remessa das faturas abertas (com itens e linha digitável por título).</summary>
    [HttpPost("remessas/gerar")]
    public async Task<IActionResult> GerarRemessa([FromBody] GerarRemessaCommand comando, CancellationToken ct)
    {
        var r = await _service.GerarRemessaBoletosAsync(comando, ct);
        return StatusCode(r.StatusCode, r);
    }
}
