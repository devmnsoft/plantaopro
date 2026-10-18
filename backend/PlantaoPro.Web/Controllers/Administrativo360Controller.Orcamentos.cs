using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Localization;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Orcamentos (ciclo completo) e reservas de itens
    [HttpGet]
    public async Task<IActionResult> Orcamentos(string? busca, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"api/administrativo360/orcamentos?busca={Uri.EscapeDataString(busca ?? "")}&situacao={Uri.EscapeDataString(situacao ?? "")}";
        if (inicio.HasValue) query += $"&inicio={inicio.Value:yyyy-MM-dd}";
        if (fim.HasValue) query += $"&fim={fim.Value:yyyy-MM-dd}";

        var resp = await ReadApiResponse<IReadOnlyList<OrcamentoResumoViewModel>>(client, query);
        ViewBag.Busca = busca;
        ViewBag.Situacao = situacao;
        ViewBag.Inicio = inicio;
        ViewBag.Fim = fim;
        ViewBag.Erro = resp.Error;

        return View(resp.Data ?? Array.Empty<OrcamentoResumoViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoNovo()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var model = new OrcamentoFormViewModel();
        await PreencherLookupsOrcamentoAsync(client, model);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoNovo(OrcamentoFormViewModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (form.HospitalId == Guid.Empty || form.ResponsavelFinanceiroId == Guid.Empty || string.IsNullOrWhiteSpace(form.Procedimento))
        {
            TempData["Error"] = UiText.L("Adm360.Orcamentos.ErrRequiredFields");
            await PreencherLookupsOrcamentoAsync(client, form);
            return View(form);
        }

        if (form.Itens.Count == 0 || form.Itens.All(x => x.Quantidade <= 0))
        {
            TempData["Error"] = UiText.L("Adm360.Orcamentos.ErrPositiveQuantity");
            await PreencherLookupsOrcamentoAsync(client, form);
            return View(form);
        }

        var payload = new
        {
            form.HospitalId,
            form.MedicoId,
            Procedimento = form.Procedimento.Trim(),
            form.ResponsavelFinanceiroId,
            form.VendedorId,
            form.DataPrevista,
            form.Validade,
            Observacoes = form.Observacoes?.Trim(),
            Itens = form.Itens.Where(i => i.Quantidade > 0).Select(i => new
            {
                i.ProdutoId,
                i.Quantidade,
                i.PrecoUnitario,
                i.Desconto
            }).ToArray()
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/orcamentos", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["Success"] = UiText.L("Adm360.Orcamentos.OkDraftCreated");
            return RedirectToAction(nameof(Orcamentos));
        }

        TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackCreate");
        await PreencherLookupsOrcamentoAsync(client, form);
        return View(form);
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoEditar(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{id}");
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackNotFound");
            return RedirectToAction(nameof(Orcamentos));
        }

        var d = resp.Data;
        var form = new OrcamentoFormViewModel
        {
            Id = d.Id,
            HospitalId = d.HospitalId,
            MedicoId = d.MedicoId,
            Procedimento = d.Procedimento,
            ResponsavelFinanceiroId = d.ResponsavelFinanceiroId,
            VendedorId = d.VendedorId,
            DataPrevista = d.DataPrevista,
            Validade = d.Validade,
            Observacoes = d.Observacoes,
            Itens = d.Itens.Select(i => new OrcamentoItemInputModel
            {
                ProdutoId = i.ProdutoId,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario,
                Desconto = i.Desconto
            }).ToList()
        };

        ViewBag.Situacao = d.Situacao;
        ViewBag.Numero = d.Numero;
        ViewBag.Revisao = d.Revisao;

        // E6 (rodada 32): espelha o NOVO — sem lookups os selects da tela de edição chegam vazios.
        await PreencherLookupsOrcamentoAsync(client, form);

        return View(form);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoEditar(Guid id, OrcamentoFormViewModel form)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            OrcamentoId = id,
            form.HospitalId,
            form.MedicoId,
            Procedimento = form.Procedimento.Trim(),
            form.ResponsavelFinanceiroId,
            form.VendedorId,
            form.DataPrevista,
            form.Validade,
            Observacoes = form.Observacoes?.Trim(),
            MotivoRevisao = form.MotivoRevisao?.Trim(),
            Itens = form.Itens.Where(i => i.Quantidade > 0).Select(i => new
            {
                i.ProdutoId,
                i.Quantidade,
                i.PrecoUnitario,
                i.Desconto
            }).ToArray()
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Put, $"api/administrativo360/orcamentos/{id}", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["Success"] = UiText.L("Adm360.Orcamentos.OkUpdated");
            return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
        }

        TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackUpdate");
        return RedirectToAction(nameof(OrcamentoEditar), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{id}");
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackNotFound");
            return RedirectToAction(nameof(Orcamentos));
        }

        var revisoes = await ReadApiResponse<IReadOnlyList<OrcamentoRevisaoHistoricoViewModel>>(client, $"api/administrativo360/orcamentos/{id}/revisoes");
        ViewBag.Revisoes = revisoes.Data ?? Array.Empty<OrcamentoRevisaoHistoricoViewModel>();

        return View(resp.Data);
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoImprimir(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{id}");
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackNotFound");
            return RedirectToAction(nameof(Orcamentos));
        }

        return View(resp.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoAprovar(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/orcamentos/{id}/aprovar", new { });
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["Success"] = UiText.L("Adm360.Orcamentos.OkApproved");
        }
        else
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackApprove");
        }

        return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoRejeitar(Guid id, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/orcamentos/{id}/rejeitar", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("motivo", motivo)
        }));

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["Success"] = UiText.L("Adm360.Orcamentos.OkRejected");
        }
        else
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackReject");
        }

        return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoCancelar(Guid id, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/orcamentos/{id}/cancelar", new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("motivo", motivo)
        }));

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["Success"] = UiText.L("Adm360.Orcamentos.OkCancelled");
        }
        else
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackCancel");
        }

        return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> OrcamentoReserva(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<PlanejamentoReservaOrcamentoViewModel>(client, $"api/administrativo360/orcamentos/{id}/reserva-planejamento");
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackNotFoundOrNotApproved");
            return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
        }

        return View(resp.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoReservarItem(Guid id, Guid produtoId, Guid loteId, Guid localId, decimal quantidade)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new
        {
            ProdutoId = produtoId,
            LoteId = loteId,
            LocalId = localId,
            Quantidade = quantidade,
            // O contrato da API exige Motivo nao-nulo em ReservarCommand; a tela de reserva nao possui campo de motivo, entao usamos um padrao descritivo.
            Motivo = "Reserva de material para orçamento cirúrgico",
            Origem = "ORCAMENTO_CIRURGICO",
            OrigemId = id,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/orcamentos/{id}/reservas", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["Success"] = UiText.L("Adm360.Orcamentos.OkMaterialReserved");
        }
        else
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackReserveMaterial");
        }

        return RedirectToAction(nameof(OrcamentoReserva), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoCancelarReserva(Guid id, Guid reservaId, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"api/administrativo360/orcamentos/{id}/reservas/{reservaId}?motivo={Uri.EscapeDataString(motivo ?? "")}";
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Delete, query, new { });

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["Success"] = UiText.L("Adm360.Orcamentos.OkReservationCancelled");
        }
        else
        {
            TempData["Error"] = resp.Error ?? UiText.L("Adm360.Orcamentos.FallbackCancelReservation");
        }

        return RedirectToAction(nameof(OrcamentoReserva), new { id });
    }

    // ==========================================
    // CADASTROS BÁSICOS
    // ==========================================
}
