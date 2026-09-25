using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR,ADMINISTRADOR_CLIENTE,DIRETOR,COORDENACAO,COORDENADOR")]
[Route("Administrativo360/DocumentosXml")]
public sealed class Adm360DocumentosXmlWebController : BaseWebController
{
    public Adm360DocumentosXmlWebController(IHttpClientFactory factory, ILogger<Adm360DocumentosXmlWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? status, [FromQuery] bool? quarentena)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"api/administrativo360/xml?status={Uri.EscapeDataString(status ?? "")}";
        if (quarentena.HasValue) query += $"&quarentena={quarentena.Value.ToString().ToLowerInvariant()}";

        var resp = await ReadApiResponse<IReadOnlyList<DocumentoRecebidoResumoViewModel>>(client, query);

        return View("~/Views/Administrativo360/DocumentosXml/Index.cshtml", new DocumentosXmlIndexViewModel
        {
            Documentos = resp.Data ?? Array.Empty<DocumentoRecebidoResumoViewModel>(),
            Status = status,
            Quarentena = quarentena,
            Erro = resp.Error
        });
    }

    [HttpGet("Detalhes/{id:guid}")]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<DocumentoRecebidoDetalhesViewModel>(client, $"api/administrativo360/xml/{id}");
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? "Documento não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        return View("~/Views/Administrativo360/DocumentosXml/Detalhes.cshtml", new DocumentoXmlDetalhesPageViewModel
        {
            Documento = resp.Data,
            Erro = resp.Error,
            Sucesso = TempData["Sucesso"]?.ToString()
        });
    }

    [HttpGet("Importar")]
    public IActionResult Importar()
    {
        return View("~/Views/Administrativo360/DocumentosXml/Importar.cshtml", new ImportarXmlManualFormModel());
    }

    [HttpPost("Importar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Importar([FromForm] ImportarXmlManualFormModel form, IFormFile? arquivoXml)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var xmlConteudo = form.XmlConteudo;
        var nomeArquivo = form.NomeArquivo;

        if (arquivoXml is not null && arquivoXml.Length > 0)
        {
            using var reader = new StreamReader(arquivoXml.OpenReadStream(), Encoding.UTF8);
            xmlConteudo = await reader.ReadToEndAsync();
            nomeArquivo = arquivoXml.FileName;
        }

        if (string.IsNullOrWhiteSpace(xmlConteudo))
        {
            ModelState.AddModelError("", "Forneça o conteúdo XML ou selecione um arquivo.");
            return View("~/Views/Administrativo360/DocumentosXml/Importar.cshtml", form);
        }

        var payload = new { XmlConteudo = xmlConteudo, NomeArquivo = nomeArquivo };
        var resp = await SendApiAsync<object, dynamic>(client, HttpMethod.Post, "api/administrativo360/xml/importar-manual", payload);

        if (resp.Data is null)
        {
            ModelState.AddModelError("", resp.Error ?? "Falha ao importar XML.");
            return View("~/Views/Administrativo360/DocumentosXml/Importar.cshtml", form);
        }

        TempData["Sucesso"] = "XML importado e analisado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("VincularRecebimento")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VincularRecebimento([FromForm] VincularRecebimentoFormModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { form.DocumentoId, form.PedidoId, form.LocalId };
        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/xml/vincular-recebimento", payload);

        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao vincular documento ao recebimento.";
        }
        else
        {
            TempData["Sucesso"] = "Documento vinculado ao recebimento de compra com sucesso.";
        }

        return RedirectToAction(nameof(Detalhes), new { id = form.DocumentoId });
    }

    [HttpPost("Manifestar/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Manifestar(Guid id, [FromForm] string tipoManifestacao, [FromForm] string? justificativa)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { DocumentoId = id, TipoManifestacao = tipoManifestacao, Justificativa = justificativa };
        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/xml/manifestar", payload);

        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao registrar manifestação fiscal.";
        }
        else
        {
            TempData["Sucesso"] = "Manifestação registrada com sucesso.";
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpGet("Sincronizacao")]
    public async Task<IActionResult> Sincronizacao()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var syncResp = await ReadApiResponse<IReadOnlyList<DfeSincronizacaoViewModel>>(client, "api/administrativo360/xml/sincronizacoes");
        var estResp = await ReadApiResponse<IReadOnlyList<EstabelecimentoViewModel>>(client, "api/administrativo360/cotacoes/estabelecimentos");

        return View("~/Views/Administrativo360/DocumentosXml/Sincronizacao.cshtml", new SincronizacaoDfeViewModel
        {
            Sincronizacoes = syncResp.Data ?? Array.Empty<DfeSincronizacaoViewModel>(),
            Estabelecimentos = estResp.Data ?? Array.Empty<EstabelecimentoViewModel>(),
            Erro = syncResp.Error ?? estResp.Error
        });
    }

    [HttpPost("ExecutarSincronizacao")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecutarSincronizacao([FromForm] Guid estabelecimentoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { EstabelecimentoId = estabelecimentoId };
        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/xml/sincronizar-dfe", payload);

        if (!success)
        {
            TempData["Error"] = error ?? "Falha na sincronização DF-e.";
        }
        else
        {
            TempData["Sucesso"] = "Consulta DF-e executada com sucesso.";
        }

        return RedirectToAction(nameof(Sincronizacao));
    }

    [HttpGet("Download/{id:guid}")]
    public async Task<IActionResult> DownloadXml(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var response = await client.GetAsync($"api/administrativo360/xml/{id}/download");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Não foi possível baixar o XML.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        var content = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName
                       ?? $"nfe_{id}.xml";

        return File(content, "application/xml", fileName.Trim('"'));
    }
}
