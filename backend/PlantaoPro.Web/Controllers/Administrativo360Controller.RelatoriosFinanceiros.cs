using PlantaoPro.CrossCutting.Localization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Relatorios financeiros (vendas/comissoes/margem) e exportacoes CSV financeiras
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
        var itens = resp.Data ?? new();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Vendedor;VendaNumero;DataBaixa;BaseCalculo;Percentual;ComissaoApropriada;Situacao");

        foreach (var it in itens)
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
}
