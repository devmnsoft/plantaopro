using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R88 (trabalhista / eSocial): proxy web da prévia trabalhista. A página carrega
/// o catálogo oficial de eventos (S-1000 a S-1299) e os colaboradores (GETs) e
/// envia declarações de jornada semanal, eventos em prévia e o cálculo do aviso
/// prévio por fetch JSON (POSTs); os resultados voltam no contrato { ok, data/erro }
/// (mesmo padrão da Folha, do Simples e dos Trilhos). O motor vive na API
/// (api/esocial-trabalhista).
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
// R91: prefixo de rota explicito — sem ele, ""/Index viravam rotas absolutas "/" e "/index" (colisao/404).
[Route("EsocialTrabalhoWeb")]
public sealed class EsocialTrabalhoWebController : BaseWebController
{
    public EsocialTrabalhoWebController(IHttpClientFactory factory, ILogger<EsocialTrabalhoWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("Catalogo")]
    public async Task<IActionResult> Catalogo()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("EsocialCatalogo", "api/esocial-trabalhista/catalogo");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/esocial-trabalhista/catalogo");
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = "Não foi possível carregar o catálogo de eventos agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpGet("Colaboradores")]
    public async Task<IActionResult> Colaboradores()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("EsocialColaboradores", "api/esocial-trabalhista/colaboradores");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/esocial-trabalhista/colaboradores");
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = "Não foi possível carregar os colaboradores agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpGet("Jornada")]
    public async Task<IActionResult> Jornada([FromQuery] string colaboradorId, [FromQuery] string mes)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var rota = "api/esocial-trabalhista/jornada?colaboradorId=" + (colaboradorId ?? "") + "&mes=" + Uri.EscapeDataString(mes ?? "");
        LogRequestContext("EsocialJornada", rota);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, rota);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest
                ? error ?? "Revise o colaborador e o mês informados."
                : "Não foi possível carregar a jornada agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("DeclararJornada")]
    public async Task<IActionResult> DeclararJornada([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("EsocialDeclararJornada", "api/esocial-trabalhista/jornadas/declarar");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/esocial-trabalhista/jornadas/declarar", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise os dados da semana informados e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("ExcluirJornada")]
    public async Task<IActionResult> ExcluirJornada([FromBody] System.Text.Json.JsonElement comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("EsocialExcluirJornada", "api/esocial-trabalhista/jornadas/excluir");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/esocial-trabalhista/jornadas/excluir", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise a declaração selecionada e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("AvisoPrevio")]
    public async Task<IActionResult> AvisoPrevio([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("EsocialAvisoPrevio", "api/esocial-trabalhista/aviso-previo");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/esocial-trabalhista/aviso-previo", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise as datas informadas e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpGet("Eventos")]
    public async Task<IActionResult> Eventos([FromQuery] string colaboradorId, [FromQuery] string periodoApuracao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var rota = "api/esocial-trabalhista/eventos?colaboradorId=" + (colaboradorId ?? "") + "&periodoApuracao=" + Uri.EscapeDataString(periodoApuracao ?? "");
        LogRequestContext("EsocialEventos", rota);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, rota);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest
                ? error ?? "Revise os filtros informados."
                : "Não foi possível carregar os eventos agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("DeclararEvento")]
    public async Task<IActionResult> DeclararEvento([FromBody] object comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("EsocialDeclararEvento", "api/esocial-trabalhista/eventos/declarar");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/esocial-trabalhista/eventos/declarar", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise os dados do evento informados e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpPost("ExcluirEvento")]
    public async Task<IActionResult> ExcluirEvento([FromBody] System.Text.Json.JsonElement comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("EsocialExcluirEvento", "api/esocial-trabalhista/eventos/excluir");
        var (data, error, status) = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/esocial-trabalhista/eventos/excluir", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized
                ? error ?? "Revise o evento selecionado e tente novamente."
                : "Não foi possível concluir a operação agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }
}
