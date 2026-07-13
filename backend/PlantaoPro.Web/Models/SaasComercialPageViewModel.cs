namespace PlantaoPro.Web.Models;

public sealed class SaasComercialPageViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApiPath { get; set; } = string.Empty;
    public IEnumerable<string> Actions { get; set; } = Array.Empty<string>();
}
