using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R90 (bloque B4 - fechamento da bateria de contratos legais): proxy web de
/// somente leitura da bateria legal 2026. A pagina carrega a bateria agrupada
/// por tema (GET) e os exemplos reais de calculo passam pelo nucleo de
/// tributacao PF da mesma API (tabela GET + calcular POST), com o contrato
/// { ok, data/erro } do padrao da casa. O motor vive na API (api/legislacao/parametros).
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
// R91: prefixo de rota explicito — sem ele, ""/Index viravam rotas absolutas "/" e "/index" (colisao/404).
[Route("LegislacaoWeb")]
public sealed class LegislacaoWebController : BaseWebController
{
    public LegislacaoWebController(IHttpClientFactory factory, ILogger<LegislacaoWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("Bateria")]
    public async Task<IActionResult> Bateria([FromQuery] int? ano)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var rota = "api/legislacao/parametros/bateria" + (ano is null ? "" : "?ano=" + ano.Value);
        LogRequestContext("LegislacaoBateria", rota);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, rota);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise o ano informado."
                : "Não foi possível carregar a bateria de parâmetros agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpGet("TributacaoTabela")]
    public async Task<IActionResult> TributacaoTabela([FromQuery] int? ano)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var rota = "api/legislacao/parametros/tributacao-pf/tabela" + (ano is null ? "" : "?ano=" + ano.Value);
        LogRequestContext("LegislacaoTributacaoTabela", rota);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, rota);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = "Não foi possível carregar a tabela de tributação PF agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpPost("TributacaoCalcular")]
    public async Task<IActionResult> TributacaoCalcular([FromBody] CalcularTributacaoPfCommand comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("LegislacaoTributacaoCalcular", "api/legislacao/parametros/tributacao-pf/calcular");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/legislacao/parametros/tributacao-pf/calcular", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise o valor informado e tente novamente."
                : "Não foi possível concluir o cálculo agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }
}
