namespace PlantaoPro.Web.Models;

public sealed class Saude360ModulePageViewModel
{
    public string ModuleTitle { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string ActionTitle { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public IReadOnlyCollection<Saude360ActionLinkViewModel> Actions { get; set; } = Array.Empty<Saude360ActionLinkViewModel>();
    public IReadOnlyCollection<string> Rules { get; set; } = Array.Empty<string>();
}

public sealed class Saude360ActionLinkViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
