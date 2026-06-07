using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
using System.Net;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public class HomeController : BaseWebController
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(IHttpClientFactory httpClientFactory, ILogger<HomeController> logger) : base(httpClientFactory, logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    public IActionResult Produto() => View();

    public async Task<IActionResult> Dashboard([FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, [FromQuery] string? hospital, [FromQuery] string? especialidade)
    {
        if (User.IsMedico()) return RedirectToAction("Index", "MinhaAgenda");
        var fallback = CriarDashboardVazio();

        try
        {
            var client = CreateApiClient();
            if (!AddBearerToken(client)) return HandleUnauthorized();

            const string endpoint = "api/dashboard";
            var (data, error, statusCode) = await ReadApiResponse<DashboardOverviewDto>(client, endpoint);

            if (statusCode == HttpStatusCode.Unauthorized)
                return HandleUnauthorized("Sessão expirada. Faça login novamente.");

            if (statusCode == HttpStatusCode.Forbidden)
                return EmptyViewWithError(fallback, "Acesso negado ao dashboard.");

            if (statusCode == HttpStatusCode.NotFound)
                return EmptyViewWithError(fallback, "Rota de dashboard não encontrada na API.");

            if ((int)statusCode >= 500)
                return EmptyViewWithError(fallback, "A API está indisponível no momento.");

            _logger.LogInformation("Acesso dashboard usuário:{User} FiltrosInicio:{Inicio} FiltrosFim:{Fim} Hospital:{Hospital} Especialidade:{Especialidade}",
                User.Identity?.Name, inicio, fim, hospital, especialidade);
            var safeData = data is null ? fallback : data with
            {
                ProximosPlantoes = data.ProximosPlantoes ?? Array.Empty<PlantaoResumoDto>(),
                UltimosPagamentos = data.UltimosPagamentos ?? Array.Empty<PagamentoResumoDto>(),
                UltimasNotificacoes = data.UltimasNotificacoes ?? Array.Empty<NotificacaoDto>(),
                PlantoesPorMes = data.PlantoesPorMes ?? Array.Empty<DashboardChartItem>(),
                PagamentosPorMes = data.PagamentosPorMes ?? Array.Empty<DashboardChartItem>(),
                PlantoesPorEspecialidade = data.PlantoesPorEspecialidade ?? Array.Empty<DashboardChartItem>(),
                PlantoesPorHospital = data.PlantoesPorHospital ?? Array.Empty<DashboardChartItem>()
            };
            safeData = AplicarFiltrosDashboard(safeData, inicio, fim, hospital, especialidade);
            if (!string.IsNullOrWhiteSpace(error)) TempData["Info"] = error;
            return View(safeData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar dashboard.");
            TempData["Error"] = "Erro ao carregar dashboard.";
            return View(fallback);
        }
    }
    private static DashboardOverviewDto CriarDashboardVazio()
    {
        return new DashboardOverviewDto(
            new DashboardDto(0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0L, 0m, 0m, 0L),
            Array.Empty<PlantaoResumoDto>(),
            Array.Empty<PagamentoResumoDto>(),
            Array.Empty<NotificacaoDto>(),
            Array.Empty<DashboardChartItem>(),
            Array.Empty<DashboardChartItem>(),
            Array.Empty<DashboardChartItem>(),
            Array.Empty<DashboardChartItem>());
    }

    private static DashboardOverviewDto AplicarFiltrosDashboard(
        DashboardOverviewDto source,
        DateTime? inicio,
        DateTime? fim,
        string? hospital,
        string? especialidade)
    {
        var inicioEfetivo = inicio?.Date;
        var fimEfetivo = fim?.Date.AddDays(1).AddTicks(-1);
        var hospitalFiltro = (hospital ?? string.Empty).Trim();
        var especialidadeFiltro = (especialidade ?? string.Empty).Trim();

        bool MatchPeriodo(DateTime data) =>
            (!inicioEfetivo.HasValue || data >= inicioEfetivo.Value) &&
            (!fimEfetivo.HasValue || data <= fimEfetivo.Value);

        bool MatchPlantao(PlantaoResumoDto p) =>
            MatchPeriodo(p.DataInicio) &&
            (string.IsNullOrWhiteSpace(hospitalFiltro) || p.HospitalNome.Contains(hospitalFiltro, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(especialidadeFiltro) || p.EspecialidadeNome.Contains(especialidadeFiltro, StringComparison.OrdinalIgnoreCase));

        bool MatchPagamento(PagamentoResumoDto p) =>
            MatchPeriodo(p.DataPlantao) &&
            (string.IsNullOrWhiteSpace(hospitalFiltro) || p.HospitalNome.Contains(hospitalFiltro, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(especialidadeFiltro) || p.EspecialidadeNome.Contains(especialidadeFiltro, StringComparison.OrdinalIgnoreCase));

        var plantoesFiltrados = source.ProximosPlantoes.Where(MatchPlantao).ToArray();
        var pagamentosFiltrados = source.UltimosPagamentos.Where(MatchPagamento).ToArray();
        var indicadores = source.Indicadores with
        {
            TotalPlantoes = plantoesFiltrados.LongLength,
            PlantoesConfirmados = plantoesFiltrados.LongCount(p => p.Status.Equals("CONFIRMADO", StringComparison.OrdinalIgnoreCase)),
            PagamentosPendentes = pagamentosFiltrados.LongCount(p => p.Status.Equals("PENDENTE", StringComparison.OrdinalIgnoreCase)),
            ValorPendente = pagamentosFiltrados.Where(p => p.Status.Equals("PENDENTE", StringComparison.OrdinalIgnoreCase)).Sum(p => p.ValorPrevisto),
            ValorPagoMes = pagamentosFiltrados.Where(p => p.Status.Equals("PAGO", StringComparison.OrdinalIgnoreCase)).Sum(p => p.ValorPago ?? 0)
        };

        return source with
        {
            Indicadores = indicadores,
            ProximosPlantoes = plantoesFiltrados,
            UltimosPagamentos = pagamentosFiltrados
        };
    }

}
