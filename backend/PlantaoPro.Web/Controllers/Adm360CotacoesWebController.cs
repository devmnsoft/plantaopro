using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR,ADMINISTRADOR_CLIENTE,DIRETOR,COORDENACAO,COORDENADOR")]
[Route("Administrativo360/Cotacoes")]
public sealed class Adm360CotacoesWebController : BaseWebController
{
    public Adm360CotacoesWebController(IHttpClientFactory factory, ILogger<Adm360CotacoesWebController> logger)
        : base(factory, logger) { }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? status, [FromQuery] string? provedor)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"api/administrativo360/cotacoes?status={Uri.EscapeDataString(status ?? "")}&provedor={Uri.EscapeDataString(provedor ?? "")}";
        var resp = await ReadApiResponse<IReadOnlyList<CotacaoResumoViewModel>>(client, query);

        return View("~/Views/Administrativo360/Cotacoes/Index.cshtml", new CotacoesIndexViewModel
        {
            Cotacoes = resp.Data ?? Array.Empty<CotacaoResumoViewModel>(),
            Status = status,
            Provedor = provedor,
            Erro = resp.Error
        });
    }

    [HttpGet("Detalhes/{id:guid}")]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var cotacaoResp = await ReadApiResponse<CotacaoDetalhesViewModel>(client, $"api/administrativo360/cotacoes/{id}");
        if (cotacaoResp.Data is null)
        {
            TempData["Error"] = cotacaoResp.Error ?? "Cotação não encontrada.";
            return RedirectToAction(nameof(Index));
        }

        var respostasResp = await ReadApiResponse<IReadOnlyList<CotacaoRespostaViewModel>>(client, "api/administrativo360/cotacoes/respostas");
        var respostas = (respostasResp.Data ?? Array.Empty<CotacaoRespostaViewModel>()).Where(r => r.CotacaoId == id).ToList();

        return View("~/Views/Administrativo360/Cotacoes/Detalhes.cshtml", new CotacaoDetalhesPageViewModel
        {
            Cotacao = cotacaoResp.Data,
            Respostas = respostas,
            Erro = cotacaoResp.Error,
            Sucesso = TempData["Sucesso"]?.ToString()
        });
    }

    [HttpGet("Relacionamento")]
    public async Task<IActionResult> Relacionamento([FromQuery] string? provedor, [FromQuery] string? tipoEntidade)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"api/administrativo360/cotacoes/mapeamentos?provedor={Uri.EscapeDataString(provedor ?? "")}&tipoEntidade={Uri.EscapeDataString(tipoEntidade ?? "")}";
        var resp = await ReadApiResponse<IReadOnlyList<MapeamentoDeParaViewModel>>(client, query);

        return View("~/Views/Administrativo360/Cotacoes/Relacionamento.cshtml", new RelacionamentoMapeamentoViewModel
        {
            Mapeamentos = resp.Data ?? Array.Empty<MapeamentoDeParaViewModel>(),
            Provedor = provedor,
            TipoEntidade = tipoEntidade,
            Erro = resp.Error
        });
    }

    [HttpPost("SalvarMapeamento")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarMapeamento([FromForm] SalvarMapeamentoFormModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            form.PortalContaId,
            form.Provedor,
            form.TipoEntidade,
            form.CodigoExterno,
            form.DescricaoExterna,
            form.EntidadeInternaId,
            form.EntidadeInternaDescricao,
            form.FatorConversao
        };

        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/cotacoes/mapeamentos", payload);
        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao salvar mapeamento.";
        }
        else
        {
            TempData["Sucesso"] = "Mapeamento salvo com sucesso.";
        }

        return RedirectToAction(nameof(Relacionamento));
    }

    [HttpPost("RelacionarItem")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RelacionarItem([FromForm] RelacionarItemFormModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            form.CotacaoItemId,
            form.ProdutoId,
            form.FatorConversao,
            form.PrecoUnitarioOfertado,
            form.Desconto,
            form.MaterialOfertado,
            form.JustificativaSubstituicao,
            form.StatusRelacionamento,
            form.MotivoNaoAtendimento
        };

        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/cotacoes/relacionar-item", payload);
        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao relacionar item.";
        }
        else
        {
            TempData["Sucesso"] = "Item relacionado com sucesso.";
        }

        return RedirectToAction(nameof(Detalhes), new { id = form.CotacaoId });
    }

    [HttpPost("GerarOrcamento/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GerarOrcamento(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, dynamic>(client, HttpMethod.Post, $"api/administrativo360/cotacoes/{id}/gerar-orcamento", new { });
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? "Falha ao gerar orçamento.";
        }
        else
        {
            TempData["Sucesso"] = "Orçamento cirúrgico gerado com sucesso.";
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost("AprovarResposta/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AprovarResposta(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, dynamic>(client, HttpMethod.Post, $"api/administrativo360/cotacoes/{id}/aprovar-resposta", new { });
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? "Falha ao aprovar resposta.";
        }
        else
        {
            TempData["Sucesso"] = "Resposta aprovada e colocada na fila de transmissão.";
        }

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpPost("TransmitirResposta/{respostaId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransmitirResposta(Guid respostaId, [FromQuery] Guid cotacaoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, $"api/administrativo360/cotacoes/respostas/{respostaId}/transmitir", new { });
        if (!success)
        {
            TempData["Error"] = error ?? "Falha na transmissão da resposta.";
        }
        else
        {
            TempData["Sucesso"] = "Tentativa de transmissão executada.";
        }

        return RedirectToAction(nameof(Detalhes), new { id = cotacaoId });
    }

    [HttpGet("ContasPortal")]
    public async Task<IActionResult> ContasPortal()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var contasResp = await ReadApiResponse<IReadOnlyList<PortalContaViewModel>>(client, "api/administrativo360/cotacoes/contas-portal");
        var estResp = await ReadApiResponse<IReadOnlyList<EstabelecimentoViewModel>>(client, "api/administrativo360/cotacoes/estabelecimentos");

        return View("~/Views/Administrativo360/Cotacoes/ContasPortal.cshtml", new PortalContasConfigViewModel
        {
            Contas = contasResp.Data ?? Array.Empty<PortalContaViewModel>(),
            Estabelecimentos = estResp.Data ?? Array.Empty<EstabelecimentoViewModel>(),
            Erro = contasResp.Error ?? estResp.Error
        });
    }

    [HttpPost("ConfigurarContaPortal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigurarContaPortal([FromForm] ConfigurarPortalContaFormModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            form.EstabelecimentoId,
            form.Provedor,
            form.NomeConta,
            form.IdentificadorExterno,
            form.UsuarioAcesso,
            form.SegredoReferencia,
            form.Ambiente
        };

        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/cotacoes/contas-portal", payload);
        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao configurar conta de portal.";
        }
        else
        {
            TempData["Sucesso"] = "Conta de portal configurada com sucesso.";
        }

        return RedirectToAction(nameof(ContasPortal));
    }

    [HttpGet("Estabelecimentos")]
    public async Task<IActionResult> Estabelecimentos()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<EstabelecimentoViewModel>>(client, "api/administrativo360/cotacoes/estabelecimentos");
        return View("~/Views/Administrativo360/Cotacoes/Estabelecimentos.cshtml", new EstabelecimentosPageViewModel
        {
            Estabelecimentos = resp.Data ?? Array.Empty<EstabelecimentoViewModel>(),
            Erro = resp.Error
        });
    }

    [HttpPost("CriarEstabelecimento")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarEstabelecimento([FromForm] CriarEstabelecimentoFormModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            form.Cnpj,
            form.RazaoSocial,
            form.NomeFantasia,
            form.InscricaoEstadual,
            form.Cnae,
            form.Ambiente
        };

        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/cotacoes/estabelecimentos", payload);
        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao cadastrar estabelecimento.";
        }
        else
        {
            TempData["Sucesso"] = "Estabelecimento cadastrado com sucesso.";
        }

        return RedirectToAction(nameof(Estabelecimentos));
    }

    [HttpGet("{id:guid}/Anexos/{anexoId:guid}")]
    public async Task<IActionResult> DownloadAnexo(Guid id, Guid anexoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var response = await client.GetAsync($"api/administrativo360/cotacoes/{id}/anexos/{anexoId}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Não foi possível baixar o anexo.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        var content = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName
                       ?? $"anexo_{anexoId}";

        return File(content, contentType, fileName.Trim('"'));
    }
}
