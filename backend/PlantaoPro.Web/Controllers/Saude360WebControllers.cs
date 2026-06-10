using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class PacientesController : Controller
{
    private readonly ISaude360WebService service;
    public PacientesController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Create()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Create"));
    }
    public IActionResult Edit()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Edit"));
    }
    public IActionResult Details()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Details"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "Pacientes", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Create", Controller = "Pacientes", Action = "Create" },
            new Saude360ActionLinkViewModel { Title = "Edit", Controller = "Pacientes", Action = "Edit" },
            new Saude360ActionLinkViewModel { Title = "Details", Controller = "Pacientes", Action = "Details" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Pacientes", "PACIENTES", actionTitle, "/api/pacientes", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class PainelChamadaController : Controller
{
    private readonly ISaude360WebService service;
    public PainelChamadaController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Tv()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Tv"));
    }
    public IActionResult Configuracoes()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Configuracoes"));
    }
    public IActionResult Historico()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Historico"));
    }
    public IActionResult Setores()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Setores"));
    }
    public IActionResult Salas()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Salas"));
    }
    public IActionResult Guiches()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Guiches"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "PainelChamada", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Tv", Controller = "PainelChamada", Action = "Tv" },
            new Saude360ActionLinkViewModel { Title = "Configuracoes", Controller = "PainelChamada", Action = "Configuracoes" },
            new Saude360ActionLinkViewModel { Title = "Historico", Controller = "PainelChamada", Action = "Historico" },
            new Saude360ActionLinkViewModel { Title = "Setores", Controller = "PainelChamada", Action = "Setores" },
            new Saude360ActionLinkViewModel { Title = "Salas", Controller = "PainelChamada", Action = "Salas" },
            new Saude360ActionLinkViewModel { Title = "Guiches", Controller = "PainelChamada", Action = "Guiches" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Painel de chamada", "PAINEL_CHAMADA", actionTitle, "/api/painel-chamada", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class AgendamentosController : Controller
{
    private readonly ISaude360WebService service;
    public AgendamentosController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Create()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Create"));
    }
    public IActionResult Edit()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Edit"));
    }
    public IActionResult Details()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Details"));
    }
    public IActionResult Calendario()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Calendario"));
    }
    public IActionResult AgendaDia()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("AgendaDia"));
    }
    public IActionResult AgendaMedico()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("AgendaMedico"));
    }
    public IActionResult CheckIn()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("CheckIn"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "Agendamentos", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Create", Controller = "Agendamentos", Action = "Create" },
            new Saude360ActionLinkViewModel { Title = "Edit", Controller = "Agendamentos", Action = "Edit" },
            new Saude360ActionLinkViewModel { Title = "Details", Controller = "Agendamentos", Action = "Details" },
            new Saude360ActionLinkViewModel { Title = "Calendario", Controller = "Agendamentos", Action = "Calendario" },
            new Saude360ActionLinkViewModel { Title = "AgendaDia", Controller = "Agendamentos", Action = "AgendaDia" },
            new Saude360ActionLinkViewModel { Title = "AgendaMedico", Controller = "Agendamentos", Action = "AgendaMedico" },
            new Saude360ActionLinkViewModel { Title = "CheckIn", Controller = "Agendamentos", Action = "CheckIn" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Agendamento clínico", "AGENDAMENTO", actionTitle, "/api/agendamentos", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class TriagemController : Controller
{
    private readonly ISaude360WebService service;
    public TriagemController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Create()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Create"));
    }
    public IActionResult Edit()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Edit"));
    }
    public IActionResult Details()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Details"));
    }
    public IActionResult Fila()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Fila"));
    }
    public IActionResult HistoricoPaciente()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("HistoricoPaciente"));
    }
    public IActionResult ClassificacaoRisco()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("ClassificacaoRisco"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "Triagem", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Create", Controller = "Triagem", Action = "Create" },
            new Saude360ActionLinkViewModel { Title = "Edit", Controller = "Triagem", Action = "Edit" },
            new Saude360ActionLinkViewModel { Title = "Details", Controller = "Triagem", Action = "Details" },
            new Saude360ActionLinkViewModel { Title = "Fila", Controller = "Triagem", Action = "Fila" },
            new Saude360ActionLinkViewModel { Title = "HistoricoPaciente", Controller = "Triagem", Action = "HistoricoPaciente" },
            new Saude360ActionLinkViewModel { Title = "ClassificacaoRisco", Controller = "Triagem", Action = "ClassificacaoRisco" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Triagem", "TRIAGEM", actionTitle, "/api/triagens", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class ConsultasController : Controller
{
    private readonly ISaude360WebService service;
    public ConsultasController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Create()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Create"));
    }
    public IActionResult Edit()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Edit"));
    }
    public IActionResult Details()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Details"));
    }
    public IActionResult Atendimento()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Atendimento"));
    }
    public IActionResult HistoricoPaciente()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("HistoricoPaciente"));
    }
    public IActionResult Resumo()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Resumo"));
    }
    public IActionResult Imprimir()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Imprimir"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "Consultas", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Create", Controller = "Consultas", Action = "Create" },
            new Saude360ActionLinkViewModel { Title = "Edit", Controller = "Consultas", Action = "Edit" },
            new Saude360ActionLinkViewModel { Title = "Details", Controller = "Consultas", Action = "Details" },
            new Saude360ActionLinkViewModel { Title = "Atendimento", Controller = "Consultas", Action = "Atendimento" },
            new Saude360ActionLinkViewModel { Title = "HistoricoPaciente", Controller = "Consultas", Action = "HistoricoPaciente" },
            new Saude360ActionLinkViewModel { Title = "Resumo", Controller = "Consultas", Action = "Resumo" },
            new Saude360ActionLinkViewModel { Title = "Imprimir", Controller = "Consultas", Action = "Imprimir" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Consultas", "CONSULTAS", actionTitle, "/api/consultas", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class CidController : Controller
{
    private readonly ISaude360WebService service;
    public CidController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Create()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Create"));
    }
    public IActionResult Edit()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Edit"));
    }
    public IActionResult Details()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Details"));
    }
    public IActionResult Importar()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Importar"));
    }
    public IActionResult Favoritos()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Favoritos"));
    }
    public IActionResult MaisUsados()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("MaisUsados"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "Cid", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Create", Controller = "Cid", Action = "Create" },
            new Saude360ActionLinkViewModel { Title = "Edit", Controller = "Cid", Action = "Edit" },
            new Saude360ActionLinkViewModel { Title = "Details", Controller = "Cid", Action = "Details" },
            new Saude360ActionLinkViewModel { Title = "Importar", Controller = "Cid", Action = "Importar" },
            new Saude360ActionLinkViewModel { Title = "Favoritos", Controller = "Cid", Action = "Favoritos" },
            new Saude360ActionLinkViewModel { Title = "MaisUsados", Controller = "Cid", Action = "MaisUsados" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Tabela CID", "CID", actionTitle, "/api/cid", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class PrescricoesController : Controller
{
    private readonly ISaude360WebService service;
    public PrescricoesController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Create()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Create"));
    }
    public IActionResult Edit()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Edit"));
    }
    public IActionResult Details()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Details"));
    }
    public IActionResult Imprimir()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Imprimir"));
    }
    public IActionResult Modelos()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Modelos"));
    }
    public IActionResult HistoricoPaciente()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("HistoricoPaciente"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "Prescricoes", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Create", Controller = "Prescricoes", Action = "Create" },
            new Saude360ActionLinkViewModel { Title = "Edit", Controller = "Prescricoes", Action = "Edit" },
            new Saude360ActionLinkViewModel { Title = "Details", Controller = "Prescricoes", Action = "Details" },
            new Saude360ActionLinkViewModel { Title = "Imprimir", Controller = "Prescricoes", Action = "Imprimir" },
            new Saude360ActionLinkViewModel { Title = "Modelos", Controller = "Prescricoes", Action = "Modelos" },
            new Saude360ActionLinkViewModel { Title = "HistoricoPaciente", Controller = "Prescricoes", Action = "HistoricoPaciente" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Prescrição médica", "PRESCRICAO", actionTitle, "/api/prescricoes", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class ClinicaFinanceiroController : Controller
{
    private readonly ISaude360WebService service;
    public ClinicaFinanceiroController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Receber()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Receber"));
    }
    public IActionResult ContasReceber()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("ContasReceber"));
    }
    public IActionResult Caixa()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Caixa"));
    }
    public IActionResult FechamentoCaixa()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("FechamentoCaixa"));
    }
    public IActionResult Repasses()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Repasses"));
    }
    public IActionResult Relatorios()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Relatorios"));
    }
    public IActionResult Glosas()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Glosas"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "ClinicaFinanceiro", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Receber", Controller = "ClinicaFinanceiro", Action = "Receber" },
            new Saude360ActionLinkViewModel { Title = "ContasReceber", Controller = "ClinicaFinanceiro", Action = "ContasReceber" },
            new Saude360ActionLinkViewModel { Title = "Caixa", Controller = "ClinicaFinanceiro", Action = "Caixa" },
            new Saude360ActionLinkViewModel { Title = "FechamentoCaixa", Controller = "ClinicaFinanceiro", Action = "FechamentoCaixa" },
            new Saude360ActionLinkViewModel { Title = "Repasses", Controller = "ClinicaFinanceiro", Action = "Repasses" },
            new Saude360ActionLinkViewModel { Title = "Relatorios", Controller = "ClinicaFinanceiro", Action = "Relatorios" },
            new Saude360ActionLinkViewModel { Title = "Glosas", Controller = "ClinicaFinanceiro", Action = "Glosas" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Financeiro da clínica", "FINANCEIRO_CLINICA", actionTitle, "/api/clinica-financeiro", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class ConveniosController : Controller
{
    private readonly ISaude360WebService service;
    public ConveniosController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Create()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Create"));
    }
    public IActionResult Edit()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Edit"));
    }
    public IActionResult Details()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Details"));
    }
    public IActionResult Contratos()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Contratos"));
    }
    public IActionResult Planos()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Planos"));
    }
    public IActionResult Tabelas()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Tabelas"));
    }
    public IActionResult Autorizacoes()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Autorizacoes"));
    }
    public IActionResult Glosas()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Glosas"));
    }
    public IActionResult Faturamento()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Faturamento"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "Convenios", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Create", Controller = "Convenios", Action = "Create" },
            new Saude360ActionLinkViewModel { Title = "Edit", Controller = "Convenios", Action = "Edit" },
            new Saude360ActionLinkViewModel { Title = "Details", Controller = "Convenios", Action = "Details" },
            new Saude360ActionLinkViewModel { Title = "Contratos", Controller = "Convenios", Action = "Contratos" },
            new Saude360ActionLinkViewModel { Title = "Planos", Controller = "Convenios", Action = "Planos" },
            new Saude360ActionLinkViewModel { Title = "Tabelas", Controller = "Convenios", Action = "Tabelas" },
            new Saude360ActionLinkViewModel { Title = "Autorizacoes", Controller = "Convenios", Action = "Autorizacoes" },
            new Saude360ActionLinkViewModel { Title = "Glosas", Controller = "Convenios", Action = "Glosas" },
            new Saude360ActionLinkViewModel { Title = "Faturamento", Controller = "Convenios", Action = "Faturamento" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Convênios", "CONVENIOS", actionTitle, "/api/convenios", actions, rules);
    }
}

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.FinanceiroArea + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public sealed class PlanosSaudeController : Controller
{
    private readonly ISaude360WebService service;
    public PlanosSaudeController(ISaude360WebService service) { this.service = service; }
    public IActionResult Index()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Index"));
    }
    public IActionResult Create()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Create"));
    }
    public IActionResult Edit()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Edit"));
    }
    public IActionResult Details()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Details"));
    }
    public IActionResult Pacientes()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Pacientes"));
    }
    public IActionResult Coberturas()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Coberturas"));
    }
    public IActionResult Autorizacoes()
    {
        return View("~/Views/Saude360/Modulo.cshtml", Page("Autorizacoes"));
    }

    private Saude360ModulePageViewModel Page(string actionTitle)
    {
        var actions = new List<Saude360ActionLinkViewModel>
        {
            new Saude360ActionLinkViewModel { Title = "Index", Controller = "PlanosSaude", Action = "Index" },
            new Saude360ActionLinkViewModel { Title = "Create", Controller = "PlanosSaude", Action = "Create" },
            new Saude360ActionLinkViewModel { Title = "Edit", Controller = "PlanosSaude", Action = "Edit" },
            new Saude360ActionLinkViewModel { Title = "Details", Controller = "PlanosSaude", Action = "Details" },
            new Saude360ActionLinkViewModel { Title = "Pacientes", Controller = "PlanosSaude", Action = "Pacientes" },
            new Saude360ActionLinkViewModel { Title = "Coberturas", Controller = "PlanosSaude", Action = "Coberturas" },
            new Saude360ActionLinkViewModel { Title = "Autorizacoes", Controller = "PlanosSaude", Action = "Autorizacoes" },
        };
        var rules = new List<string>
        {
            "Multi-tenant por cliente/tenant autenticado",
            "Auditoria operacional nas ações críticas",
            "Permissões por perfil e plano Saúde 360",
            "Integração com API validada do módulo",
        };
        return service.BuildPage("Planos de saúde", "PLANOS_SAUDE", actionTitle, "/api/planos-saude", actions, rules);
    }
}
