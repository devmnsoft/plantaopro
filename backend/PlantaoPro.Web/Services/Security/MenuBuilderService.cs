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

        AddGroup(groups, "INÍCIO", "bi-house-heart", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Visão Geral", "bi-speedometer2", "Home", "Index", "DASHBOARD", string.Empty, false, false),
            Item("Fluxo de Atendimento", "bi-signpost-2", "ClinicaDashboard", "FluxoAtendimento", "SAUDE360_DASHBOARD", string.Empty),
            Item("Pendências do Dia", "bi-lightning-charge", "PendenciasClinicas", "Index", "SAUDE360_DASHBOARD", string.Empty),
            Item("Assistente PlantãoPro", "bi-stars", "Ajuda", "PrimeirosPassos", "AJUDA", string.Empty, false, false)
        });

        AddGroup(groups, "ATENDIMENTO", "bi-heart-pulse", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Minha Agenda", "bi-calendar-heart", "MinhaAgenda", "Index", "MEDICO_AREA", RolesConstants.Medico),
            Item("Pacientes", "bi-people", "Pacientes", "Index", "SAUDE360_PACIENTES", RolesConstants.Recepcao),
            Item("Agendamentos", "bi-calendar2-plus", "Agendamentos", "Index", "SAUDE360_AGENDAMENTO", RolesConstants.Recepcao),
            Item("Check-in", "bi-person-check", "Agendamentos", "CheckIn", "SAUDE360_AGENDAMENTO", RolesConstants.Recepcao),
            Item("Painel de Chamada", "bi-megaphone", "PainelChamada", "Index", "SAUDE360_PAINEL", RolesConstants.Recepcao),
            Item("Triagem", "bi-clipboard2-pulse", "Triagem", "Index", "SAUDE360_TRIAGEM", RolesConstants.Triagem),
            Item("Consultas", "bi-journal-medical", "Consultas", "Index", "SAUDE360_CONSULTAS", RolesConstants.Medico),
            Item("Prescrições", "bi-capsule", "Prescricoes", "Index", "SAUDE360_PRESCRICAO", RolesConstants.Medico),
            Item("CID", "bi-search-heart", "Cid", "Index", "SAUDE360_CID", RolesConstants.Medico)
        });

        AddGroup(groups, "PLANTÕES", "bi-calendar2-week", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Dashboard de Plantões", "bi-speedometer", "Home", "Dashboard", "DASHBOARD", RolesConstants.Coordenador),
            Item("Plantões", "bi-calendar-event", "Plantoes", "Index", "PLANTOES", RolesConstants.Coordenador),
            Item("Escalas", "bi-calendar-check", "Escalas", "Index", "ESCALAS", RolesConstants.Coordenador),
            Item("Convites", "bi-envelope-paper", "Convites", "Index", "CONVITES", RolesConstants.Coordenador),
            Item("Médicos", "bi-person-vcard", "Medicos", "Index", "MEDICOS", RolesConstants.Coordenador),
            Item("Hospitais", "bi-hospital", "Hospitais", "Index", "HOSPITAIS", RolesConstants.Coordenador),
            Item("Especialidades", "bi-tags", "Especialidades", "Index", "ESPECIALIDADES", RolesConstants.Coordenador),
            Item("Disponibilidade Médica", "bi-clock-history", "MedicoArea", "Index", "DISPONIBILIDADE", RolesConstants.Medico),
            Item("Substituições", "bi-arrow-left-right", "MedicoArea", "Index", "SUBSTITUICOES", RolesConstants.Medico)
        });

        AddGroup(groups, "FINANCEIRO", "bi-cash-stack", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Dashboard Financeiro", "bi-graph-up", "ClinicaFinanceiro", "Index", "SAUDE360_FINANCEIRO", RolesConstants.FinanceiroClinica),
            Item("Pagamentos Médicos", "bi-cash-coin", "Pagamentos", "Index", "PAGAMENTOS", RolesConstants.Financeiro),
            Item("Contas a Receber", "bi-receipt", "ClinicaFinanceiro", "ContasReceber", "SAUDE360_FINANCEIRO", RolesConstants.FinanceiroClinica),
            Item("Recebimentos", "bi-wallet2", "ClinicaFinanceiro", "Receber", "SAUDE360_FINANCEIRO", RolesConstants.FinanceiroClinica),
            Item("Caixa", "bi-box", "ClinicaFinanceiro", "Caixa", "SAUDE360_FINANCEIRO", RolesConstants.FinanceiroClinica),
            Item("Repasses", "bi-bank", "ClinicaFinanceiro", "Repasses", "SAUDE360_FINANCEIRO", RolesConstants.FinanceiroClinica),
            Item("Relatórios Financeiros", "bi-file-earmark-bar-graph", "ClinicaFinanceiro", "Relatorios", "SAUDE360_FINANCEIRO", RolesConstants.FinanceiroClinica)
        });

        AddGroup(groups, "CONVÊNIOS", "bi-shield-plus", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Dashboard Convênios", "bi-speedometer2", "Convenios", "Dashboard", "SAUDE360_CONVENIOS", RolesConstants.FaturamentoConvenio),
            Item("Convênios", "bi-building", "Convenios", "Index", "SAUDE360_CONVENIOS", RolesConstants.FaturamentoConvenio),
            Item("Planos de Saúde", "bi-card-checklist", "PlanosSaude", "Index", "SAUDE360_PLANOS_SAUDE", RolesConstants.FaturamentoConvenio),
            Item("Autorizações", "bi-check2-square", "Convenios", "Autorizacoes", "SAUDE360_CONVENIOS", RolesConstants.FaturamentoConvenio),
            Item("Glosas", "bi-exclamation-octagon", "Convenios", "Glosas", "SAUDE360_CONVENIOS", RolesConstants.FaturamentoConvenio),
            Item("Faturamento", "bi-file-earmark-bar-graph", "Convenios", "Faturamento", "SAUDE360_CONVENIOS", RolesConstants.FaturamentoConvenio)
        });

        AddGroup(groups, "RELATÓRIOS", "bi-bar-chart", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Executivo", "bi-graph-up-arrow", "Relatorios", "Index", "RELATORIOS", RolesConstants.Auditor),
            Item("Operação Clínica", "bi-clipboard2-pulse", "ClinicaDashboard", "Index", "SAUDE360_DASHBOARD", RolesConstants.Auditor),
            Item("Plantões", "bi-calendar2-week", "Relatorios", "ProdutividadeMedica", "RELATORIOS", RolesConstants.Auditor),
            Item("Financeiro", "bi-cash-stack", "Relatorios", "FaturamentoSaas", "RELATORIOS", RolesConstants.Auditor),
            Item("Convênios", "bi-shield-plus", "Convenios", "Faturamento", "SAUDE360_CONVENIOS", RolesConstants.Auditor),
            Item("Auditoria", "bi-journal-check", "Auditoria", "Index", "AUDITORIA", RolesConstants.Auditor)
        });

        AddGroup(groups, "GESTÃO DO CLIENTE", "bi-window-sidebar", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Usuários", "bi-person-gear", "Usuarios", "Index", "USUARIOS", RolesConstants.AdministradorCliente),
            Item("Perfis", "bi-people", "Perfis", "Index", "PERFIS", RolesConstants.AdministradorCliente),
            Item("Permissões", "bi-shield-lock", "Permissoes", "Index", "PERMISSOES", RolesConstants.AdministradorCliente),
            Item("White Label", "bi-palette2", "WhiteLabel", "Index", "WHITE_LABEL", RolesConstants.AdministradorCliente, true),
            Item("Onboarding", "bi-rocket", "Onboarding", "Index", "ONBOARDING", RolesConstants.AdministradorCliente),
            Item("Meu Plano", "bi-card-checklist", "MinhaAssinatura", "Index", "ASSINATURAS", RolesConstants.AdministradorCliente),
            Item("Uso do Plano", "bi-graph-up", "MinhaAssinatura", "Uso", "ASSINATURAS", RolesConstants.AdministradorCliente),
            Item("Suporte", "bi-life-preserver", "Suporte", "Index", "SUPORTE", RolesConstants.AdministradorCliente)
        });

        AddGroup(groups, "ADMIN SAAS", "bi-buildings", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Clientes", "bi-building-check", "Clientes", "Index", "CLIENTES", RolesConstants.AdministradorGlobal),
            Item("Tenants", "bi-diagram-3", "Clientes", "Index", "CLIENTES", RolesConstants.AdministradorGlobal),
            Item("Planos", "bi-columns-gap", "Planos", "Index", "PLANOS", RolesConstants.AdministradorGlobal),
            Item("Assinaturas", "bi-receipt", "Assinaturas", "Index", "ASSINATURAS", RolesConstants.AdministradorGlobal),
            Item("Billing", "bi-credit-card", "Billing", "Faturas", "BILLING_GLOBAL", RolesConstants.AdministradorGlobal),
            Item("Marketplace", "bi-shop", "Marketplace", "Index", "MARKETPLACE", RolesConstants.AdministradorGlobal, true),
            Item("Parceiros", "bi-handshake", "ParceiroPortal", "Index", "PARCEIRO", RolesConstants.AdministradorGlobal),
            Item("Observabilidade", "bi-activity", "Observabilidade", "Index", "OBSERVABILIDADE_GLOBAL", RolesConstants.AdministradorGlobal),
            Item("Customer Success", "bi-emoji-smile", "CustomerSuccess", "Index", "CUSTOMER_SUCCESS", RolesConstants.AdministradorGlobal)
        });

        AddGroup(groups, "PARCEIRO", "bi-handshake", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Portal Parceiro", "bi-house-door", "ParceiroPortal", "Index", "PARCEIRO", RolesConstants.Parceiro),
            Item("Leads", "bi-bullseye", "ParceiroPortal", "Leads", "LEADS", RolesConstants.Parceiro),
            Item("Propostas", "bi-file-earmark-text", "ParceiroPortal", "Propostas", "PROPOSTAS", RolesConstants.Parceiro),
            Item("Comissões", "bi-percent", "ParceiroPortal", "Comissoes", "COMISSOES", RolesConstants.Parceiro)
        });

        AddGroup(groups, "AJUDA E GOVERNANÇA", "bi-shield-check", currentController, currentAction, new List<MenuItemViewModel>
        {
            Item("Manual do Perfil", "bi-journal-bookmark", "Manual", "Perfil", "AJUDA", string.Empty, false, false),
            Item("Primeiros Passos", "bi-rocket-takeoff", "Ajuda", "PrimeirosPassos", "AJUDA", string.Empty, false, false),
            Item("Base de Conhecimento", "bi-question-circle", "Ajuda", "Index", "AJUDA", string.Empty, false, false),
            Item("LGPD", "bi-shield-lock", "Lgpd", "Index", "LGPD", string.Empty, false, false),
            Item("Auditoria", "bi-journal-check", "Auditoria", "Index", "AUDITORIA", RolesConstants.Auditor),
            Item("Minha Conta", "bi-person-circle", "Usuario", "Index", "CONTA", string.Empty, false, false)
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
        if (string.Equals(minimumRole, RolesConstants.Recepcao, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.Triagem, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.FinanceiroClinica, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.FaturamentoConvenio, StringComparison.OrdinalIgnoreCase)) return currentUser.IsTenantAdmin() || currentUser.HasRole(minimumRole);
        if (string.Equals(minimumRole, RolesConstants.Coordenador, StringComparison.OrdinalIgnoreCase) || string.Equals(minimumRole, RolesConstants.Coordenacao, StringComparison.OrdinalIgnoreCase)) return currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Coordenador) || currentUser.HasRole(RolesConstants.Coordenacao) || currentUser.HasRole(RolesConstants.Operador);
        if (string.Equals(minimumRole, RolesConstants.Financeiro, StringComparison.OrdinalIgnoreCase)) return currentUser.IsTenantAdmin() || currentUser.HasRole(RolesConstants.Financeiro);
        if (string.Equals(minimumRole, RolesConstants.Medico, StringComparison.OrdinalIgnoreCase)) return currentUser.IsTenantAdmin() || currentUser.IsDoctor();
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
