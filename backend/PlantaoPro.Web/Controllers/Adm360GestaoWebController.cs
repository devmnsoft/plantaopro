using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,ADMIN_CLIENTE,GESTOR_OPERACIONAL,DIRETOR,COORDENACAO,COORDENADOR,CONSULTA_CLIENTE,AUDITOR")]
[Route("Administrativo360/Gestao")]
public sealed class Adm360GestaoWebController : BaseWebController
{
    public Adm360GestaoWebController(IHttpClientFactory factory, ILogger<Adm360GestaoWebController> logger)
        : base(factory, logger) { }

    [HttpGet("Dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] Guid? estabelecimentoId,
        [FromQuery] string? provedor,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] string? situacao)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var query = "api/administrativo360/gestao/dashboard?";
        if (estabelecimentoId.HasValue) query += $"estabelecimentoId={estabelecimentoId.Value}&";
        if (!string.IsNullOrWhiteSpace(provedor)) query += $"provedor={Uri.EscapeDataString(provedor)}&";
        if (dataInicio.HasValue) query += $"dataInicio={dataInicio.Value:yyyy-MM-dd}&";
        if (dataFim.HasValue) query += $"dataFim={dataFim.Value:yyyy-MM-dd}&";
        if (!string.IsNullOrWhiteSpace(situacao)) query += $"situacao={Uri.EscapeDataString(situacao)}&";

        var dashResp = await ReadApiResponse<DashboardAdm360ViewModel>(client, query.TrimEnd('&', '?'));
        var capResp = await ReadApiResponse<IReadOnlyList<string>>(client, "api/administrativo360/gestao/capacidades-ativas");

        return View("~/Views/Administrativo360/Gestao/Dashboard.cshtml", new DashboardGestaoViewModel
        {
            Dashboard = dashResp.Data ?? new DashboardAdm360ViewModel(
                new CotacoesIndicadoresViewModel(0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                new XmlIndicadoresViewModel(0, 0, 0, 0, 0, 0),
                new IntegracoesIndicadoresViewModel(0, 0, 0, null),
                DateTime.UtcNow
            ),
            EstabelecimentoId = estabelecimentoId,
            Provedor = provedor,
            DataInicio = dataInicio,
            DataFim = dataFim,
            Situacao = situacao,
            CapacidadesAtivas = capResp.Data ?? Array.Empty<string>(),
            Erro = dashResp.Error ?? capResp.Error
        });
    }

    [HttpGet("Monitor")]
    public async Task<IActionResult> Monitor()
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var contasResp = await ReadApiResponse<IReadOnlyList<PortalContaViewModel>>(client, "api/administrativo360/cotacoes/contas-portal");
        var dfeResp = await ReadApiResponse<IReadOnlyList<DfeSincronizacaoViewModel>>(client, "api/administrativo360/xml/sincronizacoes");
        var respResp = await ReadApiResponse<IReadOnlyList<CotacaoRespostaViewModel>>(client, "api/administrativo360/cotacoes/respostas");

        return View("~/Views/Administrativo360/Gestao/Monitor.cshtml", new MonitorIntegracoesViewModel
        {
            ContasPortal = contasResp.Data ?? Array.Empty<PortalContaViewModel>(),
            SincronizacoesDfe = dfeResp.Data ?? Array.Empty<DfeSincronizacaoViewModel>(),
            RespostasRecentes = respResp.Data ?? Array.Empty<CotacaoRespostaViewModel>(),
            Erro = contasResp.Error ?? dfeResp.Error ?? respResp.Error
        });
    }

    [HttpPost("HabilitarCapacidade")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HabilitarCapacidade([FromForm] string capacidade, [FromForm] bool habilitado)
    {
        using var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();

        var payload = new { Capacidade = capacidade, Habilitado = habilitado };
        var (success, error, _) = await SendApiWithoutResponseAsync(client, HttpMethod.Post, "api/administrativo360/cotacoes/capacidades", payload);

        if (!success)
        {
            TempData["Error"] = error ?? "Falha ao alterar capacidade contratada.";
        }
        else
        {
            TempData["Sucesso"] = $"Capacidade {capacidade} atualizada com sucesso.";
        }

        return RedirectToAction(nameof(Dashboard));
    }
}
