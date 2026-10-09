using PlantaoPro.Web.Models;
using PlantaoPro.Web.Security;

namespace PlantaoPro.Web.Services.Security;

public interface IMenuBuilderService
{
    IReadOnlyCollection<MenuGroupViewModel> Build(string currentController, string currentAction);
}

/// <summary>
/// Builds the visible navigation exclusively from the product catalog. The catalog and the
/// responsive shell retain controller compatibility for Dashboard, Pacientes, Agendamentos,
/// CheckIn, PainelChamada, Triagem, Consultas, Prescricoes, Cid, ClinicaFinanceiro, Convenios,
/// PlanosSaude, Plantoes, Escalas, Notificacoes, Relatorios, Ajuda, Manual, Jornada,
/// ItensFaturaveis and FaturamentoClinico; authorization still decides which entries are shown.
/// </summary>
public sealed class MenuBuilderService : IMenuBuilderService
{
    private const int MaximumPrimaryItems = 12;
    private readonly ICurrentUserService currentUser;
    private readonly IPermissionService permissions;
    private readonly IModuleAccessService modules;
    private readonly IFeatureCatalogService catalog;

    public MenuBuilderService(ICurrentUserService currentUser, IPermissionService permissions,
        IModuleAccessService modules, IFeatureCatalogService catalog)
    {
        this.currentUser = currentUser;
        this.permissions = permissions;
        this.modules = modules;
        this.catalog = catalog;
    }

    public IReadOnlyCollection<MenuGroupViewModel> Build(string currentController, string currentAction)
    {
        // Os filtros de disponibilidade e de acesso vêm antes do limite de exibição:
        // o Take deve contar apenas entradas visíveis para o usuário, senão ele consome
        // posições com recursos inacessíveis e esconde o que o usuário pode acessar.
        var definitions = catalog.Navigation
            .Where(item => MatchesProfile(item.Profile))
            .Select(item => new { Navigation = item, Feature = catalog.Features.FirstOrDefault(feature => feature.Code == item.FeatureCode) })
            .Where(item => item.Feature is not null && item.Feature.IsAvailable && item.Feature.Status == "CANONICAL")
            .Where(item => HasTenantContext() && HasAccess(item.Feature!))
            .OrderBy(item => item.Navigation.Order)
            .Take(MaximumPrimaryItems)
            .ToList();

        return definitions
            .GroupBy(item => item.Navigation.Group)
            .Select(group => new MenuGroupViewModel
            {
                Title = group.Key.ToUpperInvariant(),
                Icon = group.First().Navigation.Icon,
                Items = group.Select(item => ToMenuItem(item.Navigation, item.Feature!, currentController, currentAction)).ToList()
            })
            .ToList();
    }

    private bool HasTenantContext() => currentUser.IsGlobalAdmin() || currentUser.TenantId.HasValue;

    private bool HasAccess(FeatureDefinition feature)
    {
        var permission = feature.Permission.Split('.', 2);
        var action = permission.Length == 2 ? permission[1] : "VER";
        // O menu espelha exatamente o contrato do guard (permissão na ação + módulo efetivo).
        // Não se acrescenta IsFeatureEnabled(feature.Code) aqui: feature.Code é um slug de
        // exibição (ex.: CHECK_IN, FILA_ATENDIMENTO) e nem sempre coincide com um módulo, o
        // que ocultava itens cujo guard liberava. O guard permanece a autoridade de acesso —
        // o menu apenas deixa de esconder/advertir desalinhado.
        return permissions.HasPermission(feature.Module, action) &&
               modules.IsModuleEnabled(feature.Module);
    }

    /// <summary>
    /// União de perfis: um usuário pode deter múltiplos papéis e cada papel contribui
    /// com o próprio perfil no menu. A entrada é visível quando pelo menos um dos perfis
    /// declarados pertence ao conjunto efetivo do usuário (a cadeia if/else anterior
    /// mantinha apenas o primeiro ramo atingido e ocultava entradas dos demais papéis).
    /// </summary>
    private bool MatchesProfile(string profileList)
    {
        var allowedProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (currentUser.IsGlobalAdmin()) allowedProfiles.Add("Administrador Global");
        if (currentUser.IsTenantAdmin()) allowedProfiles.Add("Administrador Cliente");
        if (currentUser.HasRole(RolesConstants.AdministradorClinica)) allowedProfiles.Add("Administrador Clínica");
        if (currentUser.IsDoctor()) allowedProfiles.Add("Médico");
        if (currentUser.HasRole(RolesConstants.Recepcao)) allowedProfiles.Add("Recepção");
        if (currentUser.HasRole(RolesConstants.Triagem)) allowedProfiles.Add("Triagem");
        if (currentUser.HasRole(RolesConstants.Enfermagem)) allowedProfiles.Add("Enfermagem");
        if (currentUser.HasRole(RolesConstants.FinanceiroClinica)) allowedProfiles.Add("Financeiro Clínica");
        if (currentUser.HasRole(RolesConstants.FaturamentoConvenio)) allowedProfiles.Add("Faturamento Convênio");
        if (currentUser.HasRole(RolesConstants.Financeiro)) allowedProfiles.Add("Financeiro");
        if (currentUser.HasRole(RolesConstants.Hospital)) allowedProfiles.Add("Hospital");
        if (currentUser.HasRole(RolesConstants.Parceiro)) allowedProfiles.Add("Parceiro");
        if (currentUser.HasRole(RolesConstants.Suporte)) allowedProfiles.Add("Suporte");
        if (currentUser.HasRole(RolesConstants.AuditorClinico)) allowedProfiles.Add("Auditor Clínico");
        if (currentUser.HasRole(RolesConstants.Auditor)) allowedProfiles.Add("Auditor");
        if (currentUser.HasRole(RolesConstants.Comercial)) allowedProfiles.Add("Comercial");
        if (currentUser.HasRole(RolesConstants.CustomerSuccess)) allowedProfiles.Add("Customer Success");
        if (currentUser.HasRole(RolesConstants.Operador)) allowedProfiles.Add("Operador");
        if (currentUser.HasRole(RolesConstants.Coordenador) || currentUser.HasRole(RolesConstants.Coordenacao)) allowedProfiles.Add("Coordenação");
        return profileList.Split(',').Any(value => allowedProfiles.Contains(value.Trim()));
    }

    private static MenuItemViewModel ToMenuItem(NavigationDefinition navigation, FeatureDefinition feature,
        string currentController, string currentAction) => new MenuItemViewModel
    {
        Title = navigation.Label,
        Icon = string.IsNullOrWhiteSpace(navigation.Icon) ? feature.Icon : navigation.Icon,
        Controller = feature.Controller,
        Action = feature.Action,
        Module = feature.Module,
        Permission = feature.Permission,
        MinimumRole = navigation.Profile,
        RequiresModule = true,
        IsActive = string.Equals(currentController, feature.Controller, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(currentAction, feature.Action, StringComparison.OrdinalIgnoreCase)
    };
}
