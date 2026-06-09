using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Controllers;

[AllowAnonymous]
public sealed class CommercialDemoWebController : Controller
{
    [HttpGet("/")]
    [HttpGet("/plantaopro")]
    public IActionResult Landing() => View("Landing", Page("PlantãoPro para gestão de plantões médicos", "Uma plataforma SaaS multi-tenant, white label e pronta para demonstrar valor comercial em hospitais, clínicas e parceiros.", "landing",
        Card("Hospitais", "Reduza plantões descobertos e ganhe rastreabilidade da escala ao pagamento.", "Ver casos de uso", "CommercialDemoWeb", "CasosUso"),
        Card("Parceiros white label", "Revenda uma operação completa com marca, módulos e portal do parceiro.", "Conhecer planos", "PlanosPublicos", "Index"),
        Card("Comercial MNSOFT", "Capture leads, simule plano ideal e gere propostas com preview imprimível.", "Agendar demonstração", "CommercialDemoWeb", "Demo")));

    [HttpGet("/simulador")]
    public IActionResult Simulador() => View("Simulador", Page("Simulador de plano ideal", "Informe volume operacional, módulos e necessidades comerciais para receber uma recomendação objetiva.", "simulador"));

    [HttpPost("/simulador/resultado")]
    [ValidateAntiForgeryToken]
    public IActionResult SimuladorResultado([FromForm] int medicos, [FromForm] int unidades, [FromForm] int plantoesMes, [FromForm] bool whiteLabel, [FromForm] bool api, [FromForm] bool bi, [FromForm] bool revenda)
    {
        var enterprise = medicos > 200 || unidades > 10 || plantoesMes > 1000 || revenda;
        var profissional = !enterprise && (medicos > 40 || unidades > 2 || plantoesMes > 180 || whiteLabel || api || bi);
        var plano = enterprise ? "Enterprise" : profissional ? "Profissional" : "Essencial";
        var model = Page("Resultado do simulador", $"Plano recomendado: {plano}", "resultado",
            Card(plano, enterprise ? "Contrato customizado para alto volume, revenda, integrações e SLA avançado." : profissional ? "Melhor equilíbrio para operação com módulos avançados." : "Ideal para iniciar com self-service e governança básica.", "Receber proposta", "CommercialDemoWeb", "Contato"),
            Card("Próximo passo", "O resultado pode virar lead comercial, cadastro self-service ou proposta customizada.", "Começar cadastro", "Cadastro", "Index"));
        model.Checklist = new List<string> { $"Médicos: {medicos}", $"Unidades: {unidades}", $"Plantões/mês: {plantoesMes}", whiteLabel ? "White label solicitado" : "White label não solicitado", api ? "API solicitada" : "API não solicitada", bi ? "BI solicitado" : "BI não solicitado" };
        return View("ResultadoSimulador", model);
    }

    [HttpGet("/casos-de-uso")]
    public IActionResult CasosUso() => View("CommercialPage", Page("Casos de uso", "Demonstre o PlantãoPro por perfil de comprador e por maturidade operacional.", "casos",
        Card("Hospital", "Central de plantões, convites, escalas, substituições, pagamentos e auditoria.", "Agendar demo", "CommercialDemoWeb", "Demo"),
        Card("Clínica", "Cadastro self-service, limites por plano e onboarding guiado para operação enxuta.", "Ver planos", "PlanosPublicos", "Index"),
        Card("Parceiro", "White label, portal parceiro, propostas e comissionamento demonstrável.", "Falar com comercial", "CommercialDemoWeb", "Contato")));

    [HttpGet("/contato")]
    public IActionResult Contato() => View("Contato", Page("Fale com o comercial", "Cadastre seu interesse para receber contato, proposta ou roteiro de demonstração.", "contato"));

