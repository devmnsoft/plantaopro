using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PlantaoPro.Web.Security;
namespace PlantaoPro.Web.Controllers;

[Authorize]
public class RelatoriosController : BaseWebController
{
    public RelatoriosController(IHttpClientFactory f, ILogger<RelatoriosController> l) : base(f, l) { }
    public IActionResult Index() => View();
    public IActionResult Executivo() => View("Index");
    public IActionResult OperacaoClinica() => View("Index");
    public IActionResult FinanceiroClinica() => View("Index");
    public IActionResult Plantoes() => View("Index");
    public IActionResult Produtividade() => View("Index");

    public IActionResult Sla() => View();
    public IActionResult Convites() => View();
    public IActionResult Cobertura() => View();
    public IActionResult ProdutividadeMedica() => View();
    public IActionResult FaturamentoSaas() => View();
    public IActionResult Operacional() => View("Index");
    public IActionResult Financeiro() => View("Index");
    public IActionResult FiltrosSalvos() => View("Index");
    public IActionResult Clinico() => View("Index");
    public IActionResult Convenios() => View("Index");
    public IActionResult Execucoes() => View("Index");
    public IActionResult Exportacoes() => View("Index");
    public async Task<IActionResult> Saas(int page = 1) => await RelatorioSaasAsync("clientes", "SaaS", page);
    public async Task<IActionResult> SaasFaturamento(int page = 1) => await RelatorioSaasAsync("faturamento", "Faturamento SaaS", page);
    public async Task<IActionResult> SaasClientesRisco(int page = 1) => await RelatorioSaasAsync("clientes-risco", "Clientes em risco", page);
    public async Task<IActionResult> SaasUsoPlanos(int page = 1) => await RelatorioSaasAsync("uso-planos", "Uso dos planos", page);

    public async Task<IActionResult> ExportarSaas(string tipo = "clientes")
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var response = await client.GetAsync("api/relatorios/saas/" + tipo + "/exportar");
        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Não foi possível exportar o relatório SaaS.";
            return RedirectToAction(nameof(Saas));
        }
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return File(bytes, "text/csv", "relatorio-saas-" + tipo + ".csv");
    }

    private async Task<IActionResult> RelatorioSaasAsync(string tipo, string titulo, int page)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (data, error, _) = await ReadApiPagedResponseAsync<PlantaoPro.Web.Models.RelatorioSaasLinhaViewModel>(client, "api/relatorios/saas/" + tipo + "?page=" + Math.Max(1, page) + "&pageSize=50", page, 50);
        ViewBag.ErrorMessage = error;
        ViewBag.Titulo = titulo;
        ViewBag.Tipo = tipo;
        return View("Saas", data);
    }
}
