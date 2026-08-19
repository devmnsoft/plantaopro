using PlantaoPro.Web.Models;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Services;

public interface IFase2OperationalFlowService
{
    Fase2OperationalPageViewModel Build(string area, string section);
}

/// <summary>
/// Compatibility model for routes that have not yet received a dedicated view.
///
/// This service deliberately does not manufacture KPIs or work items. Operational
/// numbers and pending actions must come from their owning API/domain; until a route
/// is migrated, the page presents an honest empty state and links to a real module.
/// </summary>
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
        var descriptor = Describe(normalizedArea);
        var model = new Fase2OperationalPageViewModel
        {
            Area = normalizedArea,
            CurrentSection = Normalize(section),
            TenantScope = currentUser.ClienteId?.ToString() ?? "Escopo global",
            Persona = currentUser.Roles().FirstOrDefault() ?? "Usuário autenticado",
            Title = descriptor.Title,
            Subtitle = descriptor.Subtitle
        };

        model.Alerts.Add("Esta rota de compatibilidade não possui uma fonte operacional própria. Nenhum indicador ou item demonstrativo é exibido.");
        if (descriptor.Action is not null)
        {
            model.PrimaryActions.Add(descriptor.Action);
        }

        return model;
    }

    private static (string Title, string Subtitle, Fase2ActionViewModel? Action) Describe(string area) => area switch
    {
        "CENTRAL" => ("Central de escala", "Consulte a operação atual na Central de Escala.", Link("Abrir Central de Escala", "CentralEscala", "Index")),
        "FINANCEIRO" => ("Financeiro", "Consulte pagamentos e contestações persistidos no módulo Financeiro.", Link("Abrir Financeiro", "Financeiro", "Index")),
        "MEDICO" => ("Área do médico", "Consulte agenda e ações permitidas no Meu Dia.", Link("Abrir Meu Dia", "MeuDia", "Index")),
        "PENDENCIAS" => ("Central de Ações", "As ações são derivadas do estado real dos módulos contratados.", null),
        "CLIENTE" => ("Portal do cliente", "Os dados do tenant são apresentados apenas por módulos com fonte persistida.", Link("Gerenciar usuários", "Usuarios", "Index")),
        "PARCEIRO" => ("Portal do parceiro", "Nenhum indicador comercial é estimado nesta página.", null),
        _ => ("Operação", "Nenhum dado demonstrativo é usado em páginas operacionais.", null)
    };

    private static Fase2ActionViewModel Link(string label, string controller, string action) => new()
    {
        Label = label,
        Controller = controller,
        Action = action,
        Description = $"Abrir {label}"
    };

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "INDEX" : value.Trim().ToUpperInvariant();
}
