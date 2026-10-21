using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Folha;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R82 (bloco B4): proxy web da folha de pagamento PF em linguagem leiga. A pagina
/// envia os parametros por fetch JSON e o resultado volta no contrato { ok, data/erro }
/// (mesmo padrao do bloco de anexos, R72). O calculo vive na API (api/folha).
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
public sealed class FolhaWebController : BaseWebController
{
    public FolhaWebController(IHttpClientFactory factory, ILogger<FolhaWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpPost("CalcularMes")]
    public async Task<IActionResult> CalcularMes([FromBody] CalcularMesCommand comando)
        => await EnviarAsync("api/folha/mes", comando);

    [HttpPost("CalcularDecimoTerceiro")]
    public async Task<IActionResult> CalcularDecimoTerceiro([FromBody] CalcularDecimoTerceiroCommand comando)
        => await EnviarAsync("api/folha/decimo-terceiro", comando);

    [HttpPost("CalcularFerias")]
    public async Task<IActionResult> CalcularFerias([FromBody] CalcularFeriasCommand comando)
        => await EnviarAsync("api/folha/ferias", comando);

    [HttpPost("CalcularRescisao")]
    public async Task<IActionResult> CalcularRescisao([FromBody] CalcularRescisaoCommand comando)
        => await EnviarAsync("api/folha/rescisao", comando);

    private async Task<IActionResult> EnviarAsync(string endpoint, object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("FolhaCalculo", endpoint);
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, endpoint, comando);
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
