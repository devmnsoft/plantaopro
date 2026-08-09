using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Security;
using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Coordenacao + "," + RolesConstants.Coordenador + "," + RolesConstants.Operador + "," + RolesConstants.Medico)]
public sealed class ConvitesController : BaseWebController
{
    public ConvitesController(IHttpClientFactory factory, ILogger<ConvitesController> logger) : base(factory, logger) { }

    public async Task<IActionResult> Index(Guid? plantaoId)
    {
        if (!plantaoId.HasValue) return View(new ConvitesPageViewModel(null, Array.Empty<PlantaoConviteDto>(), null));
        var client = CreateApiClient();
        if (!AddBearerToken(client)) return HandleUnauthorized();
        var (items, error, _) = await ReadApiResponse<IEnumerable<PlantaoConviteDto>>(client, $"api/plantoes/{plantaoId.Value}/convites");
        return View(new ConvitesPageViewModel(plantaoId, items ?? Array.Empty<PlantaoConviteDto>(), error));
    }
}

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Financeiro + "," + RolesConstants.Medico)]
public sealed class PagamentosController : BaseWebController
{
    public PagamentosController(IHttpClientFactory factory, ILogger<PagamentosController> logger) : base(factory, logger) { }

    public Task<IActionResult> Index(string? status, DateTime? inicio, DateTime? fim, int page = 1, int pageSize = 20) =>
        this.RenderPaged<PagamentoResumoDto>($"api/financeiro/pagamentos?status={Uri.EscapeDataString(status ?? string.Empty)}&dataInicio={inicio:O}&dataFim={fim:O}&page={page}&pageSize={pageSize}");
}
