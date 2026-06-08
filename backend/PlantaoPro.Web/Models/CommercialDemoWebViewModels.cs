namespace PlantaoPro.Web.Models;

public sealed class CommercialDemoPageViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ActiveRoute { get; set; } = string.Empty;
    public IEnumerable<CommercialCardViewModel> Cards { get; set; } = Array.Empty<CommercialCardViewModel>();
    public IEnumerable<string> PrimaryActions { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Checklist { get; set; } = Array.Empty<string>();
}

public sealed class CommercialCardViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActionText { get; set; } = string.Empty;
    public string ActionController { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
}
