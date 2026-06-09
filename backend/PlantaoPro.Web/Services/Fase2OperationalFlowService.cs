using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Services;

public interface IFase2OperationalFlowService
{
    Fase2OperationalPageViewModel Build(string area, string section);
}

public sealed class Fase2OperationalFlowService : IFase2OperationalFlowService
{
    private readonly ICurrentUserService currentUser;

    public Fase2OperationalFlowService(ICurrentUserService currentUser)
    {
        this.currentUser = currentUser;
    }

    public Fase2OperationalPageViewModel Build(string area, string section)
    {
        var normalizedArea = Normalize(area);
        var normalizedSection = Normalize(section);
        var model = BasePage(normalizedArea, normalizedSection);

        if (normalizedArea == "CLIENTE") FillCliente(model);
        else if (normalizedArea == "CENTRAL") FillCentral(model);
        else if (normalizedArea == "MEDICO") FillMedico(model);
        else if (normalizedArea == "FINANCEIRO") FillFinanceiro(model);
        else if (normalizedArea == "PARCEIRO") FillParceiro(model);
        else if (normalizedArea == "COMERCIAL") FillComercial(model);
        else FillSaas(model);

        ApplySection(model, normalizedSection);
        return model;
    }

    private Fase2OperationalPageViewModel BasePage(string area, string section)
    {
        var tenant = currentUser.ClienteId.HasValue ? currentUser.ClienteId.Value.ToString() : "Admin SaaS / demonstração";
        return new Fase2OperationalPageViewModel
        {
            Area = area,
            CurrentSection = section,
            TenantScope = tenant,
            Persona = currentUser.Roles().FirstOrDefault() ?? "Usuário autenticado"
        };
    }

    private static void FillCliente(Fase2OperationalPageViewModel model)
    {
        model.Title = "Portal do cliente contratante";
        model.Subtitle = "Admin cliente gerencia plano, usuários, perfis, white label, parametrizações, onboarding, suporte e treinamento no escopo do próprio tenant.";
        model.Kpis.Add(Kpi("Plano atual", "Profissional", "Mobile, relatórios e white label básico", "OK", "bi-award"));
        model.Kpis.Add(Kpi("Usuários", "12 / 20", "8 licenças disponíveis", "OK", "bi-people"));
        model.Kpis.Add(Kpi("Plantões do mês", "342 / 500", "68% do limite contratado", "ATENÇÃO", "bi-calendar2-week"));
        model.Kpis.Add(Kpi("Faturas abertas", "1", "Vencimento em 7 dias", "ATENÇÃO", "bi-receipt"));
        model.Steps.Add(Step(1, "Parametrizar tenant", "Validar dados do contratante, LGPD, permissões e regras de substituição.", "Em andamento"));
        model.Steps.Add(Step(2, "Configurar operação", "Cadastrar hospitais, especialidades, médicos e perfis.", "Próxima ação"));
        model.Steps.Add(Step(3, "Publicar escala", "Criar plantões, convidar médicos e acompanhar aceite.", "Pendente"));
        model.WorkItems.Add(Item("Onboarding pendente", "Concluir parametrização financeira antes do primeiro pagamento.", "Aberto", "Customer Success", "Hoje", Action("Abrir onboarding", "Onboarding", "Index")));
        model.WorkItems.Add(Item("White label básico", "Plano Profissional permite identidade básica; assets avançados exigem Enterprise.", "Limitado por plano", "Admin cliente", "Esta semana", Action("Configurar", "WhiteLabel", "Index"), BlockedAction("Assets avançados", "WhiteLabel", "Assets", "Disponível no plano Enterprise.")));
        model.PrimaryActions.Add(Action("Criar usuário", "Usuarios", "Create"));
        model.PrimaryActions.Add(Action("Criar perfil", "Perfis", "Create"));
        model.PrimaryActions.Add(Action("Ver faturas", "ClientePortal", "Faturas"));
    }

