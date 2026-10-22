using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class LgpdController : BaseWebController
{
    public LgpdController(IHttpClientFactory f, ILogger<LgpdController> logger) : base(f, logger) { }
    public IActionResult Index() => View();
    public IActionResult MinhaPrivacidade() => View();
    public IActionResult Politica() => View();
    public IActionResult Consentimentos() => View();
    public IActionResult Solicitacoes() => View();
    public IActionResult Eventos() => View();
    public IActionResult ExportarDados() => View();
    public IActionResult Retencao() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarSolicitacao(LgpdSolicitacaoFormViewModel model)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var result = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/lgpd/solicitacoes", new { model.Tipo, model.Descricao });
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Solicitação LGPD registrada." : result.Error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao criar solicitação LGPD via Web");
            TempData["Error"] = "Não foi possível registrar a solicitação.";
        }
        return RedirectToAction("Solicitacoes");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarConsentimento(string finalidade, string baseLegal, string versaoPolitica, bool consentido)
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var result = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/lgpd/consentimentos/registrar", new { Finalidade = finalidade, BaseLegal = baseLegal, VersaoPolitica = versaoPolitica, Consentido = consentido });
            TempData[result.Success ? "Success" : "Error"] = result.Success ? "Consentimento registrado." : result.Error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao registrar consentimento via Web");
            TempData["Error"] = "Não foi possível registrar consentimento.";
        }
        return RedirectToAction("Consentimentos");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Exportar()
    {
        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();
            var result = await ReadApiResponseAsync<object>(client, "api/lgpd/exportar-meus-dados");
            TempData[result.Error is null ? "Success" : "Error"] = result.Error is null ? "Exportação registrada. Consulte o histórico LGPD." : result.Error;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao solicitar exportação LGPD via Web");
            TempData["Error"] = "Não foi possível solicitar a exportação dos dados.";
        }

        return RedirectToAction("ExportarDados");
    }

    // ------------------------------------------------------------------
    // R87 - LGPD + saúde: consentimento por escopo (PRONTUARIO/COAM),
    // direitos do titular (LISTAR/EXPORTAR/APAGAR) e retenção mínima legal
    // do prontuário (Lei 13.787/2018, art. 6). O motor vive na API
    // (api/lgpd-saude); esta superfície é proxy com o contrato { ok, data/erro }
    // dos demais módulos web (padrão R86).
    // ------------------------------------------------------------------

    [HttpGet("ConsentimentosSaude")]
    public IActionResult ConsentimentosSaude() => View();

    [HttpGet("SaudePacientesBuscar")]
    public async Task<IActionResult> SaudePacientesBuscar([FromQuery] string? termo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("LgpdSaudePacientesBuscar", "api/pacientes/buscar");
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/pacientes/buscar?termo=" + Uri.EscapeDataString(termo ?? "") + "&tamanho=8");
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = "Não foi possível buscar pacientes agora. Tente novamente." });
        return Json(new { ok = true, data });
    }

    [HttpGet("SaudeConsentimentos/{pacienteId:guid}")]
    public async Task<IActionResult> SaudeConsentimentos(Guid pacienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("LgpdSaudeConsentimentos", "api/lgpd-saude/consentimentos/" + pacienteId);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/lgpd-saude/consentimentos/" + pacienteId.ToString("D"));
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = error ?? "Não foi possível carregar os consentimentos agora." });
        return Json(new { ok = true, data });
    }

    [HttpPost("SaudeRegistrarConsentimento")]
    public async Task<IActionResult> SaudeRegistrarConsentimento([FromBody] System.Text.Json.JsonElement comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("LgpdSaudeRegistrarConsentimento", "api/lgpd-saude/consentimentos");
        var (data, error, status) = await SendApiAsync<System.Text.Json.JsonElement, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/lgpd-saude/consentimentos", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
        {
            var mensagem = status is System.Net.HttpStatusCode.BadRequest
                ? error ?? "Revise os dados do consentimento e tente novamente."
                : "Não foi possível registrar o consentimento agora. Tente novamente.";
            return Json(new { ok = false, erro = mensagem });
        }
        return Json(new { ok = true, data });
    }

    [HttpGet("SaudeRetencao/{pacienteId:guid}")]
    public async Task<IActionResult> SaudeRetencao(Guid pacienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("LgpdSaudeRetencao", "api/lgpd-saude/retencao/" + pacienteId);
        var (data, error, status) = await ReadApiResponseAsync<System.Text.Json.JsonElement>(client, "api/lgpd-saude/retencao/" + pacienteId.ToString("D"));
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = error ?? "Não foi possível calcular a retenção agora." });
        return Json(new { ok = true, data });
    }

    [HttpPost("SaudeDireitoExercer")]
    public async Task<IActionResult> SaudeDireitoExercer([FromBody] System.Text.Json.JsonElement comando)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        LogRequestContext("LgpdSaudeDireitoExercer", "api/lgpd-saude/direitos/exercer");
        var (data, error, status) = await SendApiAsync<System.Text.Json.JsonElement, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/lgpd-saude/direitos/exercer", comando);
        if (data.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null || error is not null)
            return Json(new { ok = false, erro = error ?? "Não foi possível exercer o direito agora. Tente novamente." });
        return Json(new { ok = true, data });
    }
}
