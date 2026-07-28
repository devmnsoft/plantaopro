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
        Feature("MEU_DIA", "Meu Dia", "Prioridades e próximos passos da operação.", "Operação", "Home", "Index", "Coordenação,Recepção,Triagem,Médico,Financeiro", "MEU_DIA.VER", "Operação diária", "Hoje"),
        Feature("PLANTOES", "Plantões", "Planeje, publique e acompanhe a cobertura.", "Plantões", "Plantoes", "Index", "Coordenação", "PLANTOES.VER", "Plantão e cobertura", "Plantões"),
        Feature("COBERTURA", "Central de Cobertura", "Encontre profissionais e acompanhe convites.", "Plantões", "CentralEscala", "Index", "Coordenação", "COBERTURA.VER", "Plantão e cobertura", "Cobertura"),
        Feature("PACIENTES", "Pacientes", "Cadastros e histórico operacional do paciente.", "Atendimento", "Pacientes", "Index", "Recepção", "PACIENTES.VER", "Atendimento", "Paciente"),
        Feature("AGENDA", "Agenda", "Organize agendamentos e a chegada dos pacientes.", "Atendimento", "Agendamentos", "Index", "Recepção", "AGENDAMENTO.VER", "Atendimento", "Agendamento"),
        Feature("CHECK_IN", "Check-in", "Registre a chegada e encaminhe o paciente.", "Atendimento", "Agendamentos", "CheckIn", "Recepção", "AGENDAMENTO.CHECKIN", "Atendimento", "Check-in"),
        Feature("TRIAGEM", "Triagem", "Priorize e encaminhe atendimentos com segurança.", "Atendimento", "Triagem", "Index", "Triagem", "TRIAGEM.VER", "Atendimento", "Triagem"),
        Feature("MINHA_AGENDA", "Minha Agenda", "Acompanhe plantões, convites e compromissos.", "Área médica", "MinhaAgenda", "Index", "Médico", "AGENDA_PROPRIA.VER", "Área médica", "Hoje"),
        Feature("PAGAMENTOS", "Meus Pagamentos", "Consulte valores previstos e realizados.", "Área médica", "Pagamentos", "Index", "Médico,Financeiro", "PAGAMENTOS.VER", "Área médica", "Pagamento")
    };

    private static readonly IReadOnlyList<NavigationDefinition> NavigationItems = new List<NavigationDefinition>
    {
        Nav("Recepção", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1), Nav("Recepção", "Rotina", "Agenda", "bi-calendar2", "AGENDA", 2), Nav("Recepção", "Rotina", "Check-in", "bi-person-check", "CHECK_IN", 3), Nav("Recepção", "Rotina", "Pacientes", "bi-people", "PACIENTES", 4),
        Nav("Coordenação", "Rotina", "Meu Dia", "bi-house-heart", "MEU_DIA", 1), Nav("Coordenação", "Rotina", "Central de Cobertura", "bi-people", "COBERTURA", 2), Nav("Coordenação", "Rotina", "Plantões", "bi-calendar-event", "PLANTOES", 3),
        Nav("Médico", "Rotina", "Hoje", "bi-house-heart", "MEU_DIA", 1), Nav("Médico", "Rotina", "Minha Agenda", "bi-calendar-heart", "MINHA_AGENDA", 2), Nav("Médico", "Rotina", "Pagamentos", "bi-cash-coin", "PAGAMENTOS", 3)
    };

    private static readonly IReadOnlyList<PageDefinition> PageItems = FeatureItems
        .Select(feature => new PageDefinition(feature.Code, feature.Name, feature.Description,
            new List<string> { "Início", feature.Domain, feature.Name }, feature.Journey,
            PrimaryAction(feature.Code), "Voltar"))
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

    private static FeatureDefinition Feature(string code, string name, string description, string domain, string controller, string action, string profile, string permission, string journey, string journeyStep) =>
        new FeatureDefinition(code, name, description, domain, controller, action, $"/{controller}/{action}", profile, permission, "Essencial", "CANÔNICO", journey, "1.22.0", string.Empty, true);

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
