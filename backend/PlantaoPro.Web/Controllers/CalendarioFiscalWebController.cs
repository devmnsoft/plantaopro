using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R84 (bloque B4): proxy web do calendario fiscal/obrigacoes legais por tenant. A pagina carrega
/// o calendario do periodo (GET Calendario) e envia as alteracoes de estado por fetch JSON
/// (POSTs Ativar/Concluir); os resultados voltam no contrato { ok, data/erro } (mesmo padrao da
/// Folha e do Simples). O motor e o estado por tenant vivem na API (api/calendario-fiscal).
/// R100: o Index carrega em melhor esforço os alertas de SLA/escalonamento do mes atual
/// (api/calendario-fiscal/alertas) para o cartao server-side; falha na leitura nao bloqueia a pagina.
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
// R91: prefixo de rota explicito — sem ele, ""/Index viravam rotas absolutas "/" e "/index" (colisao/404).
[Route("CalendarioFiscalWeb")]
public sealed class CalendarioFiscalWebController : BaseWebController
{
    private static readonly JsonSerializerOptions OpcoesJson = new() { PropertyNameCaseInsensitive = true };

    public CalendarioFiscalWebController(IHttpClientFactory factory, ILogger<CalendarioFiscalWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var agora = DateTime.Today;
        var modelo = new CalendarioFiscalWebViewModel { MesExibicao = agora.Month, AnoExibicao = agora.Year };
        using var client = CreateApiClient();
        if (AddBearerToken(client))
        {
            try
            {
                LogRequestContext("CalendarioFiscalAlertas", "api/calendario-fiscal/alertas");
                var (data, error, status) = await ReadApiResponseAsync<JsonElement>(client, "api/calendario-fiscal/alertas");
                if (data.ValueKind == JsonValueKind.Object && error is null && data.TryGetProperty("alertas", out var lista))
                {
                    modelo.Alertas = JsonSerializer.Deserialize<List<AlertaObrigacaoFiscalWebDto>>(lista.GetRawText(), OpcoesJson) ?? new List<AlertaObrigacaoFiscalWebDto>();
                }
            }
            catch
            {
                // melhor esforço: o calendario segue disponivel sem o cartao de alertas
            }
        }
        return View(modelo);
    }

    [HttpGet("Calendario")]
    public async Task<IActionResult> Calendario([FromQuery] int mes, [FromQuery] int ano)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("CalendarioFiscal", "api/calendario-fiscal/calendario");
        var endpoint = "api/calendario-fiscal/calendario?mes=" + mes.ToString(System.Globalization.CultureInfo.InvariantCulture) + "&ano=" + ano.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, endpoint);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized ? error ?? "Revise os dados informados e tente novamente." : "Não foi possível carregar o calendário fiscal agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpPost("AlterarAtiva")]
    public async Task<IActionResult> AlterarAtiva([FromBody] AlterarAtivaObrigacaoCommand comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("CalendarioFiscalAtiva", "api/calendario-fiscal/alterar-ativa");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/calendario-fiscal/alterar-ativa", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise os dados informados e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("AlternarConclusao")]
    public async Task<IActionResult> AlternarConclusao([FromBody] AlternarConclusaoObrigacaoCommand comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("CalendarioFiscalConclusao", "api/calendario-fiscal/alternar-conclusao");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/calendario-fiscal/alternar-conclusao", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise os dados informados e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }
}
