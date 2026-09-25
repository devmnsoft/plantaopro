using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR,ADMINISTRADOR_CLIENTE,DIRETOR,COORDENACAO,COORDENADOR")]
public sealed class Administrativo360Controller : BaseWebController
{
    public Administrativo360Controller(IHttpClientFactory factory, ILogger<Administrativo360Controller> logger)
        : base(factory, logger) { }

    public async Task<IActionResult> Index()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resumo = await ReadApiResponse<Administrativo360ResumoViewModel>(client, "api/administrativo360/resumo");
        var departamentos = await ReadApiResponse<IReadOnlyList<Departamento360ViewModel>>(client, "api/administrativo360/departamentos");
        var cargos = await ReadApiResponse<IReadOnlyList<Cargo360ViewModel>>(client, "api/administrativo360/cargos");
        var colaboradores = await ReadApiResponse<IReadOnlyList<Colaborador360ViewModel>>(client, "api/administrativo360/colaboradores");
        var contratos = await ReadApiResponse<IReadOnlyList<Contrato360ViewModel>>(client, "api/administrativo360/contratos");

        return View(new Administrativo360PageViewModel
        {
            Resumo = resumo.Data ?? new(0, 0, 0, 0),
            Departamentos = departamentos.Data ?? Array.Empty<Departamento360ViewModel>(),
            Cargos = cargos.Data ?? Array.Empty<Cargo360ViewModel>(),
            Colaboradores = colaboradores.Data ?? Array.Empty<Colaborador360ViewModel>(),
            Contratos = contratos.Data ?? Array.Empty<Contrato360ViewModel>(),
            Erro = resumo.Error ?? departamentos.Error ?? cargos.Error ?? colaboradores.Error ?? contratos.Error
        });
    }

    public IActionResult PedidosCompra() => View();
    public IActionResult Recebimentos() => View();
    public IActionResult Inspecoes() => View();
    public IActionResult Ocorrencias() => View();
    public IActionResult Estoque() => View();
    public IActionResult Movimentacoes() => View();
    public IActionResult Inventarios() => View();
    public IActionResult Coleta() => View();

    // ==========================================
    // ORÇAMENTOS CIRÚRGICOS E RESERVAS DE MATERIAIS
    // ==========================================

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
    public IActionResult OrcamentoNovo()
    {
        var model = new OrcamentoFormViewModel();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OrcamentoNovo(OrcamentoFormViewModel form)
    {
        if (form.HospitalId == Guid.Empty || form.ResponsavelFinanceiroId == Guid.Empty || string.IsNullOrWhiteSpace(form.Procedimento))
        {
            TempData["ErrorMessage"] = "Hospital, Responsável Financeiro e Procedimento são obrigatórios.";
            return View(form);
        }

        if (form.Itens.Count == 0 || form.Itens.All(x => x.Quantidade <= 0))
        {
            TempData["ErrorMessage"] = "Informe ao menos um produto com quantidade positiva.";
            return View(form);
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Departamento(string codigo, string nome) =>
        await Send("api/administrativo360/departamentos", new { codigo, nome }, "Departamento cadastrado.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cargo(string codigo, string nome, Guid? departamentoId) =>
        await Send("api/administrativo360/cargos", new { codigo, nome, departamentoId }, "Cargo cadastrado.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Colaborador(string matricula, string nome, string cpf, string email, Guid cargoId) =>
        await Send("api/administrativo360/colaboradores", new { matricula, nome, cpf, email, cargoId }, "Colaborador cadastrado.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contrato(Guid colaboradorId, string tipo, DateOnly inicio, DateOnly? fim, decimal salario, int cargaHorariaSemanal) =>
        await Send("api/administrativo360/contratos", new { colaboradorId, tipo, inicio, fim, salario, cargaHorariaSemanal }, "Contratação registrada.");

    // ==========================================
    // CIRURGIAS OPERACIONAIS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Cirurgias(string? busca, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (inicio.HasValue) query.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim:yyyy-MM-dd}");

        var path = "api/administrativo360/cirurgias" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var res = await ReadApiResponse<List<CirurgiaResumoViewModel>>(client, path);

        ViewBag.Busca = busca;
        ViewBag.Situacao = situacao;
        ViewBag.Inicio = inicio;
        ViewBag.Fim = fim;

        return View(res.Data ?? new List<CirurgiaResumoViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> CirurgiaNova(Guid? orcamentoId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var model = new CirurgiaFormViewModel();
        if (orcamentoId.HasValue)
        {
            var res = await ReadApiResponse<OrcamentoDetalhesViewModel>(client, $"api/administrativo360/orcamentos/{orcamentoId.Value}");
            var orc = res.Data;
            if (orc is not null)
            {
                model.OrcamentoId = orc.Id;
                model.OrcamentoRevisao = orc.Revisao;
                model.HospitalId = orc.HospitalId;
                model.MedicoId = orc.MedicoId;
                model.Procedimento = orc.Procedimento;
                model.DataPrevista = orc.DataPrevista;
            }
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CirurgiaNova(CirurgiaFormViewModel model)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (string.IsNullOrWhiteSpace(model.Procedimento))
        {
            ModelState.AddModelError(nameof(model.Procedimento), "Informe o procedimento cirúrgico.");
            return View(model);
        }

        var payload = new
        {
            model.HospitalId,
            model.MedicoId,
            model.Procedimento,
            model.DataPrevista,
            model.HoraPrevista,
            model.OrcamentoId,
            model.OrcamentoRevisao,
            model.ResponsavelId,
            model.LocalDestinoId,
            model.Observacoes
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, "api/administrativo360/cirurgias", payload);
        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
        {
            TempData["SuccessMessage"] = "Cirurgia operacional agendada com sucesso.";
            return RedirectToAction(nameof(Cirurgias));
        }

        TempData["ErrorMessage"] = resp.Error ?? "Falha ao agendar cirurgia.";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CirurgiaDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<CirurgiaDetalhesViewModel>(client, $"api/administrativo360/cirurgias/{id}");
        if (resp.Data is null) return NotFound();

        return View(resp.Data);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CirurgiaCancelar(Guid id, string motivo)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { CirurgiaId = id, Motivo = motivo };
        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/cirurgias/{id}/cancelar", payload);

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Cirurgia cancelada com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao cancelar cirurgia.";

        return RedirectToAction(nameof(CirurgiaDetalhes), new { id });
    }

    // ==========================================
    // VALES DE CONSIGNAÇÃO
    // ==========================================

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

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeNovo(ValeFormViewModel model)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        if (model.Itens.Count == 0)
        {
            TempData["ErrorMessage"] = "Adicione ao menos um item ao vale de consignação.";
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
            TempData["SuccessMessage"] = "Vale de consignação criado com sucesso.";
            return RedirectToAction(nameof(Vales));
        }

        TempData["ErrorMessage"] = resp.Error ?? "Falha ao criar vale de consignação.";
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
            TempData["SuccessMessage"] = "Conferência do item registrada.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao registrar conferência do item.";

        return RedirectToAction(nameof(ValeDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ValeConcluirSeparacao(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(client, HttpMethod.Post, $"api/administrativo360/vales/{id}/concluir-separacao", new { });

        if (resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous)
            TempData["SuccessMessage"] = "Separação de materiais concluída. Vale pronto para expedição.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao concluir separação de materiais.";

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
            TempData["SuccessMessage"] = "Vale expedido com sucesso! Materiais transferidos para custódia externa no hospital.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao expedir vale.";

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
            TempData["SuccessMessage"] = $"Evento de {tipo} registrado com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? $"Falha ao registrar evento de {tipo}.";

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
            TempData["SuccessMessage"] = "Vale reconciliado com sucesso! Pronto para valorização no próximo incremento.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao reconciliar vale.";

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
            TempData["SuccessMessage"] = "Vale cancelado com sucesso.";
        else
            TempData["ErrorMessage"] = resp.Error ?? "Falha ao cancelar vale.";

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

    [HttpGet]
    public async Task<IActionResult> Relatorios(string? aba, string? busca)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        aba = string.IsNullOrWhiteSpace(aba) ? "pendentes" : aba.ToLowerInvariant();

        var respPendentes = await ReadApiResponse<List<RelatorioValesPendentesViewModel>>(client, "api/administrativo360/relatorios/vales-pendentes");
        var pendentes = respPendentes.Data ?? new();

        var respCustodia = await ReadApiResponse<List<RelatorioCustodiaExternaViewModel>>(client, "api/administrativo360/relatorios/custodia-externa");
        var custodia = respCustodia.Data ?? new();

        var respReconciliacao = await ReadApiResponse<List<RelatorioReconciliacaoViewModel>>(client, "api/administrativo360/relatorios/reconciliacao");
        var reconciliacao = respReconciliacao.Data ?? new();

        var rastreioPath = "api/administrativo360/relatorios/rastreabilidade" + (!string.IsNullOrWhiteSpace(busca) ? $"?busca={Uri.EscapeDataString(busca)}" : "");
        var respRastreio = await ReadApiResponse<List<RelatorioRastreabilidadeViewModel>>(client, rastreioPath);
        var rastreabilidade = respRastreio.Data ?? new();

        var model = new Adm360RelatoriosIndexViewModel
        {
            AbaAtiva = aba,
            Busca = busca,
            ValesPendentes = pendentes,
            CustodiaExterna = custodia,
            Reconciliacao = reconciliacao,
            Rastreabilidade = rastreabilidade
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportarValesPendentesCsv()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<List<RelatorioValesPendentesViewModel>>(client, "api/administrativo360/relatorios/vales-pendentes");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vale;Hospital;Cirurgia;DataSaida;RetornoPrevisto;QuantidadePendente;Responsavel;DiasAtraso");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                SanitizarCsv(it.Hospital),
                SanitizarCsv(it.CirurgiaNumero ?? ""),
                it.DataSaida?.ToString("yyyy-MM-dd HH:mm") ?? "",
                it.DataRetornoPrevista?.ToString("yyyy-MM-dd") ?? "",
                it.QuantidadePendente.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Responsavel),
                it.DiasAtraso.ToString()));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"vales_pendentes_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarCustodiaExternaCsv()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<List<RelatorioCustodiaExternaViewModel>>(client, "api/administrativo360/relatorios/custodia-externa");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Local;Produto;Sku;Lote;Validade;Hospital;Vale;Quantidade");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Local),
                SanitizarCsv(it.Produto),
                SanitizarCsv(it.Sku),
                SanitizarCsv(it.Lote),
                it.Validade?.ToString("yyyy-MM-dd") ?? "",
                SanitizarCsv(it.Hospital),
                SanitizarCsv(it.ValeNumero),
                it.Quantidade.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"custodia_externa_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarReconciliacaoCsv()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<List<RelatorioReconciliacaoViewModel>>(client, "api/administrativo360/relatorios/reconciliacao");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vale;Hospital;Cirurgia;TotalExpedido;TotalConsumido;TotalDevolvido;TotalPerda;PendenteCustodia;Situacao");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                SanitizarCsv(it.Hospital),
                SanitizarCsv(it.Cirurgia ?? ""),
                it.TotalExpedido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalConsumido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalDevolvido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalPerda.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.PendenteCustodia.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"reconciliacao_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarRastreabilidadeCsv(string? busca)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var path = "api/administrativo360/relatorios/rastreabilidade" + (!string.IsNullOrWhiteSpace(busca) ? $"?busca={Uri.EscapeDataString(busca)}" : "");
        var resp = await ReadApiResponse<List<RelatorioRastreabilidadeViewModel>>(client, path);
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Produto;Lote;Validade;OrigemTipo;Documento;Vale;Hospital;LocalAtual;Condicao;Quantidade;DataMovimento");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Produto),
                SanitizarCsv(it.Lote),
                it.Validade?.ToString("yyyy-MM-dd") ?? "",
                SanitizarCsv(it.OrigemTipo),
                SanitizarCsv(it.DocumentoOrigem ?? ""),
                SanitizarCsv(it.ValeNumero ?? ""),
                SanitizarCsv(it.Hospital ?? ""),
                SanitizarCsv(it.LocalAtual),
                SanitizarCsv(it.Condicao),
                it.Quantidade.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.DataMovimento.ToString("yyyy-MM-dd HH:mm")));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"rastreabilidade_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    // ==========================================
    // BLOCO B - VALORIZAÇÃO E VENDA INTERNA
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

    [HttpGet]
    public async Task<IActionResult> Vendas(string? busca, string? situacao, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (inicio.HasValue) query.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim:yyyy-MM-dd}");

        var path = "api/administrativo360/vendas" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<VendaResumoViewModel>>(client, path);

        return View(new VendasIndexViewModel
        {
            Vendas = (IReadOnlyList<VendaResumoViewModel>?)resp.Data ?? Array.Empty<VendaResumoViewModel>(),
            Busca = busca,
            Situacao = situacao,
            Inicio = inicio,
            Fim = fim
        });
    }

    [HttpGet]
    public async Task<IActionResult> VendaDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<VendaDetalhesViewModel>(client, $"api/administrativo360/vendas/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Venda não encontrada.";
            return RedirectToAction(nameof(Vendas));
        }

        return View(new VendaDetalhesPageViewModel { Venda = resp.Data });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarVenda(Guid id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "O motivo do cancelamento é obrigatório.";
            return RedirectToAction(nameof(VendaDetalhes), new { id });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<string, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, $"api/administrativo360/vendas/{id}/cancelar", motivo);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Venda cancelada com sucesso." : resp.Error;
        return RedirectToAction(nameof(VendaDetalhes), new { id });
    }

    // ==========================================
    // BLOCO C - CONTAS A RECEBER E TÍTULOS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> TitulosReceber(string? busca, string? situacao, Guid? pagadorId, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (pagadorId.HasValue) query.Add($"pagadorId={pagadorId.Value}");
        if (inicio.HasValue) query.Add($"inicio={inicio:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<TituloReceberResumoViewModel>>(client, path);

        return View(new TitulosIndexViewModel
        {
            Titulos = (IReadOnlyList<TituloReceberResumoViewModel>?)resp.Data ?? Array.Empty<TituloReceberResumoViewModel>(),
            Busca = busca,
            Situacao = situacao,
            PagadorId = pagadorId,
            Inicio = inicio,
            Fim = fim
        });
    }

    [HttpGet]
    public async Task<IActionResult> TituloDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<TituloReceberDetalhesViewModel>(client, $"api/administrativo360/titulos/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Título não encontrado.";
            return RedirectToAction(nameof(TitulosReceber));
        }

        var contasResp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");

        return View(new TituloDetalhesPageViewModel
        {
            Titulo = resp.Data,
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)contasResp.Data ?? Array.Empty<ContaFinanceiraViewModel>()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReceberTitulo(
        Guid id,
        Guid contaFinanceiraId,
        DateOnly dataRecebimento,
        decimal valorRecebido,
        string meioPagamento,
        string? referencia,
        string? observacoes,
        string? idempotencyKey)
    {
        if (valorRecebido <= 0m)
        {
            TempData["ErrorMessage"] = "O valor recebido deve ser positivo.";
            return RedirectToAction(nameof(TituloDetalhes), new { id });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            TituloId = id,
            ContaFinanceiraId = contaFinanceiraId,
            DataRecebimento = dataRecebimento,
            ValorRecebido = valorRecebido,
            MeioPagamento = meioPagamento,
            Referencia = referencia,
            IdempotencyKey = key,
            Observacoes = observacoes
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos/receber", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Recebimento registrado manualmente com sucesso." : resp.Error;
        return RedirectToAction(nameof(TituloDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EstornarRecebimento(Guid tituloId, Guid baixaId, string motivo, string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "O motivo do estorno é obrigatório.";
            return RedirectToAction(nameof(TituloDetalhes), new { id = tituloId });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            BaixaId = baixaId,
            Motivo = motivo,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos/estornar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Baixa estornada com sucesso. Saldo e caixa revertidos." : resp.Error;
        return RedirectToAction(nameof(TituloDetalhes), new { id = tituloId });
    }

    // ==========================================
    // BLOCO C/D - FLUXO DE CAIXA E CONTAS
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> FluxoCaixa(DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var dtInicio = inicio ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var dtFim = fim ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

        var contasResp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");
        var fluxoResp = await ReadApiResponse<FluxoCaixaViewModel>(client, $"api/administrativo360/caixa/fluxo?inicio={dtInicio:yyyy-MM-dd}&fim={dtFim:yyyy-MM-dd}");

        return View(new FluxoCaixaPageViewModel
        {
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)contasResp.Data ?? Array.Empty<ContaFinanceiraViewModel>(),
            Fluxo = fluxoResp.Data ?? new FluxoCaixaViewModel(DateOnly.FromDateTime(DateTime.UtcNow), 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, Array.Empty<FluxoCaixaItemViewModel>()),
            Inicio = dtInicio,
            Fim = dtFim
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarContaFinanceira(
        string nome,
        string tipo,
        string? banco,
        string? agencia,
        string? conta,
        decimal saldoInicial,
        DateOnly? dataSaldoInicial)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["ErrorMessage"] = "O nome da conta financeira é obrigatório.";
            return RedirectToAction(nameof(FluxoCaixa));
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var cmd = new
        {
            Nome = nome,
            Tipo = tipo,
            Banco = banco,
            Agencia = agencia,
            Conta = conta,
            SaldoInicial = saldoInicial,
            DataSaldoInicial = dataSaldoInicial ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/caixa/contas", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta financeira cadastrada com sucesso." : resp.Error;
        return RedirectToAction(nameof(FluxoCaixa));
    }

    // ==========================================
    // BLOCO B - CONTAS A PAGAR & OBRIGAÇÕES
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> TitulosPagar(string? busca, string? situacao, Guid? fornecedorId, string? centroCusto, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (fornecedorId.HasValue) query.Add($"fornecedorId={fornecedorId.Value}");
        if (!string.IsNullOrWhiteSpace(centroCusto)) query.Add($"centroCusto={Uri.EscapeDataString(centroCusto)}");
        if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos-pagar" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<TituloPagarResumoViewModel>>(client, path);

        return View(new TitulosPagarIndexViewModel
        {
            Titulos = (IReadOnlyList<TituloPagarResumoViewModel>?)resp.Data ?? Array.Empty<TituloPagarResumoViewModel>(),
            Busca = busca,
            Situacao = situacao,
            FornecedorId = fornecedorId,
            CentroCusto = centroCusto,
            Inicio = inicio,
            Fim = fim
        });
    }

    [HttpGet]
    public async Task<IActionResult> TituloPagarDetalhes(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<TituloPagarDetalhesViewModel>(client, $"api/administrativo360/titulos-pagar/{id}");
        if (resp.Data is null)
        {
            TempData["ErrorMessage"] = resp.Error ?? "Título a pagar não encontrado.";
            return RedirectToAction(nameof(TitulosPagar));
        }

        var contasResp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");

        return View(new TituloPagarDetalhesPageViewModel
        {
            Titulo = resp.Data,
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)contasResp.Data ?? Array.Empty<ContaFinanceiraViewModel>()
        });
    }

    [HttpGet]
    public IActionResult NovaDespesa()
    {
        return View(new DespesaManualFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NovaDespesa(DespesaManualFormViewModel model, string? idempotencyKey)
    {
        if (model.FornecedorId == Guid.Empty || model.ValorPrincipal <= 0)
        {
            TempData["ErrorMessage"] = "Fornecedor e valor positivo são obrigatórios.";
            return View(model);
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            model.FornecedorId,
            Documento = model.Documento?.Trim(),
            model.Competencia,
            model.DataVencimento,
            model.ValorPrincipal,
            CentroCusto = model.CentroCusto?.Trim(),
            Observacoes = model.Observacoes?.Trim(),
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos-pagar/despesa-manual", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Despesa manual registrada com sucesso." : resp.Error;
        return RedirectToAction(nameof(TitulosPagar));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AprovarTituloPagar(Guid id, string? idempotencyKey)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            TituloId = id,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, $"api/administrativo360/titulos-pagar/{id}/aprovar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Obrigação aprovada com sucesso." : resp.Error;
        return RedirectToAction(nameof(TituloPagarDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PagarTitulo(
        Guid id,
        Guid contaFinanceiraId,
        DateOnly dataPagamento,
        decimal valorPago,
        string meioPagamento,
        string? referencia,
        string? idempotencyKey)
    {
        if (valorPago <= 0m)
        {
            TempData["ErrorMessage"] = "O valor do pagamento deve ser positivo.";
            return RedirectToAction(nameof(TituloPagarDetalhes), new { id });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            TituloId = id,
            ContaId = contaFinanceiraId,
            DataPagamento = dataPagamento,
            Valor = valorPago,
            MeioPagamento = meioPagamento,
            Referencia = referencia,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos-pagar/pagar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Pagamento registrado manualmente com sucesso." : resp.Error;
        return RedirectToAction(nameof(TituloPagarDetalhes), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EstornarPagamento(Guid tituloId, Guid pagamentoId, string motivo, string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "O motivo do estorno é obrigatório.";
            return RedirectToAction(nameof(TituloPagarDetalhes), new { id = tituloId });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            PagamentoId = pagamentoId,
            Motivo = motivo,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos-pagar/estornar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Pagamento estornado com sucesso. Saldo e obrigação recompostos." : resp.Error;
        return RedirectToAction(nameof(TituloPagarDetalhes), new { id = tituloId });
    }

    [HttpGet]
    public async Task<IActionResult> ComissoesPendentes(Guid? vendedorId, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (vendedorId.HasValue) query.Add($"vendedorId={vendedorId.Value}");
        if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos-pagar/comissoes-pendentes" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<ComissaoPendenteViewModel>>(client, path);

        return View(new ComissoesPendentesIndexViewModel
        {
            Comissoes = (IReadOnlyList<ComissaoPendenteViewModel>?)resp.Data ?? Array.Empty<ComissaoPendenteViewModel>(),
            VendedorId = vendedorId,
            Inicio = inicio,
            Fim = fim
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GerarTituloComissao(Guid vendedorId, DateOnly dataVencimento, string? idempotencyKey)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            VendedorId = vendedorId,
            DataVencimento = dataVencimento,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/titulos-pagar/gerar-de-comissao", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Título de comissão gerado em Contas a Pagar com sucesso." : resp.Error;
        return RedirectToAction(nameof(TitulosPagar));
    }

    // ==========================================
    // CONTAS FINANCEIRAS (CADASTRO, EDIÇÃO, INATIVAÇÃO)
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> ContasFinanceiras()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");

        return View(new ContasFinanceirasIndexViewModel
        {
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)resp.Data ?? Array.Empty<ContaFinanceiraViewModel>()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AtualizarContaFinanceira(ContaFinanceiraFormViewModel form)
    {
        if (!form.Id.HasValue || string.IsNullOrWhiteSpace(form.Nome))
        {
            TempData["ErrorMessage"] = "Conta e nome são obrigatórios.";
            return RedirectToAction(nameof(ContasFinanceiras));
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var cmd = new
        {
            ContaId = form.Id.Value,
            form.Nome,
            form.Tipo,
            form.Banco,
            form.Agencia,
            form.Conta,
            form.DataSaldoInicial,
            form.Ativo
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Put, $"api/administrativo360/caixa/contas/{form.Id.Value}", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta financeira atualizada com sucesso." : resp.Error;
        return RedirectToAction(nameof(ContasFinanceiras));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> InativarContaFinanceira(Guid id)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Delete, $"api/administrativo360/caixa/contas/{id}", new { });

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Conta financeira inativada com sucesso." : resp.Error;
        return RedirectToAction(nameof(ContasFinanceiras));
    }

    // ==========================================
    // BLOCO C - FECHAMENTO DE CAIXA
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> FechamentoCaixa(Guid? contaId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var contasResp = await ReadApiResponse<List<ContaFinanceiraViewModel>>(client, "api/administrativo360/caixa/contas");
        var query = contaId.HasValue ? $"?contaId={contaId.Value}" : "";
        var fechamentosResp = await ReadApiResponse<List<CaixaFechamentoViewModel>>(client, $"api/administrativo360/caixa/fechamentos{query}");

        return View(new FechamentoCaixaIndexViewModel
        {
            Fechamentos = (IReadOnlyList<CaixaFechamentoViewModel>?)fechamentosResp.Data ?? Array.Empty<CaixaFechamentoViewModel>(),
            Contas = (IReadOnlyList<ContaFinanceiraViewModel>?)contasResp.Data ?? Array.Empty<ContaFinanceiraViewModel>(),
            ContaId = contaId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FecharCaixa(
        Guid contaFinanceiraId,
        DateOnly dataInicio,
        DateOnly dataFim,
        decimal saldoConferido,
        string? justificativa,
        string? idempotencyKey)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString("N") : idempotencyKey;

        var cmd = new
        {
            ContaId = contaFinanceiraId,
            DataInicio = dataInicio,
            DataFim = dataFim,
            SaldoConferido = saldoConferido,
            Justificativa = justificativa,
            IdempotencyKey = key
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, "api/administrativo360/caixa/fechar", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Caixa fechado com sucesso." : resp.Error;
        return RedirectToAction(nameof(FechamentoCaixa), new { contaId = contaFinanceiraId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReabrirCaixa(Guid fechamentoId, Guid? contaId, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            TempData["ErrorMessage"] = "O motivo da reabertura é obrigatório.";
            return RedirectToAction(nameof(FechamentoCaixa), new { contaId });
        }

        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var cmd = new
        {
            FechamentoId = fechamentoId,
            Motivo = motivo
        };

        var resp = await SendApiAsync<object, System.Text.Json.JsonElement>(
            client, HttpMethod.Post, $"api/administrativo360/caixa/fechamentos/{fechamentoId}/reabrir", cmd);

        var ok = resp.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? "Fechamento de caixa reaberto com sucesso." : resp.Error;
        return RedirectToAction(nameof(FechamentoCaixa), new { contaId });
    }

    [HttpGet]
    public async Task<IActionResult> ExportarTitulosPagarCsv(
        string? busca, string? situacao, Guid? fornecedorId, string? centroCusto, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (fornecedorId.HasValue) query.Add($"fornecedorId={fornecedorId.Value}");
        if (!string.IsNullOrWhiteSpace(centroCusto)) query.Add($"centroCusto={Uri.EscapeDataString(centroCusto)}");
        if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos-pagar" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<TituloPagarResumoViewModel>>(client, path);
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Numero;Fornecedor;Origem;Documento;Competencia;Emissao;Vencimento;Parcela;ValorPrincipal;ValorPago;SaldoAberto;Situacao;CentroCusto");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                SanitizarCsv(it.Fornecedor),
                SanitizarCsv(it.OrigemTipo),
                SanitizarCsv(it.Documento ?? ""),
                it.Competencia.ToString("yyyy-MM-dd"),
                it.DataEmissao.ToString("yyyy-MM-dd"),
                it.DataVencimento.ToString("yyyy-MM-dd"),
                $"{it.Parcela}/{it.TotalParcelas}",
                it.ValorPrincipal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ValorPago.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.SaldoAberto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao),
                SanitizarCsv(it.CentroCusto ?? "")));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"titulos_pagar_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarTitulosReceberCsv(
        string? busca, string? situacao, Guid? pagadorId, DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) query.Add($"busca={Uri.EscapeDataString(busca)}");
        if (!string.IsNullOrWhiteSpace(situacao)) query.Add($"situacao={Uri.EscapeDataString(situacao)}");
        if (pagadorId.HasValue) query.Add($"pagadorId={pagadorId.Value}");
        if (inicio.HasValue) query.Add($"inicio={inicio.Value:yyyy-MM-dd}");
        if (fim.HasValue) query.Add($"fim={fim.Value:yyyy-MM-dd}");

        var path = "api/administrativo360/titulos" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var resp = await ReadApiResponse<List<TituloReceberResumoViewModel>>(client, path);
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Numero;Venda;Pagador;Parcela;Emissao;Vencimento;ValorPrincipal;ValorRecebido;SaldoAberto;Situacao");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                SanitizarCsv(it.VendaNumero),
                SanitizarCsv(it.Pagador),
                $"{it.Parcela}/{it.TotalParcelas}",
                it.DataEmissao.ToString("yyyy-MM-dd"),
                it.DataVencimento.ToString("yyyy-MM-dd"),
                it.ValorPrincipal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ValorRecebido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.SaldoAberto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"titulos_receber_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    // ==========================================
    // BLOCO D/E - RELATÓRIOS FINANCEIROS & CSV
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> RelatoriosFinanceiros(string? aba, DateOnly? inicio, DateOnly? fim, Guid? vendedorId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var dtInicio = inicio ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var dtFim = fim ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = $"?inicio={dtInicio:yyyy-MM-dd}&fim={dtFim:yyyy-MM-dd}";
        if (vendedorId.HasValue) query += $"&vendedorId={vendedorId.Value}";

        var respVendas = await ReadApiResponse<List<RelatorioVendasItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/vendas{query}");
        var respComissoes = await ReadApiResponse<List<RelatorioComissaoItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/comissoes{query}");
        var respMargem = await ReadApiResponse<List<RelatorioMargemItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/margem{query}");

        return View(new RelatoriosFinanceirosPageViewModel
        {
            Vendas = (IReadOnlyList<RelatorioVendasItemViewModel>?)respVendas.Data ?? Array.Empty<RelatorioVendasItemViewModel>(),
            Comissoes = (IReadOnlyList<RelatorioComissaoItemViewModel>?)respComissoes.Data ?? Array.Empty<RelatorioComissaoItemViewModel>(),
            Margens = (IReadOnlyList<RelatorioMargemItemViewModel>?)respMargem.Data ?? Array.Empty<RelatorioMargemItemViewModel>(),
            AbaAtiva = string.IsNullOrWhiteSpace(aba) ? "vendas" : aba,
            Inicio = dtInicio,
            Fim = dtFim,
            VendedorId = vendedorId
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportarVendasCsv(DateOnly? inicio, DateOnly? fim, Guid? vendedorId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        if (vendedorId.HasValue) query += $"&vendedorId={vendedorId.Value}";

        var resp = await ReadApiResponse<List<RelatorioVendasItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/vendas{query}");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Numero;Data;Hospital;Pagador;Vendedor;TotalBruto;Desconto;TotalLiquido;TotalCusto;ComissaoPrevista;Situacao");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Numero),
                it.Data.ToString("yyyy-MM-dd"),
                SanitizarCsv(it.Hospital),
                SanitizarCsv(it.Pagador),
                SanitizarCsv(it.Vendedor ?? ""),
                it.TotalBruto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.Desconto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalLiquido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.TotalCusto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ComissaoPrevista.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"vendas_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarComissoesCsv(DateOnly? inicio, DateOnly? fim, Guid? vendedorId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        if (vendedorId.HasValue) query += $"&vendedorId={vendedorId.Value}";

        var resp = await ReadApiResponse<List<RelatorioComissaoItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/comissoes{query}");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vendedor;VendaNumero;DataBaixa;BaseCalculo;Percentual;ComissaoApropriada;Situacao");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Vendedor),
                SanitizarCsv(it.VendaNumero),
                it.DataBaixa.ToString("yyyy-MM-dd"),
                it.BaseCalculo.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.Percentual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ComissaoApropriada.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"comissoes_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarMargemCsv(DateOnly? inicio, DateOnly? fim)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        var resp = await ReadApiResponse<List<RelatorioMargemItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/margem{query}");
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("VendaNumero;ValeNumero;Hospital;ReceitaLiquida;CustoConsumido;ComissaoPrevista;ComissaoApropriada;MargemContribuicao;MargemPercentual");

        foreach (var it in itens)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.VendaNumero),
                SanitizarCsv(it.ValeNumero),
                SanitizarCsv(it.Hospital),
                it.ReceitaLiquida.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.CustoConsumido.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ComissaoPrevista.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.ComissaoApropriada.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.MargemContribuicao.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                it.MargemPercentual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"margem_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    private static string SanitizarCsv(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;
        var limpo = valor.Replace("\"", "\"\"");
        // Prevenção contra Formula Injection no Excel (=, +, -, @)
        if (limpo.StartsWith('=') || limpo.StartsWith('+') || limpo.StartsWith('-') || limpo.StartsWith('@'))
            limpo = "'" + limpo;
        return $"\"{limpo}\"";
    }

    private async Task<IActionResult> Send<T>(string endpoint, T payload, string success)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await SendApiAsync<T, System.Text.Json.JsonElement>(client, HttpMethod.Post, endpoint, payload);
        var ok = response.StatusCode is >= System.Net.HttpStatusCode.OK and < System.Net.HttpStatusCode.Ambiguous;
        TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok ? success : response.Error;
        return RedirectToAction(nameof(Index));
    }
}


