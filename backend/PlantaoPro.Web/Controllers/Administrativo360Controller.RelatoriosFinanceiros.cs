using PlantaoPro.CrossCutting.Localization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Relatorios financeiros (vendas/comissoes/margem/vencimentos) e exportacoes CSV financeiras.
    // s9/s10 (rodada 9 r9f): a dimensao e o CLIENTE comercial — a API rejeita filtro cross-tenant
    // com 400 (superficializado como toast na tela); os vencimentos chegam POR PARCELA (um titulo
    // = uma linha, sem multiplicacao nos totais) e o periodo filtra data_vencimento.
    [HttpGet]
    public async Task<IActionResult> RelatoriosFinanceiros(string? aba, DateOnly? inicio, DateOnly? fim, Guid? vendedorId, Guid? clienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var dtInicio = inicio ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var dtFim = fim ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = $"?inicio={dtInicio:yyyy-MM-dd}&fim={dtFim:yyyy-MM-dd}";
        if (vendedorId.HasValue) query += $"&vendedorId={vendedorId.Value}";
        if (clienteId.HasValue) query += $"&clienteId={clienteId.Value}";

        var respVendas = await ReadApiResponse<List<RelatorioVendasItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/vendas{query}");
        var respComissoes = await ReadApiResponse<List<RelatorioComissaoItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/comissoes{query}");
        var respMargem = await ReadApiResponse<List<RelatorioMargemItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/margem{query}");
        var respVencimentos = await ReadApiResponse<List<RelatorioVencimentoItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/vencimentos{query}");

        // s9: qualquer erro da API aparece na tela (ex.: "O cliente informado pertence a outro tenant.").
        var erroConsulta = respVendas.Error ?? respComissoes.Error ?? respMargem.Error ?? respVencimentos.Error;
        if (erroConsulta is not null) TempData["Error"] = erroConsulta;

        // s9: opcoes do filtro por cliente — parceiros comerciais + hospitais clinicos (historico em que
        // v.cliente_id aponta para a tabela hospitais), dedup por id e ordenados por nome.
        // Falha nos lookups: lista vazia (degrada suave; o formulario segue funcionando).
        IReadOnlyList<SelectListItemViewModel> clientes = Array.Empty<SelectListItemViewModel>();
        var respLookups = await ReadApiResponse<Lookups360ViewModel>(client, "api/administrativo360/cadastros/lookups");
        if (respLookups.Data is { } lookups)
        {
            clientes = lookups.Parceiros
                .Where(p => p.Ativo)
                .Select(p => new SelectListItemViewModel(p.Id.ToString(), p.Nome))
                .Concat(lookups.Hospitais
                    .Where(h => h.Ativo)
                    .Select(h => new SelectListItemViewModel(h.Id.ToString(), h.Nome + " (clínico)")))
                .GroupBy(opcao => opcao.Value)
                .Select(grupo => grupo.First())
                .OrderBy(opcao => opcao.Text, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return View(new RelatoriosFinanceirosPageViewModel
        {
            Vendas = (IReadOnlyList<RelatorioVendasItemViewModel>?)respVendas.Data ?? Array.Empty<RelatorioVendasItemViewModel>(),
            Comissoes = (IReadOnlyList<RelatorioComissaoItemViewModel>?)respComissoes.Data ?? Array.Empty<RelatorioComissaoItemViewModel>(),
            Margens = (IReadOnlyList<RelatorioMargemItemViewModel>?)respMargem.Data ?? Array.Empty<RelatorioMargemItemViewModel>(),
            Vencimentos = (IReadOnlyList<RelatorioVencimentoItemViewModel>?)respVencimentos.Data ?? Array.Empty<RelatorioVencimentoItemViewModel>(),
            AbaAtiva = string.IsNullOrWhiteSpace(aba) ? "vendas" : aba,
            Inicio = dtInicio,
            Fim = dtFim,
            VendedorId = vendedorId,
            ClienteId = clienteId,
            Clientes = clientes
        });
    }

    [HttpGet]
    public async Task<IActionResult> ExportarVendasCsv(DateOnly? inicio, DateOnly? fim, Guid? vendedorId, Guid? clienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        if (vendedorId.HasValue) query += $"&vendedorId={vendedorId.Value}";
        if (clienteId.HasValue) query += $"&clienteId={clienteId.Value}";

        var resp = await ReadApiResponse<List<RelatorioVendasItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/vendas{query}");
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? "Falha ao consultar o relatório de vendas.";
            return RedirectToAction(nameof(RelatoriosFinanceiros));
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Numero;Data;Cliente;Pagador;Vendedor;TotalBruto;Desconto;TotalLiquido;TotalCusto;ComissaoPrevista;Situacao");

        foreach (var it in resp.Data)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.VendaNumero),
                it.Competencia.ToString("yyyy-MM-dd"),
                SanitizarCsv(it.Cliente),
                SanitizarCsv(it.Pagador),
                SanitizarCsv(it.Vendedor ?? ""),
                ValorHumano.Format(it.TotalBruto),
                ValorHumano.Format(it.Desconto),
                ValorHumano.Format(it.TotalLiquido),
                ValorHumano.Format(it.TotalCusto),
                ValorHumano.Format(it.ComissaoPrevista),
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
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? "Falha ao consultar o relatório de comissões.";
            return RedirectToAction(nameof(RelatoriosFinanceiros));
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vendedor;VendaNumero;DataBaixa;BaseCalculo;Percentual;ComissaoApropriada;Situacao");

        foreach (var it in resp.Data)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.Vendedor),
                SanitizarCsv(it.VendaNumero),
                it.DataBaixa.ToString("yyyy-MM-dd"),
                ValorHumano.Format(it.BaseCalculo),
                ValorHumano.Format(it.Percentual),
                ValorHumano.Format(it.ComissaoApropriada),
                SanitizarCsv(it.Situacao)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"comissoes_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarMargemCsv(DateOnly? inicio, DateOnly? fim, Guid? clienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        if (clienteId.HasValue) query += $"&clienteId={clienteId.Value}";

        var resp = await ReadApiResponse<List<RelatorioMargemItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/margem{query}");
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? "Falha ao consultar o relatório de margem.";
            return RedirectToAction(nameof(RelatoriosFinanceiros));
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("VendaNumero;ValeNumero;Cliente;ReceitaLiquida;CustoConsumido;ComissaoPrevista;ComissaoApropriada;MargemContribuicao;MargemPercentual");

        foreach (var it in resp.Data)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.VendaNumero),
                SanitizarCsv(it.ValeNumero),
                SanitizarCsv(it.Cliente),
                ValorHumano.Format(it.ReceitaLiquida),
                ValorHumano.Format(it.CustoConsumido),
                ValorHumano.Format(it.ComissaoPrevista),
                ValorHumano.Format(it.ComissaoApropriada),
                ValorHumano.Format(it.MargemContribuicao),
                ValorHumano.Format(it.MargemPercentual)));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"margem_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }

    // s10 (rodada 9 r9f): exportação dos vencimentos POR PARCELA — mesma dimensão e filtros da aba.
    [HttpGet]
    public async Task<IActionResult> ExportarVencimentosCsv(DateOnly? inicio, DateOnly? fim, Guid? clienteId)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = $"?inicio={inicio:yyyy-MM-dd}&fim={fim:yyyy-MM-dd}";
        if (clienteId.HasValue) query += $"&clienteId={clienteId.Value}";

        var resp = await ReadApiResponse<List<RelatorioVencimentoItemViewModel>>(client, $"api/administrativo360/relatorios-financeiros/vencimentos{query}");
        if (resp.Data is null)
        {
            TempData["Error"] = resp.Error ?? "Falha ao consultar o relatório de vencimentos.";
            return RedirectToAction(nameof(RelatoriosFinanceiros));
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("TituloNumero;VendaNumero;Competencia;Cliente;Pagador;Parcela;TotalParcelas;DataEmissao;DataVencimento;ValorPrincipal;ValorRecebido;SaldoAberto;Situacao;Vencido");

        foreach (var it in resp.Data)
        {
            sb.AppendLine(string.Join(";",
                SanitizarCsv(it.TituloNumero),
                SanitizarCsv(it.VendaNumero),
                it.Competencia.ToString("yyyy-MM-dd"),
                SanitizarCsv(it.Cliente),
                SanitizarCsv(it.Pagador),
                it.Parcela.ToString(),
                it.TotalParcelas.ToString(),
                it.DataEmissao.ToString("yyyy-MM-dd"),
                it.DataVencimento.ToString("yyyy-MM-dd"),
                ValorHumano.Format(it.ValorPrincipal),
                ValorHumano.Format(it.ValorRecebido),
                ValorHumano.Format(it.SaldoAberto),
                SanitizarCsv(it.Situacao),
                it.Vencido ? "Sim" : "Nao"));
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"vencimentos_{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }
}
