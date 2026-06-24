using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
using PlantaoPro.Web.Services;

namespace PlantaoPro.Web.Controllers;

[Authorize(Roles = RolesConstants.Saude360Assistencial + "," + RolesConstants.Saude360Financeiro + "," + RolesConstants.Saude360Convenios)]
public abstract class Saude360WebControllerBase : BaseWebController
{
    private readonly Saude360WebService service;
    private readonly IAssistenteContextualService assistente;
    protected Saude360WebControllerBase(IHttpClientFactory factory, ILogger logger, Saude360WebService service, IAssistenteContextualService assistente) : base(factory, logger) { this.service = service; this.assistente = assistente; }

    protected async Task<IActionResult> ModuloAsync(string titulo, string modulo, string descricao, string endpoint, IEnumerable<Saude360ActionLinkViewModel> acoes)
    {
        var token = GetJwtToken();
        if (string.IsNullOrWhiteSpace(token)) return HandleUnauthorized();
        var result = await service.ListarAsync(token, endpoint);
        var registros = result.Registros ?? Array.Empty<Saude360RegistroViewModel>();
        ViewBag.AssistenteContextual = assistente.Obter(ControllerContext.ActionDescriptor.ControllerName, ControllerContext.ActionDescriptor.ActionName, User.PrimaryRole(), registros.Count());
        var model = new Saude360PageViewModel
        {
            Titulo = titulo,
            Modulo = modulo,
            Descricao = descricao,
            Controller = ControllerContext.ActionDescriptor.ControllerName,
            Action = ControllerContext.ActionDescriptor.ActionName,
            Endpoint = endpoint,
            Registros = registros,
            ErrorMessage = result.Error,
            Acoes = acoes,
            Plano = PlanoModulo(modulo),
            Permissao = PermissaoModulo(modulo)
        };
        var specificView = $"~/Views/{ControllerContext.ActionDescriptor.ControllerName}/{ControllerContext.ActionDescriptor.ActionName}.cshtml";
        return View(specificView, model);
    }

