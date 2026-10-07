using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
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
            Erro = resp.Error
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

        // P0 (homologação): limites validados ANTES de ler integralmente o upload (memória previsível).
        const long maxArquivoXmlBytes = 2 * 1024 * 1024;   // 2 MiB por arquivo
        const int maxConteudoXmlChars = 2_000_000;         // limite espelhado na API

        if (arquivoXml is not null && arquivoXml.Length > maxArquivoXmlBytes)
        {
            ModelState.AddModelError("", "O arquivo XML excede o limite de 2 MiB.");
            return View("~/Views/Administrativo360/DocumentosXml/Importar.cshtml", form);
        }

        var xmlConteudo = form.XmlConteudo;
        var nomeArquivo = form.NomeArquivo;
        byte[]? xmlBytes = null;

        if (arquivoXml is not null && arquivoXml.Length > 0)
        {
            // B1: lê os bytes exatos do arquivo (preserva BOM/encoding) e os envia junto ao payload.
            using var ms = new MemoryStream();
            await arquivoXml.CopyToAsync(ms);
            xmlBytes = ms.ToArray();
            xmlConteudo = Encoding.UTF8.GetString(xmlBytes);
            nomeArquivo = arquivoXml.FileName;
        }

        if (!string.IsNullOrWhiteSpace(xmlConteudo) && xmlConteudo.Length > maxConteudoXmlChars)
        {
            ModelState.AddModelError("", "O conteúdo XML excede o limite permitido (2.000.000 caracteres).");
            return View("~/Views/Administrativo360/DocumentosXml/Importar.cshtml", form);
        }

        if (string.IsNullOrWhiteSpace(xmlConteudo))
        {
            ModelState.AddModelError("", "Forneça o conteúdo XML ou selecione um arquivo.");
            return View("~/Views/Administrativo360/DocumentosXml/Importar.cshtml", form);
        }

        // B1: envia os bytes originais do arquivo (base64) — a API os preserva como fonte da verdade.
        var payload = new { XmlConteudo = xmlConteudo, NomeArquivo = nomeArquivo, XmlBytes = xmlBytes };
        var resp = await SendApiAsync<object, ImportarXmlResultadoViewModel>(client, HttpMethod.Post, "api/administrativo360/xml/importar-manual", payload);

        if (resp.Data is null)
        {
            ModelState.AddModelError("", resp.Error ?? "Falha ao importar XML.");
            return View("~/Views/Administrativo360/DocumentosXml/Importar.cshtml", form);
        }

        // A3: mensagem derivada do resultado tipado por unidade (um arquivo pode conter múltiplos documentos).
        var r = resp.Data;
        var partes = new List<string> { $"{r.Importados} importado(s)" };
        if (r.EmQuarentena > 0) partes.Add($"{r.EmQuarentena} em quarentena");
        if (r.DuplicadosIgnorados > 0) partes.Add($"{r.DuplicadosIgnorados} duplicado(s) ignorado(s)");
        if (r.Falhas > 0) partes.Add($"{r.Falhas} falha(s)");
        TempData["Success"] = $"XML processado: {r.TotalUnidades} documento(s) encontrado(s) — {string.Join(", ", partes)}.";
        var primeiraFalha = r.Documentos.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.MensagemErro));
        if (primeiraFalha is not null)
            TempData["Error"] = $"Erro de importação: {primeiraFalha.MensagemErro}";

        return RedirectToAction(nameof(Index));
    }

    // A3: conferência autorizada do documento (gátes do registro de estoque em recebimentos).
    [HttpPost("Conferir/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Conferir(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/xml/conferir", new { DocumentoId = id });

        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao confirmar a conferência do documento.";
        }
        else
        {
            TempData["Success"] = "Conferência do documento autorizada com sucesso.";
        }

        return RedirectToAction(nameof(Detalhes), new { id });
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
            TempData["Success"] = "Documento vinculado ao recebimento de compra com sucesso.";
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
            TempData["Success"] = "Manifestação registrada com sucesso.";
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    // C11.4: fila de triagens abertas (documentos em quarentena, vencidas primeiro).
    [HttpGet("Triagens")]
    public async Task<IActionResult> Triagens()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var triagensResp = await ReadApiResponse<IReadOnlyList<TriagemDocumentoViewModel>>(client, "api/administrativo360/xml/triagens");
        var usuariosResp = await ReadApiResponse<IReadOnlyList<UsuarioResponsavelViewModel>>(client, "api/usuarios");

        return View("~/Views/Administrativo360/DocumentosXml/Triagens.cshtml", new TriagensFilaViewModel
        {
            Triagens = triagensResp.Data ?? Array.Empty<TriagemDocumentoViewModel>(),
            Usuarios = usuariosResp.Data ?? Array.Empty<UsuarioResponsavelViewModel>(),
            Erro = triagensResp.Error ?? usuariosResp.Error
        });
    }

    // C11.4: abre (ou reabre) a triagem — atribui responsável, prazo e observação.
    // O formulário envia prazo em horário local (datetime-local); convertemos para UTC aqui,
    // pois a API valida e grava PrazoUtc (validação de tenant do responsável fica na API).
    [HttpPost("Triagens/Abrir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AbrirTriagem([FromForm] Guid documentoId, [FromForm] Guid responsavelId, [FromForm] DateTimeOffset prazoLocal, [FromForm] string? observacao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { DocumentoId = documentoId, ResponsavelId = responsavelId, PrazoUtc = prazoLocal.UtcDateTime, Observacao = observacao };
        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/xml/triagens/abrir", payload);

        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao abrir a triagem.";
        }
        else
        {
            TempData["Success"] = "Triagem aberta: responsável e prazo atribuídos ao documento.";
        }

        return RedirectToAction(nameof(Triagens));
    }

    // C11.4: resolve a triagem — o documento sai da quarentena (exige responsável já atribuído).
    [HttpPost("Triagens/Resolver")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolverTriagem([FromForm] Guid documentoId, [FromForm] string justificativa)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/xml/triagens/resolver", new { DocumentoId = documentoId, Justificativa = justificativa });

        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao resolver a triagem.";
        }
        else
        {
            TempData["Success"] = "Triagem resolvida: documento liberado da quarentena (a conferência segue decisão separada).";
        }

        return RedirectToAction(nameof(Triagens));
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
            TempData["Success"] = "Consulta DF-e executada com sucesso.";
        }

        return RedirectToAction(nameof(Sincronizacao));
    }

    [HttpGet("Download/{id:guid}")]
    public async Task<IActionResult> DownloadXml(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        // P4: download idempotente — repassa If-None-Match à API e respeita 304 (não re-baixa o corpo).
        using var pedido = new HttpRequestMessage(HttpMethod.Get, $"api/administrativo360/xml/{id}/download");
        var ifNoneMatchXml = Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(ifNoneMatchXml))
            pedido.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatchXml);
        var response = await client.SendAsync(pedido);

        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            if (response.Headers.ETag != null) HttpContext.Response.Headers.ETag = response.Headers.ETag.Tag;
            return StatusCode(StatusCodes.Status304NotModified);
        }

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Não foi possível baixar o XML.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        var content = await response.Content.ReadAsByteArrayAsync();
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName
                       ?? $"nfe_{id}.xml";

        if (response.Headers.ETag != null) HttpContext.Response.Headers.ETag = response.Headers.ETag.Tag;
        return File(content, "application/xml", fileName.Trim('"'));
    }
}
