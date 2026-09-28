using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Estoque, movimentacoes, inventarios, coleta e valorizacao de ativos
    [HttpGet]
    public async Task<IActionResult> Estoque(string? busca, string? condicao, Guid? localId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) q.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(condicao)) q.Add($"condicao={Uri.EscapeDataString(condicao)}");
        if (localId.HasValue && localId.Value != Guid.Empty) q.Add($"localId={localId.Value}");

        var query = "api/administrativo360/estoque" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
        var resp = await ReadApiResponse<IReadOnlyList<SaldoEstoqueViewModel>>(client, query);
        var lookups = await CarregarLookupsAsync(client);

        return View(new EstoqueIndexViewModel
        {
            Saldos = resp.Data ?? Array.Empty<SaldoEstoqueViewModel>(),
            Locais = lookups.Locais,
            Busca = busca,
            Condicao = condicao,
            LocalId = localId,
            Erro = resp.Error
        });
    }

    [HttpGet]
    public async Task<IActionResult> Movimentacoes()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<SaldoEstoqueViewModel>>(client, "api/administrativo360/estoque");
        var lookups = await CarregarLookupsAsync(client);

        return View(new MovimentacoesIndexViewModel
        {
            Saldos = resp.Data ?? Array.Empty<SaldoEstoqueViewModel>(),
            Locais = lookups.Locais,
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferirEstoque(Guid produtoId, Guid loteId, Guid origemId, Guid destinoId, decimal quantidade, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (origemId == destinoId)
        {
            TempData["ErrorMessage"] = "Local de origem e destino devem ser diferentes.";
            return RedirectToAction(nameof(Movimentacoes));
        }

        if (quantidade <= 0)
        {
            TempData["ErrorMessage"] = "Quantidade a transferir deve ser positiva.";
            return RedirectToAction(nameof(Movimentacoes));
        }

        var payload = new
        {
            ProdutoId = produtoId,
            LoteId = loteId,
            OrigemId = origemId,
            DestinoId = destinoId,
            Quantidade = quantidade,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? "Transferência interna entre locais de armazenagem" : motivo.Trim(),
            IdempotencyKey = $"TRANSF-{Guid.NewGuid():N}"
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/estoque/transferencias", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Transferência de estoque concluída com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao transferir estoque.";

        return RedirectToAction(nameof(Movimentacoes));
    }

    // ==========================================
    // INVENTÁRIOS E AJUSTES DE ESTOQUE
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Inventarios()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<InventarioResumoViewModel>>(client, "api/administrativo360/inventarios");
        var lookups = await CarregarLookupsAsync(client);

        return View(new InventariosIndexViewModel
        {
            Inventarios = resp.Data ?? Array.Empty<InventarioResumoViewModel>(),
            Locais = lookups.Locais,
            Produtos = lookups.Produtos,
            Lotes = lookups.Lotes,
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AbrirInventario(Guid localId, string escopo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { LocalId = localId, Escopo = string.IsNullOrWhiteSpace(escopo) ? "Contagem geral de estoque" : escopo.Trim() };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/inventarios", payload);

        if (resp.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created)
            TempData["SuccessMessage"] = "Inventário aberto com sucesso. O local está em contagem de estoque.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao abrir inventário.";

        return RedirectToAction(nameof(Inventarios));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ContarInventario(Guid inventarioId, Guid produtoId, Guid loteId, decimal quantidade, string? condicao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            ProdutoId = produtoId,
            LoteId = loteId,
            Quantidade = quantidade,
            Condicao = string.IsNullOrWhiteSpace(condicao) ? "LIBERADO" : condicao
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Put, $"api/administrativo360/inventarios/{inventarioId}/contagens", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Contagem registrada no inventário.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao registrar contagem.";

        return RedirectToAction(nameof(Inventarios));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AprovarInventario(Guid inventarioId, string? justificativa)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        client.DefaultRequestHeaders.Remove("Idempotency-Key");
        client.DefaultRequestHeaders.Add("Idempotency-Key", $"INV-APROV-{inventarioId:N}");

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("justificativa", string.IsNullOrWhiteSpace(justificativa) ? "Ajuste de inventário aprovado pela gerência" : justificativa.Trim())
        });

        var message = new HttpRequestMessage(HttpMethod.Post, $"api/administrativo360/inventarios/{inventarioId}/aprovar") { Content = formContent };
        var response = await client.SendAsync(message);

        if (response.IsSuccessStatusCode)
            TempData["SuccessMessage"] = "Inventário aprovado e ajustes de saldo consolidados com sucesso.";
        else
            TempData["ErrorMessage"] = "Falha ao aprovar inventário.";

        return RedirectToAction(nameof(Inventarios));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarInventario(Guid inventarioId, string? motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("motivo", string.IsNullOrWhiteSpace(motivo) ? "Cancelado pelo usuário" : motivo.Trim())
        });

        var message = new HttpRequestMessage(HttpMethod.Post, $"api/administrativo360/inventarios/{inventarioId}/cancelar") { Content = formContent };
        var response = await client.SendAsync(message);

        if (response.IsSuccessStatusCode)
            TempData["SuccessMessage"] = "Inventário cancelado com sucesso.";
        else
            TempData["ErrorMessage"] = "Falha ao cancelar inventário.";

        return RedirectToAction(nameof(Inventarios));
    }

    // ==========================================
    // COLETA MÓVEL
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Coleta()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<IReadOnlyList<TarefaColetaViewModel>>(client, "api/administrativo360/coleta/tarefas");

        return View(new ColetaIndexViewModel
        {
            Tarefas = resp.Data ?? Array.Empty<TarefaColetaViewModel>(),
            Erro = resp.Error
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarLeitura(Guid tarefaId, string codigo, string? lote, decimal quantidade)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (string.IsNullOrWhiteSpace(codigo) || quantidade <= 0)
        {
            TempData["ErrorMessage"] = "Código de barras e quantidade positiva são obrigatórios.";
            return RedirectToAction(nameof(Coleta));
        }

        var payload = new
        {
            TarefaId = tarefaId,
            ScanId = Guid.NewGuid(),
            Codigo = codigo.Trim(),
            Lote = lote?.Trim(),
            Quantidade = quantidade
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/coleta/leituras", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Leitura de código de barras registrada com sucesso na tarefa.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao registrar leitura.";

        return RedirectToAction(nameof(Coleta));
    }

    // ==========================================
    // ORÇAMENTOS CIRÚRGICOS E RESERVAS DE MATERIAIS
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Valorizacao(string? busca)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var path = "api/administrativo360/valorizacoes/pendentes";
        var resp = await ReadApiResponse<List<ValeResumoViewModel>>(client, path);
        var pendentes = resp.Data ?? new List<ValeResumoViewModel>();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            pendentes = pendentes.Where(v =>
                v.Numero.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                (v.Hospital != null && v.Hospital.Contains(busca, StringComparison.OrdinalIgnoreCase)) ||
                (v.CirurgiaNumero != null && v.CirurgiaNumero.Contains(busca, StringComparison.OrdinalIgnoreCase)) ||
                (v.OrcamentoNumero != null && v.OrcamentoNumero.Contains(busca, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        ViewBag.Busca = busca;
        ViewBag.Erro = resp.Error;

        return View(new ValorizacaoIndexViewModel
        {
            ValesPendentes = pendentes,
            Busca = busca
        });
    }

    [HttpGet]
    public async Task<IActionResult> ValorizacaoPrevia(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<PreviaValorizacaoViewModel>(client, $"api/administrativo360/valorizacoes/previa/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Vale não encontrado para valorização prévia.";
            return RedirectToAction(nameof(Valorizacao));
        }

        return View(new ValorizacaoPreviaViewModel
        {
            Previa = resp.Data
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarValorizacao(
        Guid valeId,
        Guid pagadorId,
        Guid? vendedorId,
        decimal descontoGeral,
        decimal comissaoPercentual,
        string condicaoPagamento,
        int quantidadeParcelas,
        string? observacoes,
        string? idempotencyKey)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        // 1. Executa a valorização
        var cmdVal = new
        {
            ValeId = valeId,
            PagadorId = pagadorId,
            VendedorId = vendedorId,
            DescontoGeral = descontoGeral,
            ComissaoPercentual = comissaoPercentual,
            IdempotencyKey = $"VAL-{key}",
            Observacoes = observacoes
        };

        var respVal = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/valorizacoes", cmdVal);

        if (respVal.StatusCode is not (System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created))
        {
            TempData["ErrorMessage"] = respVal.Error ?? "Erro ao valorizar o vale.";
            return RedirectToAction(nameof(ValorizacaoPrevia), new { id = valeId });
        }

        Guid valorizacaoId = Guid.Empty;
        if (respVal.Data.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var valId))
        {
            valorizacaoId = valId;
        }

        // 2. Confirma a venda correspondente gerando títulos de cobrança
        var cmdVenda = new
        {
            ValorizacaoId = valorizacaoId,
            CondicaoPagamento = string.IsNullOrWhiteSpace(condicaoPagamento) ? "A_VISTA" : condicaoPagamento,
            QuantidadeParcelas = quantidadeParcelas <= 0 ? 1 : quantidadeParcelas,
            IdempotencyKey = $"VEN-{key}",
            Observacoes = observacoes
        };

        var respVenda = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/vendas/confirmar", cmdVenda);

        if (respVenda.StatusCode is not (System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Created))
        {
            TempData["ErrorMessage"] = respVenda.Error ?? "Vale valorizado, mas houve erro ao gerar a venda e títulos.";
            return RedirectToAction(nameof(Vendas));
        }

        Guid vendaId = Guid.Empty;
        if (respVenda.Data.TryGetProperty("id", out var vProp) && vProp.TryGetGuid(out var vId))
        {
            vendaId = vId;
        }

        TempData["SuccessMessage"] = "Vale valorizado com sucesso e venda interna gerada!";
        return RedirectToAction(nameof(VendaDetalhes), new { id = vendaId != Guid.Empty ? vendaId : valorizacaoId });
    }
}
