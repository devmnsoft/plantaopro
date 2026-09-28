using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Titulos a receber/pagar, fluxo de caixa, contas financeiras, comissoes e caixa
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
}