    private static void FillCentral(Fase2OperationalPageViewModel model)
    {
        model.Title = "Central de escala funcional";
        model.Subtitle = "Coordenação acompanha plantões por status, risco, disponibilidade médica, convites pendentes e escalas confirmadas.";
        model.Kpis.Add(Kpi("Plantões descobertos", "4", "Exigem convite ou substituição", "RISCO", "bi-exclamation-triangle"));
        model.Kpis.Add(Kpi("Convites pendentes", "18", "6 vencem hoje", "ATENÇÃO", "bi-envelope"));
        model.Kpis.Add(Kpi("Escalas de hoje", "27", "23 confirmadas", "OK", "bi-check2-circle"));
        model.Kpis.Add(Kpi("Médicos disponíveis", "63", "Filtrado por especialidade", "OK", "bi-person-check"));
        model.Steps.Add(Step(1, "Criar plantão", "Valida hospital, especialidade, período, vagas e valor.", "Disponível"));
        model.Steps.Add(Step(2, "Publicar", "Plantão entra na vitrine do tenant e fica apto a convites.", "Disponível"));
        model.Steps.Add(Step(3, "Convidar e confirmar", "Convite respeita disponibilidade, limite de plano e conflitos.", "Monitorado"));
        model.WorkItems.Add(Item("UTI noite sem médico", "Plantão crítico em aberto; sugerir médicos disponíveis sem conflito.", "Risco", "Coordenação", "Hoje 18:00", Action("Ver disponíveis", "CentralEscala", "MedicosDisponiveis"), ConfirmAction("Convidar médico", "CentralEscala", "Convidar")));
        model.WorkItems.Add(Item("Convites sem resposta", "Lote com 6 convites pendentes há mais de 12 horas.", "Atenção", "Coordenação", "Hoje", Action("Ver convites", "CentralEscala", "ConvitesPendentes")));
        model.PrimaryActions.Add(Action("Novo plantão", "Plantoes", "Create"));
        model.PrimaryActions.Add(Action("Calendário", "CentralEscala", "Calendario"));
    }

    private static void FillMedico(Fase2OperationalPageViewModel model)
    {
        model.Title = "Área do médico";
        model.Subtitle = "Médico visualiza somente seus convites, agenda, disponibilidade, substituições e pagamentos previstos ou confirmados.";
        model.Kpis.Add(Kpi("Próximos plantões", "5", "Próximos 30 dias", "OK", "bi-calendar-check"));
        model.Kpis.Add(Kpi("Convites pendentes", "3", "Resposta recomendada até hoje", "ATENÇÃO", "bi-inbox"));
        model.Kpis.Add(Kpi("Pagamentos previstos", "R$ 4.800", "2 plantões realizados", "OK", "bi-cash-coin"));
        model.Kpis.Add(Kpi("Substituições", "1", "Aguardando coordenação", "ATENÇÃO", "bi-arrow-left-right"));
        model.Steps.Add(Step(1, "Responder convite", "Aceite valida vaga disponível e conflito de agenda.", "Disponível"));
        model.Steps.Add(Step(2, "Informar disponibilidade", "Indisponibilidades bloqueiam convites indevidos.", "Disponível"));
        model.Steps.Add(Step(3, "Acompanhar pagamento", "Somente pagamentos próprios ficam visíveis.", "Disponível"));
        model.WorkItems.Add(Item("Convite em aberto", "Hospital Santa Clara, Clínica Médica, amanhã 19h.", "Pendente", "Médico", "Hoje", ConfirmAction("Aceitar", "MedicoArea", "AceitarConvite"), Action("Recusar com motivo", "MedicoArea", "Convites")));
        model.PrimaryActions.Add(Action("Minha agenda", "MedicoArea", "Agenda"));
        model.PrimaryActions.Add(Action("Disponibilidade", "MedicoArea", "Disponibilidade"));
        model.PrimaryActions.Add(Action("Pagamentos", "MedicoArea", "Pagamentos"));
    }

