using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

// R5-B6: as shells "SaasComercialPage" de cobrança viraram redirecionamentos para as
// operacoes canonicas (Assinaturas/FaturamentoSaas). Nenhuma tela aqui grava dados;
// /api/billing/* permanece apenas como superficie legada de leitura (backlog documentado).
// Roteamento explicito: a rota default do app e {action=Login}, entao um link legado
// "/Billing" sem acao caia em 404 (acao Login inexistente). Templates proprios garantem
// que os links legados sem acao tambem funcionem.
[Route("Billing")]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor + "," + RolesConstants.Financeiro)]
public sealed class BillingController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectToAction("Index", "Assinaturas");

    [HttpGet("Assinaturas")]
    public IActionResult Assinaturas() => RedirectToAction("Index", "Assinaturas");

    [HttpGet("AssinaturaDetails/{id:guid}")]
    public IActionResult AssinaturaDetails(Guid id) => RedirectToAction("Details", "Assinaturas", new { id });

    [HttpGet("Faturas")]
    public IActionResult Faturas() => RedirectToAction("Index", "FaturamentoSaas");

    [HttpGet("CreateFatura")]
    public IActionResult CreateFatura() => RedirectToAction("GerarMensal", "FaturamentoSaas");

    [HttpGet("UpgradeDowngrade/{id:guid}")]
    public IActionResult UpgradeDowngrade(Guid id) => RedirectToAction("AlterarPlano", "Assinaturas", new { id });

    [HttpGet("Inadimplencia")]
    public IActionResult Inadimplencia() => RedirectToAction("Inadimplencia", "FaturamentoSaas");
}
