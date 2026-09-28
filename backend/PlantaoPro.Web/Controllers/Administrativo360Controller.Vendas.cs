using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

public partial class Administrativo360Controller
{
    // Vendas de materiais (aprovados) e cancelamento
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
}