    private static void FillFinanceiro(Fase2OperationalPageViewModel model)
    {
        model.Title = "Financeiro operacional";
        model.Subtitle = "Equipe financeira gera pagamento de escalas realizadas, confirma valores, resolve contestações e exporta relatórios auditados.";
        model.Kpis.Add(Kpi("Pendentes", "R$ 38.200", "14 pagamentos a confirmar", "ATENÇÃO", "bi-hourglass-split"));
        model.Kpis.Add(Kpi("Confirmados", "R$ 124.900", "Competência atual", "OK", "bi-check-circle"));
        model.Kpis.Add(Kpi("Contestados", "2", "Exigem resolução", "RISCO", "bi-chat-dots"));
        model.Kpis.Add(Kpi("Hospitais", "7", "Com valores no período", "OK", "bi-hospital"));
        model.Steps.Add(Step(1, "Gerar pagamento", "Disponível apenas para escala realizada e sem duplicidade.", "Controlado"));
        model.Steps.Add(Step(2, "Confirmar", "Exige valor, data, forma e auditoria.", "Controlado"));
        model.Steps.Add(Step(3, "Exportar", "Relatório por período, médico e hospital com registro de auditoria.", "Disponível"));
        model.WorkItems.Add(Item("Pagamentos aguardando confirmação", "Conferir valores previstos com escalas realizadas.", "Pendente", "Financeiro", "Hoje", Action("Ver pendentes", "Financeiro", "Pendentes"), ConfirmAction("Confirmar lote", "Financeiro", "ConfirmarPagamento")));
        model.PrimaryActions.Add(Action("Pagamentos", "Financeiro", "Pagamentos"));
        model.PrimaryActions.Add(Action("Relatórios", "Financeiro", "Relatorios"));
        model.PrimaryActions.Add(Action("Exportações", "Financeiro", "Exportacoes"));
    }

    private static void FillParceiro(Fase2OperationalPageViewModel model)
    {
        model.Title = "Portal parceiro revendedor";
        model.Subtitle = "Parceiro trabalha leads próprios, propostas, clientes vinculados, comissões, repasses, materiais comerciais e suporte sem dados clínicos sensíveis.";
        model.Kpis.Add(Kpi("Leads", "9", "3 novos na semana", "OK", "bi-funnel"));
        model.Kpis.Add(Kpi("Propostas", "4", "1 vence em 5 dias", "ATENÇÃO", "bi-file-earmark-text"));
        model.Kpis.Add(Kpi("Clientes vinculados", "6", "Tenants ativos", "OK", "bi-building-check"));
        model.Kpis.Add(Kpi("Comissão prevista", "R$ 7.240", "Próximo repasse", "OK", "bi-currency-dollar"));
        model.Steps.Add(Step(1, "Cadastrar lead", "Origem fica vinculada ao parceiro para cálculo de comissão.", "Disponível"));
        model.Steps.Add(Step(2, "Gerar proposta", "Proposta usa plano, validade, white label e regra comercial.", "Disponível"));
        model.Steps.Add(Step(3, "Acompanhar comissão", "Comissão prevista e paga seguem regra configurada.", "Disponível"));
        model.WorkItems.Add(Item("Proposta próxima do vencimento", "Rede Norte precisa aprovar antes da validade.", "Atenção", "Parceiro", "5 dias", Action("Abrir propostas", "ParceiroPortal", "Propostas")));
        model.PrimaryActions.Add(Action("Novo lead", "ParceiroPortal", "Leads"));
        model.PrimaryActions.Add(Action("Materiais", "ParceiroPortal", "Materiais"));
    }

