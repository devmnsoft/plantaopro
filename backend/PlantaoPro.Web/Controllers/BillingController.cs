using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor + "," + RolesConstants.Financeiro)]
public sealed class BillingController : Controller
{
    private IActionResult Page(string title, string description, string apiPath, params string[] actions)
    {
        return View("SaasComercialPage", new SaasComercialPageViewModel { Title = title, Description = description, ApiPath = apiPath, Actions = actions ?? Array.Empty<string>() });
    }

    public IActionResult Index() => Assinaturas();
    public IActionResult Assinaturas() => Page("Assinaturas SaaS", "Contratos recorrentes sem armazenar cartão ou dados bancários sensíveis.", "api/billing/assinaturas", "Criar assinatura", "Ativar", "Suspender", "Cancelar", "Upgrade/Downgrade");
    public IActionResult AssinaturaDetails(Guid id) => Page("Detalhes da assinatura", "Histórico, plano, valor, vigência e eventos auditados.", $"api/billing/assinaturas/{id}", "Ativar", "Suspender", "Cancelar");
    public IActionResult Faturas() => Page("Faturas SaaS", "Competência, vencimento, valor, status e pagamento manual informativo.", "api/billing/faturas", "Criar fatura", "Marcar paga", "Marcar atrasada");
    public IActionResult CreateFatura() => Page("Nova fatura", "Registre fatura sem coletar cartão ou dado bancário sensível.", "api/billing/faturas", "Salvar fatura");
    public IActionResult UpgradeDowngrade(Guid id) => Page("Upgrade ou downgrade", "Troque plano mantendo histórico e auditoria.", $"api/billing/assinaturas/{id}/upgrade", "Simular impacto", "Confirmar alteração");
    public IActionResult Inadimplencia() => Faturas();
}
