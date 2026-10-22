using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Legislacao;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R85 (bloco B4): proxy web do DF-e nacional. A página carrega o catálogo SEFAZ por UF e os
/// layouts vigentes (GETs Ufs/Layouts) e envia a geração/validação de chave por fetch JSON
/// (POSTs GerarChave/ValidarChave); os resultados voltam no contrato { ok, data/erro } (mesmo
/// padrão da Folha, do Simples e do Calendário Fiscal). A configuração e o módulo 11 vivem na
/// API (api/dfe-nacional).
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
public sealed class DfeNacionalWebController : BaseWebController
{
    public DfeNacionalWebController(IHttpClientFactory factory, ILogger<DfeNacionalWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("Ufs")]
    public async Task<IActionResult> Ufs()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("DfeNacionalUfs", "api/dfe-nacional/ufs");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/dfe-nacional/ufs");
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized ? error ?? "Revise os dados informados e tente novamente." : "Não foi possível carregar o catálogo de UFs agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpGet("Layouts")]
    public async Task<IActionResult> Layouts()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("DfeNacionalLayouts", "api/dfe-nacional/layouts");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/dfe-nacional/layouts");
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized ? error ?? "Revise os dados informados e tente novamente." : "Não foi possível carregar os layouts oficiais agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpPost("GerarChave")]
    public async Task<IActionResult> GerarChave([FromBody] GerarChaveCommand comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("DfeNacionalGerar", "api/dfe-nacional/chave-acesso/gerar");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/dfe-nacional/chave-acesso/gerar", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise os dados informados e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("ValidarChave")]
    public async Task<IActionResult> ValidarChave([FromBody] ValidarChaveCommand comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("DfeNacionalValidar", "api/dfe-nacional/chave-acesso/validar");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/dfe-nacional/chave-acesso/validar", comando);
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