    private static void FillComercial(Fase2OperationalPageViewModel model)
    {
        model.Title = "Fluxo comercial SaaS";
        model.Subtitle = "Landing, planos, simulador, lead, proposta, aprovação, conversão, tenant, assinatura e onboarding com validações de CNPJ e provisionamento.";
        model.Kpis.Add(Kpi("Leads", "26", "8 aguardam qualificação", "OK", "bi-person-plus"));
        model.Kpis.Add(Kpi("Propostas", "11", "3 próximas do vencimento", "ATENÇÃO", "bi-file-earmark-ruled"));
        model.Kpis.Add(Kpi("Conversões", "4", "Tenant e assinatura criados", "OK", "bi-diagram-3"));
        model.Kpis.Add(Kpi("Falhas provisionamento", "1", "Reprocessamento idempotente", "RISCO", "bi-arrow-clockwise"));
        model.Steps.Add(Step(1, "Capturar lead", "Landing ou simulador registram origem e volume esperado.", "Disponível"));
        model.Steps.Add(Step(2, "Gerar proposta", "Validade, plano recomendado e condições ficam claros.", "Disponível"));
        model.Steps.Add(Step(3, "Converter", "Bloqueia CNPJ duplicado e reprocessa sem duplicar tenant.", "Controlado"));
        model.PrimaryActions.Add(Action("Criar lead", "Comercial", "Leads"));
        model.PrimaryActions.Add(Action("Nova proposta", "PropostasComerciais", "Create"));
        model.PrimaryActions.Add(Action("Onboarding", "Onboarding", "Index"));
    }

    private static void FillSaas(Fase2OperationalPageViewModel model)
    {
        model.Title = "Dashboard SaaS por perfil";
        model.Subtitle = "Indicadores por papel com limites de plano, bloqueios comerciais, auditoria, LGPD e navegação para ações reais.";
        model.Kpis.Add(Kpi("Clientes ativos", "18", "MRR estimado R$ 42 mil", "OK", "bi-buildings"));
        model.Kpis.Add(Kpi("Upgrades", "5", "Solicitações abertas", "ATENÇÃO", "bi-graph-up-arrow"));
        model.Kpis.Add(Kpi("Incidentes", "0", "Sem falhas críticas", "OK", "bi-shield-check"));
        model.Kpis.Add(Kpi("Faturas vencidas", "2", "Acompanhar cobrança", "RISCO", "bi-receipt-cutoff"));
    }

    private static void ApplySection(Fase2OperationalPageViewModel model, string section)
    {
        if (section == "WHITELABEL" && model.Area == "CLIENTE")
        {
            model.HasPlanBlock = true;
            model.PlanBlockMessage = "Assets avançados, templates multi-cliente e publicação Enterprise exigem upgrade; configuração básica permanece disponível.";
        }

        if (section == "INDEX" || string.IsNullOrWhiteSpace(section)) return;
        model.Alerts.Add("Seção atual: " + section + ". Os dados mantêm o mesmo escopo de tenant e perfil da área principal.");
    }

    private static Fase2KpiViewModel Kpi(string label, string value, string hint, string status, string icon)
    {
        return new Fase2KpiViewModel { Label = label, Value = value, Hint = hint, Status = status, Icon = icon };
    }

    private static Fase2FlowStepViewModel Step(int order, string title, string description, string status)
    {
        return new Fase2FlowStepViewModel { Order = order, Title = title, Description = description, Status = status };
    }

    private static Fase2WorkItemViewModel Item(string title, string description, string status, string owner, string due, params Fase2ActionViewModel[] actions)
    {
        return new Fase2WorkItemViewModel { Title = title, Description = description, Status = status, Owner = owner, Due = due, Actions = actions.ToList() };
    }

    private static Fase2ActionViewModel Action(string label, string controller, string action)
    {
        return new Fase2ActionViewModel { Label = label, Controller = controller, Action = action, Description = "Abrir " + label };
    }

    private static Fase2ActionViewModel ConfirmAction(string label, string controller, string action)
    {
        return new Fase2ActionViewModel { Label = label, Controller = controller, Action = action, Description = "Ação crítica com modal e auditoria", Method = "POST", RequiresConfirmation = true };
    }

    private static Fase2ActionViewModel BlockedAction(string label, string controller, string action, string reason)
    {
        return new Fase2ActionViewModel { Label = label, Controller = controller, Action = action, Description = reason, Blocked = true, BlockReason = reason };
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "INDEX" : value.Trim().ToUpperInvariant();
    }
}
