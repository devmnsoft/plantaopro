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
    public Task<IActionResult> Cobertura(DateOnly? inicio, DateOnly? fim, Guid? unidadeId, Guid? especialidadeId, Guid? profissionalId, string? situacao, int page=1) => OperacionalAsync("Cobertura",inicio,fim,unidadeId,especialidadeId,profissionalId,situacao,page);
    public Task<IActionResult> Execucao(DateOnly? inicio, DateOnly? fim, Guid? unidadeId, Guid? especialidadeId, Guid? profissionalId, string? situacao, int page=1) => OperacionalAsync("Execucao",inicio,fim,unidadeId,especialidadeId,profissionalId,situacao,page);
    public Task<IActionResult> Apuracao(DateOnly? inicio, DateOnly? fim, Guid? unidadeId, Guid? especialidadeId, Guid? profissionalId, string? situacao, int page=1) => OperacionalAsync("Apuracao",inicio,fim,unidadeId,especialidadeId,profissionalId,situacao,page);

    private async Task<IActionResult> OperacionalAsync(string kind, DateOnly? inicio, DateOnly? fim, Guid? unidadeId, Guid? especialidadeId, Guid? profissionalId, string? situacao, int page)
    {
        var today=DateOnly.FromDateTime(DateTime.UtcNow); var start=inicio??today.AddDays(-30); var end=fim??today;
        PlantaoPro.Web.Models.OperationalReportResult? data=null; string? error=null;
        if(end<start || end.DayNumber-start.DayNumber>366) error="Informe um período válido de até 366 dias.";
        else { using var client=CreateApiClient(); if(!AddBearerToken(client)) return HandleUnauthorized(); var query=Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString($"api/relatorios-operacionais/{kind}",new Dictionary<string,string?>{{"inicio",start.ToString("yyyy-MM-dd")},{"fim",end.ToString("yyyy-MM-dd")},{"unidadeId",unidadeId?.ToString()},{"especialidadeId",especialidadeId?.ToString()},{"profissionalId",profissionalId?.ToString()},{"situacao",situacao},{"page",Math.Max(1,page).ToString()},{"pageSize","25"}}.Where(x=>x.Value is not null).ToDictionary(x=>x.Key,x=>x.Value!)); var result=await ReadApiResponse<PlantaoPro.Web.Models.OperationalReportResult>(client,query); data=result.Data; error=result.Error; }
        return View("Operacional",new PlantaoPro.Web.Models.OperationalReportPageViewModel(kind,start,end,unidadeId,especialidadeId,profissionalId,situacao,data,error));
    }

    public async Task<IActionResult> ExportarOperacional(string kind, DateOnly inicio, DateOnly fim, Guid? unidadeId, Guid? especialidadeId, Guid? profissionalId, string? situacao)
    { using var client=CreateApiClient(); if(!AddBearerToken(client)) return HandleUnauthorized(); var query=Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString($"api/relatorios-operacionais/{kind}/csv",new Dictionary<string,string?>{{"inicio",inicio.ToString("yyyy-MM-dd")},{"fim",fim.ToString("yyyy-MM-dd")},{"unidadeId",unidadeId?.ToString()},{"especialidadeId",especialidadeId?.ToString()},{"profissionalId",profissionalId?.ToString()},{"situacao",situacao}}.Where(x=>x.Value is not null).ToDictionary(x=>x.Key,x=>x.Value!)); var response=await client.GetAsync(query); if(!response.IsSuccessStatusCode){TempData["Error"]="A exportação foi negada ou falhou; atualize o relatório.";return RedirectToAction(kind,new{inicio,fim,unidadeId,especialidadeId,profissionalId,situacao});} return File(await response.Content.ReadAsByteArrayAsync(),"text/csv; charset=utf-8",$"{kind.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv"); }
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
