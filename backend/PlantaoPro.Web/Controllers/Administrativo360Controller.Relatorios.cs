using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Relatorios operacionais e exportacoes CSV de custodia/reconciliacao/rastreabilidade
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
}
