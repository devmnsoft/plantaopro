using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Controllers;
public class FinanceiroController : BaseWebController
{
    public FinanceiroController(IHttpClientFactory f, ILogger<FinanceiroController> l) : base(f, l) { }
    public async Task<IActionResult> Index(Guid? medicoId, Guid? hospitalId, Guid? especialidadeId, string? status, DateTime? dataInicio, DateTime? dataFim, int page = 1, int pageSize = 20)
        => await this.RenderPaged<PagamentoResumoDto>($"api/financeiro/pagamentos?medicoId={medicoId}&hospitalId={hospitalId}&especialidadeId={especialidadeId}&status={status}&dataInicio={dataInicio:O}&dataFim={dataFim:O}&page={page}&pageSize={pageSize}");

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await this.RenderDetails<PagamentoDetailsDto>($"api/financeiro/pagamentos/{id}");
        if (model.ErrorMessage == "Sessão expirada.") return HandleUnauthorized();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> GerarPagamento(Guid escalaId)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var payload = JsonSerializer.Serialize(new GerarPagamentoRequest(escalaId, null, null));
        var response = await client.PostAsync("api/financeiro/pagamentos/gerar", new StringContent(payload, Encoding.UTF8, "application/json"));
        if (response.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (!response.IsSuccessStatusCode) { TempData["Error"] = "Não foi possível gerar pagamento."; return RedirectToAction(nameof(Index)); }
        TempData["Success"] = "Pagamento gerado com sucesso."; return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmarPagamento(Guid id, decimal valorPago, DateOnly dataPagamento, string formaPagamento, string? observacoes)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var payload = JsonSerializer.Serialize(new ConfirmarPagamentoRequest(valorPago, dataPagamento, formaPagamento, observacoes));
        var response = await client.PostAsync($"api/financeiro/pagamentos/{id}/confirmar", new StringContent(payload, Encoding.UTF8, "application/json"));
        if (response.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (!response.IsSuccessStatusCode) { TempData["Error"] = "Não foi possível confirmar pagamento."; return RedirectToAction(nameof(Details), new { id }); }
        TempData["Success"] = "Pagamento confirmado com sucesso."; return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> CancelarPagamento(Guid id, string justificativa)
    {
        var client = CreateApiClient(); if (!AddBearerToken(client)) return HandleUnauthorized();
        var payload = JsonSerializer.Serialize(new CancelarPagamentoRequest(justificativa));
        var response = await client.PostAsync($"api/financeiro/pagamentos/{id}/cancelar", new StringContent(payload, Encoding.UTF8, "application/json"));
        if (response.StatusCode == HttpStatusCode.Unauthorized) return HandleUnauthorized();
        if (!response.IsSuccessStatusCode) { TempData["Error"] = "Não foi possível cancelar pagamento."; return RedirectToAction(nameof(Details), new { id }); }
        TempData["Success"] = "Pagamento cancelado com sucesso."; return RedirectToAction(nameof(Details), new { id });
    }
}
