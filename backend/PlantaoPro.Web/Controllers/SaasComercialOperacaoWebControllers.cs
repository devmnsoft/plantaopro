using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente)]
public abstract class SaasComercialPageController : Controller
{
    protected IActionResult Page(string title, string description, string apiPath, params string[] actions)
    {
        return View("SaasComercialPage", new SaasComercialPageViewModel { Title = title, Description = description, ApiPath = apiPath, Actions = actions ?? Array.Empty<string>() });
    }
}

public sealed class SaasClientesController : SaasComercialPageController
{
    public IActionResult Index() => Page("Clientes SaaS", "Cadastro, ativação e suspensão de clínicas e hospitais clientes.", "api/saas/clientes", "Criar cliente", "Editar cliente", "Ativar", "Suspender");
    public IActionResult Create() => Page("Novo cliente SaaS", "Informe dados comerciais, responsável e segmento.", "api/saas/clientes", "Salvar cliente", "Criar tenant inicial");
    public IActionResult Edit(Guid id) => Page("Editar cliente SaaS", "Atualize dados sem excluir histórico.", $"api/saas/clientes/{id}", "Salvar alterações", "Auditar mudança");
    public IActionResult Details(Guid id) => Page("Detalhes do cliente", "Visão de tenant, plano, assinatura, chamados e health score.", $"api/saas/clientes/{id}", "Ver tenants", "Ver billing", "Ver suporte");
}
public sealed class SaasTenantsController : SaasComercialPageController
{
    public IActionResult Index() => Page("Tenants SaaS", "Ambientes isolados por cliente com subdomínio, plano e status.", "api/saas/tenants", "Criar tenant", "Ativar", "Suspender");
    public IActionResult Create() => Page("Novo tenant", "Crie o ambiente do cliente e vincule plano inicial.", "api/saas/tenants", "Validar subdomínio", "Salvar tenant");
    public IActionResult Edit(Guid id) => Page("Editar tenant", "Ajuste plano, status e observações operacionais.", $"api/saas/tenants/{id}", "Salvar", "Auditar");
    public IActionResult Details(Guid id) => Page("Detalhes do tenant", "Visão de limites, uso, módulos e white label.", $"api/saas/tenants/{id}", "Ver módulos", "Ver uso", "Ver white label");
}
public sealed class SaasPlanosController : SaasComercialPageController
{
    public IActionResult Index() => Page("Planos SaaS", "Essencial, Profissional, Enterprise e White Label.", "api/saas/planos", "Criar plano", "Editar plano", "Configurar módulos");
    public IActionResult Create() => Page("Novo plano", "Defina preço, limites e posicionamento comercial.", "api/saas/planos", "Salvar plano");
    public IActionResult Edit(Guid id) => Page("Editar plano", "Mantenha limites e módulos alinhados ao contrato.", $"api/saas/planos/{id}", "Salvar alterações");
    public IActionResult Modulos(Guid id) => Page("Módulos do plano", "Libere recursos conforme o plano contratado.", $"api/saas/planos/{id}/modulos", "Adicionar módulo", "Remover módulo");
    public IActionResult Limites(Guid id) => Page("Limites do plano", "Controle usuários, médicos, API, white label e suporte.", "api/saas/limites", "Definir limite", "Liberar exceção");
}
public sealed class SaasUsoController : SaasComercialPageController { public IActionResult Index() => Page("Uso mensal SaaS", "Acompanhe consumo e bloqueios por tenant.", "api/saas/uso", "Filtrar por tenant", "Ver bloqueios"); }
public sealed class OperacaoSaasController : SaasComercialPageController { public IActionResult Health() => Page("Health operacional", "API, Web, banco, erros, uso e auditoria.", "api/operacao/health", "Ver erros", "Ver performance"); public IActionResult Erros() => Page("Erros recentes", "Erros agrupados e sanitizados.", "api/operacao/erros", "Filtrar tenant"); public IActionResult Performance() => Page("Performance", "Endpoints lentos e tendência de uso.", "api/operacao/endpoints-lentos", "Investigar"); public IActionResult Uso() => Page("Uso operacional", "Uso por tenant e módulo.", "api/operacao/uso", "Exportar resumo"); public IActionResult Auditoria() => Page("Auditoria operacional", "Ações críticas e falhas de permissão.", "api/operacao/auditoria-resumo", "Filtrar período"); }
