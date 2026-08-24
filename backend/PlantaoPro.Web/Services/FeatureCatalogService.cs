using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

public interface IFeatureCatalogService
{
    IReadOnlyList<FeatureDefinition> Features { get; }
    IReadOnlyList<NavigationDefinition> Navigation { get; }
    IReadOnlyList<PageDefinition> Pages { get; }
    FeatureDefinition? FindFeature(string controller, string action);
    PageDefinition? FindPage(string controller, string action);
}

public sealed class FeatureCatalogService : IFeatureCatalogService
{
    private static readonly IReadOnlyList<FeatureDefinition> FeatureItems = new List<FeatureDefinition>
    {
        Feature("MEU_DIA", "Meu Dia", "Prioridades e próximos passos da operação.", "Operação", "MeuDia", "Index", "bi-house-heart", "Administrador Global,Administrador Cliente,Administrador Clínica,Coordenação,Operador,Recepção,Triagem,Enfermagem,Médico,Financeiro,Financeiro Clínica,Faturamento Convênio,Hospital,Parceiro,Suporte,Auditor,Auditor Clínico,Comercial,Customer Success", "MEU_DIA", "MEU_DIA.VER", "Operação diária", "Hoje"),
        Feature("PLANTOES", "Plantões", "Planeje, publique e acompanhe a cobertura.", "Plantões", "Plantoes", "Index", "bi-calendar-event", "Coordenação,Médico", "PLANTOES", "PLANTOES.VER", "Plantão e cobertura", "Plantões"),
        Feature("COBERTURA", "Central de Cobertura", "Encontre profissionais e acompanhe convites.", "Plantões", "CentralEscala", "Index", "bi-people", "Coordenação", "CENTRAL_ESCALA", "COBERTURA.VER", "Plantão e cobertura", "Cobertura"),
        Feature("PACIENTES", "Pacientes", "Cadastros e histórico operacional do paciente.", "Atendimento", "Pacientes", "Index", "bi-people", "Recepção", "SAUDE360_PACIENTES", "PACIENTES.VER", "Atendimento", "Paciente"),
        Feature("AGENDA", "Agenda", "Organize agendamentos e a chegada dos pacientes.", "Atendimento", "Agendamentos", "Index", "bi-calendar2", "Recepção", "SAUDE360_AGENDAMENTO", "AGENDAMENTO.VER", "Atendimento", "Agendamento"),
        Feature("CHECK_IN", "Check-in", "Registre a chegada e encaminhe o paciente.", "Atendimento", "Agendamentos", "CheckIn", "bi-person-check", "Recepção", "SAUDE360_AGENDAMENTO", "AGENDAMENTO.CHECKIN", "Atendimento", "Check-in"),
        Feature("PAINEL_CHAMADA", "Painel de chamada", "Chame e encaminhe pacientes sem expor dados sensíveis.", "Atendimento", "PainelChamada", "Index", "bi-megaphone", "Recepção", "SAUDE360_PAINEL", "PAINEL_CHAMADA.OPERAR", "Atendimento", "Painel de chamada"),
        Feature("FILA_ATENDIMENTO", "Fila de Atendimento", "Acompanhe pacientes aguardando chamada e encaminhamento.", "Atendimento", "PainelChamada", "Fila", "bi-list", "Recepção", "SAUDE360_PAINEL", "PAINEL_CHAMADA.OPERAR", "Atendimento", "Fila de Atendimento"),
        Feature("TRIAGEM", "Triagem", "Priorize e encaminhe atendimentos com segurança.", "Atendimento", "Triagem", "Index", "bi-clipboard2-pulse", "Triagem", "SAUDE360_TRIAGEM", "TRIAGEM.VER", "Atendimento", "Triagem"),
        Feature("MINHA_AGENDA", "Minhas Escalas", "Acompanhe plantões, convites e compromissos.", "Área médica", "MinhaAgenda", "Index", "bi-calendar-heart", "Médico", "MINHA_AGENDA", "AGENDA_PROPRIA.VER", "Área médica", "Escalas"),
        Feature("PAGAMENTOS", "Meus Pagamentos", "Consulte valores previstos e realizados.", "Área médica", "Pagamentos", "Index", "bi-cash-coin", "Médico,Financeiro", "PAGAMENTOS", "PAGAMENTOS.VER", "Área médica", "Pagamento")
    };