    [HttpPost("/contato")]
    [ValidateAntiForgeryToken]
    public IActionResult EnviarContato([FromForm] string nome, [FromForm] string email, [FromForm] string telefone, [FromForm] string empresa, [FromForm] string website)
    {
        if (!string.IsNullOrWhiteSpace(website))
        {
            TempData["Error"] = "Não foi possível enviar sua solicitação.";
            return RedirectToAction(nameof(Contato));
        }
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(telefone) || string.IsNullOrWhiteSpace(empresa))
        {
            TempData["Error"] = "Informe nome, e-mail, telefone e empresa.";
            return RedirectToAction(nameof(Contato));
        }
        TempData["Success"] = "Solicitação registrada. O comercial entrará em contato.";
        return RedirectToAction(nameof(Contato));
    }

    [HttpGet("/demo")]
    public IActionResult Demo() => View("CommercialPage", Page("Demo comercial guiada", "Use roteiros por hospital, clínica e parceiro white label sem expor dados reais.", "demo",
        Card("Roteiro hospital", "Lead, proposta, conversão, tenant, plantão, convite, escala e pagamento.", "Abrir simulador", "CommercialDemoWeb", "Simulador"),
        Card("Roteiro parceiro", "Portal parceiro, lead, proposta white label, materiais e comissão.", "Ver parceiro", "ParceiroPortal", "Index"),
        Card("Roteiro SaaS", "Admin SaaS, billing, onboarding, módulos, feature flags e CS.", "Ver Admin SaaS", "AdminSaas", "Index")));

    private static CommercialDemoPageViewModel Page(string title, string subtitle, string route, params CommercialCardViewModel[] cards) => new CommercialDemoPageViewModel
    {
        Title = title,
        Subtitle = subtitle,
        ActiveRoute = route,
        Cards = cards,
        PrimaryActions = new List<string> { "Começar agora", "Agendar demonstração", "Ver planos" },
        Checklist = new List<string> { "Multi-tenant", "White label", "Billing e planos", "LGPD e auditoria", "Operação de plantões" }
    };

    private static CommercialCardViewModel Card(string title, string description, string actionText, string controller, string action) => new CommercialCardViewModel { Title = title, Description = description, ActionText = actionText, ActionController = controller, ActionName = action };
}

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Suporte + "," + RolesConstants.Auditor)]
public sealed class AdminSaasController : Controller
{
    public IActionResult Index() => View("Dashboard", Dashboard("Admin SaaS MNSOFT", "Leads do mês, propostas, clientes, MRR, implantação, billing e alertas críticos."));
    public IActionResult Clientes() => View("Dashboard", Dashboard("Clientes SaaS", "Clientes ativos, trial, implantação, inadimplência e risco."));
    public IActionResult Tenants() => View("Dashboard", Dashboard("Tenants", "Isolamento, status e módulos habilitados por tenant."));
    public IActionResult Planos() => View("Dashboard", Dashboard("Planos", "Comparação, limites e módulos contratados."));
    public IActionResult Assinaturas() => View("Dashboard", Dashboard("Assinaturas", "MRR, faturas e status de cobrança."));
    public IActionResult Propostas() => View("Dashboard", Dashboard("Propostas", "Funil comercial e validade."));
    public IActionResult Leads() => View("Dashboard", Dashboard("Leads", "Origem, status e conversão."));
    public IActionResult Parceiros() => View("Dashboard", Dashboard("Parceiros", "Revendedores, tenants vinculados e comissões."));
    public IActionResult Billing() => View("Dashboard", Dashboard("Billing", "Receita recebida, pendências e inadimplência."));
    public IActionResult Alertas() => View("Dashboard", Dashboard("Alertas", "Clientes em risco, incidentes e limites próximos."));
    public IActionResult Implantacoes() => View("Dashboard", Dashboard("Implantações", "Onboarding e tarefas pendentes."));
    public IActionResult Monitoramento() => View("Dashboard", Dashboard("Monitoramento", "Saúde operacional e observabilidade."));
    private static CommercialDemoPageViewModel Dashboard(string title, string subtitle) => CommercialDemoWebControllerPageFactory.Build(title, subtitle);
}

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor)]
public sealed class ClientePortalController : Controller
{
    public IActionResult Index() => View("Dashboard", CommercialDemoWebControllerPageFactory.Build("Portal do cliente", "Meu plano, uso, faturas, usuários, white label, onboarding e suporte."));
    public IActionResult MeuPlano() => Index(); public IActionResult Uso() => Index(); public IActionResult Faturas() => Index(); public IActionResult Usuarios() => Index(); public IActionResult Perfis() => Index(); public IActionResult WhiteLabel() => Index(); public IActionResult Parametrizacoes() => Index(); public IActionResult Onboarding() => Index(); public IActionResult Suporte() => Index(); public IActionResult Treinamento() => Index();
}

