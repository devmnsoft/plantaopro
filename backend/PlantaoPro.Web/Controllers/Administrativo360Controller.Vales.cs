using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Vales de transporte de pacientes (custodia externa), separacao, expedicao e rastreio
    [HttpGet]
    public async Task<IActionResult> Vales(string? busca, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (inicio.HasValue) query.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim:yyyy-MM-dd}");

        var path = "api/administrativo360/vales" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var res = await ReadApiResponse<List<ValeResumoViewModel>>(client, path);

        ViewBag.Busca = busca;
        ViewBag.Situacao = situacao;
        ViewBag.Inicio = inicio;
        ViewBag.Fim = fim;
        ViewBag.Erro = res.Error;

        return View(res.Data ?? new List<ValeResumoViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> ValeNovo(Guid? orcamentoId, Guid? cirurgiaId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var model = new ValeFormViewModel
        {
            OrcamentoId = orcamentoId,
            CirurgiaId = cirurgiaId
        };

        if (cirurgiaId.HasValue)
        {
            var res = await ReadApiResponse<CirurgiaDetalhesViewModel>(client, $"api/administrativo360/cirurgias/{cirurgiaId.Value}");
            var cirurgia = res.Data;
            if (cirurgia is not null)
            {
                model.HospitalId = cirurgia.HospitalId;
                model.LocalDestinoId = cirurgia.LocalDestinoId;
                model.OrcamentoId ??= cirurgia.OrcamentoId;
                model.OrcamentoRevisao = cirurgia.OrcamentoRevisao;
                model.DataSaidaPrevista = cirurgia.DataPrevista;
                model.DataRetornoPrevista = cirurgia.DataPrevista.AddDays(7);
            }
        }
        else if (orcamentoId.HasValue)
        {
            var res = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{orcamentoId.Value}");
            var orc = res.Data;
            if (orc is not null)
            {
                model.HospitalId = orc.HospitalId;
                model.OrcamentoRevisao = orc.Revisao;
                model.DataSaidaPrevista = orc.DataPrevista;
                model.DataRetornoPrevista = orc.DataPrevista.AddDays(7);
            }
        }

        await PreencherLookupsValeAsync(client, model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeNovo(ValeFormViewModel model)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (model.Itens.Count == 0)
        {
            TempData["Error"] = "Adicione ao menos um item ao vale de consignação.";
            await PreencherLookupsValeAsync(client, model);
            return View(model);
        }

        var payload = new
        {
            model.CirurgiaId,
            model.OrcamentoId,
            model.OrcamentoRevisao,
            model.HospitalId,
            model.CustodianteId,
            model.LocalOrigemId,
            model.LocalDestinoId,
            model.DataSaidaPrevista,
            model.DataRetornoPrevista,
            model.Observacoes,
            Itens = model.Itens.Select(i => new
            {
                i.ProdutoId,
                i.LoteId,
                i.ReservaId,
                i.QuantidadeSolicitada,
                i.PrecoUnitario
            }).ToList(),
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/vales", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["Success"] = "Vale de consignação criado com sucesso.";
            return RedirectToAction(nameof(Vales));
        }

        TempData["Error"] = resp.Error ?? "Falha ao criar vale de consignação.";
        await PreencherLookupsValeAsync(client, model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ValeDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<ValeDetalhesViewModel>(client, $"api/administrativo360/vales/{id}");
        if (resp.Data is null) return NotFound();

        return View(resp.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeSepararItem(Guid id, Guid itemId, decimal quantidade)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { ValeItemId = itemId, QuantidadeSeparada = quantidade };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/separar-item", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = "Conferência do item registrada.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao registrar conferência do item.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeConcluirSeparacao(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/concluir-separacao", new { });

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = "Separação de materiais concluída. Vale pronto para expedição.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao concluir separação de materiais.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeExpedir(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { ValeId = id, IdempotencyKey = Guid.NewGuid().ToString("N") };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/expedir", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = "Vale expedido com sucesso! Materiais transferidos para custódia externa no hospital.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao expedir vale.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeEvento(Guid id, Guid itemId, string tipo, decimal quantidade, string? motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var endpoint = tipo switch
        {
            "CONSUMO" => $"api/administrativo360/vales/{id}/consumo",
            "RETORNO" => $"api/administrativo360/vales/{id}/retorno",
            "PERDA" => $"api/administrativo360/vales/{id}/perda",
            _ => throw new ArgumentException("Tipo de evento inválido.")
        };

        var payload = new
        {
            ValeId = id,
            ValeItemId = itemId,
            Quantidade = quantidade,
            Motivo = motivo,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, endpoint, payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = $"Evento de {tipo} registrado com sucesso.";
        else
            TempData["Error"] = resp.Error ?? $"Falha ao registrar evento de {tipo}.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeReconciliar(Guid id, string? observacoes)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            ValeId = id,
            Observacoes = observacoes,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/reconciliar", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = "Vale reconciliado com sucesso! Pronto para valorização no próximo incremento.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao reconciliar vale.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeCancelar(Guid id, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { ValeId = id, Motivo = motivo };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/cancelar", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["Success"] = "Vale cancelado com sucesso.";
        else
            TempData["Error"] = resp.Error ?? "Falha ao cancelar vale.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> ValeImprimir(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<ValeDetalhesViewModel>(client, $"api/administrativo360/vales/{id}");
        if (resp.Data is null) return NotFound();

        return View(resp.Data);
    }

    // ==========================================
    // RELATÓRIOS FUNCIONAIS E EXPORTAÇÃO CSV
    // ==========================================
}
