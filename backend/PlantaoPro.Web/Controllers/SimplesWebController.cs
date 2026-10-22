using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R83 (bloco B4): proxy web da previa do DAS do Simples Nacional. A pagina carrega a tabela
/// oficial vigente (GET Tabelas) e envia a simulacao por fetch JSON (POST CalcularDas); o
/// resultado volta no contrato { ok, data/erro } (mesmo padrao da Folha, R82). O calculo
/// vive na API (api/simples).
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
// R91: prefixo de rota explicito — sem ele, ""/Index viravam rotas absolutas "/" e "/index" (colisao/404).
[Route("SimplesWeb")]
public sealed class SimplesWebController : BaseWebController
{
    public SimplesWebController(IHttpClientFactory factory, ILogger<SimplesWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("Tabelas")]
    public async Task<IActionResult> Tabelas([FromQuery] int? ano)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("SimplesTabelas", "api/simples/tabela");
        var endpoint = "api/simples/tabela" + (ano.HasValue ? "?ano=" + ano.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, endpoint);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = status == System.Net.HttpStatusCode.BadRequest ? error ?? "Revise os dados informados e tente novamente." : "Não foi possível carregar as tabelas oficiais agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpPost("CalcularDas")]
    public async Task<IActionResult> CalcularDas([FromBody] PreviaDasCommand comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("SimplesPrevias", "api/simples/calcular-das");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/simples/calcular-das", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status == System.Net.HttpStatusCode.BadRequest
                ? error ?? "Revise os dados informados e tente novamente."
                : "Não foi possível concluir o cálculo agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }
}
