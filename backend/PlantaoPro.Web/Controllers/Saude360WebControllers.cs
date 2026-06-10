using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Operacao + "," + RolesConstants.Financeiro + "," + RolesConstants.Medico + "," + RolesConstants.Recepcao + "," + RolesConstants.Triagem + "," + RolesConstants.FinanceiroClinica + "," + RolesConstants.FaturamentoConvenio + "," + RolesConstants.AdministradorClinica)]
public abstract class Saude360WebControllerBase : BaseWebController
{
    private readonly Saude360WebService service;
    protected Saude360WebControllerBase(IHttpClientFactory factory, ILogger logger, Saude360WebService service) : base(factory, logger) { this.service = service; }

    protected async Task<IActionResult> ModuloAsync(string titulo, string modulo, string descricao, string endpoint, IEnumerable<Saude360ActionLinkViewModel> acoes)
    {
        var token = GetJwtToken();
        if (string.IsNullOrWhiteSpace(token)) return HandleUnauthorized();
        var result = await service.ListarAsync(token, endpoint);
        return View("~/Views/Saude360/Modulo.cshtml", new Saude360PageViewModel
        {
            Titulo = titulo,
            Modulo = modulo,
            Descricao = descricao,
            Controller = ControllerContext.ActionDescriptor.ControllerName,
            Action = ControllerContext.ActionDescriptor.ActionName,
            Endpoint = endpoint,
            Registros = result.Registros,
            ErrorMessage = result.Error,
            Acoes = acoes,
            Plano = PlanoModulo(modulo),
            Permissao = PermissaoModulo(modulo)
        });
    }

