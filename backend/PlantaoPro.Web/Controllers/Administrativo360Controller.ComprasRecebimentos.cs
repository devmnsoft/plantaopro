using Microsoft.AspNetCore.Mvc;
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
            TempData["Error"] = "Fornecedor e ao menos um produto são obrigatórios.";
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
            TempData["Error"] = "Informe ao menos um produto com quantidade maior que zero.";
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
            TempData["Success"] = "Pedido de compra cadastrado com sucesso em rascunho.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao criar pedido de compra.";

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
            TempData["Success"] = "Pedido de compra aprovado com sucesso. Valores congelados.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao aprovar pedido de compra.";

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

        if (pedidoId == Guid.Empty || string.IsNullOrWhiteSpace(documento) || quantidade <= 0 || localId == Guid.Empty)
        {
            TempData["Error"] = "Pedido, documento da nota fiscal, quantidade e local de destino são obrigatórios.";
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
            TempData["Success"] = "Recebimento confirmado com sucesso. Material direcionado para quarentena/inspeção e obrigação gerada no financeiro.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao registrar recebimento.";

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
            TempData["Error"] = "Informe uma quantidade aprovada ou reprovada.";
            return RedirectToAction(nameof(Inspecoes));
        }

        if (reprovada > 0 && string.IsNullOrWhiteSpace(justificativa))
        {
            TempData["Error"] = "A reprovação exige justificativa técnica obrigatória.";
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
            TempData["Success"] = "Decisão de inspeção registrada com sucesso. Material liberado para estoque conforme quantidade aprovada.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao registrar decisão da inspeção.";

        return RedirectToAction(nameof(Inspecoes));
    }

    [HttpGet]
    public async Task<IActionResult> Ocorrencias()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<InspecaoPendenteViewModel>>(client, "api/administrativo360/qualidade/pendentes");
        var lista = (resp.Data ?? Array.Empty<InspecaoPendenteViewModel>()).Select(p => new OcorrenciaViewModel(
            p.RecebimentoItemId, "QUARENTENA_RECEBIMENTO",
            $"Item em quarentena aguardando laudo de qualidade: {p.Produto} (Lote: {p.Lote})",
            p.Pendente, "ABERTA", DateOnly.FromDateTime(DateTime.Today.AddDays(2)), p.Local, DateTimeOffset.UtcNow
        )).ToList();

        return View(new OcorrenciasIndexViewModel
        {
            Ocorrencias = lista,
            Erro = resp.Error
        });
    }

    // ==========================================
    // ESTOQUE, LOTES E MOVIMENTAÇÕES
    // ==========================================
}
