using Microsoft.AspNetCore.Mvc;
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
            TempData["ErrorMessage"] = "Hospital, Responsável Financeiro e Procedimento são obrigatórios.";
            await PreencherLookupsOrcamentoAsync(client, form);
            return View(form);
        }

        if (form.Itens.Count == 0 || form.Itens.All(x => x.Quantidade <= 0))
        {
            TempData["ErrorMessage"] = "Informe ao menos um produto com quantidade positiva.";
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
            TempData["SuccessMessage"] = "Orçamento cirúrgico cadastrado em rascunho com sucesso.";
            return RedirectToAction(nameof(Orcamentos));
        }

        TempData["ErrorMessage"] = resp.Error ?? "Falha ao cadastrar orçamento.";
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
            TempData["ErrorMessage"] = resp.Error ?? "Orçamento não encontrado.";
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
            TempData["SuccessMessage"] = "Orçamento cirúrgico atualizado com sucesso.";
            return RedirectToAction(nameof(OrcamentoDetalhes), new { id });
        }

        TempData["ErrorMessage"] = resp.Error ?? "Falha ao atualizar orçamento.";
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
            TempData["ErrorMessage"] = resp.Error ?? "Orçamento não encontrado.";
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
            TempData["ErrorMessage"] = resp.Error ?? "Orçamento não encontrado.";
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
            TempData["SuccessMessage"] = "Orçamento aprovado com sucesso! A versão comercial está congelada e apta para reserva de materiais.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao aprovar orçamento.";
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
            TempData["SuccessMessage"] = "Orçamento rejeitado.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao rejeitar orçamento.";
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
            TempData["SuccessMessage"] = "Orçamento cancelado e reservas ativas liberadas.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao cancelar orçamento.";
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
            TempData["ErrorMessage"] = resp.Error ?? "Orçamento não encontrado ou ainda não aprovado para reserva.";
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
            TempData["SuccessMessage"] = "Material reservado com sucesso para a cirurgia.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao realizar reserva de material.";
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
            TempData["SuccessMessage"] = "Reserva de material cancelada com sucesso.";
        }
        else
        {
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao cancelar reserva.";
        }

        return RedirectToAction(nameof(OrcamentoReserva), new { id });
    }

    // ==========================================
    // CADASTROS BÁSICOS
    // ==========================================
}
