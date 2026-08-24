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
        var definitions = catalog.Navigation
            .Where(item => MatchesProfile(item.Profile))
            .OrderBy(item => item.Order)
            .Take(MaximumPrimaryItems)
            .Select(item => new { Navigation = item, Feature = catalog.Features.FirstOrDefault(feature => feature.Code == item.FeatureCode) })
            .Where(item => item.Feature is not null && item.Feature.IsAvailable && item.Feature.Status == "CANONICAL")
            .Where(item => HasTenantContext() && HasAccess(item.Feature!))
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
        return permissions.HasPermission(feature.Module, action) &&
               modules.IsModuleEnabled(feature.Module) && modules.IsFeatureEnabled(feature.Code);
    }

    private bool MatchesProfile(string profileList)
    {
        if (currentUser.IsGlobalAdmin()) return Contains(profileList, "Administrador Global");
        if (currentUser.IsTenantAdmin()) return Contains(profileList, "Administrador Cliente");
        if (currentUser.HasRole(RolesConstants.AdministradorClinica)) return Contains(profileList, "Administrador Clínica");
        if (currentUser.IsDoctor()) return Contains(profileList, "Médico");
        if (currentUser.HasRole(RolesConstants.Recepcao)) return Contains(profileList, "Recepção");
        if (currentUser.HasRole(RolesConstants.Triagem)) return Contains(profileList, "Triagem");
        if (currentUser.HasRole(RolesConstants.Enfermagem)) return Contains(profileList, "Enfermagem");
        if (currentUser.HasRole(RolesConstants.FinanceiroClinica)) return Contains(profileList, "Financeiro Clínica");
        if (currentUser.HasRole(RolesConstants.FaturamentoConvenio)) return Contains(profileList, "Faturamento Convênio");
        if (currentUser.HasRole(RolesConstants.Financeiro)) return Contains(profileList, "Financeiro");
        if (currentUser.HasRole(RolesConstants.Hospital)) return Contains(profileList, "Hospital");
        if (currentUser.HasRole(RolesConstants.Parceiro)) return Contains(profileList, "Parceiro");
        if (currentUser.HasRole(RolesConstants.Suporte)) return Contains(profileList, "Suporte");
        if (currentUser.HasRole(RolesConstants.AuditorClinico)) return Contains(profileList, "Auditor Clínico");
        if (currentUser.HasRole(RolesConstants.Auditor)) return Contains(profileList, "Auditor");
        if (currentUser.HasRole(RolesConstants.Comercial)) return Contains(profileList, "Comercial");
        if (currentUser.HasRole(RolesConstants.CustomerSuccess)) return Contains(profileList, "Customer Success");
        if (currentUser.HasRole(RolesConstants.Operador)) return Contains(profileList, "Operador");
        return Contains(profileList, "Coordenação") &&
               (currentUser.HasRole(RolesConstants.Coordenador) || currentUser.HasRole(RolesConstants.Coordenacao));
    }

    private static bool Contains(string profiles, string profile) => profiles.Split(',')
        .Any(value => string.Equals(value.Trim(), profile, StringComparison.OrdinalIgnoreCase));

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
