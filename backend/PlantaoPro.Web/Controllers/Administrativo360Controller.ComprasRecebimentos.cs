using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Localization;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Pedidos de compra, recebimentos, inspecoes e ocorrencias de fornecedor
    [HttpGet]
    public async Task<IActionResult> PedidosCompra(string? fornecedor, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(fornecedor)) q.Add($"fornecedor={Uri.EscapeDataString(fornecedor)}");
        if (!string.IsNullOrWhiteSpace(situacao) && situacao != "Todas") q.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (inicio.HasValue) q.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) q.Add($"fim={fim:yyyy-MM-dd}");

        var query = "api/administrativo360/compras" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
        var resp = await ReadApiResponse<IReadOnlyList<PedidoCompraResumoViewModel>>(client, query);
        var lookups = await CarregarLookupsAsync(client);

        return View(new PedidosCompraIndexViewModel
        {
            Pedidos = resp.Data ?? Array.Empty<PedidoCompraResumoViewModel>(),
            Fornecedores = lookups.Parceiros.Where(p => p.Fornecedor).ToArray(),
            Produtos = lookups.Produtos,
            Fornecedor = fornecedor,
            Situacao = situacao,
            Inicio = inicio,
            Fim = fim,
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarPedidoCompra(Guid fornecedorId, DateOnly? previsao, decimal frete, Guid[] produtoId, decimal[] quantidade, decimal[] precoUnitario, decimal[] desconto)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (fornecedorId == Guid.Empty || produtoId == null || produtoId.Length == 0)
        {
            TempData["Error"] = UiText.L("Adm360.Compras.ErrSupplierProductRequired");
            return RedirectToAction(nameof(PedidosCompra));
        }

        var itens = new List<object>();
        for (int i = 0; i < produtoId.Length; i++)
        {
            var pId = produtoId[i];
            var qtd = quantidade != null && i < quantidade.Length ? quantidade[i] : 0;
            var preco = precoUnitario != null && i < precoUnitario.Length ? precoUnitario[i] : 0;
            var desc = desconto != null && i < desconto.Length ? desconto[i] : 0;
            if (pId != Guid.Empty && qtd > 0)
            {
                itens.Add(new { ProdutoId = pId, Quantidade = qtd, PrecoUnitario = preco, Desconto = desc });
            }
        }

        if (itens.Count == 0)
        {
            TempData["Error"] = UiText.L("Adm360.Compras.ErrZeroQuantity");
            return RedirectToAction(nameof(PedidosCompra));
        }

        var payload = new
        {
            FornecedorId = fornecedorId,
            Previsao = previsao,
            Frete = frete,
            Itens = itens
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/compras", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["Success"] = UiText.L("Adm360.Compras.OkOrderDraft");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Compras.FallbackCreateOrder");

        return RedirectToAction(nameof(PedidosCompra));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AprovarPedidoCompra(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", $"APROV-{id:N}");

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/compras/{id}/aprovar", new { });
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = UiText.L("Adm360.Compras.OkOrderApproved");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Compras.FallbackApproveOrder");

        return RedirectToAction(nameof(PedidosCompra));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarPedidoCompra(Guid id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["Error"] = UiText.L("Adm360.Compras.ErrCancelMotivo");
            return RedirectToAction(nameof(PedidosCompra));
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        // R8-B4: cancelamento com compensação server-side; 409 fundamentado volta como resp.Error
        // (estoque já consumido/transferido/inspecionado, reserva ativa ou título pago — exige estorno/ajuste prévio).
        var resp = await SendApiAsync<string, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/compras/{id}/cancelar", motivo);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = UiText.L("Adm360.Compras.OkOrderCancelled");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Compras.FallbackCancelOrder");

        return RedirectToAction(nameof(PedidosCompra));
    }

    // ==========================================
    // RECEBIMENTOS E CONFERÊNCIA
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Recebimentos()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var respAprov = await ReadApiResponse<IReadOnlyList<PedidoCompraResumoViewModel>>(client, "api/administrativo360/compras?situacao=APROVADO");
        var respParc = await ReadApiResponse<IReadOnlyList<PedidoCompraResumoViewModel>>(client, "api/administrativo360/compras?situacao=PARCIAL");
        var lookups = await CarregarLookupsAsync(client);

        // A3: documentos XML fora de quarentena — vinculação opcional ao recebimento.
        var respXml = await ReadApiResponse<IReadOnlyList<DocumentoRecebidoResumoViewModel>>(client, "api/administrativo360/xml?quarentena=false");

        var list = new List<PedidoCompraResumoViewModel>();
        if (respAprov.Data != null) list.AddRange(respAprov.Data);
        if (respParc.Data != null) list.AddRange(respParc.Data);

        return View(new RecebimentosIndexViewModel
        {
            PedidosPendentes = list,
            Locais = lookups.Locais.Where(l => l.Tipo == "INTERNO").ToArray(),
            DocumentosXml = respXml.Data ?? Array.Empty<DocumentoRecebidoResumoViewModel>(),
            Erro = respAprov.Error ?? respParc.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarRecebimento(Guid pedidoId, string documento, Guid pedidoItemId, decimal quantidade, string? lote, DateOnly? validade, Guid localId, Guid? documentoXmlId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (pedidoId == Guid.Empty || pedidoItemId == Guid.Empty || string.IsNullOrWhiteSpace(documento) || quantidade <= 0 || localId == Guid.Empty)
        {
            TempData["Error"] = UiText.L("Adm360.Compras.ErrReceiptRequiredFields");
            return RedirectToAction(nameof(Recebimentos));
        }

        var key = $"REC-{Guid.NewGuid():N}";
        var payload = new
        {
            PedidoId = pedidoId,
            Documento = documento.Trim(),
            IdempotencyKey = key,
            DocumentoXmlId = documentoXmlId,
            Itens = new[]
            {
                new
                {
                    PedidoItemId = pedidoItemId,
                    Quantidade = quantidade,
                    Lote = string.IsNullOrWhiteSpace(lote) ? "LOTE-PADRAO" : lote.Trim(),
                    Validade = validade,
                    LocalId = localId
                }
            }
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/compras/recebimentos", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["Success"] = UiText.L("Adm360.Compras.OkReceiptConfirmed");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Compras.FallbackRegisterReceipt");

        return RedirectToAction(nameof(Recebimentos));
    }

    // ==========================================
    // QUALIDADE E INSPEÇÕES
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Inspecoes()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<InspecaoPendenteViewModel>>(client, "api/administrativo360/qualidade/pendentes");

        return View(new InspecoesIndexViewModel
        {
            Pendentes = resp.Data ?? Array.Empty<InspecaoPendenteViewModel>(),
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DecidirInspecao(Guid recebimentoItemId, decimal aprovada, decimal reprovada, string? justificativa, string? destino)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (aprovada + reprovada <= 0)
        {
            TempData["Error"] = UiText.L("Adm360.Compras.ErrInspectionQuantity");
            return RedirectToAction(nameof(Inspecoes));
        }

        if (reprovada > 0 && string.IsNullOrWhiteSpace(justificativa))
        {
            TempData["Error"] = UiText.L("Adm360.Compras.ErrRejectionJustification");
            return RedirectToAction(nameof(Inspecoes));
        }

        var payload = new
        {
            RecebimentoItemId = recebimentoItemId,
            Aprovada = aprovada,
            Reprovada = reprovada,
            Justificativa = string.IsNullOrWhiteSpace(justificativa) ? "Inspeção visual e documental conforme normas da Qualidade" : justificativa.Trim(),
            Destino = string.IsNullOrWhiteSpace(destino) ? "ALMOXARIFADO_LIBERADO" : destino.Trim(),
            IdempotencyKey = $"INSP-{Guid.NewGuid():N}"
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/qualidade/decisoes", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = UiText.L("Adm360.Compras.OkInspectionDecision");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Compras.FallbackRegisterInspection");

        return RedirectToAction(nameof(Inspecoes));
    }

    // ==========================================
    // OCORRÊNCIAS E NÃO CONFORMIDADES (A12)
    // r10b1: lista e comandos sobre a tabela adm360_ocorrencias (antes a página mostrava
    // apenas linhas virtuais derivadas de quarentena pendente — nada persistia).
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Ocorrencias(string? situacao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<Ocorrencia360ViewModel>>(client, "api/administrativo360/ocorrencias");
        var lookups = await CarregarLookupsAsync(client);

        return View(new OcorrenciasIndexViewModel
        {
            Ocorrencias = resp.Data ?? Array.Empty<Ocorrencia360ViewModel>(),
            Produtos = (lookups.Produtos ?? Array.Empty<Produto360ViewModel>()).Where(p => p.Ativo).ToArray(),
            Lotes = lookups.Lotes ?? Array.Empty<Lote360ViewModel>(),
            Locais = (lookups.Locais ?? Array.Empty<Local360ViewModel>()).Where(l => l.Tipo == "INTERNO").ToArray(),
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarOcorrencia(Guid produtoId, Guid loteId, Guid localId, string tipo, string descricao, decimal quantidade, DateOnly? prazo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (produtoId == Guid.Empty || loteId == Guid.Empty || localId == Guid.Empty
            || string.IsNullOrWhiteSpace(tipo) || string.IsNullOrWhiteSpace(descricao) || quantidade <= 0)
        {
            TempData["Error"] = UiText.L("Adm360.Occ.ErrRequiredFields");
            return RedirectToAction(nameof(Ocorrencias));
        }

        var payload = new
        {
            ProdutoId = produtoId,
            LoteId = loteId,
            LocalId = localId,
            Tipo = tipo.Trim(),
            Descricao = descricao.Trim(),
            Quantidade = quantidade,
            Prazo = prazo
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/ocorrencias", payload);
        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["Success"] = UiText.L("Adm360.Occ.OkCreated");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Occ.FallbackCreate");

        return RedirectToAction(nameof(Ocorrencias));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TratarOcorrencia(Guid id, long versao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/ocorrencias/{id}/tratar", new VersaoPayload(versao));
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = UiText.L("Adm360.Occ.OkTratando");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Occ.FallbackTransition");

        return RedirectToAction(nameof(Ocorrencias));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolverOcorrencia(Guid id, long versao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/ocorrencias/{id}/resolver", new VersaoPayload(versao));
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = UiText.L("Adm360.Occ.OkResolved");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Occ.FallbackTransition");

        return RedirectToAction(nameof(Ocorrencias));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EncerrarOcorrencia(Guid id, long versao, string destino, string justificativa, string efeitoFisico)
    {
        if (string.IsNullOrWhiteSpace(destino) || string.IsNullOrWhiteSpace(justificativa))
        {
            TempData["Error"] = UiText.L("Adm360.Occ.ErrCloseFields");
            return RedirectToAction(nameof(Ocorrencias));
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            Versao = versao,
            Destino = destino.Trim(),
            JustificativaEncerramento = justificativa.Trim(),
            // Rodada 15 r15b1: efeito físico canônico escolhido na UI (DEVOLUCAO/BAIXA);
            // vazio ⇒ DEVOLUCAO pela normalização do domínio (compatibilidade).
            EfeitoFisico = (efeitoFisico ?? string.Empty).Trim()
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/ocorrencias/{id}/encerrar", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = UiText.L("Adm360.Occ.OkClosed");
        else
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Occ.FallbackTransition");

        return RedirectToAction(nameof(Ocorrencias));
    }

    private sealed record VersaoPayload(long Versao);

    // ==========================================
    // ESTOQUE, LOTES E MOVIMENTAÇÕES
    // ==========================================
}
