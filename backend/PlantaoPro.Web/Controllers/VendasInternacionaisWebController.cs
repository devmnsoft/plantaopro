using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R89 (venda para fora do país): proxy web do faturamento internacional
/// simplificado. A página carrega cotações, destinos com VAT e faturas
/// candidatas (GETs) e envia registro de cotação, simulação, emissão e anulação
/// da nota não-tradicional por fetch JSON (POSTs); os resultados voltam no
/// contrato { ok, data/erro } (mesmo padrão da Folha, do Simples e dos Trilhos).
/// O motor vive na API (api/vendas-internacionais).
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
// R91: prefixo de rota explicito — sem ele, ""/Index viravam rotas absolutas "/" e "/index" (colisao/404).
[Route("VendasInternacionaisWeb")]
public sealed class VendasInternacionaisWebController : BaseWebController
{
    public VendasInternacionaisWebController(IHttpClientFactory factory, ILogger<VendasInternacionaisWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("Cotacoes")]
    public async Task<IActionResult> Cotacoes([FromQuery] string moeda, [FromQuery] string de, [FromQuery] string ate)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var rota = "api/vendas-internacionais/cotacoes?moeda=" + Uri.EscapeDataString(moeda ?? "") + "&de=" + (de ?? "") + "&ate=" + (ate ?? "");
        LogRequestContext("ViCotacoes", rota);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, rota);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest
                ? error ?? "Revise os filtros informados."
                : "Não foi possível carregar as cotações agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("RegistrarCotacao")]
    public async Task<IActionResult> RegistrarCotacao([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("ViRegistrarCotacao", "api/vendas-internacionais/cotacoes");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/vendas-internacionais/cotacoes", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise a moeda, a data e a cotação informadas e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpGet("PaisesVat")]
    public async Task<IActionResult> PaisesVat()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("ViPaisesVat", "api/vendas-internacionais/paises-vat");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/vendas-internacionais/paises-vat");
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = "Não foi possível carregar os destinos agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpGet("Faturas")]
    public async Task<IActionResult> Faturas([FromQuery] int limite)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var rota = "api/vendas-internacionais/faturas?limite=" + limite;
        LogRequestContext("ViFaturas", rota);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, rota);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = "Não foi possível carregar as faturas candidatas agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpPost("SimularNota")]
    public async Task<IActionResult> SimularNota([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("ViSimularNota", "api/vendas-internacionais/notas/simular");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/vendas-internacionais/notas/simular", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise a fatura, o destino e a moeda informados e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("EmitirNota")]
    public async Task<IActionResult> EmitirNota([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("ViEmitirNota", "api/vendas-internacionais/notas");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/vendas-internacionais/notas", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.NotFound
                ? error ?? "Revise a fatura, o destino e a moeda informados e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpGet("Notas")]
    public async Task<IActionResult> Notas([FromQuery] string faturaId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var rota = "api/vendas-internacionais/notas?faturaId=" + (faturaId ?? "");
        LogRequestContext("ViNotas", rota);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, rota);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest
                ? error ?? "Revise os filtros informados."
                : "Não foi possível carregar as notas agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("AnularNota")]
    public async Task<IActionResult> AnularNota([FromBody] System.Text.Json.JsonElement comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("ViAnularNota", "api/vendas-internacionais/notas/anular");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/vendas-internacionais/notas/anular", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise a nota selecionada e o motivo informado e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }
}
