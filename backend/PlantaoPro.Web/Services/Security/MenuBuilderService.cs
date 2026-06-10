using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Services.Security;

public interface IMenuBuilderService
{
    IReadOnlyCollection<MenuGroupViewModel> Build(string currentController, string currentAction);
}

public sealed class MenuBuilderService : IMenuBuilderService
{
    private readonly ICurrentUserService currentUser;
    private readonly IPermissionService permissions;
    private readonly IModuleAccessService modules;

    public MenuBuilderService(ICurrentUserService currentUser, IPermissionService permissions, IModuleAccessService modules)
    {
        this.currentUser = currentUser;
        this.permissions = permissions;
        this.modules = modules;
    }

    public IReadOnlyCollection<MenuGroupViewModel> Build(string currentController, string currentAction)
    {
        var groups = new List<MenuGroupViewModel>();

        AddGroup(groups, "ADMIN SAAS", "bi-buildings", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Visão Geral", "bi-speedometer2", "AdminSaas", "Index", "ADMIN_SAAS", RolesConstants.AdministradorGlobal),
            Item("Clientes", "bi-building-check", "Clientes", "Index", "CLIENTES", RolesConstants.AdministradorGlobal),
            Item("Tenants", "bi-diagram-3", "Clientes", "Index", "CLIENTES", RolesConstants.AdministradorGlobal),
            Item("Planos", "bi-columns-gap", "Planos", "Index", "PLANOS", RolesConstants.AdministradorGlobal),
            Item("Assinaturas", "bi-receipt", "Assinaturas", "Index", "ASSINATURAS", RolesConstants.AdministradorGlobal),
            Item("Billing", "bi-credit-card", "Billing", "Faturas", "BILLING_GLOBAL", RolesConstants.AdministradorGlobal),
            Item("Propostas", "bi-file-earmark-richtext", "PropostasComerciais", "Index", "PROPOSTAS", RolesConstants.AdministradorGlobal),
            Item("Parceiros", "bi-handshake", "ParceiroPortal", "Index", "PARCEIRO", RolesConstants.AdministradorGlobal),
            Item("Marketplace", "bi-shop", "Marketplace", "Index", "MARKETPLACE", RolesConstants.AdministradorGlobal, true),
            Item("White Label", "bi-palette", "WhiteLabel", "Index", "WHITE_LABEL", RolesConstants.AdministradorGlobal),
            Item("Permissões", "bi-shield-lock", "Permissoes", "Index", "PERMISSOES", RolesConstants.AdministradorGlobal),
            Item("Auditoria", "bi-journal-check", "Auditoria", "Index", "AUDITORIA", RolesConstants.AdministradorGlobal),
            Item("Observabilidade", "bi-activity", "Observabilidade", "Index", "OBSERVABILIDADE_GLOBAL", RolesConstants.AdministradorGlobal)
        });