[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Parceiro)]
public sealed class ParceiroPortalController : Controller
{
    public IActionResult Index() => View("Dashboard", CommercialDemoWebControllerPageFactory.Build("Portal do parceiro", "Leads, clientes, propostas, comissões, repasses, materiais e suporte."));
    public IActionResult Leads() => Index(); public IActionResult Clientes() => Index(); public IActionResult Propostas() => Index(); public IActionResult Comissoes() => Index(); public IActionResult Repasses() => Index(); public IActionResult Materiais() => Index(); public IActionResult Suporte() => Index();
}

[Authorize]
public sealed class PropostasComerciaisController : Controller
{
    public IActionResult Index() => View("Dashboard", CommercialDemoWebControllerPageFactory.Build("Propostas comerciais", "Gere proposta com plano, módulos, setup, mensalidade, SLA, condições, validade e preview."));
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index([FromForm] string cliente) { TempData["Success"] = "Proposta salva para demonstração comercial."; return View("Dashboard", CommercialDemoWebControllerPageFactory.Build("Propostas comerciais", "Proposta registrada e pronta para preview imprimível.")); }
    public IActionResult Create() => View("PropostaForm", CommercialDemoWebControllerPageFactory.Build("Nova proposta", "Preencha os dados comerciais e gere preview imprimível."));
    public IActionResult Edit(Guid id) => Create(); public IActionResult Details(Guid id) => Index(); public IActionResult Preview(Guid id) => View("Preview", CommercialDemoWebControllerPageFactory.Build("Preview da proposta", "HTML imprimível para salvar como PDF pelo navegador.")); public IActionResult Enviar(Guid id) => Index();
}

[Authorize]
public sealed class ModulosController : Controller
{
    public IActionResult Index() => View("Dashboard", CommercialDemoWebControllerPageFactory.Build("Governança de módulos", "Controle por plano, tenant, adicionais contratados e beta."));
    public IActionResult Create() => Index(); public IActionResult Edit(Guid id) => Index(); public IActionResult Details(Guid id) => Index(); public IActionResult Tenant(Guid id) => Index();
}

[Authorize]
public sealed class FeatureFlagsController : Controller
{
    public IActionResult Index() => View("Dashboard", CommercialDemoWebControllerPageFactory.Build("Feature flags", "Habilite funcionalidades globalmente ou por tenant com auditoria."));
}

[Authorize]
[Route("Demo")]
public sealed class DemoAdminController : Controller
{
    [HttpGet("Index")] public IActionResult Index() => View("Dashboard", CommercialDemoWebControllerPageFactory.Build("Modo demo comercial", "Gere e limpe apenas dados fictícios marcados como demo."));
    [HttpGet("GerarDados")] public IActionResult GerarDados() => Index();
    [HttpGet("Roteiros")] public IActionResult Roteiros() => Index();
    [HttpGet("Status")] public IActionResult Status() => Index();
}

internal static class CommercialDemoWebControllerPageFactory
{
    public static CommercialDemoPageViewModel Build(string title, string subtitle) => new CommercialDemoPageViewModel
    {
        Title = title,
        Subtitle = subtitle,
        Cards = new List<CommercialCardViewModel>
        {
            new CommercialCardViewModel { Title = "KPIs", Description = "Indicadores agregados sem dados sensíveis desnecessários.", ActionText = "Atualizar visão", ActionController = "Home", ActionName = "Dashboard" },
            new CommercialCardViewModel { Title = "Ações", Description = "Links conectados a controllers reais para demonstração navegável.", ActionText = "Ver landing", ActionController = "CommercialDemoWeb", ActionName = "Landing" },
            new CommercialCardViewModel { Title = "Auditoria", Description = "Operações críticas da API registram trilha de auditoria.", ActionText = "Ver auditoria", ActionController = "Auditoria", ActionName = "Index" }
        },
        Checklist = new List<string> { "Permissões por perfil", "Isolamento por tenant", "Módulos e limites", "White label", "Billing e onboarding" }
    };
}
