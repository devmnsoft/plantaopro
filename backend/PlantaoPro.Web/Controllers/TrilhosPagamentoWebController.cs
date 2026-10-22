using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R86 (trilhos de pagamento): proxy web dos trilhos. A página carrega a configuração
/// e as faturas abertas (GETs), e envia salvar configuração, gerar PIX (estático/por
/// fatura), validar payload EMV ou linha digitável e gerar a remessa CNAB 400 por fetch
/// JSON (POSTs); os resultados voltam no contrato { ok, data/erro } (mesmo padrão da
/// Folha, do Simples e do Calendário Fiscal). A configuração e o motor vivem na API
/// (api/trilhos-pagamento).
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
// R91: prefixo de rota explicito — sem ele, ""/Index viravam rotas absolutas "/" e "/index" (colisao/404).
[Route("TrilhosPagamentoWeb")]
public sealed class TrilhosPagamentoWebController : BaseWebController
{
    public TrilhosPagamentoWebController(IHttpClientFactory factory, ILogger<TrilhosPagamentoWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("Config")]
    public async Task<IActionResult> Config()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("TrilhosConfig", "api/trilhos-pagamento/config");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/trilhos-pagamento/config");
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = "Não foi possível carregar a configuração dos trilhos agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpPost("SalvarConfig")]
    public async Task<IActionResult> SalvarConfig([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("TrilhosSalvarConfig", "api/trilhos-pagamento/config");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/trilhos-pagamento/config", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise os dados informados e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpGet("FaturasAbertas")]
    public async Task<IActionResult> FaturasAbertas([FromQuery] int limite)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("TrilhosFaturasAbertas", "api/trilhos-pagamento/faturas-abertas");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/trilhos-pagamento/faturas-abertas?limite=" + limite);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = "Não foi possível carregar as faturas abertas agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpGet("PixEstatico")]
    public async Task<IActionResult> PixEstatico()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("TrilhosPixEstatico", "api/trilhos-pagamento/pix/estatico");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/trilhos-pagamento/pix/estatico");
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise a configuração dos trilhos e tente novamente."
                : "Não foi possível gerar o payload PIX agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("PixFatura")]
    public async Task<IActionResult> PixFatura([FromBody] System.Text.Json.JsonElement comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var id = comando.TryGetProperty("faturaId", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() : null;
        LogRequestContext("TrilhosPixFatura", "api/trilhos-pagamento/pix/faturas/" + id);
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/trilhos-pagamento/pix/faturas/" + id, new object());
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise a fatura selecionada e tente novamente."
                : "Não foi possível gerar o payload PIX agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("ValidarPix")]
    public async Task<IActionResult> ValidarPix([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("TrilhosValidarPix", "api/trilhos-pagamento/pix/validar");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/trilhos-pagamento/pix/validar", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise o payload informado e tente novamente."
                : "Não foi possível validar o payload agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("ValidarLinha")]
    public async Task<IActionResult> ValidarLinha([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("TrilhosValidarLinha", "api/trilhos-pagamento/boletos/validar-linha");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/trilhos-pagamento/boletos/validar-linha", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise a linha digitável informada e tente novamente."
                : "Não foi possível validar a linha agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("GerarRemessa")]
    public async Task<IActionResult> GerarRemessa([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("TrilhosGerarRemessa", "api/trilhos-pagamento/remessas/gerar");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/trilhos-pagamento/remessas/gerar", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise os parâmetros da remessa e tente novamente."
                : "Não foi possível gerar a remessa agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }
}