        AddGroup(groups, "OPERAÇÃO", "bi-calendar2-week", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Dashboard", "bi-speedometer", "Home", "Dashboard", "DASHBOARD", RolesConstants.Coordenador),
            Item("Central de Escala", "bi-kanban", "CentralEscala", "Index", "CENTRAL_ESCALA", RolesConstants.Coordenador),
            Item("Plantões", "bi-calendar-event", "Plantoes", "Index", "PLANTOES", RolesConstants.Coordenador),
            Item("Convites", "bi-envelope-paper", "Convites", "Index", "CONVITES", RolesConstants.Coordenador),
            Item("Escalas", "bi-calendar-check", "Escalas", "Index", "ESCALAS", RolesConstants.Coordenador),
            Item("Médicos", "bi-person-vcard", "Medicos", "Index", "MEDICOS", RolesConstants.Coordenador),
            Item("Hospitais", "bi-hospital", "Hospitais", "Index", "HOSPITAIS", RolesConstants.Coordenador),
            Item("Especialidades", "bi-tags", "Especialidades", "Index", "ESPECIALIDADES", RolesConstants.Coordenador),
            Item("Agenda", "bi-calendar3", "Agenda", "Index", "AGENDA", RolesConstants.Coordenador),
            Item("Comunicação", "bi-chat-dots", "Comunicacao", "Index", "COMUNICACAO", RolesConstants.Coordenador)
        });

        AddGroup(groups, "FINANCEIRO", "bi-cash-stack", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Pagamentos", "bi-cash-coin", "Pagamentos", "Index", "PAGAMENTOS", RolesConstants.Financeiro),
            Item("Faturas", "bi-receipt-cutoff", "Billing", "Faturas", "FATURAS", RolesConstants.Financeiro),
            Item("Relatórios", "bi-file-earmark-bar-graph", "Relatorios", "Index", "RELATORIOS", RolesConstants.Financeiro),
            Item("Exportações", "bi-download", "Relatorios", "Index", "RELATORIOS", RolesConstants.Financeiro)
        });

        AddGroup(groups, "SAÚDE 360 - RECEPÇÃO", "bi-person-lines-fill", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Painel de chamada", "bi-megaphone", "PainelChamada", "Index", "SAUDE360_PAINEL", string.Empty),
            Item("Agendamento", "bi-calendar2-plus", "Agendamentos", "Index", "SAUDE360_AGENDAMENTO", string.Empty),
            Item("Check-in", "bi-person-check", "Agendamentos", "CheckIn", "SAUDE360_AGENDAMENTO", string.Empty),
            Item("Pacientes", "bi-people", "Pacientes", "Index", "SAUDE360_PACIENTES", string.Empty)
        });

        AddGroup(groups, "SAÚDE 360 - TRIAGEM", "bi-clipboard2-pulse", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Fila de triagem", "bi-people", "Triagem", "Fila", "SAUDE360_TRIAGEM", string.Empty),
            Item("Nova triagem", "bi-plus-circle", "Triagem", "Create", "SAUDE360_TRIAGEM", string.Empty),
            Item("Histórico de triagem", "bi-clock-history", "Triagem", "HistoricoPaciente", "SAUDE360_TRIAGEM", string.Empty)
        });

        AddGroup(groups, "SAÚDE 360 - MÉDICO", "bi-heart-pulse", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Consultas", "bi-journal-medical", "Consultas", "Index", "SAUDE360_CONSULTAS", string.Empty),
            Item("Prescrições", "bi-capsule", "Prescricoes", "Index", "SAUDE360_PRESCRICAO", string.Empty),
            Item("CID", "bi-search-heart", "Cid", "Index", "SAUDE360_CID", string.Empty),
            Item("Histórico do paciente", "bi-folder2-open", "Consultas", "HistoricoPaciente", "SAUDE360_CONSULTAS", string.Empty)
        });

        AddGroup(groups, "SAÚDE 360 - FINANCEIRO", "bi-cash-stack", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Contas a receber", "bi-receipt", "ClinicaFinanceiro", "ContasReceber", "SAUDE360_FINANCEIRO", string.Empty),
            Item("Caixa", "bi-box", "ClinicaFinanceiro", "Caixa", "SAUDE360_FINANCEIRO", string.Empty),
            Item("Repasses", "bi-bank", "ClinicaFinanceiro", "Repasses", "SAUDE360_FINANCEIRO", string.Empty),
            Item("Relatórios", "bi-graph-up", "ClinicaFinanceiro", "Relatorios", "SAUDE360_FINANCEIRO", string.Empty)
        });

        AddGroup(groups, "SAÚDE 360 - CONVÊNIOS", "bi-shield-plus", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Convênios", "bi-building", "Convenios", "Index", "SAUDE360_CONVENIOS", string.Empty),
            Item("Autorizações", "bi-check2-square", "Convenios", "Autorizacoes", "SAUDE360_CONVENIOS", string.Empty),
            Item("Glosas", "bi-exclamation-octagon", "Convenios", "Glosas", "SAUDE360_CONVENIOS", string.Empty),
            Item("Faturamento", "bi-file-earmark-bar-graph", "Convenios", "Faturamento", "SAUDE360_CONVENIOS", string.Empty),
            Item("Planos de saúde", "bi-card-checklist", "PlanosSaude", "Index", "SAUDE360_PLANOS_SAUDE", string.Empty)
        });

        AddGroup(groups, "CLIENTE", "bi-window-sidebar", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Meu Portal", "bi-house-heart", "ClientePortal", "Index", "CLIENTE_PORTAL", RolesConstants.AdministradorCliente),
            Item("Meu Plano", "bi-card-checklist", "MinhaAssinatura", "Index", "ASSINATURAS", RolesConstants.AdministradorCliente),
            Item("Uso", "bi-graph-up", "MinhaAssinatura", "Uso", "ASSINATURAS", RolesConstants.AdministradorCliente),
            Item("Usuários", "bi-person-gear", "Usuarios", "Index", "USUARIOS", RolesConstants.AdministradorCliente),
            Item("Perfis", "bi-people", "Perfis", "Index", "PERFIS", RolesConstants.AdministradorCliente),
            Item("Parametrizações", "bi-sliders", "Configuracoes", "Index", "CONFIGURACOES", RolesConstants.AdministradorCliente),
            Item("White Label", "bi-palette2", "WhiteLabel", "Index", "WHITE_LABEL", RolesConstants.AdministradorCliente, true),
            Item("Onboarding", "bi-rocket", "Onboarding", "Index", "ONBOARDING", RolesConstants.AdministradorCliente),
            Item("Suporte", "bi-life-preserver", "Suporte", "Index", "SUPORTE", RolesConstants.AdministradorCliente),
            Item("Treinamento", "bi-mortarboard", "Treinamento", "Index", "TREINAMENTO", RolesConstants.AdministradorCliente)
        });

        AddGroup(groups, "MÉDICO", "bi-heart-pulse", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Minha Agenda", "bi-calendar-heart", "MedicoArea", "Index", "MEDICO_AREA", RolesConstants.Medico),
            Item("Convites", "bi-envelope-open", "Convites", "Index", "CONVITES", RolesConstants.Medico),
            Item("Disponibilidade", "bi-clock-history", "MedicoArea", "Index", "DISPONIBILIDADE", RolesConstants.Medico),
            Item("Substituições", "bi-arrow-left-right", "MedicoArea", "Index", "SUBSTITUICOES", RolesConstants.Medico),
            Item("Meus Pagamentos", "bi-wallet2", "Pagamentos", "Index", "PAGAMENTOS_PROPRIOS", RolesConstants.Medico)
        });

        AddGroup(groups, "PARCEIRO", "bi-handshake", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Portal Parceiro", "bi-house-door", "ParceiroPortal", "Index", "PARCEIRO", RolesConstants.Parceiro),
            Item("Leads", "bi-bullseye", "ParceiroPortal", "Leads", "LEADS", RolesConstants.Parceiro),
            Item("Propostas", "bi-file-earmark-text", "ParceiroPortal", "Propostas", "PROPOSTAS", RolesConstants.Parceiro),
            Item("Clientes", "bi-buildings", "ParceiroPortal", "Clientes", "PARCEIRO", RolesConstants.Parceiro),
            Item("Comissões", "bi-percent", "ParceiroPortal", "Comissoes", "COMISSOES", RolesConstants.Parceiro),
            Item("Repasses", "bi-bank", "ParceiroPortal", "Repasses", "REPASSES", RolesConstants.Parceiro),
            Item("Materiais", "bi-folder2-open", "ParceiroPortal", "Materiais", "MATERIAIS", RolesConstants.Parceiro)
        });

        AddGroup(groups, "SUPORTE E GOVERNANÇA", "bi-shield-check", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Ajuda", "bi-question-circle", "Ajuda", "Index", "AJUDA", string.Empty, false, false),
            Item("LGPD", "bi-shield-lock", "Lgpd", "Index", "LGPD", string.Empty, false, false),
            Item("Auditoria", "bi-journal-check", "Auditoria", "Index", "AUDITORIA", RolesConstants.Auditor),
            Item("Suporte", "bi-life-preserver", "Suporte", "Index", "SUPORTE", RolesConstants.Suporte),
            Item("Minha conta", "bi-person-circle", "Usuario", "Index", "CONTA", string.Empty, false, false)
        });

        return groups;
    }

    private void AddGroup(IList<MenuGroupViewModel> groups, string title, string icon, string currentController, string currentAction, IList<MenuItemViewModel> items)
    {
        var visible = new List<MenuItemViewModel>();
        foreach (var item in items)
        {
            var hasMinimumRole = HasMinimumRole(item.MinimumRole);
            var permitted = hasMinimumRole && (string.IsNullOrWhiteSpace(item.Module) || permissions.HasPermission(item.Module, item.Permission));
            var enabled = !item.RequiresModule || modules.IsModuleEnabled(item.Module);
            item.IsLocked = permitted && !enabled;
            item.LockedReason = item.IsLocked ? "Módulo disponível mediante contratação ou liberação do plano." : string.Empty;
            item.IsActive = string.Equals(currentController, item.Controller, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(item.Action) || string.Equals(currentAction, item.Action, StringComparison.OrdinalIgnoreCase));

            if (permitted || item.IsLocked)
            {
                visible.Add(item);
            }
        }

        if (visible.Count > 0)
        {
            groups.Add(new MenuGroupViewModel { Title = title, Icon = icon, Items = visible });
        }
    }

    private bool HasMinimumRole(string minimumRole)
    {
        if (string.IsNullOrWhiteSpace(minimumRole)) return true;
        if (currentUser.IsGlobalAdmin()) return true;
        if (currentUser.HasRole(RolesConstants.AdministradorClinica) && (string.Equals(minimumRole, RolesConstants.Recepcao, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.Triagem, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.FinanceiroClinica, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.FaturamentoConvenio, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.Medico, StringComparison.OrdinalIgnoreCase))) return true;
        if (string.Equals(minimumRole, RolesConstants.AdministradorGlobal, StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(minimumRole, RolesConstants.AdministradorCliente, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.Administrador, StringComparison.OrdinalIgnoreCase)) return currentUser.IsTenantAdmin();
        if (string.Equals(minimumRole, RolesConstants.Coordenador, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.Coordenacao, StringComparison.OrdinalIgnoreCase)) return currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Coordenador) || currentUser.HasRole(RolesConstants.Coordenacao) || currentUser.HasRole(RolesConstants.Operador);
        if (string.Equals(minimumRole, RolesConstants.Financeiro, StringComparison.OrdinalIgnoreCase)) return currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Financeiro);
        if (string.Equals(minimumRole, RolesConstants.Medico, StringComparison.OrdinalIgnoreCase)) return currentUser.IsDoctor();
        if (string.Equals(minimumRole, RolesConstants.Parceiro, StringComparison.OrdinalIgnoreCase)) return currentUser.IsPartner();
        return currentUser.HasRole(minimumRole);
    }

    private static MenuItemViewModel Item(string title, string icon, string controller, string action, string module, string minimumRole, bool requiresPlan = false, bool requiresModule = true)
    {
        return new MenuItemViewModel
        {
            Title = title,
            Icon = icon,
            Controller = controller,
            Action = action,
            Module = module,
            Permission = "VER",
            MinimumRole = minimumRole,
            RequiresPlan = requiresPlan,
            RequiresModule = requiresModule
        };
    }
}
