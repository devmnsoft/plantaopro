using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

// ============================================================================
// R5-B6 - Superfície de cobrança SaaS.
// Administração (GLOBAL/ADMIN): providers, gerar cobrança, eventos da fatura.
// Checkout sandbox e webhook são anônimos: a referencia opaca e a assinatura
// HMAC são as credenciais dessas pontas (mesma lógica dos convites B5).
// ============================================================================

public sealed record CobrarFaturaRequest(string? ProviderCodigo);
public sealed record SimularPagamentoRequest(bool Aprovado);

[ApiController]
[Route("api/cobranca")]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Tags("SaaS - Cobrança")]
public sealed class CobrancaAdminController : ControllerBase
{
    private readonly CobrancaService _service;
    public CobrancaAdminController(CobrancaService service) => _service = service;

    /// <summary>Providers cadastrados com modo/status e credencial configurada (o segredo nunca sai daqui).</summary>
    [HttpGet("providers")]
    public async Task<IActionResult> Providers(CancellationToken ct)
    {
        var r = await _service.ListarProvidersAsync(ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Gera a cobrança de uma fatura aberta/vencida. Idempotente: retorna a cobrança ativa existente.</summary>
    [HttpPost("faturas/{id:guid}/cobrar")]
    public async Task<IActionResult> Cobrar(Guid id, [FromBody] CobrarFaturaRequest? request, CancellationToken ct)
    {
        var r = await _service.CobrarAsync(id, request?.ProviderCodigo, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Linha do tempo da fatura: tentativas de cobrança, webhooks processados e trilha financeira.</summary>
    [HttpGet("faturas/{id:guid}/eventos")]
    public async Task<IActionResult> Eventos(Guid id, CancellationToken ct)
    {
        var r = await _service.EventosFaturaAsync(id, ct);
        return StatusCode(r.StatusCode, r);
    }
}

/// <summary>
/// Checkout do sandbox (público). A GET apenas lê o status — abrir/recarregar o
/// link jamais muta nada (redirect de navegador não paga fatura). Só o POST
/// explicito simula o meio de pagamento, assinando o webhook server-side.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/cobranca/sandbox")]
[Tags("SaaS - Cobrança sandbox")]
public sealed class CobrancaSandboxController : ControllerBase
{
    private readonly CobrancaService _service;
    public CobrancaSandboxController(CobrancaService service) => _service = service;

    [HttpGet("{referencia}")]
    public async Task<IActionResult> Status(string referencia, CancellationToken ct)
    {
        var r = await _service.ObterPublicaAsync(referencia, ct);
        return StatusCode(r.StatusCode, r);
    }

    [HttpPost("{referencia}/simular")]
    public async Task<IActionResult> Simular(string referencia, [FromBody] SimularPagamentoRequest request, CancellationToken ct)
    {
        var r = await _service.SimularPagamentoAsync(referencia, request.Aprovado, ct);
        return StatusCode(r.StatusCode, r);
    }

    /// <summary>Página de checkout do sandbox (HTML). Somente leitura + ação explícita de simulação.</summary>
    [HttpGet("{referencia}/pagina")]
    [Produces("text/html")]
    public async Task<IActionResult> Pagina(string referencia, CancellationToken ct)
    {
        var r = await _service.ObterPublicaAsync(referencia, ct);
        if (!r.Success || r.Data is null)
            return Content(PaginaHtml(referencia, null, r.Message), "text/html; charset=utf-8");
        return Content(PaginaHtml(referencia, r.Data, null), "text/html; charset=utf-8");
    }

    private static string PaginaHtml(string referencia, CobrancaPublicaDto? dados, string? erro)
    {
        static string Enc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
        string corpo;
        string scriptTag;
        if (dados is null)
        {
            corpo = "<p class=\"erro\">" + Enc(erro ?? "Cobrança não encontrada.") + "</p>";
            scriptTag = string.Empty;
        }
        else
        {
            corpo =
                "<table>" +
                "<tr><th>Cliente</th><td>" + Enc(dados.ClienteNome) + "</td></tr>" +
                "<tr><th>Competência</th><td>" + dados.Competencia.ToString("MM/yyyy") + "</td></tr>" +
                "<tr><th>Vencimento</th><td>" + dados.Vencimento.ToString("dd/MM/yyyy") + "</td></tr>" +
                "<tr><th>Valor</th><td>R$ " + dados.Valor.ToString("N2") + "</td></tr>" +
                "<tr><th>Status da cobrança</th><td id=\"status-cobranca\">" + Enc(dados.Status) + "</td></tr>" +
                "<tr><th>Status da fatura</th><td id=\"status-fatura\">" + Enc(dados.FaturaStatus) + "</td></tr>" +
                "<tr><th>Expira em</th><td>" + dados.ExpiraEm.ToString("dd/MM/yyyy HH:mm") + " UTC</td></tr>" +
                "</table>" +
                "<div id=\"resultado\" role=\"status\"></div>" +
                "<div class=\"acoes\">" +
                "<button id=\"btn-pagar\" type=\"button\">Pagar (sandbox)</button> " +
                "<button id=\"btn-falhar\" type=\"button\" class=\"recusar\">Recusar pagamento</button>" +
                "</div>";
            // Referencia e gerada por RNG e so contem hex; mesmo assim escapa para o contexto HTML/JS.
            scriptTag = "<script>" +
                "async function simular(aprovado){" +
                "var alvo=document.getElementById(aprovado?'btn-pagar':'btn-falhar');" +
                "alvo.disabled=true;" +
                "try{" +
                "var resp=await fetch('/api/cobranca/sandbox/" + Enc(referencia) + "/simular',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({aprovado:aprovado})});" +
                "var json=await resp.json();" +
                "document.getElementById('resultado').textContent=((json&&json.message)||'Processado.')+' Recarregando...';" +
                "setTimeout(function(){location.reload();},900);" +
                "}catch(e){" +
                "alvo.disabled=false;" +
                "document.getElementById('resultado').textContent='Falha de comunicacao; nenhuma alteracao foi garantida.';" +
                "}}" +
                "document.getElementById('btn-pagar').addEventListener('click',function(){simular(true);});" +
                "document.getElementById('btn-falhar').addEventListener('click',function(){simular(false);});" +
                "</script>";
        }
        return "<!doctype html>" +
            "<html lang=\"pt-br\"><head>" +
            "<meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" +
            "<title>Checkout sandbox PLANTAOPro</title>" +
            "<style>" +
            "body{font-family:system-ui,-apple-system,\"Segoe UI\",Roboto,sans-serif;max-width:560px;margin:3rem auto;padding:0 1rem;color:#1c2430}" +
            "h1{font-size:1.25rem}.badge{display:inline-block;background:#eef2f7;border-radius:6px;padding:.15rem .5rem;font-size:.8rem}" +
            "table{width:100%;border-collapse:collapse;margin:1rem 0}th,td{text-align:left;padding:.4rem .2rem;border-bottom:1px solid #dde3ea}" +
            "th{color:#5b6675;font-weight:600;width:40%}" +
            "button{background:#0b6e4f;color:#fff;border:0;border-radius:8px;padding:.6rem 1rem;font-size:1rem;cursor:pointer}" +
            "button.recusar{background:#8a3b3b}button:disabled{opacity:.5;cursor:default}" +
            "#resultado{margin:.8rem 0;color:#33415a}.erro{color:#8a3b3b}" +
            ".nota{color:#5b6675;font-size:.85rem}" +
            "</style></head><body>" +
            "<h1>Checkout sandbox <span class=\"badge\">PLANTÃOPro</span></h1>" +
            "<p class=\"nota\">Ambiente de teste sem dados de cartao. O pagamento aqui percorre exatamente o mesmo pipeline de webhook assinado do provedor real.</p>" +
            corpo + scriptTag +
            "</body></html>";
    }
}

/// <summary>
/// Webhook anônimo do meio de pagamento. Autenticação = HMAC-SHA256 do corpo
/// bruto no header "X-Cobranca-Signature: sha256=&lt;hex&gt;". Respostas:
/// 404 provider inexistente, 409 inativo, 503 sem credencial, 401 assinatura
/// inválida (nada persistido — replay não assinado não consome o dedupe),
/// 200 com resultado APLICADO/IGNORADO/DUPLICADO/RECUSADO no corpo.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/cobranca/webhooks")]
[Tags("SaaS - Webhook cobrança")]
public sealed class CobrancaWebhookController : ControllerBase
{
    private readonly CobrancaService _service;
    public CobrancaWebhookController(CobrancaService service) => _service = service;

    [HttpPost("{codigo}")]
    public async Task<IActionResult> Receber(string codigo, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        var corpo = ms.ToArray();
        if (corpo.Length == 0 || corpo.Length > 256 * 1024)
            return StatusCode(400, ApiResponse<string>.Fail("Corpo do webhook vazio ou acima de 256 KB.", 400));
        var assinatura = Request.Headers["X-Cobranca-Signature"].FirstOrDefault();
        var r = await _service.ProcessarWebhookAsync(codigo, corpo, assinatura, ct);
        return StatusCode(r.StatusCode, r);
    }
}