    protected IActionResult Formulario(string titulo, string endpoint, Guid? id = null)
    {
        ViewBag.AssistenteContextual = assistente.Obter(ControllerContext.ActionDescriptor.ControllerName, ControllerContext.ActionDescriptor.ActionName, User.PrimaryRole(), 0);
        var model = new Saude360FormViewModel
        {
            Titulo = titulo,
            Controller = ControllerContext.ActionDescriptor.ControllerName,
            Action = ControllerContext.ActionDescriptor.ActionName,
            ApiEndpoint = endpoint,
            Id = id
        };
        var specificView = $"~/Views/{ControllerContext.ActionDescriptor.ControllerName}/{ControllerContext.ActionDescriptor.ActionName}.cshtml";
        return View(specificView, model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salvar(Saude360FormViewModel form)
    {
        var token = GetJwtToken();
        if (string.IsNullOrWhiteSpace(token)) return HandleUnauthorized();
        var result = await service.EnviarAsync(token, form.ApiEndpoint, form);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        if (!result.Success)
        {
            form.Titulo = string.IsNullOrWhiteSpace(form.Titulo) ? "Revise os dados do formulário" : form.Titulo;
            var specificView = $"~/Views/{form.Controller}/{form.Action}.cshtml";
            return View(specificView, form);
        }
        return RedirectToAction("Index");
    }

    protected static IEnumerable<Saude360ActionLinkViewModel> Links(params Saude360ActionLinkViewModel[] links) { return links; }
    protected static Saude360ActionLinkViewModel Link(string title, string action, string icon) { return new Saude360ActionLinkViewModel { Titulo = title, Action = action, Icone = icon }; }
    protected static Saude360ActionLinkViewModel LinkTo(string title, string controller, string action, string icon) { return new Saude360ActionLinkViewModel { Titulo = title, Controller = controller, Action = action, Icone = icon }; }
    private static string PlanoModulo(string modulo) { return modulo == "Convênios" || modulo == "Planos de saúde" ? "Enterprise" : modulo == "Triagem" || modulo == "Prescrição médica" || modulo == "CID" || modulo == "Financeiro clínica" ? "Profissional" : "Essencial"; }
    private static string PermissaoModulo(string modulo) { return "Permissão por tenant, perfil e plano para " + modulo + "."; }
}

public sealed class ClinicaDashboardController : Saude360WebControllerBase
{
    public ClinicaDashboardController(IHttpClientFactory f, ILogger<ClinicaDashboardController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Dashboard clínico", "Dashboard clínico", "KPIs reais da jornada Paciente -> Agendamento -> Check-in -> Painel -> Triagem -> Consulta -> Prescrição.", "api/clinica-dashboard/resumo", Links(Link("Fluxo de Atendimento", "FluxoAtendimento", "bi-signpost-2"), LinkTo("Pacientes", "Pacientes", "Index", "bi-people"), LinkTo("Agendamentos", "Agendamentos", "Index", "bi-calendar3"), LinkTo("Triagem", "Triagem", "Index", "bi-clipboard2-pulse"), LinkTo("Consultas", "Consultas", "Index", "bi-journal-medical"))); }
    public IActionResult FluxoAtendimento() { return View("~/Views/ClinicaDashboard/FluxoAtendimento.cshtml"); }
}

public sealed class PendenciasClinicasController : Saude360WebControllerBase
{
    public PendenciasClinicasController(IHttpClientFactory f, ILogger<PendenciasClinicasController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Pendências do Dia", "Pendências clínicas", "Próxima ação recomendada para recepção, triagem, médico e financeiro.", "api/pendencias-clinicas", Links(LinkTo("Fluxo de Atendimento", "ClinicaDashboard", "FluxoAtendimento", "bi-signpost-2"))); }
}

public sealed class PainelChamadaController : Saude360WebControllerBase
{
    public PainelChamadaController(IHttpClientFactory f, ILogger<PainelChamadaController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Painel de chamada", "Painel de chamada", "Fila, chamada, rechamada, ausentes e histórico sem expor dados sensíveis na TV.", "api/painel-chamada", Links(Link("Configurações", "Configuracoes", "bi-gear"), Link("TV", "Tv", "bi-tv"), Link("Histórico", "Historico", "bi-clock-history"))); }
    public Task<IActionResult> Tv() { return ModuloAsync("TV do painel", "Painel de chamada", "Visualização white label para sala de espera com token público seguro quando habilitado.", "api/painel-chamada", Links(Link("Fila", "Index", "bi-list"))); }
    public Task<IActionResult> Configuracoes() { return ModuloAsync("Configurações do painel", "Painel de chamada", "Setores, salas, guichês, token seguro e regras de exibição.", "api/painel-chamada", Links(Link("Setores", "Setores", "bi-diagram-3"), Link("Salas", "Salas", "bi-door-open"), Link("Guichês", "Guiches", "bi-window"))); }
    public Task<IActionResult> Historico() { return ModuloAsync("Histórico de chamadas", "Painel de chamada", "Auditoria operacional de chamadas, rechamadas, ausências e finalizações.", "api/painel-chamada/historico", Links(Link("Fila", "Index", "bi-list"))); }
    public Task<IActionResult> Setores() { return Configuracoes(); }
    public Task<IActionResult> Salas() { return Configuracoes(); }
    public Task<IActionResult> Guiches() { return Configuracoes(); }
    public Task<IActionResult> Fila() { return ModuloAsync("Fila do painel", "Painel de chamada", "Pacientes aguardando chamada, rechamada ou encaminhamento.", "api/painel-chamada", Links(Link("Painel", "Index", "bi-megaphone"))); }
}

public sealed class AgendamentosController : Saude360WebControllerBase
{
    public AgendamentosController(IHttpClientFactory f, ILogger<AgendamentosController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Agendamentos", "Agendamento", "Agenda clínica por tenant, médico, paciente e status, com bloqueio de conflito de horário.", "api/agendamentos", Links(Link("Novo", "Create", "bi-plus-circle"), Link("Calendário", "Calendario", "bi-calendar3"), Link("Check-in", "CheckIn", "bi-person-check"))); }
    public IActionResult Create() { return Formulario("Novo agendamento", "api/agendamentos"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar agendamento", "api/agendamentos/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do agendamento", "Agendamento", "Dados operacionais e status do agendamento.", "api/agendamentos/" + id, Links(Link("Agenda", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Calendario() { return ModuloAsync("Calendário clínico", "Agendamento", "Visão de calendário para recepção e gestão clínica.", "api/agendamentos/calendario", Links(Link("Novo", "Create", "bi-plus-circle"))); }
    public Task<IActionResult> AgendaDia() { return ModuloAsync("Agenda do dia", "Agendamento", "Agenda operacional do dia para recepção, check-in e encaixes.", "api/agendamentos?periodo=hoje", Links(Link("Novo", "Create", "bi-plus-circle"), Link("Check-in", "CheckIn", "bi-person-check"))); }
    public Task<IActionResult> AgendaMedico() { return ModuloAsync("Agenda por médico", "Agendamento", "Consulta de agenda por profissional, especialidade e status.", "api/agendamentos/agenda-medico", Links(Link("Agenda do dia", "AgendaDia", "bi-calendar-day"))); }
    public Task<IActionResult> CheckIn() { return ModuloAsync("Check-in", "Agendamento", "Check-in altera status e pode enviar o paciente para painel e triagem.", "api/agendamentos", Links(Link("Agenda", "Index", "bi-calendar"))); }
    public Task<IActionResult> Cancelamentos() { return ModuloAsync("Cancelamentos", "Agendamento", "Cancelamentos exigem motivo e geram histórico auditável.", "api/agendamentos?status=CANCELADO", Links(Link("Agenda", "Index", "bi-calendar"))); }
}

public sealed class TriagemController : Saude360WebControllerBase
{
    public TriagemController(IHttpClientFactory f, ILogger<TriagemController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Triagens", "Triagem", "Fila assistencial com classificação de risco, sinais vitais e encaminhamento para consulta.", "api/triagens", Links(Link("Nova", "Create", "bi-plus-circle"), Link("Fila", "Fila", "bi-people"), Link("Classificação", "ClassificacaoRisco", "bi-flag"))); }
    public IActionResult Create() { return Formulario("Nova triagem", "api/triagens"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar triagem", "api/triagens/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes da triagem", "Triagem", "Resumo assistencial auditado da triagem.", "api/triagens/" + id, Links(Link("Triagens", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Fila() { return ModuloAsync("Fila de triagem", "Triagem", "Pacientes aguardando classificação e encaminhamento.", "api/triagens/fila", Links(Link("Nova", "Create", "bi-plus-circle"))); }
    public Task<IActionResult> HistoricoPaciente() { return ModuloAsync("Histórico do paciente", "Triagem", "Histórico de triagens por paciente com acesso auditado e sem exposição desnecessária de dados sensíveis.", "api/triagens/historico-paciente", Links(Link("Fila", "Fila", "bi-people"))); }
    public Task<IActionResult> ClassificacaoRisco() { return ModuloAsync("Classificação de risco", "Triagem", "Protocolos e cores de risco configuráveis por clínica.", "api/triagens", Links(Link("Fila", "Fila", "bi-people"))); }
}

public sealed class ConsultasController : Saude360WebControllerBase
{
    public ConsultasController(IHttpClientFactory f, ILogger<ConsultasController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Consultas", "Consultas", "Atendimento médico com vínculo a triagem, CID, prescrição e auditoria de impressão.", "api/consultas", Links(Link("Nova", "Create", "bi-plus-circle"), Link("Atendimento", "Atendimento", "bi-clipboard2-pulse"), Link("Histórico", "HistoricoPaciente", "bi-journal-medical"))); }
    public IActionResult Create() { return Formulario("Nova consulta", "api/consultas"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar consulta", "api/consultas/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes da consulta", "Consultas", "Resumo clínico com restrição por médico/tenant.", "api/consultas/" + id, Links(Link("Consultas", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Atendimento() { return ModuloAsync("Atendimento médico", "Consultas", "Tela de condução do atendimento com vínculo a triagem, CID, prescrição e finalização segura.", "api/consultas/atendimento", Links(Link("Nova consulta", "Create", "bi-plus-circle"), LinkTo("CID", "Cid", "Index", "bi-search-heart"), LinkTo("Prescrições", "Prescricoes", "Index", "bi-capsule"))); }
    public Task<IActionResult> HistoricoPaciente() { return ModuloAsync("Histórico clínico do paciente", "Consultas", "Histórico de consultas por paciente com acesso auditado e visão adequada ao perfil médico.", "api/consultas/historico-paciente", Links(Link("Atendimento", "Atendimento", "bi-clipboard2-pulse"))); }
    public Task<IActionResult> Resumo() { return ModuloAsync("Resumo de consultas", "Consultas", "Resumo operacional de consultas por status, médico e prioridade.", "api/consultas/resumo", Links(Link("Consultas", "Index", "bi-journal-medical"))); }
    public Task<IActionResult> Imprimir() { return ModuloAsync("Impressão de consulta", "Consultas", "Impressão com registro em auditoria e sem log técnico de conteúdo sensível.", "api/consultas", Links(Link("Consultas", "Index", "bi-arrow-left"))); }
}

public sealed class CidController : Saude360WebControllerBase
{
    public CidController(IHttpClientFactory f, ILogger<CidController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Tabela CID", "CID", "Busca rápida, favoritos por médico/tenant e histórico de uso em consulta.", "api/cid", Links(Link("Novo", "Create", "bi-plus-circle"), Link("Importar", "Importar", "bi-upload"), Link("Favoritos", "Favoritos", "bi-star"))); }
    public IActionResult Create() { return Formulario("Novo CID", "api/cid"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar CID", "api/cid/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do CID", "CID", "Código, descrição e status do CID.", "api/cid/" + id, Links(Link("CID", "Index", "bi-arrow-left"))); }
    public IActionResult Importar() { return Formulario("Importar CID", "api/cid/importar"); }
    public Task<IActionResult> Favoritos() { return ModuloAsync("CID favoritos", "CID", "CIDs favoritos por médico e tenant para acelerar o atendimento.", "api/cid/favoritos", Links(Link("Tabela CID", "Index", "bi-search-heart"), Link("Mais usados", "MaisUsados", "bi-graph-up"))); }
    public Task<IActionResult> MaisUsados() { return ModuloAsync("CID mais usados", "CID", "Ranking de CIDs usados em consultas para apoiar padronização clínica.", "api/cid/mais-usados", Links(Link("Favoritos", "Favoritos", "bi-star"), Link("Tabela CID", "Index", "bi-search-heart"))); }
}

public sealed class PrescricoesController : Saude360WebControllerBase
{
    public PrescricoesController(IHttpClientFactory f, ILogger<PrescricoesController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Prescrições", "Prescrição médica", "Prescrição por médico, modelos reutilizáveis, finalização, cancelamento justificado e impressão auditada.", "api/prescricoes", Links(Link("Nova", "Create", "bi-plus-circle"), Link("Modelos", "Modelos", "bi-files"), Link("Histórico", "HistoricoPaciente", "bi-clock-history"))); }
    public IActionResult Create() { return Formulario("Nova prescrição", "api/prescricoes"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar prescrição", "api/prescricoes/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes da prescrição", "Prescrição médica", "Resumo sem expor conteúdo clínico em logs técnicos.", "api/prescricoes/" + id, Links(Link("Prescrições", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Imprimir() { return ModuloAsync("Imprimir prescrição", "Prescrição médica", "Impressão auditada da prescrição médica.", "api/prescricoes", Links(Link("Prescrições", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Modelos() { return ModuloAsync("Modelos de prescrição", "Prescrição médica", "Modelos por médico/tenant para acelerar o atendimento.", "api/prescricoes/modelos", Links(Link("Nova prescrição", "Create", "bi-plus-circle"))); }
    public Task<IActionResult> HistoricoPaciente() { return ModuloAsync("Histórico de prescrições do paciente", "Prescrição médica", "Prescrições anteriores do paciente com acesso auditado e impressão controlada.", "api/prescricoes/historico-paciente", Links(Link("Prescrições", "Index", "bi-capsule"))); }
}

public sealed class ClinicaFinanceiroController : Saude360WebControllerBase
{
    public ClinicaFinanceiroController(IHttpClientFactory f, ILogger<ClinicaFinanceiroController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Dashboard financeiro", "Financeiro clínica", "Contas a receber, recebimentos, caixa, repasses, glosas e relatórios por tenant.", "api/clinica-financeiro/resumo", Links(Link("Receber pagamento", "Receber", "bi-cash-coin"), Link("Caixa", "Caixa", "bi-box"), Link("Contas a receber", "ContasReceber", "bi-receipt"), Link("Relatórios", "Relatorios", "bi-graph-up"))); }
    public IActionResult Receber() { return Formulario("Receber pagamento", "api/clinica-financeiro/receber"); }
    public Task<IActionResult> ContasReceber() { return ModuloAsync("Contas a receber", "Financeiro clínica", "Títulos abertos, vencidos e recebidos com filtro por paciente, convênio e status.", "api/clinica-financeiro/contas-receber", Links(Link("Receber pagamento", "Receber", "bi-cash-coin"), Link("Caixa", "Caixa", "bi-box"))); }
    public Task<IActionResult> Caixa() { return ModuloAsync("Caixa clínico", "Financeiro clínica", "Abertura, movimentação e fechamento de caixa auditados.", "api/clinica-financeiro/caixa", Links(Link("Fechamento", "FechamentoCaixa", "bi-lock"))); }
    public Task<IActionResult> FechamentoCaixa() { return Caixa(); }
    public Task<IActionResult> Repasses() { return ModuloAsync("Repasses", "Financeiro clínica", "Repasses a médicos, parceiros e convênios com trilha auditável.", "api/clinica-financeiro/repasses", Links(Link("Relatórios", "Relatorios", "bi-graph-up"))); }
    public Task<IActionResult> Relatorios() { return ModuloAsync("Relatórios financeiros", "Financeiro clínica", "Indicadores financeiros sem acesso a evolução clínica.", "api/clinica-financeiro/relatorios", Links(Link("Contas", "ContasReceber", "bi-receipt"))); }
    public Task<IActionResult> Glosas() { return ModuloAsync("Glosas financeiras", "Financeiro clínica", "Acompanhamento de glosas, recursos e impacto financeiro.", "api/clinica-financeiro/glosas", Links(Link("Contas a receber", "ContasReceber", "bi-receipt"))); }
}

public sealed class ConveniosController : Saude360WebControllerBase
{
    public ConveniosController(IHttpClientFactory f, ILogger<ConveniosController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Convênios", "Convênios", "Visão de convênios, contratos, planos vinculados e faturamento.", "api/convenios", Links(Link("Dashboard", "Dashboard", "bi-speedometer2"), Link("Novo convênio", "Create", "bi-plus-circle"), Link("Autorizações", "Autorizacoes", "bi-check2-square"))); }
    public Task<IActionResult> Dashboard() { return ModuloAsync("Dashboard de convênios", "Convênios", "Contratos, planos, autorizações, glosas e faturamento integrados ao financeiro.", "api/convenios/resumo", Links(Link("Novo convênio", "Create", "bi-plus-circle"), Link("Autorizações", "Autorizacoes", "bi-check2-square"), Link("Faturamento", "Faturamento", "bi-file-earmark-bar-graph"), LinkTo("Planos de saúde", "PlanosSaude", "Index", "bi-card-checklist"))); }
    public IActionResult Create() { return Formulario("Novo convênio", "api/convenios"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar convênio", "api/convenios/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do convênio", "Convênios", "Cadastro e status do convênio por tenant.", "api/convenios/" + id, Links(Link("Convênios", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Contratos() { return ModuloAsync("Contratos de convênio", "Convênios", "Contratos, vigências, regras de cobrança e reajustes por convênio.", "api/convenios/contratos", Links(Link("Convênios", "Index", "bi-building"))); }
    public Task<IActionResult> Planos() { return ModuloAsync("Planos por convênio", "Convênios", "Planos comercializados por convênio e regras de elegibilidade.", "api/convenios/planos", Links(LinkTo("Planos de saúde", "PlanosSaude", "Index", "bi-card-checklist"))); }
    public Task<IActionResult> Tabelas() { return ModuloAsync("Tabelas de convênio", "Convênios", "Tabelas, pacotes e parâmetros de faturamento por contrato.", "api/convenios/tabelas", Links(Link("Contratos", "Contratos", "bi-file-earmark-text"))); }
    public Task<IActionResult> Autorizacoes() { return ModuloAsync("Autorizações de convênio", "Convênios", "Solicitação, aprovação e negativa com motivo obrigatório.", "api/convenios/autorizacoes", Links(Link("Convênios", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Glosas() { return ModuloAsync("Glosas de convênio", "Convênios", "Glosas por lote, recurso, motivo e impacto no faturamento de convênios.", "api/convenios/glosas", Links(Link("Faturamento", "Faturamento", "bi-file-earmark-bar-graph"))); }
    public Task<IActionResult> Faturamento() { return ModuloAsync("Faturamento de convênios", "Convênios", "Lotes, guias, conferência e envio de faturamento.", "api/convenios/faturamento", Links(Link("Glosas", "Glosas", "bi-exclamation-octagon"))); }
}

public sealed class PlanosSaudeController : Saude360WebControllerBase
{
    public PlanosSaudeController(IHttpClientFactory f, ILogger<PlanosSaudeController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Planos de saúde", "Planos de saúde", "Planos, coberturas, pacientes, carteirinha, plano principal e autorizações.", "api/planos-saude", Links(Link("Novo", "Create", "bi-plus-circle"), Link("Pacientes", "Pacientes", "bi-people"), Link("Coberturas", "Coberturas", "bi-shield-plus"))); }
    public IActionResult Create() { return Formulario("Novo plano de saúde", "api/planos-saude"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar plano de saúde", "api/planos-saude/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do plano", "Planos de saúde", "Cadastro, status e regras do plano.", "api/planos-saude/" + id, Links(Link("Planos", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Pacientes() { return ModuloAsync("Pacientes por plano", "Planos de saúde", "Vínculo de paciente com plano, carteirinha e marcação de plano principal.", "api/planos-saude", Links(Link("Vincular", "Create", "bi-person-plus"))); }
    public Task<IActionResult> Coberturas() { return ModuloAsync("Coberturas", "Planos de saúde", "Coberturas, carências e regras de autorização por plano.", "api/planos-saude/coberturas", Links(Link("Planos", "Index", "bi-card-checklist"))); }
    public Task<IActionResult> Autorizacoes() { return ModuloAsync("Autorizações por plano", "Planos de saúde", "Solicitações e retornos de autorização vinculados ao plano de saúde.", "api/planos-saude/autorizacoes", Links(Link("Coberturas", "Coberturas", "bi-shield-plus"))); }
}

public sealed class PacientesController : Saude360WebControllerBase
{
    public PacientesController(IHttpClientFactory f, ILogger<PacientesController> l, Saude360WebService s, IAssistenteContextualService a) : base(f, l, s, a) { }
    public Task<IActionResult> Index() { return ModuloAsync("Pacientes", "Pacientes", "Cadastro assistencial de pacientes por tenant para agendamento, triagem, consulta e financeiro.", "api/pacientes", Links(Link("Novo", "Create", "bi-person-plus"))); }
    public IActionResult Create() { return Formulario("Novo paciente", "api/pacientes"); }
    public IActionResult Edit(Guid id) { return Formulario("Editar paciente", "api/pacientes/" + id, id); }
    public Task<IActionResult> Details(Guid id) { return ModuloAsync("Detalhes do paciente", "Pacientes", "Identificação operacional do paciente com dados mínimos.", "api/pacientes/" + id, Links(Link("Pacientes", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Historico(Guid id) { return ModuloAsync("Histórico do paciente", "Pacientes", "Histórico administrativo e assistencial auditado.", "api/pacientes/" + id + "/historico", Links(Link("Pacientes", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> ResumoClinico(Guid id) { return ModuloAsync("Resumo clínico", "Pacientes", "Resumo clínico com auditoria de acesso e controle VerDadosSensiveis.", "api/pacientes/" + id + "/resumo-clinico", Links(Link("Pacientes", "Index", "bi-arrow-left"))); }
    public Task<IActionResult> Buscar(string termo) { return ModuloAsync("Buscar pacientes", "Pacientes", "Busca por nome, CPF, telefone e status respeitando tenant.", "api/pacientes/buscar?termo=" + Uri.EscapeDataString(termo ?? string.Empty), Links(Link("Novo", "Create", "bi-person-plus"))); }
}
