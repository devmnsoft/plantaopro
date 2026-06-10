using PlantaoPro.Web.Models;

namespace PlantaoPro.Web.Services;

public interface ISaude360WebService
{
    Saude360ModulePageViewModel BuildPage(string moduleTitle, string moduleCode, string actionTitle, string endpoint, IReadOnlyCollection<Saude360ActionLinkViewModel> actions, IReadOnlyCollection<string> rules);
}

public sealed class Saude360WebService : ISaude360WebService
{
    public Saude360ModulePageViewModel BuildPage(string moduleTitle, string moduleCode, string actionTitle, string endpoint, IReadOnlyCollection<Saude360ActionLinkViewModel> actions, IReadOnlyCollection<string> rules)
    {
        return new Saude360ModulePageViewModel
        {
            ModuleTitle = moduleTitle,
            ModuleCode = moduleCode,
            ActionTitle = actionTitle,
            Endpoint = endpoint,
            Actions = actions,
            Rules = rules
        };
    }
}
