using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class DashboardsPremiumController : BaseWebController
{
    public DashboardsPremiumController(IHttpClientFactory httpClientFactory, ILogger<DashboardsPremiumController> logger) : base(httpClientFactory, logger) { }

    public Task<IActionResult> AdminGlobal() { return PerfilAsync("admin-global", "Admin Global"); }
    public Task<IActionResult> AdministradorCliente() { return PerfilAsync("admin-cliente", "Administrador Cliente"); }
    public Task<IActionResult> Coordenacao() { return PerfilAsync("coordenacao", "Coordenação"); }
    public Task<IActionResult> Medico() { return PerfilAsync("medico", "Médico"); }
    public Task<IActionResult> Financeiro() { return PerfilAsync("financeiro", "Financeiro"); }
    public Task<IActionResult> Saude360() { return PerfilAsync("saude360", "Saúde 360"); }

    private async Task<IActionResult> PerfilAsync(string perfilApi, string titulo)
    {
        var model = new DashboardPremiumPageViewModel
        {
            Titulo = titulo,
            PerfilApi = perfilApi,
            Fonte = "Indisponível",
            StatusPlanoModulo = "Contexto de tenant, perfil, plano e módulo validado pela API."
        };

        try
        {
            var client = CreateApiClient();
            AddBearerToken(client);
            var result = await ReadApiResponseAsync<DashboardPremiumApiViewModel>(client, "api/dashboards/" + perfilApi);
            if (result.Data == null)
            {
                model.ErrorMessage = ApiErrorPresenter.ToFriendlyMessage(result.Error ?? "Dashboard indisponível no momento.");
                return View("Perfil", model);
            }

            model.Kpis = result.Data.Kpis;
            model.Alertas = result.Data.Alertas;
            model.ProximosPassos = result.Data.ProximosPassos;
            model.Atalhos = result.Data.Atalhos;
            model.Series = result.Data.Series;
            model.StatusPlanoModulo = result.Data.StatusPlanoModulo;
            model.Fonte = model.Kpis.Any() || model.Series.Any() ? "Real" : "Parcial";
            return View("Perfil", model);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao carregar dashboard premium {PerfilApi}", perfilApi);
            model.ErrorMessage = ApiErrorPresenter.ToFriendlyMessage(ex.Message);
            return View("Perfil", model);
        }
    }
}