    protected IActionResult Formulario(string titulo, string endpoint, Guid? id = null)
    {
        return View("~/Views/Saude360/Formulario.cshtml", new Saude360FormViewModel
        {
            Titulo = titulo,
            Controller = ControllerContext.ActionDescriptor.ControllerName,
            Action = ControllerContext.ActionDescriptor.ActionName,
            ApiEndpoint = endpoint,
            Id = id
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(Saude360FormViewModel form)
    {
        var token = GetJwtToken();
        if (string.IsNullOrWhiteSpace(token)) return HandleUnauthorized();
        var result = await service.EnviarAsync(token, form.ApiEndpoint, form);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction("Index");
    }

    protected static IEnumerable<Saude360ActionLinkViewModel> Links(params Saude360ActionLinkViewModel[] links) { return links; }
    protected static Saude360ActionLinkViewModel Link(string title, string action, string icon) { return new Saude360ActionLinkViewModel { Titulo = title, Action = action, Icone = icon }; }
    private static string PlanoModulo(string modulo) { return modulo == "Convênios" || modulo == "Planos de saúde" ? "Enterprise" : modulo == "Triagem" || modulo == "Prescrição médica" || modulo == "CID" || modulo == "Financeiro clínica" ? "Profissional" : "Essencial"; }
    private static string PermissaoModulo(string modulo) { return "Permissão por tenant, perfil e plano para " + modulo + "."; }
}

public sealed class PainelChamadaController : Saude360WebControllerBase
{
    public PainelChamadaController(IHttpClientFactory f, ILogger<PainelChamadaController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Painel de chamada", "Painel de chamada", "Fila, chamada, rechamada, ausentes e histórico sem expor dados sensíveis na TV.", "api/painel-chamada", Links(Link("Configurações", "Configuracoes", "bi-gear"), Link("TV", "Tv", "bi-tv"), Link("Histórico", "Historico", "bi-clock-history"))); }
    public Task<IActionResult> Tv() { return ModuloAsync("TV do painel", "Painel de chamada", "Visualização white label para sala de espera com token público seguro quando habilitado.", "api/painel-chamada", Links(Link("Fila", "Index", "bi-list"))); }
    public Task<IActionResult> Configuracoes() { return ModuloAsync("Configurações do painel", "Painel de chamada", "Setores, salas, guichês, token seguro e regras de exibição.", "api/painel-chamada", Links(Link("Setores", "Setores", "bi-diagram-3"), Link("Salas", "Salas", "bi-door-open"), Link("Guichês", "Guiches", "bi-window"))); }
    public Task<IActionResult> Historico() { return ModuloAsync("Histórico de chamadas", "Painel de chamada", "Auditoria operacional de chamadas, rechamadas, ausências e finalizações.", "api/painel-chamada/historico", Links(Link("Fila", "Index", "bi-list"))); }
    public Task<IActionResult> Setores() { return Configuracoes(); }
    public Task<IActionResult> Salas() { return Configuracoes(); }
    public Task<IActionResult> Guiches() { return Configuracoes(); }
}

public sealed class AgendamentosController : Saude360WebControllerBase
{
    public AgendamentosController(IHttpClientFactory f, ILogger<AgendamentosController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Agendamentos", "Agendamento", "Agenda clínica por tenant, médico, paciente e status, com bloqueio de conflito de horário.", "api/agendamentos", Links(Link("Novo", "Create", "bi-plus-circle"), Link("Calendário", "Calendario", "bi-calendar3"), Link("Check-in", "CheckIn", "bi-person-check"))); }
    public IActionResult Create() { return Formulario("Novo agendamento", "api/agendamentos"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar agendamento", "api/agendamentos/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do agendamento", "Agendamento", "Dados operacionais e status do agendamento.", "api/agendamentos/" + id, Links(Link("Agenda", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Calendario() { return ModuloAsync("Calendário clínico", "Agendamento", "Visão de calendário para recepção e gestão clínica.", "api/agendamentos/calendario", Links(Link("Novo", "Create", "bi-plus-circle"))); }
    public Task<IActionResult> AgendaDia() { return Index(); }
    public Task<IActionResult> AgendaMedico() { return Index(); }
    public Task<IActionResult> CheckIn() { return ModuloAsync("Check-in", "Agendamento", "Check-in altera status e pode enviar o paciente para painel e triagem.", "api/agendamentos", Links(Link("Agenda", "Index", "bi-calendar"))); }
}

public sealed class TriagemController : Saude360WebControllerBase
{
    public TriagemController(IHttpClientFactory f, ILogger<TriagemController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Triagens", "Triagem", "Fila assistencial com classificação de risco, sinais vitais e encaminhamento para consulta.", "api/triagens", Links(Link("Nova", "Create", "bi-plus-circle"), Link("Fila", "Fila", "bi-people"), Link("Classificação", "ClassificacaoRisco", "bi-flag"))); }
    public IActionResult Create() { return Formulario("Nova triagem", "api/triagens"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar triagem", "api/triagens/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes da triagem", "Triagem", "Resumo assistencial auditado da triagem.", "api/triagens/" + id, Links(Link("Triagens", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Fila() { return ModuloAsync("Fila de triagem", "Triagem", "Pacientes aguardando classificação e encaminhamento.", "api/triagens/fila", Links(Link("Nova", "Create", "bi-plus-circle"))); }
    public Task<IActionResult> HistoricoPaciente() { return Index(); }
    public Task<IActionResult> ClassificacaoRisco() { return ModuloAsync("Classificação de risco", "Triagem", "Protocolos e cores de risco configuráveis por clínica.", "api/triagens", Links(Link("Fila", "Fila", "bi-people"))); }
}

public sealed class ConsultasController : Saude360WebControllerBase
{
    public ConsultasController(IHttpClientFactory f, ILogger<ConsultasController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Consultas", "Consultas", "Atendimento médico com vínculo a triagem, CID, prescrição e auditoria de impressão.", "api/consultas", Links(Link("Nova", "Create", "bi-plus-circle"), Link("Atendimento", "Atendimento", "bi-clipboard2-pulse"), Link("Histórico", "HistoricoPaciente", "bi-journal-medical"))); }
    public IActionResult Create() { return Formulario("Nova consulta", "api/consultas"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar consulta", "api/consultas/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes da consulta", "Consultas", "Resumo clínico com restrição por médico/tenant.", "api/consultas/" + id, Links(Link("Consultas", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Atendimento() { return Index(); }
    public Task<IActionResult> HistoricoPaciente() { return Index(); }
    public Task<IActionResult> Resumo() { return Index(); }
    public Task<IActionResult> Imprimir() { return ModuloAsync("Impressão de consulta", "Consultas", "Impressão com registro em auditoria e sem log técnico de conteúdo sensível.", "api/consultas", Links(Link("Consultas", "Index", "bi-arrow-left"))); }
}

public sealed class CidController : Saude360WebControllerBase
{
    public CidController(IHttpClientFactory f, ILogger<CidController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Tabela CID", "CID", "Busca rápida, favoritos por médico/tenant e histórico de uso em consulta.", "api/cid", Links(Link("Novo", "Create", "bi-plus-circle"), Link("Importar", "Importar", "bi-upload"), Link("Favoritos", "Favoritos", "bi-star"))); }
    public IActionResult Create() { return Formulario("Novo CID", "api/cid"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar CID", "api/cid/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do CID", "CID", "Código, descrição e status do CID.", "api/cid/" + id, Links(Link("CID", "Index", "bi-arrow-left"))); }
    public IActionResult Importar() { return Formulario("Importar CID", "api/cid/importar"); }
    public Task<IActionResult> Favoritos() { return Index(); }
    public Task<IActionResult> MaisUsados() { return Index(); }
}

public sealed class PrescricoesController : Saude360WebControllerBase
{
    public PrescricoesController(IHttpClientFactory f, ILogger<PrescricoesController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Prescrições", "Prescrição médica", "Prescrição por médico, modelos reutilizáveis, finalização, cancelamento justificado e impressão auditada.", "api/prescricoes", Links(Link("Nova", "Create", "bi-plus-circle"), Link("Modelos", "Modelos", "bi-files"), Link("Histórico", "HistoricoPaciente", "bi-clock-history"))); }
    public IActionResult Create() { return Formulario("Nova prescrição", "api/prescricoes"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar prescrição", "api/prescricoes/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes da prescrição", "Prescrição médica", "Resumo sem expor conteúdo clínico em logs técnicos.", "api/prescricoes/" + id, Links(Link("Prescrições", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Imprimir() { return ModuloAsync("Imprimir prescrição", "Prescrição médica", "Impressão auditada da prescrição médica.", "api/prescricoes", Links(Link("Prescrições", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Modelos() { return ModuloAsync("Modelos de prescrição", "Prescrição médica", "Modelos por médico/tenant para acelerar o atendimento.", "api/prescricoes/modelos", Links(Link("Nova prescrição", "Create", "bi-plus-circle"))); }
    public Task<IActionResult> HistoricoPaciente() { return Index(); }
}

public sealed class ClinicaFinanceiroController : Saude360WebControllerBase
{
    public ClinicaFinanceiroController(IHttpClientFactory f, ILogger<ClinicaFinanceiroController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Financeiro da clínica", "Financeiro clínica", "Contas a receber, recebimentos, caixa, repasses, glosas e relatórios por tenant.", "api/clinica-financeiro/contas-receber", Links(Link("Receber", "Receber", "bi-cash-coin"), Link("Caixa", "Caixa", "bi-box"), Link("Relatórios", "Relatorios", "bi-graph-up"))); }
    public IActionResult Receber() { return Formulario("Receber pagamento", "api/clinica-financeiro/receber"); }
    public Task<IActionResult> ContasReceber() { return Index(); }
    public Task<IActionResult> Caixa() { return ModuloAsync("Caixa clínico", "Financeiro clínica", "Abertura, movimentação e fechamento de caixa auditados.", "api/clinica-financeiro/caixa", Links(Link("Fechamento", "FechamentoCaixa", "bi-lock"))); }
    public Task<IActionResult> FechamentoCaixa() { return Caixa(); }
    public Task<IActionResult> Repasses() { return Index(); }
    public Task<IActionResult> Relatorios() { return ModuloAsync("Relatórios financeiros", "Financeiro clínica", "Indicadores financeiros sem acesso a evolução clínica.", "api/clinica-financeiro/relatorios", Links(Link("Contas", "ContasReceber", "bi-receipt"))); }
    public Task<IActionResult> Glosas() { return Index(); }
}

public sealed class ConveniosController : Saude360WebControllerBase
{
    public ConveniosController(IHttpClientFactory f, ILogger<ConveniosController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Convênios", "Convênios", "Contratos, planos, tabelas, autorizações, glosas e faturamento integrados ao financeiro.", "api/convenios", Links(Link("Novo", "Create", "bi-plus-circle"), Link("Autorizações", "Autorizacoes", "bi-check2-square"), Link("Faturamento", "Faturamento", "bi-file-earmark-bar-graph"))); }
    public IActionResult Create() { return Formulario("Novo convênio", "api/convenios"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar convênio", "api/convenios/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do convênio", "Convênios", "Cadastro e status do convênio por tenant.", "api/convenios/" + id, Links(Link("Convênios", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Contratos() { return Index(); }
    public Task<IActionResult> Planos() { return Index(); }
    public Task<IActionResult> Tabelas() { return Index(); }
    public Task<IActionResult> Autorizacoes() { return ModuloAsync("Autorizações de convênio", "Convênios", "Solicitação, aprovação e negativa com motivo obrigatório.", "api/convenios/autorizacoes", Links(Link("Convênios", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Glosas() { return Index(); }
    public Task<IActionResult> Faturamento() { return Index(); }
}

public sealed class PlanosSaudeController : Saude360WebControllerBase
{
    public PlanosSaudeController(IHttpClientFactory f, ILogger<PlanosSaudeController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Planos de saúde", "Planos de saúde", "Planos, coberturas, pacientes, carteirinha, plano principal e autorizações.", "api/planos-saude", Links(Link("Novo", "Create", "bi-plus-circle"), Link("Pacientes", "Pacientes", "bi-people"), Link("Coberturas", "Coberturas", "bi-shield-plus"))); }
    public IActionResult Create() { return Formulario("Novo plano de saúde", "api/planos-saude"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar plano de saúde", "api/planos-saude/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do plano", "Planos de saúde", "Cadastro, status e regras do plano.", "api/planos-saude/" + id, Links(Link("Planos", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Pacientes() { return ModuloAsync("Pacientes por plano", "Planos de saúde", "Vínculo de paciente com plano, carteirinha e marcação de plano principal.", "api/planos-saude", Links(Link("Vincular", "Create", "bi-person-plus"))); }
    public Task<IActionResult> Coberturas() { return Index(); }
    public Task<IActionResult> Autorizacoes() { return Index(); }
}

public sealed class PacientesController : Saude360WebControllerBase
{
    public PacientesController(IHttpClientFactory f, ILogger<PacientesController> l, Saude360WebService s) : base(f, l, s) { }
    public Task<IActionResult> Index() { return ModuloAsync("Pacientes", "Pacientes", "Cadastro assistencial de pacientes por tenant para agendamento, triagem, consulta e financeiro.", "api/pacientes", Links(Link("Novo", "Create", "bi-person-plus"))); }
    public IActionResult Create() { return Formulario("Novo paciente", "api/pacientes"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar paciente", "api/pacientes/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do paciente", "Pacientes", "Identificação operacional do paciente com dados mínimos.", "api/pacientes/" + id, Links(Link("Pacientes", "Index", "bi-arrow-left"))); }
}