    private static readonly IReadOnlyList<NavigationDefinition> NavigationItems = new List<NavigationDefinition>
    {
        Nav("Recepção", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1), Nav("Recepção", "Rotina", "Agenda", "bi-calendar2", "AGENDA", 2), Nav("Recepção", "Rotina", "Check-in", "bi-person-check", "CHECK_IN", 3), Nav("Recepção", "Rotina", "Painel de chamada", "bi-megaphone", "PAINEL_CHAMADA", 4), Nav("Recepção", "Rotina", "Fila de Atendimento", "bi-list", "FILA_ATENDIMENTO", 5), Nav("Recepção", "Rotina", "Pacientes", "bi-people", "PACIENTES", 6),
        Nav("Coordenação", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1), Nav("Coordenação", "Rotina", "Central de Cobertura", "bi-people", "COBERTURA", 2), Nav("Coordenação", "Rotina", "Plantões", "bi-calendar-event", "PLANTOES", 3),
        Nav("Médico", "Rotina", "Hoje", "bi-house-heart", "MEU_DIA", 1), Nav("Médico", "Rotina", "Minha Agenda", "bi-calendar-heart", "MINHA_AGENDA", 2), Nav("Médico", "Rotina", "Pagamentos", "bi-cash-coin", "PAGAMENTOS", 3)
        ,Nav("Administrador Global", "Visão geral", "Visão Executiva", "bi-speedometer2", "MEU_DIA", 1)
        ,Nav("Administrador Cliente", "Visão geral", "Visão Geral", "bi-speedometer2", "MEU_DIA", 1)
        ,Nav("Administrador Clínica", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Operador", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Triagem", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Enfermagem", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Financeiro", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Financeiro Clínica", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Faturamento Convênio", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Hospital", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Parceiro", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Suporte", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Auditor", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Auditor Clínico", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Comercial", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
        ,Nav("Customer Success", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1)
    };

    private static readonly IReadOnlyList<PageDefinition> PageItems = FeatureItems
        .Select(feature => new PageDefinition(feature.Code, feature.Name, feature.Description,
            new List<string> { "Início", feature.Domain, feature.Name }, feature.JourneyStep,
            feature.PrimaryAction, "Voltar"))
        .ToList();

    public IReadOnlyList<FeatureDefinition> Features => FeatureItems;
    public IReadOnlyList<NavigationDefinition> Navigation => NavigationItems;
    public IReadOnlyList<PageDefinition> Pages => PageItems;

    public FeatureDefinition? FindFeature(string controller, string action) => FeatureItems.FirstOrDefault(item =>
        string.Equals(item.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(item.Action, action, StringComparison.OrdinalIgnoreCase));

    public PageDefinition? FindPage(string controller, string action)
    {
        var feature = FindFeature(controller, action);
        return feature is null ? null : PageItems.FirstOrDefault(item => item.FeatureCode == feature.Code);
    }

    private static FeatureDefinition Feature(string code, string name, string description, string domain, string controller, string action, string icon, string profile, string module, string permission, string journey, string journeyStep) =>
        new FeatureDefinition(code, name, description, domain, controller, action, $"/{controller}/{action}", icon, profile, module, permission, "Essencial", "CANONICAL", journey, journeyStep, PrimaryAction(code), "Voltar", "1.22.2", string.Empty, true, true);

    private static NavigationDefinition Nav(string profile, string group, string label, string icon, string featureCode, int order) => new NavigationDefinition(profile, group, label, icon, featureCode, order);

    private static string PrimaryAction(string code) => code switch
    {
        "PLANTOES" => "Criar plantão",
        "PACIENTES" => "Cadastrar paciente",
        "AGENDA" => "Novo agendamento",
        "CHECK_IN" => "Localizar paciente",
        "TRIAGEM" => "Abrir fila",
        _ => "Ver prioridades"
    };
}
