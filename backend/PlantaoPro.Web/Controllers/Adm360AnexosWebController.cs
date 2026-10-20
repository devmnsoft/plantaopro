using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// R72 (rodada 72): proxy web dos anexos de arquivos binarios (PDF/Word/imagem) — pedido do usuario.
/// Recebe IFormFile do navegador, converte para base64 e encaminha JSON a API
/// (mesma estrategia da importacao manual da Central XML em Adm360DocumentosXmlWebController).
/// </summary>
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
[Route("Administrativo360/Anexos")]
public sealed class Adm360AnexosWebController : BaseWebController
{
    private const long TamanhoMaximoBytes = 8 * 1024 * 1024;
    private static readonly HashSet<string> ExtensoesAceitas = new(StringComparer.OrdinalIgnoreCase)
    { "pdf", "doc", "docx", "jpg", "jpeg", "png", "webp", "gif", "xml" };

    public Adm360AnexosWebController(IHttpClientFactory factory, ILogger<Adm360AnexosWebController> logger)
        : base(factory, logger) { }

    [HttpGet("Lista")]
    public async Task<IActionResult> Lista([FromQuery] string? categoria, [FromQuery] Guid? entidadeId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"api/administrativo360/anexos?categoria={Uri.EscapeDataString(categoria ?? "")}";
        if (entidadeId.HasValue) query += $"&entidadeId={entidadeId.Value:N}";

        var (data, error, status) = await ReadApiListResponseAsync<AnexoArquivoItemViewModel>(client, query);
        if (error is not null)
            return Json(new { ok = false, erro = error });
        return Json(data.ToList());
    }

    [HttpPost("Upload")]
    public async Task<IActionResult> Upload(IFormFile? arquivo, string? categoria, Guid? entidadeId, string? observacao)
    {
        if (arquivo is null || arquivo.Length == 0)
            return Json(new { ok = false, erro = "Selecione um arquivo para enviar." });

        var ext = Path.GetExtension(arquivo.FileName ?? "").TrimStart('.').ToLowerInvariant();
        if (!ExtensoesAceitas.Contains(ext))
            return Json(new { ok = false, erro = "Formato não aceito. Envie PDF, Word (.doc/.docx) ou imagem (JPG/PNG/WebP/GIF)." });

        if (arquivo.Length > TamanhoMaximoBytes)
            return Json(new { ok = false, erro = "O arquivo excede o limite de 8 MB." });

        using var ms = new MemoryStream();
        await arquivo.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            categoria = categoria ?? "",
            entidadeId,
            nomeArquivo = arquivo.FileName,
            tipoMime = arquivo.ContentType,
            conteudoBase64 = base64,
            tamanho = (int)arquivo.Length,
            observacao
        };
        var (resp, error, status) = await SendApiAsync<object, AnexoArquivoCriadoDto>(client, HttpMethod.Post, "api/administrativo360/anexos", payload);
        if (resp is null || error is not null)
            return Json(new { ok = false, erro = status == HttpStatusCode.BadRequest ? error : "Não foi possível enviar o arquivo agora. Tente novamente." });
        return Json(new { ok = true, id = resp.Id });
    }

    [HttpPost("Remover/{id:guid}")]
    public async Task<IActionResult> Remover(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (ok, error, status) = await SendApiWithoutResponseAsync<object>(client, HttpMethod.Post, $"api/administrativo360/anexos/{id:N}/remover", new { });
        if (!ok)
            return Json(new { ok = false, erro = status == HttpStatusCode.NotFound ? "Arquivo não encontrado." : error ?? "Não foi possível remover o arquivo agora." });
        return Json(new { ok = true });
    }

    [HttpGet("Baixar/{id:guid}")]
    public async Task<IActionResult> Baixar(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        try
        {
            var resp = await client.GetAsync($"api/administrativo360/anexos/{id:N}/download");
            if (!resp.IsSuccessStatusCode)
                return File(Array.Empty<byte>(), "application/octet-stream");
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            var mime = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            string? nome = null;
            var disposicao = resp.Content.Headers.ContentDisposition?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(disposicao))
            {
                foreach (var parte in disposicao.Split(';'))
                {
                    var p = parte.Trim();
                    if (p.StartsWith("filename*=utf-8''", StringComparison.OrdinalIgnoreCase)) nome = Uri.UnescapeDataString(p["filename*=utf-8''".Length..]);
                    else if (p.StartsWith("filename=", StringComparison.OrdinalIgnoreCase)) nome = p["filename=".Length..].Trim('"');
                }
            }
            return File(bytes, mime, nome);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao baixar anexo {AnexoId} da API.", id);
            return File(Array.Empty<byte>(), "application/octet-stream");
        }
    }
}
