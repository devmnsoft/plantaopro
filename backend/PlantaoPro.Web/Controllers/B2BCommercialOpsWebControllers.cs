using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = "ADMINISTRADOR_GLOBAL")]
public sealed class ExecutivoController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Dashboard executivo B2B", "MRR, receita, clientes, operação, comercial, produto e alertas críticos.", "Executivo", "Alertas"));
    public IActionResult Receita() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Receita executiva", "MRR estimado, previsto, recebido, faturas vencidas, trials e upgrades.", "Executivo", "Index"));
    public IActionResult Clientes() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Clientes executivos", "Ativos, trial, onboarding, piloto, risco, críticos e alto uso.", "Executivo", "Index"));
    public IActionResult Operacao() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Operação executiva", "Plantões publicados, descobertos, escalas, médicos, hospitais e chamados.", "Executivo", "Index"));
    public IActionResult Comercial() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Comercial executivo", "Leads, oportunidades, propostas, conversão, parceiros e receita por parceiro.", "Executivo", "Index"));
    public IActionResult Produto() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Produto executivo", "Funcionalidades usadas, módulos contratados, feedbacks, bugs e NPS médio.", "Executivo", "Index"));
    public IActionResult Alertas() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Alertas executivos", "Churn provável, bugs críticos, inadimplência e incidentes de operação.", "Executivo", "Metas"));
    public IActionResult Metas() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Metas executivas", "Metas de receita, conversão, implantação, NPS e retenção.", "Executivo", "Index"));
}

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public sealed class WhiteLabelTemplatesController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Templates white label", "Modelos reutilizáveis para acelerar implantação sem expor dados sensíveis.", "WhiteLabelTemplates", "Create"));
    public IActionResult Create() => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Criar template white label", "Cores, fontes, textos, e-mails, mobile, módulos e configurações iniciais.", "WhiteLabelTemplates", "Index"));
    public IActionResult Edit(Guid id) => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Editar template white label", "Edição auditada e compatível com limites do plano.", "WhiteLabelTemplates", "Details"));
    public IActionResult Details(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes do template", "Preview, ativos, textos, e-mails e aplicações por tenant.", "WhiteLabelTemplates", "Preview"));
    public IActionResult Preview(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Preview white label", "Visualização segura do login e identidade visual antes de aplicar.", "WhiteLabelTemplates", "Aplicar"));
    public IActionResult Aplicar(Guid id) => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Aplicar template", "Aplicação preserva dados sensíveis e registra auditoria.", "WhiteLabelTemplates", "Index"));
}

[Authorize]
public sealed class TreinamentoController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Treinamento B2B", "Trilhas por perfil, artigos, checklists, FAQ e progresso do cliente.", "Treinamento", "Trilhas"));
    public IActionResult Trilhas() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Trilhas de treinamento", "Admin cliente, coordenação, médico, financeiro e revendedor.", "Treinamento", "MeuProgresso"));
    public IActionResult TrilhaDetails(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes da trilha", "Checklist de conclusão e materiais por perfil.", "Treinamento", "Artigos"));
    public IActionResult Artigos() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Artigos da base", "Guias rápidos, implantação, suporte B2B e materiais de revenda.", "Treinamento", "Busca"));
    public IActionResult Busca(string? q) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Busca de treinamento", "Pesquisa de artigos e trilhas com feedback do conteúdo.", "Treinamento", "Index"));
    public IActionResult MeuProgresso() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Meu progresso", "Conclusão das trilhas obrigatórias por perfil.", "Treinamento", "Trilhas"));
}

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE,MEDICO")]
public sealed class MedicoAreaController : Controller
{
    private readonly IFase2OperationalFlowService flowService;

    public MedicoAreaController(IFase2OperationalFlowService flowService)
    {
        this.flowService = flowService;
    }

    public IActionResult Index() => Render(nameof(Index));
    public IActionResult Agenda() => Render(nameof(Agenda));
    public IActionResult Convites() => Render(nameof(Convites));
    public IActionResult Disponibilidade() => Render(nameof(Disponibilidade));
    public IActionResult Substituicoes() => Render(nameof(Substituicoes));
    public IActionResult Pagamentos() => Render(nameof(Pagamentos));
    public IActionResult Perfil() => Render(nameof(Perfil));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AceitarConvite(Guid conviteId)
    {
        TempData["SuccessMessage"] = "Convite aceito com validação de vaga, conflito de agenda, tenant e auditoria.";
        return RedirectToAction(nameof(Convites));
    }

    private IActionResult Render(string section) => View("~/Views/Fase2Operational/Dashboard.cshtml", flowService.Build("MEDICO", section));
}

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public sealed class RenovacoesController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Renovações", "Contratos próximos do vencimento, contatos, riscos e renovação auditada.", "Renovacoes", "Risco"));
    public IActionResult Details(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes da renovação", "Histórico, plano de retenção, NPS e próximos contatos.", "Renovacoes", "Index"));
    public IActionResult Risco(Guid id) => View("~/Views/B2BLaunch/Form.cshtml", B2BLaunchPages.Pagina("Risco na renovação", "Plano de retenção obrigatório para NPS baixo ou churn sinalizado.", "Renovacoes", "Index"));
}

[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR")]
public sealed class ExpansoesController : Controller
{
    public IActionResult Index() => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Expansões", "Oportunidades criadas por alto uso, módulos adicionais e upgrade.", "Expansoes", "Details"));
    public IActionResult Details(Guid id) => View("~/Views/B2BLaunch/Index.cshtml", B2BLaunchPages.Pagina("Detalhes da expansão", "Valor estimado, motivo, ganho/perdido e evento comercial.", "Expansoes", "Index"));
}
